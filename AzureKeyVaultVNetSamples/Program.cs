using System;
using System.Configuration;
using System.Threading.Tasks;

namespace AzureKeyVaultVNetSamples
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // run vnet samples
            Console.WriteLine("\n\n** Running Key Vault VNet access rule management sample..");
            Task.Run(() => KeyVaultVNetSample.DemonstrateVNetAccessRuleManagementAsync()).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
