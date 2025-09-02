using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace WindowsService
{
    public partial class SLinkMonitor : ServiceBase
    {
        public SLinkMonitor()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {


            // See current network connections

            // Looping SLink when connected
            // If SLink is connected, start a timer
            // If SLink disconnects, stop the timer and log the time

            // Send log to file into C:\SLinkMonitor\Logs or into Google Drive

            

        }

        protected override void OnStop()
        {
        }
    }
}
