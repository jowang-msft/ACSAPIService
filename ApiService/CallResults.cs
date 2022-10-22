using System;
using System.Collections.Generic;

namespace CallApiService
{
    public class CallResults
    {
        public CallResults()
        {
            // Gather some runtime environment information
            Date = DateTime.Now;
            HosterArchitecture = (IntPtr.Size == 4) ? "32-bit" : "64-bit";
            HostingProcess = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            WorkingFolder = System.IO.Directory.GetCurrentDirectory();
            Journal = new List<string>() { $"{System.Environment.OSVersion.Version.Major}.{System.Environment.OSVersion.Version.Minor}.{ System.Environment.OSVersion.Version.Build}" };
        }

        public DateTime Date { get; set; }
        public string HosterArchitecture { get; set; }
        public string HostingProcess { get; set; }
        public string WorkingFolder { get; set; }
        public List<string> AudioDevices { get; set; }
        public List<string> Journal { get; set; }
    }
}
