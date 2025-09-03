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
        private const string STARLINK_SSID = "Pirarara";
        private const int TIMER_INTERVAL_MS = 60000; // 1 minute

        private System.Timers.Timer _monitorTimer;
        private Stopwatch _connectionStopwatch;
        private bool _wasConnectedLastCheck = false;


        public SLinkMonitor()
        {
            _connectionStopwatch = new Stopwatch();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                WriteLog("Service initialized...");

                // Initialize and start the timer
                _monitorTimer = new System.Timers.Timer(TIMER_INTERVAL_MS)
                {
                    AutoReset = true
                };

                // Attach the event handler
                _monitorTimer.Elapsed += OnTimedEvent;
                _monitorTimer.Start();

                WriteLog($"Monitor timer started with {TIMER_INTERVAL_MS}ms interval");
            }
            catch (Exception ex)
            {
                WriteLog($"{ex} Error starting service");
                throw;
            }
        }

        protected override void OnStop() 
        {

            WriteLog("Stopping Starlink Monitor Service...");

            _monitorTimer?.Stop();

            if (_connectionStopwatch?.IsRunning == true)
            {
                _connectionStopwatch?.Stop();
                WriteLog($"Final connection duration: {_connectionStopwatch.Elapsed}");
            }

            WriteLog("Service stopped successfully");
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
                    WriteLog($"Reconnection attempt to {STARLINK_SSID}");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"{ex} Reconnection failed");
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
                WriteLog("Starlink connection estabilished.");
            }
          
            else if (!isCurrentlyConnected && _wasConnectedLastCheck)
            {
                _connectionStopwatch.Stop(); // Stop the stopwatch
                _wasConnectedLastCheck = false; // Update the connection status
                WriteLog($"Starlink connection lost. Lasted for {_connectionStopwatch.Elapsed}");

                TryReconnect(STARLINK_SSID);
            }
        }

        private void WriteLog(string message)
        {
            string source = "Starlink Monitor Service";
            string log = "Application";
            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, log);
            }
            EventLog.WriteEntry(source, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }
}
