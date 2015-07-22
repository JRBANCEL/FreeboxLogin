namespace FreeboxOS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class CallLogEntry
    {
        public string Number { get; set; }

        public CallType Type { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTime Timestamp { get; set; }

        public string Name { get; set; }
    }

    public enum CallType
    {
        Accepted = 0,
        Outgoing = 1,
        Missed = 2
    }
}
