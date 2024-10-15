using System;
using System.Threading.Tasks;

namespace Billing.BatchSupport.BatchJob.Event
{
    public class RespondMessage
    {
        public string Jobid { get; set; }
        public int CallSequence { get; set; }
        public bool Success { get; set; }
        public string Origin { get; set; }
        public string Output { get; set; }
        public DateTime Dt { get; set; }
    }
}
