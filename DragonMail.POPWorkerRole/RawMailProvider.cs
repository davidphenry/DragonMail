using DragonMail.DTO;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.POPWorkerRole
{
    public interface IRawMailProvider
    {
        Task<byte[]> GetRawMail(DSMail source);
        string GetRawMailText(DSMail source);
    }

    public class RawMailProvider : IRawMailProvider
    {
        private DocumentClient Client;
        private Uri CollectionUri;
        public RawMailProvider(DocumentClient docClient, Uri collectionUri)
        {
            this.Client = docClient;
            this.CollectionUri = collectionUri;
        }

        public async Task<byte[]> GetRawMail(DSMail source)
        {
            var docUri = UriFactory.CreateDocumentUri(DTO.Constants.ConnectionSettings.DOCDB_DATABASE_NAME,
                DTO.Constants.ConnectionSettings.DOCDB_COLLECTION_NAME, source.id);
            var response = await Client.ReadDocumentAsync(docUri);

            string attachmentLink = response.Resource.AttachmentsLink;
            var attachment = Client.CreateAttachmentQuery(attachmentLink)
                .Where(a => a.Id == source.MessageId)
                .AsEnumerable()
                .FirstOrDefault();

            if (attachment == null)
                throw new ApplicationException("Not Found");
            var mediaResponse = await Client.ReadMediaAsync(attachment.MediaLink);

            byte[] rawMail = new byte[mediaResponse.ContentLength];
            await mediaResponse.Media.ReadAsync(rawMail, 0, (int)mediaResponse.ContentLength);
            return rawMail;
        }

        public string GetRawMailText(DSMail source)
        {
            var task = GetRawMail(source);
            task.RunSynchronously();
            task.Wait();
            byte[] rawMail = task.Result;
            ASCIIEncoding encoder = new ASCIIEncoding();
            string rawText = encoder.GetString(rawMail, 0, rawMail.Length);
            return rawText;
        }
    }
}
