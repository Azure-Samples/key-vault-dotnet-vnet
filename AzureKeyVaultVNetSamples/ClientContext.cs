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

        static readonly string VNetProvider = "Microsoft.Network";
        static readonly string VNetType = "virtualNetworks";
        static readonly string SubnetType = "subnets";

        #region construction
        public static ClientContext Build(string tenantId, string vaultMgmtAppId, string vaultMgmtAppSecret, string objectId, string subscriptionId, string vaultResourceGroupName, string location, string vaultName, string vnetSubnetResourceId)
        {
            if (String.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException(nameof(tenantId));
            if (String.IsNullOrWhiteSpace(vaultMgmtAppId)) throw new ArgumentException(nameof(vaultMgmtAppId));
            if (String.IsNullOrWhiteSpace(vaultMgmtAppSecret)) throw new ArgumentException(nameof(vaultMgmtAppSecret));
            if (String.IsNullOrWhiteSpace(objectId)) throw new ArgumentException(nameof(objectId));
            if (String.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentException(nameof(subscriptionId));
            if (String.IsNullOrWhiteSpace(vaultResourceGroupName)) throw new ArgumentException(nameof(vaultResourceGroupName));
            if (String.IsNullOrWhiteSpace(vnetSubnetResourceId)) throw new ArgumentException(nameof(vnetSubnetResourceId));

            var subnetResId = new AzureResourceIdentifier(vnetSubnetResourceId);
            if (!ValidateResourceIsVNetOrSubnet(subnetResId))
                throw new ArgumentException("the specified resource identifier is not a valid virtual network or subnet");

            return new ClientContext
            {
                TenantId = tenantId,
                VaultMgmtApplicationId = vaultMgmtAppId,
                VaultAccessorObjectId = objectId,
                SubscriptionId = subscriptionId,
                VaultResourceGroupName = vaultResourceGroupName,
                PreferredLocation = location ?? "southcentralus",
                VaultName = vaultName ?? "keyvaultsample",
                VNetName = subnetResId.Name,
                SubnetName = subnetResId.ChildName,
                VNetResourceGroupName = subnetResId.ResourceGroup,
                SubnetResourceId = vnetSubnetResourceId
            };
        }
        #endregion

        #region properties
        public string TenantId { get; private set; }

        public string VaultMgmtApplicationId { get; private set; }

        public string VaultAccessorObjectId { get; private set; }

        public string SubscriptionId { get; private set; }

        public string PreferredLocation { get; private set; }

        public string VaultName { get; private set; }

        public string VaultResourceGroupName { get; private set; }

        public string VNetResourceGroupName { get; private set; }

        public string VNetName { get; private set; }

        public string SubnetName { get; private set; }

        public string SubnetResourceId { get; private set; }
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

        private static bool ValidateResourceIsVNetOrSubnet(AzureResourceIdentifier resourceId)
        {
            return resourceId.Provider.Equals(VNetProvider, StringComparison.InvariantCultureIgnoreCase)
                && resourceId.Type.Equals(VNetType, StringComparison.InvariantCultureIgnoreCase)
                && (String.IsNullOrWhiteSpace(resourceId.ChildType)
                    || resourceId.ChildType.Equals(SubnetType, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
