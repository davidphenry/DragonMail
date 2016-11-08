using DragonMail.DTO;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.POPWorkerRole
{
    public class POPProcessor
    {
        private static DocumentClient _client;
        private static DocumentClient Client
        {
            get
            {
                if (_client == null)
                {
                    string docDBendPoint = RoleEnvironment.GetConfigurationSettingValue(DTO.Constants.ConnectionSettings.DOCDB_ENDPOINT_URI.Replace(":", ""));
                    string docDBKey = RoleEnvironment.GetConfigurationSettingValue(DTO.Constants.ConnectionSettings.DOCDB_KEY.Replace(":", ""));
                    _client = new DocumentClient(new Uri(docDBendPoint), docDBKey);
                }

                return _client;
            }
        }
        private static Uri _collectionUri;
        public static Uri CollectionUri
        {
            get
            {
                if (_collectionUri == null)
                    _collectionUri = UriFactory.CreateDocumentCollectionUri(DTO.Constants.ConnectionSettings.DOCDB_DATABASE_NAME, DTO.Constants.ConnectionSettings.DOCDB_COLLECTION_NAME);
                return _collectionUri;
            }
        }
        TcpClient client;
        List<DSMail> mailBoxMail;
        public POPProcessor(TcpClient client, List<DSMail> mailBoxMail)
        {
            this.client = client;
            this.mailBoxMail = mailBoxMail;
        }

        public void ProcessUIDL(string message)
        {
            var listArr = message.Split(' ');
            string listResponse = null;
            if (listArr.Length == 2)
            {
                int messageNum = 0;
                int.TryParse(listArr[1], out messageNum);
                listResponse = string.Format("+OK {0} {1})", messageNum, mailBoxMail[messageNum - 1].id);
            }
            else
            {
                TCPServiceWorker.Write(client.GetStream(), "+OK");
                for (int i = 1; i <= mailBoxMail.Count; i++)
                {
                    TCPServiceWorker.Write(client.GetStream(), string.Format("{0} {1}", i, mailBoxMail[i - 1].id));
                }
                TCPServiceWorker.Write(client.GetStream(), ".");
            }
        }

        public void ProcessList(string message)
        {
            var listArr = message.Split(' ');
            string listResponse = null;
            if (listArr.Length == 2)
            {
                int messageNum = 0;
                int.TryParse(listArr[1], out messageNum);
                listResponse = string.Format("+OK {0} {1})", messageNum, mailBoxMail[messageNum - 1].RawMailSize);
                TCPServiceWorker.Write(client.GetStream(), listResponse);
            }
            else
            {
                listResponse = string.Format("+OK {0} messages", mailBoxMail.Count);
                TCPServiceWorker.Write(client.GetStream(), listResponse);

                for (int i = 1; i <= mailBoxMail.Count; i++)
                {
                    listResponse = string.Format("{0} {1}", i, mailBoxMail[i - 1].RawMailSize);
                    TCPServiceWorker.Write(client.GetStream(), listResponse);
                }
                TCPServiceWorker.Write(client.GetStream(), ".");
            }
        }

        public void ProcessTop(string message)
        {
            TCPServiceWorker.Write(client.GetStream(), "+OK");
            var topArr = message.Split(' ');
            int messageNum = 0;
            int numLines = 0;
            if (topArr.Length == 3)
            {
                int.TryParse(topArr[2], out messageNum);
                int.TryParse(topArr[3], out numLines);
            }
            var dsMail = mailBoxMail[messageNum - 1];
            string mailText = GetRawMailText(dsMail);
            var lines = mailText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            int blankIndex = 0;
            for (blankIndex = 0; blankIndex < lines.Length; blankIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[blankIndex]))
                    break;
            }
            var bldr = new StringBuilder(mailText.Substring(0, blankIndex));
            bldr.AppendLine();
            bldr.Append(mailText.Substring(blankIndex + 1, blankIndex + 1 + numLines));

            TCPServiceWorker.Write(client.GetStream(), bldr.ToString());
            TCPServiceWorker.Write(client.GetStream(), ".");
        }

        private async Task<byte[]> GetRawMail(DSMail source)
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

        private string GetRawMailText(DSMail source)
        {
            var task = GetRawMail(source);
            task.RunSynchronously();
            task.Wait();
            byte[] rawMail = task.Result;
            ASCIIEncoding encoder = new ASCIIEncoding();
            string rawText = encoder.GetString(rawMail, 0, rawMail.Length);
            return rawText;
        }

        public void ProcessDelete(string message)
        {
            var delArr = message.Split(' ');
            if (delArr.Length == 2)
            {
                int messageNum = 0;
                int.TryParse(delArr[1], out messageNum);
                mailBoxMail[messageNum - 1].MessageStatus = 1;
                TCPServiceWorker.Write(client.GetStream(), "+OK message deleted");
            }
            else
            {
                TCPServiceWorker.Write(client.GetStream(), "-ERR message not found");
            }
        }

        public void ProcessRetrieve(string message)
        {
            var readArr = message.Split(' ');
            if (readArr.Length == 2)
            {
                int messageNum = 0;
                int.TryParse(readArr[1], out messageNum);
                var mail = mailBoxMail[messageNum - 1];
                TCPServiceWorker.Write(client.GetStream(), string.Format("+OK {0} octets", mail.RawMailSize));
                var readTask = GetRawMail(mail);
                readTask.Wait();
                byte[] rawMail = readTask.Result;
                TCPServiceWorker.Write(client.GetStream(), rawMail);
                TCPServiceWorker.Write(client.GetStream(), ".");
            }
            else
            {
                TCPServiceWorker.Write(client.GetStream(), "-ERR message not found");
            }
        }
    }
}
