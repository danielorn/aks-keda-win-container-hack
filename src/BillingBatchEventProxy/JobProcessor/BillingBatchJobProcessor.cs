
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Billing.BatchSupport.BatchJob.Event;

namespace Billing.Batch.EventProxy
{
    public class BillingBatchJobProcessor
    {

        static ComExecuteWrapper comExecuteWrapper;

        /**
        * ProcessJob is the method that will call the COM component method
        * and send the response back to the replyTo queue.
        * It assumes lifecycle management of the COM component 
        * calls are handled by the caller.
        */
        public static async IAsyncEnumerable<RespondMessage> ProcessJob(JobMessage batchJob, string correlationId)
        {

            // Process each jobItem in the batchJob
            // For each jobItem, call the COM component method and send the response back to the replyTo queue
            using (comExecuteWrapper = new ComExecuteWrapper())
            {
                foreach (var jobItem in batchJob.JobItems)
                {
                    // for each jobItem, call the COM component method
                    var response = ProcessJobItem(batchJob, jobItem);

                    //response.Output = "2;failed";
                    //throw new Exception("Error processing jobItem");

                    // Critical! Check if the response is null or if the response is an error
                    if (response == null )
                    {
                        throw new Exception("Error processing jobItem");
                    }

                    yield return response;

                    // Conditional check if the response is conditioned to terminate further processing
                    if (!BatchJob.AnalyzeResponce(response.Output) || !response.Success)
                    {
                        yield break;
                    }
                }
            }
        }

        /**
         * ProcessJob is the main method that processes the job.
         */
        private static RespondMessage ProcessJobItem(JobMessage batchJob, JobItem jobItem)
        {
            //convert jobitem.Parameters values to an []Object needed for the COM component
            var arguments = new List<Object>();
            foreach (var parameter in jobItem.Parameters)
            {
                arguments.Add(parameter.Value);
            }

            // Call the COM component method
            Console.WriteLine($"INFO: Executing jobItem step on method [{jobItem.Method}]");
            var comresp = CallComponentMethod(batchJob.ProgId, jobItem.Method, arguments);

            var isComSuccess = comresp != null;

            // Send the response back to the replyTo queue
            var respondMessage = new RespondMessage
            {
                Jobid = batchJob.JobId,
                Origin = batchJob.ProgId,
                Success = isComSuccess,
                Dt = DateTime.UtcNow,
                Output = isComSuccess ? comresp : BatchResults.XStateInitError,
                CallSequence = jobItem.CallSequence
            };

            return respondMessage;
        }

         /**
         * callComponentMethod is a helper method that calls the COM component method
         * and returns the result.
         */
        private static string CallComponentMethod(string progId, string methode, List<object> parameters)
        {
            foreach (var parameter in parameters)
            {
                comExecuteWrapper.AddParameter(parameter);
            }
            string resp = comExecuteWrapper.ExecuteMethod(progId, methode);

            if (resp == null)
            {
                Console.WriteLine("ERROR: Method call failed.");
                return null;
            }

            return resp;
        }
    }
}
