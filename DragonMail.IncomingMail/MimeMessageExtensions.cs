using DragonMail.DTO;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.IncomingMail
{
    public static class MimeMessageExtensions
    {
        public static ParsedMail MimeMessageToDSMail(this MimeMessage mimeMessage, string messageId)
        {
            var parsedMail = new ParsedMail();
            parsedMail.Mail = new List<DSMail>();
            parsedMail.Attachments = new List<ParsedAttachment>();

            //find inline attachments
            var attachments = new List<MimePart>();
            if (mimeMessage.BodyParts != null)
            {
                attachments.AddRange(mimeMessage.BodyParts.OfType<MimePart>()
                    .Where(p => !string.IsNullOrEmpty(p.FileName) &&
                    (p.ContentDisposition == null || string.IsNullOrEmpty(p.ContentDisposition.Disposition) || p.ContentDisposition.Disposition == ContentDisposition.Inline))
                    .ToList());
            }
            if (mimeMessage.Attachments != null)
            {
                attachments.AddRange(mimeMessage.Attachments.OfType<MimePart>());

                foreach (var attachment in mimeMessage.Attachments.OfType<MessagePart>())
                    AddAttachment(parsedMail, attachment);
            }

            foreach (var attachment in attachments)
                AddAttachment(parsedMail, attachment);

            foreach (var toAddress in mimeMessage.To)
            {
                var mail = new DSMail();
                parsedMail.Mail.Add(mail);

                if (mimeMessage.From != null && mimeMessage.From.Count > 0)
                {
                    var firstFrom = mimeMessage.From.First() as MailboxAddress;
                    mail.FromEmail = firstFrom.Address;
                    mail.FromName = firstFrom.Name;
                }
                var toMailBox = toAddress as MailboxAddress;
                mail.ToName = toMailBox.Name;
                mail.ToEmail = toMailBox.Address;

                mail.Subject = mimeMessage.Subject;
                mail.HtmlBody = mimeMessage.HtmlBody;
                mail.TextBody = mimeMessage.TextBody;
                mail.Queue = DSMail.MessageQueue(mail.ToEmail);
                mail.SentDate = DateTime.Now;
                mail.MessageId = messageId;
                mail.Attachments = parsedMail.Attachments.ToDictionary(a => a.Name, a => a.File.Length);
            }

            return parsedMail;
        }
        internal static void AddAttachment(ParsedMail parsedMail, MimePart attachment)
        {
            byte[] fileBytes;
            using (var stream = new MemoryStream())
            {
                attachment.ContentObject.DecodeTo(stream);
                fileBytes = stream.ToArray();
            }
            parsedMail.Attachments.Add(new ParsedAttachment(attachment.FileName, fileBytes, attachment.ContentType.MimeType));
        }
        internal static void AddAttachment(ParsedMail parsedMail, MessagePart attachment)
        {
            byte[] fileBytes;
            using (var stream = new MemoryStream())
            {
                attachment.Message.WriteTo(stream);
                fileBytes = stream.ToArray();
            }
            parsedMail.Attachments.Add(new ParsedAttachment(string.Format("{0}_{1}.eml", attachment.Message.Subject, parsedMail.Attachments.Count + 1), fileBytes, attachment.ContentType.MimeType));
        }
    }
}
