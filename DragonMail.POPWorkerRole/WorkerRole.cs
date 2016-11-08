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
using Microsoft.ApplicationInsights;

namespace DragonMail.POPWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("DragonMail.POPWorkerRole is running");
            var endPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["POPEndpoint"].IPEndpoint;

            var appInsights = new TelemetryClient();
            var service = new POPService();
            service.Run(e => appInsights.TrackException(e), endPoint);
        }


        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("DragonMail.POPWorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("DragonMail.POPWorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("DragonMail.POPWorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var appInsights = new TelemetryClient();
            try
            {
                var endPoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["POPEndpoint"].IPEndpoint;
                var service = new POPService();
                service.Run(e => appInsights.TrackException(e), endPoint);
            }
            catch (Exception x)
            {
                appInsights.TrackException(x);
            }
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                await Task.Delay(1000);
            }
        }
    }
}
