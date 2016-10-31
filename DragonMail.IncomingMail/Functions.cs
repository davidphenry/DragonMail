using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using DragonMail.DTO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using MimeKit;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using System.Net;

namespace DragonMail.IncomingMail
{
    public class Functions
    {
        public static async Task ProcessQueueMessageAsync([QueueTrigger("incoming")] string message, TextWriter log)
        {
            try
            {
                var dsMail = ParseMessage(message);
                await WriteMessage(dsMail);

            }
            catch (Exception e)
            {
                throw;
            }
        }
        private const string ENDPOINT_URI = "https://dragonmail.documents.azure.com:443/";
        private const string DOCDB_KEY = "b4TqVBpGPZVbh9BahrDrkx22zfXa79GJNb1hUtklbOikI5cP3S0NXRyITuCmYRT1cEdi1sWgTYrWBd6cdwhdpg";
        private const string DATABASE_NAME= "mailDB";
        private const string COLLECTION_NAME = "mailColl";
        private static async Task WriteMessage(IEnumerable<DSMail> messages)
        {
            using (var client = new DocumentClient(new Uri(ENDPOINT_URI), DOCDB_KEY))
            {
                foreach (var message in messages)
                {
                    var messageUri = UriFactory.CreateDocumentCollectionUri(DATABASE_NAME, COLLECTION_NAME);
                    await client.CreateDocumentAsync(messageUri, message);
                }
            }
        }

        private static IEnumerable<DSMail> ParseMessage(string Message)
        {
            var smtpEmail = JsonConvert.DeserializeObject<SMTPEmailDTO>(Message);

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(smtpEmail.Message);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            var mimeMessage = MimeMessage.Load(stream);
            var parsedMails = new List<DSMail>();
            foreach (var toAddress in mimeMessage.To)
            {
                var mail = new DSMail();
                if (mimeMessage.From != null && mimeMessage.From.Count > 0)
                {
                    var firstFrom = mimeMessage.From.First() as MailboxAddress;
                    mail.FromEmail = firstFrom.Address;
                    mail.FromName = firstFrom.Name;
                }
                var toMailBox = toAddress as MailboxAddress;
                mail.ToName = toMailBox.Name;
                mail.ToEmail = toMailBox.Address;

                mail.Subject = mimeMessage.Subject;
                mail.HtmlBody = mimeMessage.HtmlBody;
                mail.TextBody = mimeMessage.TextBody;
                mail.Queue = DSMail.MessageQueue(mail.ToEmail);
                mail.SentDate = DateTime.Now.ToString();
                parsedMails.Add(mail);
            }
            return parsedMails;
        }      
    }
}
