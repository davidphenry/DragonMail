using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.DTO.Constants
{
    public static class ConnectionSettings
    {
        public static readonly string WEBJOB_STORAGE = "WebJob:Storage";

        public static readonly string DOCDB_ENDPOINT_URI = "DocDB:URI";
        public static readonly string DOCDB_KEY = "DocDB:Key";
        public static readonly string DOCDB_DATABASE_NAME = "mailDB";
        public static readonly string DOCDB_COLLECTION_NAME = "mailColl";
    }
}
