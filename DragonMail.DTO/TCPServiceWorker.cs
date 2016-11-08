using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DragonMail.DTO
{
    public abstract class TCPServiceWorker
    {
        protected readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        protected readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        protected TcpListener listener;

        public virtual void Run(Action<Exception> exceptionHandler, IPEndPoint endPoint)
        {
            listener = new TcpListener(endPoint);
            listener.ExclusiveAddressUse = false;
            listener.Start();
            Trace.WriteLine("Listening on " + endPoint.Address.ToString(), "Information");

            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    if (listener.Pending())
                    {
                        Trace.WriteLine("Incoming Request", "Information");
                        using (TcpClient c = listener.AcceptTcpClient())
                        {
                            ProcessClient(c);
                            c.Close();
                        }
                    }
                }
                catch (Exception x)
                {
                    if (exceptionHandler != null)
                        exceptionHandler.Invoke(x);
                    else
                        throw;
                }
            }

        }

        protected abstract void ProcessClient(TcpClient c);

    }
}
