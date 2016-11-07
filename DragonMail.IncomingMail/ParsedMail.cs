using DragonMail.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.IncomingMail
{
    public class ParsedMail
    {
        public List<DSMail> Mail { get; set; }
        public List<ParsedAttachment> Attachments { get; set; }
    }
}
