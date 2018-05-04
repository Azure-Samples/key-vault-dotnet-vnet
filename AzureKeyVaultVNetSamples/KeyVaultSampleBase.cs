using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;

namespace AzureKeyVaultVNetSamples
{
    /// <summary>
    /// Base class for KeyVault recovery samples.
    /// </summary>
    public class KeyVaultSampleBase
    {
        /// <summary>
        /// Represents the client context - Azure tenant, subscription, identity etc.
        /// </summary>
        protected ClientContext context;

        /// <summary>
        /// KeyVault management (Control Plane) client instance.
        /// </summary>
        public KeyVaultManagementClient KVManagementClient { get; private set; }

        /// <summary>
        /// KeyVault data (Data Plane) client instance.
        /// </summary>
        public KeyVaultClient DataClient { get; private set; }

        /// <summary>
        /// Networking management (Control Plane) client instance.
        /// </summary>
        public NetworkManagementClient NetworkManagementClient { get; private set; } 

        /// <summary>
        /// Builds a sample object from the specified parameters.
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        /// <param name="appId">AAD application id.</param>
        /// <param name="appSecret">AAD application secret.</param>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <param name="vaultResourceGroupName">Resource group name.</param>
        /// <param name="vaultLocation">Vault location.</param>
        /// <param name="vaultName">Vault name.</param>
        public KeyVaultSampleBase(string tenantId, string appId, string appSecret, string objectId, string subscriptionId, string vaultResourceGroupName, string vaultLocation, string vaultName, string vnetSubnetResourceId)
        {
            InstantiateSample(tenantId, appId, appSecret, objectId, subscriptionId, vaultResourceGroupName, vaultLocation, vaultName, vnetSubnetResourceId);
        }

