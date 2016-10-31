using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.DTO
{
    public class SMTPEmailDTO
    {
        public string From { get; set; }
        public List<String> To { get; set; }

        public string Message { get; set; }

        public static string ParseLine(string strMessage)
        {
            string lineValue = strMessage.Split(':')[1];
            return lineValue.Replace("<", "").Replace(">", "");
        }

        public void AddToAddress(string to)
        {
            if (To == null)
                To = new List<string>();
            To.Add(to);
        }
    }
}
