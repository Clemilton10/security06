using IdentityServer4;
using IdentityServer4.Models;

namespace IdentityServer
{
	public static class Config
	{
		public static IEnumerable<IdentityResource> GetIdentityResources()
		{
			return new List<IdentityResource>
			{
				new IdentityResources.OpenId(),
				new IdentityResources.Profile(),
			};
		}

		public static IEnumerable<ApiScope> ApiScopes =>
			new[] { new ApiScope("api1"), };


		public static IEnumerable<ApiResource> GetApis()
		{
			return new List<ApiResource>
			{
				new ApiResource("api1", "My API")
			};
		}

		public static IEnumerable<Client> GetClients()
		{
			return new List<Client>
			{
				new Client
				{
					ClientId = "client",

                    // Sem usuário interativo, use o cliente/secreto para autenticação
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    // segredo para autenticação
                    ClientSecrets =
					{
						new Secret("secret".Sha256())
					},

                    // escopos a que o cliente tem acesso a
                    AllowedScopes = { "api1" }
				},

                // Cliente de concessão de senha do proprietário do recurso
                new Client
				{
					ClientId = "ro.client",
					AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

					ClientSecrets =
					{
						new Secret("secret".Sha256())
					},
					AllowedScopes = { "api1" }
				},

                // Cliente de fluxo híbrido OpenID Connect (MVC)
                new Client
				{
					ClientId = "mvc",
					ClientName = "MVC Client",
					AllowedGrantTypes = GrantTypes.Code,

					ClientSecrets =
					{
						new Secret("secret".Sha256())
					},

					RedirectUris           = { "https://localhost:7088/signin-oidc" },
					PostLogoutRedirectUris = { "https://localhost:7088/signout-callback-oidc" },

					AllowedScopes =
					{
						IdentityServerConstants.StandardScopes.OpenId,
						IdentityServerConstants.StandardScopes.Profile,
						"api1"
					},

					AllowOfflineAccess = true,
					RequirePkce = true,
				}
			};
		}
	}
}