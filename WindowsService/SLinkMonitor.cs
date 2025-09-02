using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace WindowsService
{
    public partial class SLinkMonitor : ServiceBase
    {
        const string NETWORK_INTERFACE_DOMAIN = "Starlink";
        
        private readonly ILogger<SLinkMonitor> _logger;

        public SLinkMonitor(ILogger<SLinkMonitor> logger)
        {
            _logger = logger;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _logger.LogInformation("Service initialized...");

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 10000;
            timer.AutoReset = true;
            timer.Elapsed += OnTimedEvent;
            timer.Start();
        }

        protected override void OnStop()
        {

        }

        public void SLinkConnection(string ssid)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netsh", $"wlan connect name=\"{ssid}\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);

                _logger.LogInformation("Trying to connect into: {SSID}", ssid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error trying to connect into network!");
            }
        }

        public void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();

            bool isNetworkAvailable = NetworkInterface.GetAllNetworkInterfaces()
                  .Any(ni => ni.Name.Contains(NETWORK_INTERFACE_DOMAIN) && ni.OperationalStatus == OperationalStatus.Up);

            if (isNetworkAvailable)
            {
                // Connection estabilished
                stopwatch.Start();

                _logger.LogInformation("Connection intialized...");

                // Connection lost
                if (!isNetworkAvailable)

                    stopwatch.Stop();

                    _logger.LogInformation("Last connection duration: {ts}", stopwatch.Elapsed);

                    stopwatch.Reset();

                    SLinkConnection(NETWORK_INTERFACE_DOMAIN);
            }
        }
    }
}
