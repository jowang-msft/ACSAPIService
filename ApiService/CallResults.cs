using System;
using System.Collections.Generic;

namespace CallApiService
{
    public class CallResults
    {
        public DateTime Date { get; set; }
        public string HosterArchitecture { get; set; }
        public string HostingProcess { get; set; }
        public string WorkingFolder { get; set; }
        public List<string> AudioDevices { get; set; }
        public List<string> Journal { get; set; }
    }
}
