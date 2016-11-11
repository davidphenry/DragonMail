using DragonMail.DTO;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.IncomingMail
{
    public interface IMailParser
    {
        Task ParseMessage(string messageId);
    }
    public class IncomingMailParser : IMailParser
    {
        string docDBendPoint;
        string docDBKey;
        CloudStorageAccount storageAccount;
        public IncomingMailParser(string docDBendPoint, string docDBKey, CloudStorageAccount storageAccount)
        {
            this.docDBendPoint = docDBendPoint;
            this.docDBKey = docDBKey;
            this.storageAccount = storageAccount;

        }
        public async Task ParseMessage(string messageId)
        {
            var parsedMail = await GenerateParsedMail(messageId);

            await SaveParsedMail(parsedMail);
        }
        public async Task<ParsedMail> GenerateParsedMail(string messageId)
        {
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
            var parsedMail = mimeMessage.MimeMessageToDSMail(messageId);
            parsedMail.RawMail = rawMail;
            return parsedMail;
        }
        internal async Task SaveParsedMail(ParsedMail parsedMail)
        {

            Uri messageUri = UriFactory.CreateDocumentCollectionUri(DTO.Constants.ConnectionSettings.DOCDB_DATABASE_NAME,
                DTO.Constants.ConnectionSettings.DOCDB_COLLECTION_NAME);

            using (var client = new DocumentClient(new Uri(docDBendPoint), docDBKey))
            {
                foreach (var message in parsedMail.Mail)
                {
                    message.RawMailSize = parsedMail.RawMail.Length;
                    var response = await client.CreateDocumentAsync(messageUri, message);

                    await client.CreateAttachmentAsync(response.Resource.AttachmentsLink, new MemoryStream(parsedMail.RawMail),
                        new MediaOptions { ContentType = "application/octect-stream", Slug = message.MessageId });

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
    }
}
