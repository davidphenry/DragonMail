using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.Tests
{
    public class SMTPClientTest
    {
        public void EnsureSMTPReceived()
        {
            var message = new MailMessage("dhenry@dragonspears.com", "test@127.0.0.1", "from unit test", DateTime.Now.ToString());
            message.Attachments.Add(new Attachment(@"C:\Development\AdobeXMLFormsSamples.pdf"));
            var smtp = new SmtpClient("mail.dragonspears.com");
            smtp.Send(message);
            smtp.Dispose();
        }
    }
}
