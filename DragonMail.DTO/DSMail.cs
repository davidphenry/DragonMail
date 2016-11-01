using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.DTO
{
    public class DSMail
    {
        public DSMail(){}
       
        public string ToName { get; set; }
        public string ToEmail { get; set; }
        public string FromName{ get; set; }
        public string FromEmail { get; set; }
        public string Content { get; set; }
        public byte[] Attachments { get; set; }
        public string Subject { get; set; }
        public string TextBody { get; set; }
        public string HtmlBody { get; set; }
        public DateTime? SentDate { get; set; }
        public string Queue { get; set; }
        public string LogMessage()
        {
            string miniContent = Content;
            if (string.IsNullOrEmpty(Content))
                miniContent = "NB";
            else if (Content.Length > 25)
                miniContent = Content.Substring(0, 24);
            
            return string.Format("To:{0};From:{1};Content:{2}", ToEmail, FromEmail, miniContent);
        }

        public static string MessageQueue(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            return email.ToLower().Replace('@', '-').Replace('.', '-');
        }
    }
}
