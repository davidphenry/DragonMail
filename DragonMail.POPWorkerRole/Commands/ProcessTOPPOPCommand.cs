using DragonMail.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.POPWorkerRole.Commands
{
    public class ProcessTOPPOPCommand : BasePOPCommand
    {
        IRawMailProvider rawMailProvider;
        public ProcessTOPPOPCommand(NetworkStream clientStream, List<DSMail> mailBoxMail, IRawMailProvider rawMailProvider) : base(clientStream, mailBoxMail)
        {
            this.rawMailProvider = rawMailProvider;
        }

        public override void Execute(string message)
        {
            clientStream.Write("+OK");
            var topArr = message.Split(' ');
            int messageNum = 0;
            int numLines = 0;
            if (topArr.Length == 3)
            {
                int.TryParse(topArr[2], out messageNum);
                int.TryParse(topArr[3], out numLines);
            }
            var dsMail = mailBoxMail[messageNum - 1];
            string mailText = rawMailProvider.GetRawMailText(dsMail);
            var lines = mailText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            int blankIndex = 0;
            for (blankIndex = 0; blankIndex < lines.Length; blankIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[blankIndex]))
                    break;
            }
            var bldr = new StringBuilder(mailText.Substring(0, blankIndex));
            bldr.AppendLine();
            bldr.Append(mailText.Substring(blankIndex + 1, blankIndex + 1 + numLines));

            clientStream.Write(bldr.ToString());
            clientStream.Write(".");
        }
    }
}
