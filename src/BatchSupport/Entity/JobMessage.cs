using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Billing.BatchSupport.BatchJob.Event
{
    public class JobMessage
    {
        // Define a delegate that matches the signature of the SendMessage method

        public string JobId { get; set; }
        public string ProgId { get; set; }
        public string Origin { get; set; }
        public DateTime Dt { get; set; }
        public List<JobItem> JobItems { get; set; }
    }
}
