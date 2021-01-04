﻿using System;
using System.Threading.Tasks;

using DurableTask.TypedProxy;

using KeyVault.Acmebot.Contracts;
using KeyVault.Acmebot.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace KeyVault.Acmebot
{
    public class RenewCertificatesFunctions
    {
        [FunctionName(nameof(RenewCertificates))]
        public async Task RenewCertificates([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var activity = context.CreateActivityProxy<ISharedFunctions>();

            // 期限切れまで 30 日以内の証明書を取得する
            var certificates = await activity.GetExpiringCertificates(context.CurrentUtcDateTime);

            // 更新対象となる証明書がない場合は終わる
            if (certificates.Count == 0)
            {
                log.LogInformation("Certificates are not found");

                return;
            }

            // 証明書の更新を行う
            foreach (var certificate in certificates)
            {
                var dnsNames = certificate.DnsNames;

                log.LogInformation($"{certificate.Id} - {certificate.ExpiresOn}");

                var request = new AddCertificateRequest();
                request.FrontDoor = certificate.FrontDoor;
                request.DnsNames = new string[dnsNames.Count];
                dnsNames.CopyTo(request.DnsNames, 0);

                try
                {
                    // 証明書の更新処理を開始
                    await context.CallSubOrchestratorAsync(nameof(SharedFunctions.IssueCertificate), (request));
                }
                catch (Exception ex)
                {
                    // 失敗した場合はログに詳細を書き出して続きを実行する
                    log.LogError($"Failed sub orchestration with DNS names = {string.Join(",", dnsNames)}");
                    log.LogError(ex.Message);
                }
            }
        }

        [FunctionName(nameof(RenewCertificates_Timer))]
        public static async Task RenewCertificates_Timer(
            [TimerTrigger("0 0 0 * * 1,3,5")] TimerInfo timer,
            [DurableClient] IDurableClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            var instanceId = await starter.StartNewAsync(nameof(RenewCertificates), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}
