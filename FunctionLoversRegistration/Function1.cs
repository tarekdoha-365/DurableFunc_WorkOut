using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FunctionLoversRegistration.DTO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FunctionLoversRegistration
{
    public static class Function1
    {
        [FunctionName("RegisterAttendee")]
        public static async Task<HttpResponseMessage> RegisterAttendee(
           [HttpTrigger(AuthorizationLevel.Function, "get","post")]
           HttpRequestMessage request,
           [DurableClient] IDurableOrchestrationClient starter,
           ILogger log
           )
        {
            var potentialAttendee = await request.Content.ReadAsAsync<PotentialAttendee>();
            log.LogWarning($"Processing {potentialAttendee}");
            var instanceId = await starter.StartNewAsync("RunRegistration", potentialAttendee);
            return request.CreateResponse(System.Net.HttpStatusCode.OK);
        }
        [FunctionName("RunRegistration")]
        public static async Task<RegistrationResult> RunRegistrationFlow(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log
            )
        {
            var potentialAttendee = context.GetInput<PotentialAttendee>();
            log.LogWarning($"StartingRegistration of " +
                $"{potentialAttendee.Name} in" +
                $"orchestartion {context.InstanceId}");

            var dataIsValid = await context.CallActivityAsync<bool>("VerifyData",
                new DataToVerify(potentialAttendee.EmailAddress));

            log.LogWarning("Data is {0}valid", dataIsValid ? string.Empty : "not ");
            if(!dataIsValid)
            {
                return new RegistrationResult()
                {
                    Reason= "No valid data given",
                    RegistrationStatus = RegistrationStatus.NoValidData
                };
            }

            return new RegistrationResult()
            {
                RegistrationStatus = RegistrationStatus.Ok,
                Reason = "No issue"
            };
        }
        [FunctionName("VerifyData")]
        public static async Task<bool> VerifyData(
            [ActivityTrigger] DataToVerify dataToVerify, ILogger log)
        {
            log.LogWarning($"Checking email {dataToVerify.EmailAddress}");
            var result = dataToVerify.EmailAddress.Contains("@");
            return await Task.FromResult(result);
        }
    }
}