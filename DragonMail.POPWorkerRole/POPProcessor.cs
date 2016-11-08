using DragonMail.DTO;
using DragonMail.POPWorkerRole.Commands;
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
    public class POPCommandProcessor
    {
        private DocumentClient Client;
        private Uri CollectionUri;
        private NetworkStream clientStream;
        private List<DSMail> mailBoxMail;
        public POPCommandProcessor(NetworkStream clientStream, List<DSMail> mailBoxMail, DocumentClient docClient, Uri collectionUri)
        {
            this.Client = docClient;
            this.CollectionUri = collectionUri;
            this.clientStream = clientStream;
            this.mailBoxMail = mailBoxMail;
        }

        internal void ProcessUIDL(string message)
        {
            var cmd = new ProcessUIDLPOPCommand(clientStream, mailBoxMail);
            cmd.Execute(message);
        }

        internal void ProcessList(string message)
        {
            var cmd = new ProcessLISTPOPCommand(clientStream, mailBoxMail);
            cmd.Execute(message);
        }

        internal void ProcessTop(string message)
        {
            var cmd = new ProcessTOPPOPCommand(clientStream, mailBoxMail, new RawMailProvider(Client, CollectionUri));
            cmd.Execute(message);
        }
        internal void ProcessDelete(string message)
        {
            var cmd = new ProcessDELPOPCommand(clientStream, mailBoxMail);
            cmd.Execute(message);
        }

        internal void ProcessRetrieve(string message)
        {
            var cmd = new ProcessRETRPOPCommand(clientStream, mailBoxMail, new RawMailProvider(Client, CollectionUri));
            cmd.Execute(message);
        }

        public void MapTransactionCommand(string message)
        {
            if (message.StartsWith("UIDL"))
            {
                ProcessUIDL(message);
            }
            else if (message.StartsWith("LIST"))
            {
                ProcessList(message);
            }
            else if (message.StartsWith("TOP"))
            {
                ProcessTop(message);
            }
            else if (message.StartsWith("DELE"))
            {
                ProcessDelete(message);
            }
            else if (message.StartsWith("RETR"))
            {
                ProcessRetrieve(message);
            }
        }
    }
}
