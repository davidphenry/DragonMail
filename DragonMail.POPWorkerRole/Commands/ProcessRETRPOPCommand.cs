using DragonMail.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
namespace DragonMail.POPWorkerRole.Commands
{
    public class ProcessRETRPOPCommand : BasePOPCommand
    {
        IRawMailProvider rawMailProvider;
        public ProcessRETRPOPCommand(NetworkStream clientStream, List<DSMail> mailBoxMail, IRawMailProvider rawMailProvider) : base(clientStream, mailBoxMail)
        {
            this.rawMailProvider = rawMailProvider;
        }

        public override void Execute(string message)
        {
            var readArr = message.Split(' ');
            if (readArr.Length == 2)
            {
                int messageNum = 0;
                int.TryParse(readArr[1], out messageNum);
                var mail = mailBoxMail[messageNum - 1];
                clientStream.Write(string.Format("+OK {0} octets", mail.RawMailSize));
                var readTask = rawMailProvider.GetRawMail(mail);
                readTask.Wait();
                byte[] rawMail = readTask.Result;
                clientStream.Write(rawMail);
                clientStream.Write(".");
            }
            else
            {
                clientStream.Write("-ERR message not found");
            }
        }
    }
}
