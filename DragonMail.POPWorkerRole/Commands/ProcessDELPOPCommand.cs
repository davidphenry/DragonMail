using DragonMail.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.POPWorkerRole.Commands
{
    public class ProcessDELPOPCommand : BasePOPCommand
    {
        public ProcessDELPOPCommand(NetworkStream clientStream, List<DSMail> mailBoxMail) : base(clientStream, mailBoxMail) { }

        public override void Execute(string message)
        {
            var delArr = message.Split(' ');
            if (delArr.Length == 2)
            {
                int messageNum = 0;
                int.TryParse(delArr[1], out messageNum);
                mailBoxMail[messageNum - 1].MessageStatus = 1;
                clientStream.Write("+OK message deleted");
            }
            else
            {
                clientStream.Write("-ERR message not found");
            }
        }
    }
}
