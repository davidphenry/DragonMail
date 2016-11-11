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
                var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting(DTO.Constants.ConnectionSettings.WEBJOB_STORAGE));
                string docDBendPoint = CloudConfigurationManager.GetSetting(DTO.Constants.ConnectionSettings.DOCDB_ENDPOINT_URI);
                string docDBKey = CloudConfigurationManager.GetSetting(DTO.Constants.ConnectionSettings.DOCDB_KEY);

                IMailParser parser = new IncomingMailParser(docDBendPoint, docDBKey, storageAccount);
                await parser.ParseMessage(message);
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
