using System.Collections.Generic;

namespace Billing.BatchSupport.BatchJob.Event
{
    public class JobItem
    {
        public int CallSequence { get; set; }
        public string Method { get; set; }
        public List<Parameter> Parameters { get; set; }
    }
}
