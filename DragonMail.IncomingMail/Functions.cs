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
                    message.RawMailSize = parsedMail.RawMail.Length;
                    var response = await client.CreateDocumentAsync(messageUri, message);

                    await client.CreateAttachmentAsync(response.Resource.AttachmentsLink, new MemoryStream(parsedMail.RawMail),
                        new MediaOptions { ContentType = "application/octect-stream", Slug = message.MessageId});

                    if (!parsedMail.Attachments.Any())
                        continue;

                    foreach (var attachment in parsedMail.Attachments)
                    {
                        await client.CreateAttachmentAsync(response.Resource.AttachmentsLink, new MemoryStream(attachment.File),
                            new MediaOptions { ContentType = attachment.ContentType, Slug = attachment.Name });

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
            byte[] rawMail = null;
            using (MemoryStream ms = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(ms);
                rawMail = ms.ToArray();
                mimeMessage = MimeMessage.Load(new MemoryStream(rawMail));
                await blob.DeleteAsync();
            }
            var parsedMail = MimeMessageToDSMail(messageId, mimeMessage);
            parsedMail.RawMail = rawMail;
            return parsedMail;
        }
        internal static ParsedMail MimeMessageToDSMail(string messageId, MimeMessage mimeMessage)
        {
            var parsedMail = new ParsedMail();
            parsedMail.Mail = new List<DSMail>();
            parsedMail.Attachments = new List<ParsedAttachment>();

            if (mimeMessage.Attachments != null)
            {
                foreach (var attachment in mimeMessage.Attachments.OfType<MimePart>().ToList())
                {
                    byte[] fileBytes;
                    using (var stream = new MemoryStream())
                    {
                        attachment.ContentObject.DecodeTo(stream);
                        fileBytes = stream.ToArray();
                    }
                    parsedMail.Attachments.Add(new ParsedAttachment(attachment.FileName, fileBytes, attachment.ContentType.MimeType));
                }
            }

            foreach (var toAddress in mimeMessage.To)
            {
                var mail = new DSMail();
                parsedMail.Mail.Add(mail);

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
                mail.Attachments = parsedMail.Attachments.ToDictionary(a => a.Name, a => a.File.Length);
            }

            return parsedMail;
        }
    }
}
