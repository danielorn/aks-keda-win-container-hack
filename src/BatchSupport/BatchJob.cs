using System;
using System.Collections.Generic;
using System.Linq;

namespace Billing.BatchSupport.BatchJob.Event
{
    public class BatchJob : JobMessage
    {

        public Dictionary<int, JobItemStatus> JobItemsStatus { get; set; }
        public JobStatus JobStatus { get; set; }
        private int CurrentCallSequence { get; set; }
        public int NextCallSequence { get { return CurrentCallSequence + 1; } }

        public BatchJob(string jobId, string origin)
        {
            JobItems = new List<JobItem>();
            JobItemsStatus = new Dictionary<int, JobItemStatus>();
            JobId = jobId;
            Origin = origin;
            Dt = System.DateTime.Now;
            CurrentCallSequence = 0;
        }

        public void AddJobItem(string method, object[] parameters)
        {
            JobItems.Add(
                    new JobItem
                    {
                        CallSequence = JobItems.Count + 1,
                        Method = method,
                        Parameters = parameters.Select(arg => new Parameter { Value = arg }).ToList()
                    });
        }

        public void SetJobItemStatus(int callSequence, string returnMessage)
        {

            if (returnMessage == null)
                returnMessage = "4;Fatal Error";

            // Check if the call sequence is already processed
            if (JobItemsStatus.ContainsKey(callSequence))
            {
                JobStatus = JobStatus.Failed;
                return;
            }
            

            if (callSequence != NextCallSequence)
                return;

            CurrentCallSequence = NextCallSequence;

            var itemStatus = ItemStatus.Pending;

            if (ReturnMessageToJobItemStatus(returnMessage) == ItemStatus.Error)
            {
                JobStatus = JobStatus.Failed;
                itemStatus = ItemStatus.Error;
            }
            else itemStatus = ItemStatus.Ok;

            JobItemsStatus.Add(callSequence, new JobItemStatus { ReturnMessage = returnMessage, Status = itemStatus });

            // Check if all items are done
            if (JobItemsStatus.Count == JobItems.Count)
            {
                if (JobItemsStatus.Values.All(x => x.Status == ItemStatus.Ok))
                    JobStatus = JobStatus.Completed;
                else
                    JobStatus = JobStatus.Failed;
            }
        }

        public static bool AnalyzeResponce(String response)
        {
            // Check if >= XStateError
            if (ReturnMessageToJobItemStatus(response) >= ItemStatus.Error)
            {
                return false;
            }
            else return true;
        }


        public static ItemStatus ReturnMessageToJobItemStatus(string returnMessage)
        {

            // 0 ;Ok
            // 1 ;Warning
            // 2 ;Error
            // 3 ;Fatal Error
            // 4 ;Panic

            // return status based on return message stsrt  n;
            var status = returnMessage?.Split(';')[0];
            switch (status)
            {
                case "0":
                    return ItemStatus.Ok;
                case "1":
                    return ItemStatus.Warning;
                case "2":
                    return ItemStatus.Error;
                case "3":
                    return ItemStatus.Error;
                case "4":
                    return ItemStatus.Error;
                default:
                    return ItemStatus.Ok; // default to ok to handle demo com
            }
        }
    }

    public class JobItemStatus
    {
       
        public string ReturnMessage;
        public ItemStatus Status;
    }
    public enum JobStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }
    public enum ItemStatus
    {
        Pending,
        Ok,
        Warning,
        Error,
    }

    public static class BatchResults
    {
        public const string XStateOk = "0;Ok";

        public const string XStateWarning = "1;Varning - Se kvitto och fellogg för mer detaljer";

        public const string XStateError = "2;Fel - Se fellogg och kontakta support";

        public const string XStateInitError = "3;Fel - Internt fel vid initiering. Kontakta support";

        public const string XStateUninitError = "3;Fel - Internt fel vid avinitiering. Kontakta support";

        public const string XStatePanic = "4;Fel - Kontakta support";

    }



}
