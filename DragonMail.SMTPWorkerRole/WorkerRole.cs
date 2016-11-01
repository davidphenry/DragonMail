using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Net.Sockets;
using System.IO;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text;
using Newtonsoft.Json;
using DragonMail.DTO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WorkerRole1
{

    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
            var endPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["SMTPEndpoint"].IPEndpoint;
            TcpListener listener = new TcpListener(endPoint);
            listener.ExclusiveAddressUse = false;
            listener.Start();
            Trace.WriteLine("Listening on " + endPoint.Address.ToString(), "Information");

            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    if (listener.Pending())
                    {
                        Trace.WriteLine("Incoming Request", "Information");
                        ReceiveMessage(listener);
                    }
                }
                catch (Exception x)
                {
                    Trace.WriteLine(x.Message, "Exception");
                }
            }

        }
        private static void ReceiveMessage(TcpListener listener)
        {
            TcpClient c = listener.AcceptTcpClient();
            string text = HandleRequest(c);
            string id = Guid.NewGuid().ToString();

            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(RoleEnvironment.GetConfigurationSettingValue("ProcessQueueName"));
            queue.CreateIfNotExists();
            if (string.IsNullOrEmpty(text))
            {
                queue.AddMessage(new CloudQueueMessage("misfire " + DateTime.Now.ToString()));
                return;
            }

            var blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(RoleEnvironment.GetConfigurationSettingValue("ProcessContainerName"));
            container.CreateIfNotExists();
            var blob = container.GetBlockBlobReference(id);
            blob.UploadText(text);

            queue.AddMessage(new CloudQueueMessage(id));
            c.Close();
        }


        public static string HandleRequest(TcpClient client)
        {
            Write(client, "220 localhost -- Fake proxy server");
            string messageData = null;
            while (true)
            {
                string tcpMessage = null;
                try
                {
                    tcpMessage = Read(client);
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
                    client.Close();
                    break;//exit while
                }
                if (tcpMessage.StartsWith("EHLO") || tcpMessage.StartsWith("RCPT TO") || tcpMessage.StartsWith("MAIL FROM"))
                {
                    Write(client, "250 OK");
                }
                else if (tcpMessage.StartsWith("DATA"))
                {
                    Write(client, "354 Start mail input; end with");
                    messageData = ReadData(client);
                    Write(client, "250 OK");
                }
            }
            return messageData;
        }
        private static void Write(TcpClient client, String strMessage)
        {
            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = encoder.GetBytes(strMessage + "\r\n");

            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
        }

        private static string Read(TcpClient client)
        {
            byte[] messageBytes = new byte[8192];
            int bytesRead = 0;
            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            bytesRead = clientStream.Read(messageBytes, 0, 8192);
            string strMessage = encoder.GetString(messageBytes, 0, bytesRead);
            return strMessage;
        }
        private static string ReadData(TcpClient client)
        {
            var reader = new StreamReader(client.GetStream());
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
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
