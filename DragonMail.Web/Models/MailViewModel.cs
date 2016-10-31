using DragonMail.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DragonMail.Web.Models
{
    public class MailViewModel
    {        
        public MailViewModel()
        {
            MailMessages = new List<DSMail>();
        }

        public string MailBox { get; set; }
        public List<DSMail> MailMessages { get; set; }
    }


}