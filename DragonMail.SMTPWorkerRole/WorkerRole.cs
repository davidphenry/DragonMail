using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Net.Sockets;
using System.IO;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text;
using Newtonsoft.Json;
using DragonMail.DTO;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.ApplicationInsights;

namespace DragonMail.SMTPWorkerRole
{

    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("SMTPWorker is running");
            var endPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["SMTPEndpoint"].IPEndpoint;

            var appInsights = new TelemetryClient();
            var service = new SMTPService();
            service.Run(e => appInsights.TrackException(e), endPoint);

        }
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("SMTPWorker has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("SMTPWorker is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("SMTPWorker has stopped");
        }

    }
}
