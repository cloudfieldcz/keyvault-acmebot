using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azure.WebJobs.Extensions.HttpApi;

using DurableTask.TypedProxy;

using KeyVault.Acmebot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace KeyVault.Acmebot.Functions
{
    class GetFrontDoors : HttpFunctionBase
    {
        public GetFrontDoors(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        [FunctionName(nameof(GetFrontDoors) + "_" + nameof(Orchestrator))]
        public async Task<IList<AzureFrontDoor>> Orchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var activity = context.CreateActivityProxy<ISharedActivity>();

            var frontdoors = await activity.GetAllFDoors(null);

            frontdoors.Insert(0, new AzureFrontDoor("#CERTIFICATE-2048#", new List<string>()));
            frontdoors.Insert(0, new AzureFrontDoor("#CERTIFICATE-4096#", new List<string>()));
            frontdoors.Insert(0, new AzureFrontDoor("#CERTIFICATE-EXP-2048#", new List<string>()));
            frontdoors.Insert(0, new AzureFrontDoor("#CERTIFICATE-EXP-4096#", new List<string>()));

            return frontdoors.ToArray();
        }

        [FunctionName(nameof(GetFrontDoors_HttpStart))]
        public async Task<IActionResult> GetFrontDoors_HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "get-frontdoors")] HttpRequest req,
            [DurableClient] IDurableClient starter,
            ILogger log)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(GetFrontDoors) + "_" + nameof(Orchestrator), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId, TimeSpan.FromMinutes(2));
        }
    }
}
