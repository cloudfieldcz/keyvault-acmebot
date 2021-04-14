﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ACMESharp.Protocol;

using DurableTask.TypedProxy;

using KeyVault.Acmebot.Models;

using Microsoft.Extensions.Logging;

namespace KeyVault.Acmebot.Functions
{
    public interface ISharedActivity
    {
        Task<IReadOnlyList<CertificateItem>> GetExpiringCertificates(DateTime currentDateTime);

        Task<IReadOnlyList<CertificateItem>> GetAllCertificates(object input = null);

        Task<IReadOnlyList<string>> GetZones(object input = null);

        Task<OrderDetails> Order(IReadOnlyList<string> dnsNames);

        Task Dns01Precondition(IReadOnlyList<string> dnsNames);

        Task<(IReadOnlyList<AcmeChallengeResult>, int)> Dns01Authorization(IReadOnlyList<string> authorizationUrls);

        [RetryOptions("00:00:15", 12, HandlerType = typeof(ExceptionRetryStrategy<RetriableActivityException>))]
        Task CheckDnsChallenge(IReadOnlyList<AcmeChallengeResult> challengeResults);

        Task AnswerChallenges(IReadOnlyList<AcmeChallengeResult> challengeResults);

        [RetryOptions("00:00:15", 12, HandlerType = typeof(ExceptionRetryStrategy<RetriableActivityException>))]
        Task CheckIsReady((OrderDetails, IReadOnlyList<AcmeChallengeResult>) input);

        Task<OrderDetails> FinalizeOrder((string, IReadOnlyList<string>, OrderDetails, string) input);

        [RetryOptions("00:00:10", 12, HandlerType = typeof(ExceptionRetryStrategy<RetriableActivityException>))]
        Task CheckIsValid(OrderDetails orderDetails);

        Task<CertificateItem> MergeCertificate((string, OrderDetails, string, string[]) input);

        Task CleanupDnsChallenge(IReadOnlyList<AcmeChallengeResult> challengeResults);

        Task SendCompletedEvent((string, DateTimeOffset?, IReadOnlyList<string>) input);

        Task<List<AzureFrontDoor>> GetAllFDoors(object input = null);
    }
}
