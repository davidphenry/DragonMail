using DragonMail.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace DragonMail.POPWorkerRole
{
    public class POPService : TCPServiceWorker
    {
        public static readonly string mailBoxPassword = "DragonMail2016!";

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

        protected override void ProcessClient(TcpClient client)
        {
            Write(client.GetStream(), "+OK POP3 server ready");

            string mailBox = null;
            //authorization
            if (!AuthorizeClient(client, out mailBox))
            {
                Write(client.GetStream(), "-ERR mailBox does not exist");
                return;
            }
            Write(client.GetStream(), string.Format("+OK {0} is a valid mailbox", mailBox));

            //transaction state
            var mailBoxMail = GetMail(mailBox);
            var commandProcessor = new POPProcessor(client, mailBoxMail);
            bool isInTransaction = false;
            while (true)
            {
                string message = Read(client.GetStream());
                if (string.IsNullOrEmpty(message) || message.StartsWith("QUIT"))
                {
                    if (isInTransaction)
                    {
                        SaveMail(mailBoxMail);
                    }
                    Write(client.GetStream(), "+OK bye");
                    break;
                }
                else if (message.StartsWith("NOOP"))
                {
                    Write(client.GetStream(), "+OK");
                }
                else if (message.StartsWith("STAT"))
                {
                    string mailStats = string.Format("+OK {0} {1}", mailBoxMail.Count, mailBoxMail.Sum(m => m.RawMailSize));
                    Write(client.GetStream(), mailStats);
                }
                else if (message.StartsWith("UIDL"))
                {
                    commandProcessor.ProcessUIDL(message);
                }
                else if (message.StartsWith("LIST"))
                {
                    commandProcessor.ProcessList(message);
                }
                else if (message.StartsWith("TOP"))
                {
                    commandProcessor.ProcessTop(message);
                }
                else if (message.StartsWith("DELE"))
                {
                    commandProcessor.ProcessDelete(message);
                }
                else if (message.StartsWith("RETR"))
                {
                    commandProcessor.ProcessRetrieve(message);
                }
                isInTransaction = true;
            }
        }

        private bool AuthorizeClient(TcpClient client, out string mailBox)
        {
            mailBox = null;

            string message = Read(client.GetStream());
            if (string.IsNullOrEmpty(message))
                return false;

            while (!message.StartsWith("USER"))
            {
                if (string.Compare(message, "capa\r\n", true) == 0)
                {
                    Write(client.GetStream(), "+OK List of capabilities follows");
                    Write(client.GetStream(), "USER");
                    Write(client.GetStream(), "UIDL");
                    Write(client.GetStream(), ".");
                }
                else if (string.Compare(message, "auth \r\n", true) == 0)
                    Write(client.GetStream(), "-ERR no");

                message = Read(client.GetStream());
            }

            var authUser = message.Split(' ');
            if (authUser.Length != 2)
                return false;

            if (string.Compare(authUser[0], "user", true) != 0)
                return false;

            mailBox = authUser[1].Split('@')[0];
            Write(client.GetStream(), string.Format("+OK {0} is a valid mailbox", mailBox));

            //password
            message = Read(client.GetStream());
            if (string.IsNullOrEmpty(message))
                return false;

            authUser = message.Split(' ');
            if (authUser.Length != 2)
                return false;

            if (string.Compare(authUser[0], "pass", true) != 0)
                return false;

            return string.Compare(authUser[0], mailBoxPassword, true) != 0;
        }

        private List<DSMail> GetMail(string mailBox)
        {
            string queueName = DSMail.MessageQueue(string.Format("{0}@dragonmail.dragonspears.com", mailBox));

            var mail = Client.CreateDocumentQuery<DSMail>(CollectionUri, new FeedOptions { MaxItemCount = -1 })
                .Where(m => m.Queue == queueName)
               .ToList();
            return mail.Where(m => m.MessageStatus == 0)
            .ToList();//message status 0 is unprocessed
        }

        private void SaveMail(IEnumerable<DSMail> mail)
        {
            var collUri = UriFactory.CreateDocumentCollectionUri(DTO.Constants.ConnectionSettings.DOCDB_DATABASE_NAME, DTO.Constants.ConnectionSettings.DOCDB_COLLECTION_NAME);
            var tasks = new List<Task>();
            foreach (var doc in mail.Where(m => m.MessageStatus == 1)) //pending delete
            {
                var t = Client.UpsertDocumentAsync(collUri, doc);
                t.Start();
                tasks.Add(t);
            }
            Task.WaitAll(tasks.ToArray());
        }

       
    }
}
