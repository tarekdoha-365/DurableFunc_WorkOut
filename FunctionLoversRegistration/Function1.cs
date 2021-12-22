using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FunctionLoversRegistration.Dtos;
using FunctionLoversRegistration.Entities;
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

            List<UserGroupInfo> allGroups =
               await context.CallActivityAsync<List<UserGroupInfo>>(
               "LoadGroups", null);
            log.LogError("Starting Checking with groups");
            var allCalls = new List<Task<bool>>();
            foreach (var group in allGroups)
            {
                allCalls.Add(context.CallActivityAsync<bool>("CheckWithGroup",
                    new UserGroupNotification()
                    {
                        GroupName = group.GroupName,
                        AttendeeName = potentialAttendee.Name
                    }));
            }
            bool[] data = await Task.WhenAll(allCalls);

            var dataIsValid = await context.CallActivityAsync<bool>("VerifyData",
                new DataToVerify(potentialAttendee.EmailAddress));

            log.LogWarning("Data is {0}valid", dataIsValid ? string.Empty : "not ");
            if (!dataIsValid)
            {
                return new RegistrationResult()
                {
                    Reason = "No valid data given",
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
        [FunctionName("CheckWithGroup")]
        public static async Task<bool> CheckWithGroup(
            [ActivityTrigger] UserGroupNotification userGroupNotification,
            ILogger log
            )
        {
            var random = new Random();
            bool res = random.NextDouble() > 0.1;
            log.LogWarning(res ? $"Group {userGroupNotification.GroupName} says 'yes' ."
                : $"Group {userGroupNotification.GroupName} says 'no' .");
            return await Task.FromResult(res);
        }
        [FunctionName("LoadGroups")]
        public static async Task<List<UserGroupInfo>> LoadGroups([ActivityTrigger] object notUsed,
            ILogger log)
        {
            string? data = Environment.GetEnvironmentVariable("Groups");
            string[] allParts = data.Split(",");

            List<UserGroupInfo> result =
                allParts.Select(
                    part => new UserGroupInfo()
                    { GroupName = part.Trim() }).ToList();
            log.LogWarning($"Loaded {allParts.Length} groups .");

            return await Task.FromResult(result);
        }
    }
}