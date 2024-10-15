using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Billing.BatchSupport.BatchJob.Event;

namespace Billing.Batch.EventProxy
{

    internal class BatchComponentEventProxy
    {

        private readonly ServiceBusClient Client;
        private readonly string QueueName;
        private readonly int MinDelay;
        private readonly int MaxDelay;
        private readonly TaskCompletionSource<bool> Tcs = new TaskCompletionSource<bool>();
        private BillingBatchJobProcessor JobProcessor;

        private delegate Task SendMessageDelegate(string replyTo, string correlationId, RespondMessage body);

        /**
         * 
         */
        public static async Task Main(string[] args)
        {
            var program = new BatchComponentEventProxy();
            await program.RunAsync();
            
            Console.WriteLine($"Shutdown.");
            Environment.Exit(0);
        }

        public BatchComponentEventProxy()
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddXmlFile("app.config", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Get configuration settings
            var connectionString = configuration["ConnectionStrings:ServiceBus"];
            QueueName = configuration["QueueSettings:QueueName"];
            MinDelay = configuration.GetSection("ProcessSettings").GetValue<int>("MinDelay", 3000);
            MaxDelay = configuration.GetSection("ProcessSettings").GetValue<int>("MaxDelay", 3000);

            Console.WriteLine($"Starting BillingBatchEventProxy listen on queue [{QueueName}]");

            JobProcessor = new BillingBatchJobProcessor();

            // Create a ServiceBusClient using the connectionString
            Client = new ServiceBusClient(connectionString);
        }

        /**
         * RunAsync is the main method that will process the messages
         * 
         */
        public async Task RunAsync()
        {
            // Create a processor that we can use to process the messages
            ServiceBusProcessor processor = Client.CreateProcessor(QueueName, new ServiceBusProcessorOptions());

            // Add handler to process messages
            processor.ProcessMessageAsync += MessageHandler;

            // Add handler to process any errors
            processor.ProcessErrorAsync += ErrorHandler;

            // Start processing
            await processor.StartProcessingAsync();

            // Wait until the first message is processed
            await Tcs.Task;

            // Stop processing and clean up resources
            Console.WriteLine($"Cleaning up");
            await processor.StopProcessingAsync();
            await processor.CloseAsync();
            await Client.DisposeAsync();
        }

        /* Handles the message received from the queue.
           The message is deserialized into a BatchJob object.
           Each job item in the BatchJob will receive a corresponding response.
           The responses are processed asynchronously using await foreach.
           Each response is then sent to the replyTo queue using the SendMessage method.
           If deserialization fails, an error is logged and appropriate error handling is performed.
           Finally, the message is marked as complete.
        */
        public async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();             // Retrieve Message Body

            var batchJob = ConvertToJobMessage(body);


            if (string.IsNullOrEmpty(args.Message.ReplyTo))
            {
                // nowhere to send the response
                Console.WriteLine($"Critical!, No replyTo address in message: {body}");
                Tcs.SetResult(true);
                return;
            }


            // If deserialization was successful, process the message
            if (batchJob != null)
            {
                try
                {
                    // Iterate over the responses from the ProcessJob method
                    await foreach (var response in BillingBatchJobProcessor.ProcessJob(batchJob, args.Message.CorrelationId))
                    {
                        // Handle each RespondMessage
                        Console.WriteLine($"Processed response: {response.Output}");

                        // send the response message using the SendMessage method
                        await SendMessage(args.Message.ReplyTo, args.Message.CorrelationId, response);

                        // delay between MinDelay and MaxDelay
                        Random rnd = new Random();
                        int delay = rnd.Next(MinDelay, MaxDelay);
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    var respondMessage = new RespondMessage
                    {
                        Jobid = batchJob.JobId,
                        Origin = batchJob.ProgId,
                        Success = false,
                        Dt = DateTime.UtcNow,
                        Output = BatchResults.XStatePanic,
                        CallSequence = 0
                    };
                    await SendMessage(args.Message.ReplyTo, args.Message.CorrelationId, respondMessage);

                    Console.WriteLine($"Error processing job: {ex.Message}");
                    // Send the response back to the replyTo queue

                }
            }
            else 
            {
                var message = $"Failed to deserialize msgBody: {body}";
                Console.WriteLine(message);

                var respondMessage = new RespondMessage
                {
                    Jobid = batchJob.JobId,
                    Origin = batchJob.ProgId,
                    Success = false,
                    Dt = DateTime.UtcNow,
                    Output = BatchResults.XStatePanic,
                    CallSequence = 0
                };
                await SendMessage(args.Message.ReplyTo, args.Message.CorrelationId, respondMessage);

                //await args.DeadLetterMessageAsync(args.Message, message); 
            }
            
            await args.CompleteMessageAsync(args.Message);
            Tcs.SetResult(true);
        }


        /**
         * sendMessage is a helper method that sends a message to the replyTo queue
         */
        private async Task SendMessage(string replyTo, string correlationId, RespondMessage body)
        {
            // Create a sender for the replyTo queue
            ServiceBusSender sender = Client.CreateSender(replyTo);

            // Create a response message
            var responseMessage = new ServiceBusMessage(JsonSerializer.Serialize(body))
            {
                ContentType = "application/json",
                CorrelationId = correlationId
            };

            // Send the response message
            await sender.SendMessageAsync(responseMessage);
            await sender.CloseAsync();
        }


        private async Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"Error: {args.Exception}");
            // Error handling needs to be implemented here, for example sending 
            // a message to a dead-letter queue or responding to the replyTo Queue with an error message

            Tcs.SetResult(true);
        }

        /**
         * ConvertToJobMessage is a helper method that deserializes a JSON string into a JobMessage object
         */
        private static JobMessage ConvertToJobMessage(string body)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonParameterConverter() },
                WriteIndented = true
            };

            // Deserialize the JSON string into msgBody
            Console.WriteLine($"Attempting to deserialize msgBody to an batchJob: {body}");
            var batchJob = JsonSerializer.Deserialize<JobMessage>(body, options);

            // If deserialization was successful, return the batchJob
            if (batchJob != null)
            {
                return batchJob;
            }
            else
            {
                Console.WriteLine($"Failed to deserialize msgBody: {body}");
                // Error handling needs to be implemented here, for example sending 
                // a message to a dead-letter queue or responding to the replyTo Queue with an error message
            }
            return null;
        }

    }



}
