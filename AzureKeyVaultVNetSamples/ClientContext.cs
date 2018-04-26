using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;

namespace AzureKeyVaultVNetSamples
{
    /// <summary>
    /// Represents the Azure context of the client running the samples - tenant, subscription, client id and credentials.
    /// </summary>
    public sealed class ClientContext
    {
        private static ClientCredential _servicePrincipalCredential = null;

        #region construction
        public static ClientContext Build(string tenantId, string vaultMgmtAppId, string vaultMgmtAppSecret, string subscriptionId, string resourceGroupName, string location, string vaultName)
        {
            if (String.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(vaultMgmtAppId)) throw new ArgumentException(nameof(vaultMgmtAppId));
            if (String.IsNullOrWhiteSpace(vaultMgmtAppSecret)) throw new ArgumentException(nameof(vaultMgmtAppSecret));
            if (String.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentException(nameof(subscriptionId));
            if (String.IsNullOrWhiteSpace(resourceGroupName)) throw new ArgumentException(nameof(resourceGroupName));

            return new ClientContext
            {
                TenantId = tenantId,
                VaultMgmtApplicationId = vaultMgmtAppId,
                SubscriptionId = subscriptionId,
                ResourceGroupName = resourceGroupName,
                PreferredLocation = location ?? "southcentralus",
                VaultName = vaultName ?? "keyvaultsample",
            };
        }
        #endregion

        #region properties
        public string TenantId { get; set; }

        public string VaultMgmtApplicationId { get; set; }

        public string SubscriptionId { get; set; }

        public string PreferredLocation { get; set; }

        public string VaultName { get; set; }

        public string ResourceGroupName { get; set; }
        #endregion

        #region authentication helpers
        /// <summary>
        /// Returns a task representing the attempt to log in to Azure public as the specified
        /// service principal, with the specified credential.
        /// </summary>
        /// <param name="certificateThumbprint"></param>
        /// <returns></returns>
        public static Task<ServiceClientCredentials> GetServiceCredentialsAsync(string tenantId, string applicationId, string appSecret)
        {
            if (_servicePrincipalCredential == null)
            {
                _servicePrincipalCredential = new ClientCredential(applicationId, appSecret);
            }

            return ApplicationTokenProvider.LoginSilentAsync(
                tenantId,
                _servicePrincipalCredential,
                ActiveDirectoryServiceSettings.Azure,
                TokenCache.DefaultShared);
        }

        public static async Task<string> AcquireAccessTokenAsync(string authority, string resource, string scope)
        {
            if (_servicePrincipalCredential == null)
            {
                // read directly from config
                var appId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultMgmtAppId];
                var spSecret = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultMgmtAppSecret];

                _servicePrincipalCredential = new ClientCredential(appId, spSecret);
            }

            AuthenticationContext ctx = new AuthenticationContext(authority, false, TokenCache.DefaultShared);
            AuthenticationResult result = await ctx.AcquireTokenAsync(resource, _servicePrincipalCredential).ConfigureAwait(false);

            return result.AccessToken;
        }
        #endregion
    }
}
