---
page_type: sample
languages:
- csharp
products:
- azure
description: "This repo contains sample code demonstrating the management of access policies for Azure key vault, restricting access to clients from specific virtual networks, using the Azure .NET SDK."
urlFragment: net-sdk-sample-management
---

# .NET SDK sample illustrating virtual network-based management of access policies for Azure key vaults.  

This repo contains sample code demonstrating the management of access policies for Azure key vault, restricting access to clients from specific virtual networks, using the [Azure .Net SDK](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/key-vault?view=azure-dotnet). The scenarios covered by these samples include:

* Setting up, retrieving and deleting vnet access rules


## Getting Started

### Prerequisites

- OS: Windows
- SDKs:
    - KeyVault data SDK: Microsoft.Azure.KeyVault ver. 3.0.0+
- Azure:
    - an active Azure subscription, in which you have the Key Vault Contributor role
	- an Azure key vault
    - an Azure Active Directory application, created in the tenant associated with the subscription, and with access to KeyVault; please see [Accessing Key Vault from a native application](https://blogs.technet.microsoft.com/kv/2016/09/17/accessing-key-vault-from-a-native-application) for details.
    - the credentials of the AAD application, in the form of a client secret
    - an Azure Virtual Network, with a subnet configured to allow Azure Key Vault as a service endpoint; please see [Azure Virtual Network - Service Endpoint overview](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-service-endpoints-overview) for more details.
  
### Installation

- open the solution in Visual Studio - NuGet should resolve the necessary packages

### Quickstart
Follow these steps to get started with this sample:

1. git clone https://github.com/Azure-Samples/key-vault-dotnet-vnet.git
2. cd key-vault-dotnet-vnet
4. edit the app.config file, specifying the tenant, subscription, AD app id and secret, and storage account and its resource id
5. dotnet run --project AzureKeyVaultVNetSamples\AzureKeyVaultVNetSamples.csproj

## Demo

A demo app is included to show how to use the project.

To run the demo, follow these steps:

(Add steps to start up the demo)

1.
2.
3.

## Resources

- Link to supporting information
- [Virtual Network Management .Net sample](https://github.com/Azure-Samples/network-dotnet-manage-virtual-network)
- ...
