﻿using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;

using Microsoft.Rest;

namespace KeyVault.Acmebot.Internal
{
    internal class ManagedIdentityTokenProvider : ITokenProvider
    {
        public ManagedIdentityTokenProvider(AzureEnvironment environment)
        {
            _environment = environment;

            _tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                AuthorityHost = environment.ActiveDirectory
            });
        }

        private readonly AzureEnvironment _environment;
        private readonly TokenCredential _tokenCredential;

        public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(CancellationToken cancellationToken)
        {
            var context = new TokenRequestContext(new[] { _environment.ResourceManager.ToString() });

            var accessToken = await _tokenCredential.GetTokenAsync(context, cancellationToken);

            return new AuthenticationHeaderValue("Bearer", accessToken.Token);
        }
    }
}
