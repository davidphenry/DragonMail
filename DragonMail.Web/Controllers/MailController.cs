using DragonMail.DTO;
using DragonMail.Web.Models;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DragonMail.Web.Controllers
{
    public class MailController : Controller
    {       

        // GET: Mail
        public ActionResult Index(string mailBox = null)
        {
            var model = new MailViewModel();

            if (string.IsNullOrEmpty(mailBox))
                return View(model);

            model.MailBox = mailBox;
            string queueName = DSMail.MessageQueue(string.Format("{0}@dragonmail.dragonspears.com", mailBox));

            string docDBendPoint = ConfigurationManager.AppSettings.Get(DTO.Constants.ConnectionSettings.DOCDB_ENDPOINT_URI);
            string docDBKey = ConfigurationManager.AppSettings.Get(DTO.Constants.ConnectionSettings.DOCDB_KEY);

            using (var client = new DocumentClient(new Uri(docDBendPoint), docDBKey))
            {
                var queryOptions = new FeedOptions { MaxItemCount = -1 };
                Uri collectionUri = UriFactory.CreateDocumentCollectionUri(DTO.Constants.ConnectionSettings.DOCDB_DATABASE_NAME, DTO.Constants.ConnectionSettings.DOCDB_COLLECTION_NAME);
                var mailQuery = client.CreateDocumentQuery<DSMail>(collectionUri, queryOptions)
                    .Where(m => m.Queue == queueName)
                    .OrderByDescending(m => m.SentDate);
                model.MailMessages = mailQuery.ToList();
            }

            return View(model);
        }
    }
}