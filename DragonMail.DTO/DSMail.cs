﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.DTO
{
    public class DSMail
    {
        public DSMail(){}
        public string id { get; set; }

        public string MessageId { get; set; }

        public string ToName { get; set; }
        public string ToEmail { get; set; }
        public string FromName{ get; set; }
        public string FromEmail { get; set; }
        public string Content { get; set; }        
        public string Subject { get; set; }
        public string TextBody { get; set; }
        public string HtmlBody { get; set; }
        public DateTime? SentDate { get; set; }
        public string Queue { get; set; }       
        public Dictionary<string, int> Attachments { get; set; }
        public int RawMailSize { get; set; }

        public int MessageStatus { get; set; }
        public static string MessageQueue(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            return email.ToLower().Replace('@', '-').Replace('.', '-');
        }
     
        public string TextPreview()
        {
            string miniContent = TextBody;
            if (string.IsNullOrEmpty(TextBody))
                miniContent = "NB";
            else if (TextBody.Length > 100)
                miniContent = TextBody.Substring(0, 99);
            return miniContent;
        }
     
        public string SubjectDisplay()
        {
            return string.IsNullOrEmpty(Subject) ? "[No Subject]" : Subject;
        }   
    }
}
