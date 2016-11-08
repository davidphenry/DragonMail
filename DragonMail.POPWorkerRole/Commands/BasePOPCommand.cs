using DragonMail.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace DragonMail.POPWorkerRole.Commands
{
    public abstract class BasePOPCommand : TCPServiceCommand
    {
        protected List<DSMail> mailBoxMail;
        protected BasePOPCommand(NetworkStream clientStream, List<DSMail> mailBoxMail)
            : base(clientStream)
        {
            this.mailBoxMail = mailBoxMail;
        }

        public override void Execute(string message)
        {
            throw new NotImplementedException();
        }
    }
}