        /// <summary>
        /// Builds a sample object from configuration.
        /// </summary>
        public KeyVaultSampleBase()
        {
            // retrieve parameters from configuration
            var tenantId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.TenantId];
            var appSecret = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultMgmtAppSecret];
            var appId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultMgmtAppId];
            var subscriptionId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.SubscriptionId];
            var objectId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultAccessorOId];
            var vaultRGName = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.ResourceGroupName];
            var vaultLocation = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultLocation];
            var vaultName = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VaultName];
            var vnetSubnetResourceId = ConfigurationManager.AppSettings[SampleConstants.ConfigKeys.VNetSubnetResourceId];
            
            InstantiateSample(tenantId, appId, appSecret, objectId, subscriptionId, vaultRGName, vaultLocation, vaultName, vnetSubnetResourceId);
        }

        /// <summary>
        /// Instantiates the base sample class, creating the service clients. 
        /// </summary>
        /// <param name="tenantId">Tenant identifier which contains the Service Principal used to run this test.</param>
        /// <param name="appId">Identifier of the AD application used to log into Azure.</param>
        /// <param name="appSecret">Secret of the AD application.</param>
        /// <param name="objectId">Object identifier of the Service Principal corresponding to the AD application used to run this test.</param>
        /// <param name="subscriptionId">Subscription containing the resources.</param>
        /// <param name="vaultResourceGroupName">Vault resource group name.</param>
        /// <param name="vaultLocation">Vault location.</param>
        /// <param name="vaultName">Vault name.</param>
        /// <param name="vnetSubnetResourceId">Resource identifier of the subnet to use for testing; 
        /// all other coordinates are derived from this resource id.</param>
        private void InstantiateSample(string tenantId, string appId, string appSecret, string objectId, string subscriptionId, string vaultResourceGroupName, string vaultLocation, string vaultName, string vnetSubnetResourceId)
        {
            context = ClientContext.Build(tenantId, appId, appSecret, objectId, subscriptionId, vaultResourceGroupName, vaultLocation, vaultName, vnetSubnetResourceId);

            // log in with as the specified service principal for vault management operations
            var serviceCredentials = Task.Run(() => ClientContext.GetServiceCredentialsAsync(tenantId, appId, appSecret)).ConfigureAwait(false).GetAwaiter().GetResult();

            // instantiate the management client
            KVManagementClient = new KeyVaultManagementClient(serviceCredentials);
            KVManagementClient.SubscriptionId = subscriptionId;

            // instantiate the data client
            DataClient = new KeyVaultClient(ClientContext.AcquireAccessTokenAsync);

            // instantiate the network management client
            NetworkManagementClient = new NetworkManagementClient(serviceCredentials);
            NetworkManagementClient.SubscriptionId = context.SubscriptionId;
        }

        #region utilities
        /// <summary>
        /// Creates a vault with the specified parameters and coordinates.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="vaultName"></param>
        /// <param name="vaultLocation"></param>
        /// <param name="enableSoftDelete"></param>
        /// <param name="enablePurgeProtection"></param>
        /// <returns></returns>
        protected VaultCreateOrUpdateParameters CreateVaultParameters(string resourceGroupName, string vaultName, string vaultLocation, bool enableSoftDelete, bool enablePurgeProtection)
        {
            var properties = new VaultProperties
            {
                TenantId = Guid.Parse(context.TenantId),
                Sku = new Sku(),
                AccessPolicies = new List<AccessPolicyEntry>(),
                EnabledForDeployment = false,
                EnabledForDiskEncryption = false,
                EnabledForTemplateDeployment = false,
                EnableSoftDelete = enableSoftDelete ? (bool?)enableSoftDelete : null,
                CreateMode = CreateMode.Default
            };

            // add an access control entry for the test SP
            properties.AccessPolicies.Add(new AccessPolicyEntry
            {
                TenantId = properties.TenantId,
                ObjectId = context.VaultAccessorObjectId,
                Permissions = new Permissions
                {
                    Secrets = new string[] { "get", "set", "list", "delete", "recover", "backup", "restore", "purge" },
                }
            });

            return new VaultCreateOrUpdateParameters(vaultLocation, properties);
        }

        protected VirtualNetworkInner CreateVNetParameters(string vnetName, string vnetAddressSpace, string subnetName, string subnetAddressSpace)
        {
            return new VirtualNetworkInner(
                location: context.PreferredLocation,
                id: null,
                name: vnetName,
                type: null,
                tags: null,
                addressSpace: new AddressSpace(new List<string> { SampleConstants.VNetAddressSpace } ),
                dhcpOptions: null,
                subnets: new List<SubnetInner> { new SubnetInner(subnetName, subnetAddressSpace) },
                virtualNetworkPeerings: null,
                resourceGuid: null,
                provisioningState: null,
                enableDdosProtection: null,
                enableVmProtection: null,
                ddosProtectionPlan: null,
                etag: null);
        }

        protected async Task<Vault> CreateOrRetrieveVaultAsync(string resourceGroupName, string vaultName, bool enableSoftDelete, bool enablePurgeProtection)
        {
            Vault vault = null;

            try
            {
                // check whether the vault exists
                Console.Write("Checking the existence of the vault...");
                vault = await KVManagementClient.Vaults.GetAsync(resourceGroupName, vaultName).ConfigureAwait(false);
                Console.WriteLine("done.");
            }
            catch (Exception e)
            {
                VerifyExpectedException<CloudException>(e, HttpStatusCode.NotFound, (ex) => { return ex.Response.StatusCode; });
            }

            if (vault == null)
            { 
                // create a new vault
                var vaultParameters = CreateVaultParameters(resourceGroupName, vaultName, context.PreferredLocation, enableSoftDelete, enablePurgeProtection);

                try
                {
                    // create new soft-delete-enabled vault
                    Console.Write("Vault does not exist; creating...");
                    vault = await KVManagementClient.Vaults.CreateOrUpdateAsync(resourceGroupName, vaultName, vaultParameters).ConfigureAwait(false);
                    Console.WriteLine("done.");

                    // wait for the DNS record to propagate; verify properties
                    Console.Write("Waiting for DNS propagation..");
                    Thread.Sleep(10 * 1000);
                    Console.WriteLine("done.");

                    Console.Write("Retrieving newly created vault...");
                    vault = await KVManagementClient.Vaults.GetAsync(resourceGroupName, vaultName).ConfigureAwait(false);
                    Console.WriteLine("done.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception encountered updating or retrieving the vault: {0}", e.Message);
                    throw;
                }
            }

            return vault;
        }

        protected async Task<VirtualNetworkInner> CreateOrRetrieveNetworkAsync(string resourceGroupName, string vnetName, string subnetName)
        {
            VirtualNetworkInner vnet = null;

            // attempt to retrieve an existing vnet
            try
            {
                Console.Write("Checking the existence of vnet '{0}' in resource group '{1}'...", vnetName, resourceGroupName);
                var vNetResponse = await NetworkManagementClient.VirtualNetworks.GetWithHttpMessagesAsync(resourceGroupName, vnetName).ConfigureAwait(false);
                Console.WriteLine("done");

                vnet = vNetResponse.Body;
            }
            catch (Exception e)
            {
                VerifyExpectedException<CloudException>(e, HttpStatusCode.NotFound, (ex) => { return ex.Response.StatusCode; });
            }

            // create one if we must
            if (vnet == null)
            {
                try
                {
                    Console.Write("Creating vnet '{0}' in resource group '{1}'...", vnetName, resourceGroupName);
                    var vNetResponse = await NetworkManagementClient.VirtualNetworks.CreateOrUpdateWithHttpMessagesAsync(
                        resourceGroupName, 
                        vnetName, 
                        CreateVNetParameters(vnetName, SampleConstants.VNetAddressSpace, subnetName, SampleConstants.VNetSubnetAddressSpace))
                        .ConfigureAwait(false);
                    Console.WriteLine("done.");

                    vnet = vNetResponse.Body;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception encountered creating the virtual network: {0}", e.Message);
                    throw;
                }
            }

            // attempt to retrieve the specified subnet; create if not existing
            var subnetEntry = new SubnetInner()
            {
                Name = subnetName,
                ServiceEndpoints = new List<ServiceEndpointPropertiesFormat> { new ServiceEndpointPropertiesFormat("Microsoft.KeyVault") },
                AddressPrefix = SampleConstants.VNetSubnetAddressSpace
            };

            bool hasMatchingSubnet = false;
            if (vnet.Subnets.Count > 0)
            {
                // enumerate existing subnets and look for a match.
                // Invoking vnet.Subnets.Contains() will only return true
                // for a complete match of all the properties of the subnet,
                // some of which we can't know beforehand.
                for (var subnetIt = vnet.Subnets.GetEnumerator();
                    subnetIt.MoveNext();
                    )
                {
                    hasMatchingSubnet |= subnetIt.Current.Name.Equals(subnetEntry.Name, StringComparison.InvariantCultureIgnoreCase);
                    if (hasMatchingSubnet)
                        break;
                }
            }

            if (!hasMatchingSubnet)
            {
                try
                {
                    Console.Write("Creating subnet '{0}' in resource group '{1}'...", subnetEntry.Name, resourceGroupName);
                    var subnetResponse = await NetworkManagementClient.Subnets.CreateOrUpdateWithHttpMessagesAsync(
                        resourceGroupName, 
                        vnet.Name, 
                        subnetEntry.Name, 
                        subnetEntry)
                        .ConfigureAwait(false);
                    Console.WriteLine("done.");

                    // retrieve the updated vnet
                    Console.Write("Retrieving the updated vnet '{0}' in resource group '{1}'...", vnetName, resourceGroupName);
                    var vNetResponse = await NetworkManagementClient.VirtualNetworks.GetWithHttpMessagesAsync(resourceGroupName, vnetName).ConfigureAwait(false);
                    Console.WriteLine("done");

                    vnet = vNetResponse.Body;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception encountered updating the virtual network: {0}", e.Message);
                    throw;
                }
            }

            return vnet;
        }

        /// <summary>
        /// Verifies the specified exception is a CloudException, and its status code matches the expected value.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="expectedStatusCode"></param>
        protected static void VerifyExpectedException<TException>(Exception e, HttpStatusCode expectedStatusCode, Func<TException, HttpStatusCode> errorCodeRetriever)
            where TException: RestException
        {
            // verify that the exception is a CloudError one
            var expectedException = e as TException;
            if (expectedException == null)
            {
                Console.WriteLine("Unexpected exception encountered running sample: {0}", e.Message);
                throw e;
            }

            // verify that the exception has the expected status code
            if (errorCodeRetriever(expectedException) != expectedStatusCode)
            {
                Console.WriteLine("Encountered unexpected exception; expected status code: {0}, actual: {1}", errorCodeRetriever(expectedException), expectedStatusCode);
                throw e;
            }
        }

        /// <summary>
        /// Retries the specified function, representing an http request, according to the specified policy.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="functionName"></param>
        /// <param name="initialBackoff"></param>
        /// <param name="numAttempts"></param>
        /// <param name="continueOn"></param>
        /// <param name="retryOn"></param>
        /// <param name="abortOn"></param>
        /// <returns></returns>
        public async static Task<HttpOperationResponse> RetryHttpRequestAsync(
            Func<Task<HttpOperationResponse>> function,
            string functionName,
            int initialBackoff,
            int numAttempts,
            HashSet<HttpStatusCode> continueOn,
            HashSet<HttpStatusCode> retryOn,
            HashSet<HttpStatusCode> abortOn = null)
        {
            HttpOperationResponse response = null;

            for (int idx = 0, backoff = initialBackoff; idx < numAttempts; idx++, backoff <<= 1)
            {
                try
                {
                    response = await function().ConfigureAwait(false);

                    break;
                }
                catch (KeyVaultErrorException kvee)
                {
                    var statusCode = kvee.Response.StatusCode;

                    Console.Write("attempt #{0} to {1} returned: {2};", idx, functionName, statusCode);
                    if (continueOn.Contains(statusCode))
                    {
                        Console.WriteLine("{0} is expected, continuing..", statusCode);
                        break;
                    }
                    else if (retryOn.Contains(statusCode))
                    {
                        Console.WriteLine("{0} is retriable, retrying after {1}s..", statusCode, backoff);
                        Thread.Sleep(TimeSpan.FromSeconds(backoff));

                        continue;
                    }
                    else if (abortOn != null && abortOn.Contains(statusCode))
                    {
                        Console.WriteLine("{0} is designated 'abort', terminating..", statusCode);

                        string message = String.Format("status code {0} is designated as 'abort'; terminating request", statusCode);
                        throw new InvalidOperationException(message);
                    }
                    else
                    {
                        Console.WriteLine("handling of {0} is unspecified; retrying after {1}s..", statusCode, backoff);
                        Thread.Sleep(TimeSpan.FromSeconds(backoff));
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Retries the specified function according to the specified retry policy.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="functionName"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        public static Task<HttpOperationResponse> RetryHttpRequestAsync(
            Func<Task<HttpOperationResponse>> function,
            string functionName,
            RetryPolicy policy)
        {
            if (policy != null)
                return RetryHttpRequestAsync(function, functionName, policy.InitialBackoff, policy.MaxAttempts, policy.ContinueOn, policy.RetryOn, policy.AbortOn);
            else
                return function();
        }
        #endregion
    }
}
