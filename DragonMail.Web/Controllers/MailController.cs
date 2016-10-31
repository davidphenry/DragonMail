using DragonMail.DTO;
using DragonMail.Web.Models;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DragonMail.Web.Controllers
{
    public class MailController : Controller
    {
        private const string ENDPOINT_URI = "https://dragonmail.documents.azure.com:443/";
        private const string DOCDB_KEY = "b4TqVBpGPZVbh9BahrDrkx22zfXa79GJNb1hUtklbOikI5cP3S0NXRyITuCmYRT1cEdi1sWgTYrWBd6cdwhdpg";
        private const string DATABASE_NAME = "mailDB";
        private const string COLLECTION_NAME = "mailColl";

        // GET: Mail
        public ActionResult Index(string mailBox = null)
        {
            var model = new MailViewModel();

            if (string.IsNullOrEmpty(mailBox))
                return View(model);

            model.MailBox = mailBox;
            string queueName = DSMail.MessageQueue(string.Format("{0}@dragonmail.dragonspears.com", mailBox));
            using (var client = new DocumentClient(new Uri(ENDPOINT_URI), DOCDB_KEY))
            {
                var queryOptions = new FeedOptions { MaxItemCount = -1 };
                var mailQuery = client.CreateDocumentQuery<DSMail>(UriFactory.CreateDocumentCollectionUri(DATABASE_NAME, COLLECTION_NAME), queryOptions)
                    .Where(m => m.Queue == queueName)
                    .OrderByDescending(m => m.SentDate);
                model.MailMessages = mailQuery.ToList();
            }

            return View(model);
        }
    }
}