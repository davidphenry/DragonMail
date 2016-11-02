using DragonMail.DTO;
using DragonMail.Web.Models;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace DragonMail.Web.Controllers
{
    public class MailController : Controller
    {
        private static DocumentClient _client;
        private static DocumentClient Client
        {
            get
            {
                if (_client == null)
                {
                    string docDBendPoint = ConfigurationManager.AppSettings.Get(DTO.Constants.ConnectionSettings.DOCDB_ENDPOINT_URI);
                    string docDBKey = ConfigurationManager.AppSettings.Get(DTO.Constants.ConnectionSettings.DOCDB_KEY);
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

        // GET: Mail
        public ActionResult Index(string id = null, string mailBox = null)
        {
            var model = new MailViewModel();

            if (string.IsNullOrEmpty(mailBox))
                return View(model);

            model.MailBox = mailBox;
            string queueName = DSMail.MessageQueue(string.Format("{0}@dragonmail.dragonspears.com", mailBox));
            var mailQuery = Client.CreateDocumentQuery<DSMail>(CollectionUri, new FeedOptions { MaxItemCount = -1 })
                .Where(m => m.Queue == queueName)
                .OrderByDescending(m => m.SentDate);

            model.MailMessages = mailQuery.ToList();
            if (string.IsNullOrEmpty(id))
                model.SelectedItem = model.MailMessages.FirstOrDefault();
            else
                model.SelectedItem = model.MailMessages.FirstOrDefault(m => m.Id == id);


            return View(model);
        }

        public async Task<ActionResult> Download(string id, string fileName)
        {
            var docUri = UriFactory.CreateDocumentUri(DTO.Constants.ConnectionSettings.DOCDB_DATABASE_NAME, DTO.Constants.ConnectionSettings.DOCDB_COLLECTION_NAME, id);
            var response = await Client.ReadDocumentAsync(docUri);
            var mailDTO = (DSMail)(dynamic)response.Resource;

            if (mailDTO == null)
                return new HttpNotFoundResult();

            string attachmentLink = response.Resource.AttachmentsLink;
            var attachment = Client.CreateAttachmentQuery(attachmentLink)
                .Where(a => a.Id == fileName)
                .AsEnumerable()
                .FirstOrDefault();

            if (attachment == null)
                return new HttpNotFoundResult();
            var mediaResponse = await Client.ReadMediaAsync(attachment.MediaLink);

            byte[] bytes = new byte[mediaResponse.ContentLength];
            await mediaResponse.Media.ReadAsync(bytes, 0, (int)mediaResponse.ContentLength);
            return new FileContentResult(bytes, mediaResponse.ContentType);
            
        }
    }
}
