using DragonMail.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.POPWorkerRole.Commands
{
    public class ProcessUIDLPOPCommand : BasePOPCommand
    {
        public ProcessUIDLPOPCommand(NetworkStream clientStream, List<DSMail> mailBoxMail) : base(clientStream, mailBoxMail) { }

        public override void Execute(string message)
        {
            var listArr = message.Split(' ');
            string listResponse = null;
            if (listArr.Length == 2)
            {
                int messageNum = 0;
                int.TryParse(listArr[1], out messageNum);
                listResponse = string.Format("+OK {0} {1})", messageNum, mailBoxMail[messageNum - 1].id);
            }
            else
            {
                clientStream.Write("+OK");
                for (int i = 1; i <= mailBoxMail.Count; i++)
                {
                    clientStream.Write(string.Format("{0} {1}", i, mailBoxMail[i - 1].id));
                }
                clientStream.Write(".");
            }
        }
    }
}
