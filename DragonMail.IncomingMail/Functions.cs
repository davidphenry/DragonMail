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

                parsedMails.Add(mail);
            }
            return parsedMails;
        }
        //private static readonly string _crlf = "\r\n";
        //private static DSMail ParseMessage(string Message)
        //{
        //    DSMail result = new DSMail();

        //    try
        //    {
        //        string[] messageFragments = Message.Split(_crlf.ToCharArray());
        //        //is the response for this message OK?
        //        //if (!messageFragments[0].StartsWith("+OK"))
        //            //throw new Exception("Error: Message is not in appropriate format.");

        //        string ContentType = "";
        //        bool ReadingMessage = false;
        //        bool LookingForStartOfMessage = false;
        //        bool ReadingTo = false;
        //        foreach (string s in messageFragments)
        //        {
        //            //set from
        //            if (s.StartsWith("From:"))
        //                result.From = s.Split('"')[1];
        //            //set to
        //            if (s.StartsWith("To:"))
        //            {
        //                ReadingTo = true;
        //                result.To = new string[] {
        //                        s.Split('<')[1].Replace(">","").Replace(",","") };
        //            }
        //            //reading to fields
        //            if (!s.StartsWith("To:") && !s.StartsWith("Subject:") && ReadingTo &&
        //              !string.IsNullOrEmpty(s))
        //            {
        //                List<string> to = result.To.ToList();
        //                to.Add(s.Split('<')[1].Replace(">", "").Replace(",", ""));
        //                result.To = to.ToArray();
        //            }
        //            //set subject;
        //            if (s.StartsWith("Subject:"))
        //            {
        //                ReadingTo = false;
        //                result.Subject = s.Substring(9);
        //            }
        //            //set date
        //            if (s.StartsWith("Date:"))
        //                result.SentDate = s.Substring(6);
        //            //set content type and start looking for message
        //            if (s.StartsWith("Content-Type:") && (s.Substring(14) == "text/plain;"
        //               || s.Substring(14) == "text/html;"))
        //            {
        //                ContentType = s.Substring(14);
        //                LookingForStartOfMessage = true;
        //            }
        //            //read message
        //            if (!string.IsNullOrEmpty(ContentType) && LookingForStartOfMessage &&
        //               string.IsNullOrEmpty(s))
        //            {
        //                LookingForStartOfMessage = false;
        //                ReadingMessage = true;
        //            }
        //            //found end of message
        //            if (ReadingMessage && s.StartsWith("------=_NextPart"))
        //                ReadingMessage = false;
        //            //reading text part of multi-part message
        //            if (ReadingMessage && ContentType == "text/plain;" && !s.Contains("charset") && !s.Contains("Content-Transfer-Encoding"))
        //                result.TextBody += s;
        //            //reading html part of multi-part message
        //            if (ReadingMessage && ContentType == "text/html;" && !s.Contains("charset") && !s.Contains("Content-Transfer-Encoding"))
        //                result.HtmlBody += s;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw;
        //    }
        //    return result;
        //}
    }
}
