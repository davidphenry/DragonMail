using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.SMTPWorkerRole
{
    public class SMTPService : DTO.TCPServiceWorker
    {
        protected override void ProcessClient(TcpClient c)
        {
            string smptData = HandleRequest(c);

            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"));
            var queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference(RoleEnvironment.GetConfigurationSettingValue("ProcessQueueName"));
            queue.CreateIfNotExists();

            var blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(RoleEnvironment.GetConfigurationSettingValue("ProcessContainerName"));
            container.CreateIfNotExists();

            SaveMail(queue, container, smptData);
        }

        private void SaveMail(CloudQueue queue, CloudBlobContainer container, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                queue.AddMessage(new CloudQueueMessage("misfire " + DateTime.Now.ToString()));
                return;
            }

            string id = Guid.NewGuid().ToString();
            var blob = container.GetBlockBlobReference(id);
            blob.UploadText(text);

            queue.AddMessage(new CloudQueueMessage(id));
        }

        public static string HandleRequest(TcpClient client)
        {
            Write(client.GetStream(), "220 dragonmail -- Dynamic email server");
            string messageData = null;
            while (true)
            {
                string tcpMessage = null;
                try
                {
                    tcpMessage = Read(client.GetStream());
                }
                catch (Exception e)
                {
                    //a socket error has occured
                    break;
                }

                if (tcpMessage.Length <= 0)
                    continue;

                if (tcpMessage.StartsWith("QUIT"))
                {
                    //client.Close();
                    break;//exit while
                }
                if (tcpMessage.StartsWith("EHLO") || tcpMessage.StartsWith("RCPT TO") || tcpMessage.StartsWith("MAIL FROM"))
                {
                    Write(client.GetStream(), "250 OK");
                }
                else if (tcpMessage.StartsWith("DATA"))
                {
                    Write(client.GetStream(), "354 Start mail input; end with");
                    messageData = ReadData(client.GetStream());
                    Write(client.GetStream(), "250 OK");
                }
            }
            return messageData;
        }
        private static string ReadData(Stream clientStream)
        {
            var reader = new StreamReader(clientStream);
            StringBuilder data = new StringBuilder();
            string line = reader.ReadLine();

            if (line != null && line != ".")
            {
                data.AppendLine(line);

                for (line = reader.ReadLine(); line != null && line != "."; line = reader.ReadLine())
                {
                    data.AppendLine(line);
                }
            }

            var message = data.ToString();
            return message;
        }
    }
}
