using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.IncomingMail
{    public class ParsedAttachment
    {
        public ParsedAttachment(string name, byte[] file, string contentType)
        {
            Name = name;
            File = file;
            ContentType = contentType;
        }
        public string Name { get; set; }
        public byte[] File { get; set; }
        public string ContentType { get; set; }
    }
}
