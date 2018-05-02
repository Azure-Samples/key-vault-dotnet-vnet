using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.KeyVault.Models;

namespace AzureKeyVaultVNetSamples
{
    public sealed class KeyVaultVNetSample : KeyVaultSampleBase
    {
        /// <summary>
        /// Sample demonstrating the management of VNet access rules for a key vault.
        /// </summary>
        /// <returns>Task representing the asynchronous execution of this method.</returns>
        internal static async Task DemonstrateNetworkAccessRuleManagementAsync()
        {
            // This sample illustrates the management of virtual network-based access
            // for Azure Key Vaults. The sample performs the following workflow:
            //
            // - attempts to retrieve a known vault; if not existing, one will be created
            // - attempts to retrieve an existing virtual network
            // - verifies the presence of a specified subnet, with KV configured as a service endpoint
            // - adds a vnet access rule 
            // - adds an ip (v4) access rule
            // - list and retrieve network access rules
            // - enable network access rule enforcement
            // - undo network access rule updates
            //
            var sample = new KeyVaultVNetSample();
            var vaultRGName = sample.context.VaultResourceGroupName;
            var vaultName = sample.context.VaultName;
            var vnetName = sample.context.VNetName;
            var subnetName = sample.context.SubnetName;
            var subnetResId = sample.context.SubnetResourceId;
            var vnetRGName = sample.context.VNetResourceGroupName;

            // get or create the specified virtual network
            var vNet = await sample.CreateOrRetrieveNetworkAsync(vnetRGName, vnetName, subnetName)
                .ConfigureAwait(false);

            // get or create the specified vault
            var vault = await sample.CreateOrRetrieveVaultAsync(vaultRGName, vaultName, enableSoftDelete: false, enablePurgeProtection: false)
                .ConfigureAwait(false);

            //
            // 1. Add a vnet access rule
            //
            // examine the vault's access policy, looking for network access rules.
            var hasMatchingVnetAccessRule = VaultHasMatchingVnetAccessRule(vault, subnetResId);

            // add a vnet access rule, if one does not exist
            if (!hasMatchingVnetAccessRule)
            { 
                if (vault.Properties != null
                    && vault.Properties.NetworkAcls == null)
                {
                    // the vault does not contain any network access rules
                    vault.Properties.NetworkAcls = new NetworkRuleSet(
                        bypass: "AzureServices",        // do not enforce access for Azure services
                        defaultAction: "Allow",         // allow access
                        ipRules: new List<IPRule>(), virtualNetworkRules: new List<VirtualNetworkRule>());
                }

                // add the vnet rule
                vault.Properties.NetworkAcls.VirtualNetworkRules.Add(new VirtualNetworkRule(subnetResId));
            }

            //
            // 2. Add an IP access rule
            //
            var hasMatchingIpAccessRule = VaultHasMatchingIPAccessRule(vault, SampleConstants.IpV4Address);
            if (!hasMatchingIpAccessRule)
            {
                if (vault.Properties.NetworkAcls.IpRules == null)
                    vault.Properties.NetworkAcls.IpRules = new List<IPRule>();
                vault.Properties.NetworkAcls.IpRules.Add(new IPRule(SampleConstants.IpV4Address));
            }

            // 
            // 3. Update the vault, if necessary
            //
            if (!hasMatchingIpAccessRule
                || !hasMatchingVnetAccessRule)
            {
                try
                {
                    Console.Write("Updating network access policy on vault '{0}' in resource group '{1}'...");
                    var vaultUpdateResponse = await sample.KVManagementClient.Vaults.CreateOrUpdateWithHttpMessagesAsync(
                        vaultRGName,
                        vaultName,
                        new VaultCreateOrUpdateParameters(vault.Location, vault.Properties))
                        .ConfigureAwait(false);
                    Console.WriteLine("done.");

                    vault = vaultUpdateResponse.Body;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception encountered updating the network access policy on the vault: {0}", e.Message);
                    throw;
                }
            }

            // enumerate the vnet rules on the vault
            Console.WriteLine("Enumerating vnet access rules on vault '{0}' in resource group '{1}'...", vaultName, vaultRGName);
            foreach (var vnetRule in vault.Properties.NetworkAcls.VirtualNetworkRules)
            {
                Console.WriteLine("\t* {0}", vnetRule.Id);
            }

            Console.WriteLine("Enumerating ip access rules on vault '{0}' in resource group '{1}'...", vaultName, vaultRGName);
            foreach (var ipRule in vault.Properties.NetworkAcls.IpRules)
            {
                Console.WriteLine("\t* {0}", ipRule.Value);
            }

            //
            // 4. Enabling network access rule enforcement.
            //
            if (vault.Properties.NetworkAcls.DefaultAction.Equals("Allow", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    Console.Write("Updating vault '{0}' in resource group '{1}' to enable network ACL enforcement...");
                    vault.Properties.NetworkAcls.DefaultAction = "Deny";
                    var vaultUpdateResponse = await sample.KVManagementClient.Vaults.CreateOrUpdateWithHttpMessagesAsync(
                        vaultRGName,
                        vaultName,
                        new VaultCreateOrUpdateParameters(vault.Location, vault.Properties))
                        .ConfigureAwait(false);
                    Console.WriteLine("done.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception encountered updating the network access policy on the vault: {0}", e.Message);
                    throw;
                }
            }

            //
            // 5. Verify this client can't access the vault's content.
            //
            try
            {
                Console.WriteLine("attempting to access the vault's content after enabling network ACL enforcement.");
                Console.WriteLine("this request should fail unless the client matches either of the network access rules currently on the vault.");

                var secretsResponse = await sample.DataClient.GetSecretsWithHttpMessagesAsync(vault.Properties.VaultUri)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                VerifyExpectedException<KeyVaultErrorException>(e, HttpStatusCode.Forbidden, (ex) => { return ex.Response.StatusCode; });
                Console.WriteLine("caught expected exception: {0}", e.Message);
            }

            // 
            // 6. Disable network ACL enforcement and remove the vnet access rules.
            //
            try
            {
                Console.Write("Disabling network access rule enforcement, and removing rules from vault '{0}' in resource group '{1}'...", vaultName, vaultRGName);
                vault.Properties.NetworkAcls.DefaultAction = "Allow";
                vault.Properties.NetworkAcls.IpRules.Clear();
                vault.Properties.NetworkAcls.VirtualNetworkRules.Clear();
                var vaultUpdateResponse = await sample.KVManagementClient.Vaults.CreateOrUpdateWithHttpMessagesAsync(
                    vaultRGName,
                    vaultName,
                    new VaultCreateOrUpdateParameters(vault.Location, vault.Properties))
                    .ConfigureAwait(false);
                Console.WriteLine("done.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception encountered updating the network access policy on the vault: {0}", e.Message);
                throw;
            }

            //
            // 7. Verify this client can now access the vault's content.
            //
            try
            {
                Console.WriteLine("attempting to access the vault's content after disabling network ACL enforcement; this request should pass.");
                var secretsResponse = await sample.DataClient.GetSecretsWithHttpMessagesAsync(vault.Properties.VaultUri)
                    .ConfigureAwait(false);

                Console.WriteLine("verified access was restored.");
            }
            catch (Exception e)
            {
                VerifyExpectedException<KeyVaultErrorException>(e, HttpStatusCode.Forbidden, (ex) => { return ex.Response.StatusCode; });
            }
        }

        private static bool VaultHasMatchingVnetAccessRule(Vault vault, string subnetResourceId)
        {
            if (vault.Properties == null
                || vault.Properties.NetworkAcls == null)
            {
                return false;
            }

            // the vault contains network access rules; look for a matching one
            bool hasMatchingVnetAccessRule = false;
            foreach (var vnetRule in vault.Properties.NetworkAcls.VirtualNetworkRules)
            {
                hasMatchingVnetAccessRule |= vnetRule.Id.Equals(subnetResourceId, StringComparison.InvariantCultureIgnoreCase);
                if (hasMatchingVnetAccessRule)
                    break;
            }

            return hasMatchingVnetAccessRule;
        }

        private static bool VaultHasMatchingIPAccessRule(Vault vault, string ipAddress)
        {
            if (vault.Properties.NetworkAcls == null)
            {
                return false;
            }

            // the vault contains network access rules; look for a matching one
            bool hasMatchingIpAccessRule = false;
            foreach (var ipRule in vault.Properties.NetworkAcls.IpRules)
            {
                hasMatchingIpAccessRule |= ipRule.Value.Equals(ipAddress);
                if (hasMatchingIpAccessRule)
                    break;
            }

            return hasMatchingIpAccessRule;
        }
    }
}
