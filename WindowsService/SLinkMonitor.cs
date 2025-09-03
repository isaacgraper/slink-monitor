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
        private const string STARLINK_SSID = "Starlink";
        private const int TIMER_INTERVAL_MS = 60000; // 1 minute

        private System.Timers.Timer _monitorTimer;
        private Stopwatch _connectionStopwatch;
        private bool _wasConnectedLastCheck = false;

        private readonly ILogger<SLinkMonitor> _logger;

        public SLinkMonitor(ILogger<SLinkMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionStopwatch = new Stopwatch();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _logger.LogInformation("Service initialized...");

                // Initialize and start the timer
                _monitorTimer = new System.Timers.Timer(TIMER_INTERVAL_MS)
                {
                    AutoReset = true
                };

                // Attach the event handler
                _monitorTimer.Elapsed += OnTimedEvent;
                _monitorTimer.Start();

                _logger.LogInformation("Monitor timer started with {Interval}ms interval", TIMER_INTERVAL_MS);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting service");
                throw;
            }
        }

        protected override void OnStop() 
        {
            try
            {
                _logger.LogInformation("Stopping Starlink Monitor Service...");

                _monitorTimer?.Stop();
                _connectionStopwatch?.Stop();

                if (_connectionStopwatch?.IsRunning == true)
                {
                    _logger.LogInformation("Final connection duration: {Duration}", _connectionStopwatch.Elapsed);
                }

                _logger.LogInformation("Service stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping service");
            }
        }

        private void TryReconnect(string ssid)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", $"wlan connect name=\"{STARLINK_SSID}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit(5000);
                    _logger.LogInformation("Reconnection attempt to {SSID}", STARLINK_SSID);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnection failed");
            }
        }

        public void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            bool isCurrentlyConnected = NetworkInterface.GetAllNetworkInterfaces()
                  .Any(ni => ni.Name.Contains(STARLINK_SSID) && ni.OperationalStatus == OperationalStatus.Up);

            if (isCurrentlyConnected && !_wasConnectedLastCheck)
            {
                _connectionStopwatch.Restart(); // Reset the stopwatch
                _wasConnectedLastCheck = true; // Update the connection status
                _logger.LogInformation("Starlink connection estabilished.");
            }
          
            else if (!isCurrentlyConnected && _wasConnectedLastCheck)
            {
                _connectionStopwatch.Stop(); // Stop the stopwatch
                _wasConnectedLastCheck = false; // Update the connection status
                _logger.LogWarning("Starlink connection lost. Lasted for {Duration}", _connectionStopwatch.Elapsed);

                TryReconnect(STARLINK_SSID);
            }
        }
    }
}
