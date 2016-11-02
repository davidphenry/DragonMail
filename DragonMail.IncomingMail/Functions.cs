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
using Microsoft.WindowsAzure.Storage.Blob;

namespace DragonMail.IncomingMail
{
    public class ParsedMail
    {
        public List<DSMail> Mail { get; set; }
        public List<MimePart> Attachments { get; set; }
    }
    public class Functions
    {
        public static async Task ProcessQueueMessageAsync([QueueTrigger("incoming")] string message, TextWriter log)
        {
            try
            {
                var dsMail = await ParseMessage(message);
                await WriteMessage(dsMail);
            }
            catch (Exception e)
            {
                throw;
            }
        }
        internal static async Task WriteMessage(ParsedMail parsedMail)
        {
            string docDBendPoint = CloudConfigurationManager.GetSetting(DTO.Constants.ConnectionSettings.DOCDB_ENDPOINT_URI);
            string docDBKey = CloudConfigurationManager.GetSetting(DTO.Constants.ConnectionSettings.DOCDB_KEY);

            Uri messageUri = UriFactory.CreateDocumentCollectionUri(DTO.Constants.ConnectionSettings.DOCDB_DATABASE_NAME,
                DTO.Constants.ConnectionSettings.DOCDB_COLLECTION_NAME);

            using (var client = new DocumentClient(new Uri(docDBendPoint), docDBKey))
            {
                foreach (var message in parsedMail.Mail)
                {
                    var response = await client.CreateDocumentAsync(messageUri, message);

                    if (!parsedMail.Attachments.Any())
                        continue;

                    foreach (var attachment in parsedMail.Attachments)
                    {
                        byte[] fileBytes;
                        using (var stream = new MemoryStream())
                        {
                            attachment.ContentObject.DecodeTo(stream);
                            fileBytes = stream.ToArray();
                        }
                        await client.CreateAttachmentAsync(response.Resource.AttachmentsLink, new MemoryStream(fileBytes),
                            new MediaOptions { ContentType = attachment.ContentType.MimeType, Slug = attachment.FileName });

                    }
                }
            }

        }

        internal static async Task<ParsedMail> ParseMessage(string messageId)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting(DTO.Constants.ConnectionSettings.WEBJOB_STORAGE));
            var blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("rawmail");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(messageId);

            MimeMessage mimeMessage;
            using (MemoryStream ms = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(ms);
                mimeMessage = MimeMessage.Load(new MemoryStream(ms.ToArray()));
                await blob.DeleteAsync();
            }
            return MimeMessageToDSMail(messageId, mimeMessage);
        }
        internal static ParsedMail MimeMessageToDSMail(string messageId, MimeMessage mimeMessage)
        {
            var parsedMails = new ParsedMail();
            parsedMails.Mail = new List<DSMail>();

            if (mimeMessage.Attachments != null)
                parsedMails.Attachments = mimeMessage.Attachments.OfType<MimePart>().ToList();
            else
                parsedMails.Attachments = new List<MimePart>();

            foreach (var toAddress in mimeMessage.To)
            {
                var mail = new DSMail();
                parsedMails.Mail.Add(mail);

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
                mail.SentDate = DateTime.Now;
                mail.MessageId = messageId;
                mail.Attachments = parsedMails.Attachments.Select(a => a.FileName).ToList();
            }

            return parsedMails;
        }
    }
}
