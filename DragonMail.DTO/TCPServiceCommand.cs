using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DragonMail.DTO
{
    public interface ITCPServiceCommand
    {
        void Execute(string message);
    }
    public abstract class TCPServiceCommand : ITCPServiceCommand
    {
        protected NetworkStream clientStream;
        protected TCPServiceCommand(NetworkStream clientStream)
        {
            this.clientStream = clientStream;
        }
        public abstract void Execute(string message);
    }
}
