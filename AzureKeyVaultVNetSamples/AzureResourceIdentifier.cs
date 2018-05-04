using System;
using System.Collections.Generic;
using System.Text;

namespace AzureKeyVaultVNetSamples
{
    /// <summary>
    /// Sample representation of a resource identifier.
    /// </summary>
    /// <remarks>
    /// Note this sample assumes at most one subresource.
    /// </remarks>
    internal sealed class AzureResourceIdentifier
    {
        private const string SubscriptionKey = "subscriptions";
        private const string ResourceGroupKey = "resourceGroups";
        private const string ProviderKey = "providers";

        public AzureResourceIdentifier(string resourceId)
        {
            if (String.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentNullException(nameof(resourceId));

            const int maxTokens = 10;
            const int minTokens = 8;

            var tokens = resourceId.Split(new char[] { '/' }, maxTokens, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < minTokens)
                throw new ArgumentException("the specified resource identifier is not valid");

            // example:
            //
            // /subscriptions/33f39d49-6173-49bf-9789-db5548ee6d73/resourceGroups/kvsdkpsh-samples/providers/Microsoft.Network/virtualNetworks/kvpshsdksample/subnets/subnet-0
            //
            if (SubscriptionKey.Equals(tokens[0], StringComparison.InvariantCultureIgnoreCase))
                Subscription = tokens[1];
            else
                throw new ArgumentException("the specified resource identifier is not valid");

            if (ResourceGroupKey.Equals(tokens[2], StringComparison.InvariantCultureIgnoreCase))
                ResourceGroup = tokens[3];
            else
                throw new ArgumentException("the specified resource identifier is not valid");

            if (ProviderKey.Equals(tokens[4], StringComparison.InvariantCultureIgnoreCase))
                Provider = tokens[5];
            else
                throw new ArgumentException("the specified resource identifier is not valid");

            Type = tokens[6];
            Name = tokens[7];

            if (tokens.Length > minTokens)
            {
                if (tokens.Length < maxTokens)
                    throw new ArgumentException("the specified resource identifier is not valid");

                ChildType = tokens[8];
                ChildName = tokens[9];
            }
        }

        public string Subscription { get; private set; }

        public string ResourceGroup { get; private set; }

        public string Provider { get; private set; }

        public string Type { get; private set; }

        public string Name { get; private set; }

        public string ChildType { get; private set; }

        public string ChildName { get; private set; }
    }
}
