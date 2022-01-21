using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace ConsoleApp1;

public class AzureAuth
{
	private static readonly DefaultAzureCredential defaultCredential = new();

	public static Microsoft.Azure.Management.Fluent.Azure.IAuthenticated Authenticate()
	{
		var managementToken = defaultCredential
			.GetToken(new TokenRequestContext(new[] {"https://management.azure.com/.default"}))
			.Token;

		var defaultTokenCredentials = new Microsoft.Rest.TokenCredentials(managementToken);
		var azureCredentials = new Microsoft.Azure.Management.ResourceManager.Fluent.Authentication.AzureCredentials(
			defaultTokenCredentials,
			defaultTokenCredentials,
			null,
			AzureEnvironment.AzureGlobalCloud);

		return Microsoft.Azure.Management.Fluent.Azure.Configure()
			.WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
			.Authenticate(azureCredentials);
	}
}
