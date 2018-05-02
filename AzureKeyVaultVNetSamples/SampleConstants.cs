using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Management.Network.Fluent.Models;

namespace AzureKeyVaultVNetSamples
{
    public static class SampleConstants
    {
        /// <summary>
        /// Constants representing the configuration setting keys.
        /// </summary>
        public static class ConfigKeys
        {
            public static readonly string TenantId = "TenantId";
            public static readonly string VaultName = "VaultName";
            public static readonly string VaultLocation = "VaultLocation";
            public static readonly string ResourceGroupName = "ResourceGroupName";
            public static readonly string SubscriptionId = "SubscriptionId";

            // coordinates of the AD application used to access/manage the vault
            public static readonly string VaultMgmtAppId = "ApplicationId";
            public static readonly string VaultMgmtAppSecret = "ApplicationSecret";
            public static readonly string VaultAccessorOId = "ObjectId";

            // coordinates of virtual network
            public static readonly string VNetSubnetResourceId = "VNetSubnetResourceId";
        }

        // VNet constants
        public static readonly string VNetAddressSpace = "10.0.0.0/16";         // sample vnet ip address space
        public static readonly string VNetSubnetAddressSpace = "10.0.0.0/24";   // sample subnet ip address space
        public static readonly string IpV4Address = "192.168.1.1";              // sample ip address

        /// <summary>
        /// Predetermined policies for retrying KeyVault-bound requests.
        /// </summary>
        public static class RetryPolicies
        {
            /// <summary>
            /// status codes for retriable operations.
            /// </summary>
            public static HashSet<HttpStatusCode> SuccessStatusCodes
                = new HashSet<HttpStatusCode>(new List<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent });

            public static HashSet<HttpStatusCode> SoftDeleteRetriableStatusCodes
                = new HashSet<HttpStatusCode>(new List<HttpStatusCode> { HttpStatusCode.Conflict, HttpStatusCode.NotFound });

            public static HashSet<HttpStatusCode> AbortStatusCodes
                = new HashSet<HttpStatusCode>(new List<HttpStatusCode> { HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, HttpStatusCode.Forbidden });


            /// <summary>
            /// Number of seconds to wait after a first, failed attempt to execute a soft-delete-related operation (such as delete, recover, purge).
            /// </summary>
            private static int SoftDeleteInitialBackoff = 15;
            private static int SoftDeleteMaxAttempts = 3;

            /// <summary>
            /// Standard retry policy for soft-delete-related operations which attempt to modify transitioning entities.
            /// </summary>
            public static RetryPolicy DefaultSoftDeleteRetryPolicy = new RetryPolicy(
                SoftDeleteInitialBackoff,
                SoftDeleteMaxAttempts,
                continueOn: SuccessStatusCodes,
                retryOn: SoftDeleteRetriableStatusCodes,
                abortOn: AbortStatusCodes);

            /// <summary>
            /// Standard retry policy for soft-delete-related operations which attempt to consume the outcome of an async operation.
            /// </summary>
            public static RetryPolicy WaitForAsyncDeletionRetryPolicy = new RetryPolicy(
                SoftDeleteInitialBackoff,
                SoftDeleteMaxAttempts,
                continueOn: new HashSet<HttpStatusCode> { HttpStatusCode.NotFound },                                            // keep spinning until entity is marked as deleted
                retryOn: new HashSet<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Conflict },   // retry on success and conflicts
                abortOn: AbortStatusCodes);
        }
    }
}
