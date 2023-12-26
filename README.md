> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)

# Identity Server

```sh
mkdir security06
cd security06
dotnet new sln -n security

# Aplicativo Web ASP.NET Core (Razor Pages)
dotnet new webapp -n is4 -f net6.0
dotnet sln add is4
```

is4/is4.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
	<PropertyGroup>
		<TargetFramework>net6.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="IdentityServer4" Version="4.1.2" />
		<PackageReference Include="IdentityServer4.AspNetIdentity" Version="4.1.2" />
		<PackageReference Include="IdentityServer4.EntityFramework" Version="4.1.2" />
		<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="6.0.11" />
		<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="6.0.11" />
		<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="6.0.11" />
		<PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="6.0.11" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="7.0.0" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="7.0.0">
			<PrivateAssets>all</PrivateAssets>
			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
		</PackageReference>
		<PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="6.0.10" />
	</ItemGroup>
</Project>

```

is4/Properties/launchSettings.json

```json
{
	"profiles": {
		"is4": {
			"commandName": "Project",
			"dotnetRunMessages": true,
			"launchBrowser": true,
			"applicationUrl": "https://localhost:7000",
			"environmentVariables": {
				"ASPNETCORE_ENVIRONMENT": "Development"
			}
		}
	}
}
```

```sh
cd is4
mkdir Data
touch Data/AspNetIdentityDbContext.cs
```

is4/Data/AspNetIdentityDbContext.cs

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IndentityServer.Data
{
	public class AspNetIdentityDbContext : IdentityDbContext
	{
		public AspNetIdentityDbContext(DbContextOptions<AspNetIdentityDbContext> options) : base(options) { }
	}
}
```

```sh
mkdir Models
touch Models/RegisterModel.cs
```

is4/Models/RegisterModel.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models
{
	public class RegisterModel
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }
		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm Password")]
		[Compare("Password", ErrorMessage = "Password and confirmation password not match.")]
		public string ConfirmPassword { get; set; }
	}
}
```

```sh
touch Config.cs
```

is4/Config.cs

```csharp
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
```

```sh
rm -rf Pages
```

Clique no link [IdentityServer4.Quickstart.UI](https://github.com/IdentityServer/IdentityServer4.Quickstart.UI), baixe o arquivo zip depois extraia as pastas:

-   wwwroot
-   Quickstart
-   Views
-   getmain.ps1
-   getmain.sh

```sh
# Feche os aplicativos caso dê ocupado
./getmain.sh
```

dentro da pasta do projeto

is4/appsettings.json

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=TestDatabase6;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
	},
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft.AspNetCore": "Warning"
		}
	},
	"AllowedHosts": "*"
}
```

is4/Quickstart/Account/AccountController.cs

```csharp
using IdentityModel;
using IdentityServer.Models;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Quickstart.UI
{
	/// <summary>
	/// Este controlador de amostra implementa um fluxo de trabalho típico de login/logout/provisão para contas locais e externas.
	/// O serviço de login encapsula as interações com o armazenamento de dados do usuário.Este armazenamento de dados é apenas na memória e não pode ser usado para produção!
	/// O Serviço de Interação fornece uma maneira de a interface do usuário se comunicar com o IdentityServer para validação e recuperação de contexto
	/// </summary>
	[SecurityHeaders]
	[AllowAnonymous]
	public class AccountController : Controller
	{
		//private readonly TestUserStore _users;
		private readonly IIdentityServerInteractionService _interaction;
		private readonly IClientStore _clientStore;
		private readonly IAuthenticationSchemeProvider _schemeProvider;
		private readonly IEventService _events;
		private readonly SignInManager<IdentityUser> _signInManager;
		private readonly UserManager<IdentityUser> _userManager;

		public AccountController(
			IIdentityServerInteractionService interaction,
			IClientStore clientStore,
			IAuthenticationSchemeProvider schemeProvider,
			IEventService events,
			SignInManager<IdentityUser> signInManager,
			UserManager<IdentityUser> userManager)
		{
			//_users = users ?? new TestUserStore(TestUsers.Users);

			_interaction = interaction;
			_clientStore = clientStore;
			_schemeProvider = schemeProvider;
			_events = events;
			_signInManager = signInManager;
			_userManager = userManager;
		}



		/// <summary>
		/// Registro para amostra de login de usuário
		/// </summary>
		/// <returns></returns>
		public IActionResult Register()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Register(RegisterModel model)
		{
			if (ModelState.IsValid)
			{
				var user = new IdentityUser
				{
					UserName = model.Email,
					Email = model.Email,
				};

				var result = await _userManager.CreateAsync(user, model.Password);

				if (result.Succeeded)
				{
					ViewBag.Success = "User successfuly added!!";

					return View();
				}

				foreach (var error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}

				ModelState.AddModelError(string.Empty, "Invalid Login Attempt");

			}
			return View(model);
		}

		/// <summary>
		/// Ponto de entrada no fluxo de trabalho de login
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Login(string returnUrl)
		{
			// Construa um modelo para sabermos o que mostrar na página de login
			var vm = await BuildLoginViewModelAsync(returnUrl);

			if (vm.IsExternalLoginOnly)
			{
				// Temos apenas uma opção para fazer login e é um provedor externo
				return RedirectToAction("Challenge", "External", new { scheme = vm.ExternalLoginScheme, returnUrl });
			}

			return View(vm);
		}

		/// <summary>
		/// lidera o postback do nome de usuário/login de senha
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginInputModel model, string button)
		{
			// Verifique se estamos no contexto de uma solicitação de autorização
			var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

			// O usuário clicou no botão "Cancelar"
			if (button != "login")
			{
				if (context != null)
				{
					// Se o usuário cancelar, envie um resultado de volta ao IdentityServer como se eles
					// negou o consentimento (mesmo que esse cliente não exija consentimento).
					// Isso enviará de volta um acesso negou a resposta de erro OIDC ao cliente.
					await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

					// Podemos confiar no modelo.returnurl desde que o getAuthorizationContextasync retornou
					if (context.IsNativeClient())
					{
						// o cliente é nativo, então essa mudança em como
						// retorna a resposta é para melhor UX para o usuário final.
						return this.LoadingPage("Redirect", model.ReturnUrl);
					}

					return Redirect(model.ReturnUrl);
				}
				else
				{
					// Como não temos um contexto válido, então voltamos para a página inicial
					return Redirect("~/");
				}
			}

			if (ModelState.IsValid)
			{
				var user = await _signInManager.UserManager.FindByNameAsync(model.Username);

				if (user is not null)
				{

					var userLogin = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);

					// Validar nome de usuário/senha contra a loja na memória
					if (userLogin == Microsoft.AspNetCore.Identity.SignInResult.Success)
					{
						await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));

						// Defina apenas a expiração explícita aqui se o usuário escolher "Lembre -se de mim".
						// Caso contrário, contamos com a expiração configurada no middleware de cookie.
						AuthenticationProperties props = null;
						if (AccountOptions.AllowRememberLogin && model.RememberLogin)
						{
							props = new AuthenticationProperties
							{
								IsPersistent = true,
								ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
							};
						};

						// emita um cookie de autenticação com ID de assunto e nome de usuário
						var isuser = new IdentityServerUser(user.Id)
						{
							DisplayName = user.UserName
						};

						await HttpContext.SignInAsync(isuser, props);

						if (context != null)
						{
							if (context.IsNativeClient())
							{
								// o cliente é nativo, então essa mudança em como
								// retorna a resposta é para melhor UX para o usuário final.
								return this.LoadingPage("Redirect", model.ReturnUrl);
							}

							// Podemos confiar no modelo.returnurl desde que getAuthorizationContextasync retornou não-nulo
							return Redirect(model.ReturnUrl);
						}

						// Solicitação de uma página local
						if (Url.IsLocalUrl(model.ReturnUrl))
						{
							return Redirect(model.ReturnUrl);
						}
						else if (string.IsNullOrEmpty(model.ReturnUrl))
						{
							return Redirect("~/");
						}
						else
						{
							// O usuário pode ter clicado em um link malicioso - deve ser registrado
							throw new Exception("invalid return URL");
						}
					}
				}


				await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
				ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
			}

			// algo deu errado, mostre forma com erro
			var vm = await BuildLoginViewModelAsync(model);
			return View(vm);
		}


		/// <summary>
		/// Mostrar página de logout
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Logout(string logoutId)
		{
			// Construa um modelo para que a página de logout saiba o que exibir
			var vm = await BuildLogoutViewModelAsync(logoutId);

			if (vm.ShowLogoutPrompt == false)
			{
				// Se a solicitação de logout foi autenticada adequadamente do IdentityServer, então
				// Não precisamos mostrar o prompt e podemos apenas registrar o usuário diretamente.
				return await Logout(vm);
			}

			return View(vm);
		}

		/// <summary>
		/// Lidar com o postback da página de logout
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout(LogoutInputModel model)
		{
			// Construa um modelo para que a página registrada saiba o que exibir
			var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

			if (User?.Identity.IsAuthenticated == true)
			{
				await _signInManager.SignOutAsync(); //identidade de saída

				// Excluir cookie de autenticação local
				await HttpContext.SignOutAsync();

				// Aumente o evento de logout
				await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
			}

			// Verifique se precisamos acionar a saída em um provedor de identidade a montante
			if (vm.TriggerExternalSignout)
			{
				// Construa um URL de retorno para que o provedor a montante redirecione
				// para nós depois que o usuário foi registrado.Isso nos permite então
				// Preencha nosso processamento único de assinatura.
				string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

				// Isso desencadeia um redirecionamento para o provedor externo para sair
				return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
			}

			return View("LoggedOut", vm);
		}

		[HttpGet]
		public IActionResult AccessDenied()
		{
			return View();
		}


		/*****************************************/
		/* APIs auxiliares para o ContaController */
		/*****************************************/
		private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
		{
			var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
			if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
			{
				var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

				// Isso pretende fazer o curto -circuito a interface do usuário e apenas acionar o One Externo IDP
				var vm = new LoginViewModel
				{
					EnableLocalLogin = local,
					ReturnUrl = returnUrl,
					Username = context?.LoginHint,
				};

				if (!local)
				{
					vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
				}

				return vm;
			}

			var schemes = await _schemeProvider.GetAllSchemesAsync();

			var providers = schemes
				.Where(x => x.DisplayName != null)
				.Select(x => new ExternalProvider
				{
					DisplayName = x.DisplayName ?? x.Name,
					AuthenticationScheme = x.Name
				}).ToList();

			var allowLocal = true;
			if (context?.Client.ClientId != null)
			{
				var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
				if (client != null)
				{
					allowLocal = client.EnableLocalLogin;

					if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
					{
						providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
					}
				}
			}

			return new LoginViewModel
			{
				AllowRememberLogin = AccountOptions.AllowRememberLogin,
				EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
				ReturnUrl = returnUrl,
				Username = context?.LoginHint,
				ExternalProviders = providers.ToArray()
			};
		}

		private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
		{
			var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
			vm.Username = model.Username;
			vm.RememberLogin = model.RememberLogin;
			return vm;
		}

		private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
		{
			var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

			if (User?.Identity.IsAuthenticated != true)
			{
				// Se o usuário não for autenticado, basta mostrar a página logada
				vm.ShowLogoutPrompt = false;
				return vm;
			}

			var context = await _interaction.GetLogoutContextAsync(logoutId);
			if (context?.ShowSignoutPrompt == false)
			{
				// é seguro sair automaticamente
				vm.ShowLogoutPrompt = false;
				return vm;
			}

			// Mostra o prompt de logout.Isso evita ataques onde o usuário
			// é assinado automaticamente por outra página da Web maliciosa.
			return vm;
		}

		private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
		{
			// Obtenha informações de contexto (nome do cliente, pós -logout Redirecionar URI e IFRAME para federada FEDERATE)
			var logout = await _interaction.GetLogoutContextAsync(logoutId);

			var vm = new LoggedOutViewModel
			{
				AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
				PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
				ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
				SignOutIframeUrl = logout?.SignOutIFrameUrl,
				LogoutId = logoutId
			};

			if (User?.Identity.IsAuthenticated == true)
			{
				var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
				if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
				{
					var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
					if (providerSupportsSignout)
					{
						if (vm.LogoutId == null)
						{
							// Se não houver contexto de logout atual, precisamos criar um
							// Isso captura as informações necessárias do usuário logado atual
							// Antes de se inscrever e redirecionar para o IDP externo para fazer a inscrição
							vm.LogoutId = await _interaction.CreateLogoutContextAsync();
						}

						vm.ExternalAuthenticationScheme = idp;
					}
				}
			}

			return vm;
		}
	}
}
```

```sh
touch Views/Account/Register.cshtml
```

is4/Views/Account/Register.cshtml

```csharp
@model IdentityServer.Models.RegisterModel

@{
	ViewData["Title"] = "Register";
}

<h1>Register</h1>

<div class="row">
	<div class="col-md-12">
		<div class="text-success">
			<p>@ViewBag.Success</p>
		</div>
		<form method="post">
			<div asp-validation-summary="All" class="text-danger"></div>
			<div class="form-group">
				<label asp-for="Email"></label>
				<input asp-for="Email" class="form-control" />
				<span asp-validation-for="Email" class="text-danger"></span>
			</div>

			<div class="form-group">
				<label asp-for="Password"></label>
				<input asp-for="Password" class="form-control" />
				<span asp-validation-for="Password" class="text-danger"></span>
			</div>

			<div class="form-group">
				<label asp-for="ConfirmPassword"></label>
				<input asp-for="ConfirmPassword" class="form-control" />
				<span asp-validation-for="ConfirmPassword" class="text-danger"></span>
			</div>
			<button type="submit" class="btn-primary">Register</button>
		</form>

	</div>
</div>

@section Scripts {
	@{
		await Html.RenderPartialAsync("_ValidationScriptsPartial");
	}
}
```

```sh
touch Views/Shared/_ValidationScriptsPartial.cshtml
```

```csharp
<environment names="Development">
	<script src="~/lib/jquery-validation/dist/jquery.validate.js"></script>
	<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js"></script>
</environment>
<environment names="Staging,Production">
	<script src="https://ajax.aspnetcdn.com/ajax/jquery.validate/1.17.0/jquery.validate.min.js"
			asp-fallback-src="~/lib/jquery-validation/dist/jquery.validate.min.js"
			asp-fallback-test="window.jQuery && window.jQuery.validator"
			crossorigin="anonymous"
			integrity="sha384-rZfj/ogBloos6wzLGpPkkOr/gpkBNLZ6b6yLy4o+ok+t/SAKlL5mvXLr0OXNi1Hp">
	</script>
	<script src="https://ajax.aspnetcdn.com/ajax/jquery.validation.unobtrusive/3.2.9/jquery.validate.unobtrusive.min.js"
			asp-fallback-src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"
			asp-fallback-test="window.jQuery && window.jQuery.validator && window.jQuery.validator.unobtrusive"
			crossorigin="anonymous"
			integrity="sha384-ifv0TYDWxBHzvAk2Z0n8R434FL1Rlv/Av18DXE43N/1rvHyOG4izKst0f2iSLdds">
	</script>
</environment>
```

is4/Program.cs

```csharp
using IdentityServer;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IndentityServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//Essa linha de código recupera o nome da assembléia em que o programa atual está em execução.GetName () `Obtém o nome da Assembléia.
var assembly = typeof(Program).Assembly.GetName().Name;
var defaultConnString = builder.Configuration.GetConnectionString("DefaultConnection");

// Este código está adicionando um DBContext chamado "aspnetIdentityDbContext" à coleção de serviços em um objeto Builder.O DBContext está configurado para usar um banco de dados SQL Server com a string de conexão especificada por "DefaultConnString".O código também especifica que as migrações para este DBContext estarão localizadas na montagem especificada por "Assembléia".
builder.Services.AddDbContext<AspNetIdentityDbContext>(options =>
	options.UseSqlServer(defaultConnString,
		 b => b.MigrationsAssembly(assembly)));

// Este código está configurando o Serviço de Identidade em um aplicativo ASP.NET Core. O método `AddIFentity` é usado para adicionar o serviço de identidade à coleção de serviços do aplicativo.São necessários dois parâmetros de tipo: `identityUser` e` identityRole`.Esses tipos representam as entidades do usuário e da função usadas pelo sistema de autenticação e autorização do aplicativo.
// O método `addentityframeworkstores` é usado para configurar o serviço de identidade para usar a estrutura da entidade como armazenamento de dados para informações de usuário e função.É necessário um parâmetro de tipo `aspnetIdentityDbContext`, que representa a classe de contexto do banco de dados usada para interagir com o banco de dados subjacente.
// Ao chamar esses métodos e passar dos parâmetros de tipo apropriado, o serviço de identidade é configurado para usar as entidades de usuário e função especificadas e armazenar os dados no contexto do banco de dados especificado da estrutura da entidade.
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
	.AddEntityFrameworkStores<AspNetIdentityDbContext>();


// Este snippet de código está configurando o IdentityServer em um aplicativo .NET Core.
// O método `addIDentityServer ()` adiciona os serviços de identidade do servidor à coleção de serviços do aplicativo.
// O método `ADDASPNETIDENTIDADE <DentityUser> ()` configura o IdentityServer para usar a identidade ASP.NET para gerenciamento de usuários.A classe `identityUser` é a classe de usuário padrão fornecida pelo ASP.NET Identity.
// O método `addConfigurationStore ()` configura o IdentityServer para usar um banco de dados para armazenar dados de configuração, como clientes, recursos e escopos.A propriedade `options.configuredbcontext` é usada para configurar o contexto do banco de dados e a string de conexão.Neste exemplo, ele está usando o SQL Server como provedor de banco de dados e a sequência de conexão é especificada pela variável `defaultConnString`.O `opt => opt.MigransAssEmbly (Assembly)` Parte especifica o conjunto onde as migrações do banco de dados estão localizadas.
// O método `addOperationalStore ()` configura o IdentityServer para usar um banco de dados para armazenar dados operacionais como tokens, consentimentos e códigos de dispositivo.Ele usa uma configuração semelhante ao método `addConfigurationStore ()`.
// O método `addDevelonderSigningCredential ()` adiciona uma credencial de assinatura de desenvolvedor ao identityServer.Isso é usado para assinar tokens durante o desenvolvimento.Em um ambiente de produção, uma credencial de assinatura mais segura deve ser usada.
// No geral, este código configura o IdentityServer para usar a identidade do ASP.NET para gerenciamento de usuários e servidor SQL para armazenar dados de configuração e operacional.Ele também adiciona uma credencial de assinatura de desenvolvedor para assinatura de token durante o desenvolvimento.
builder.Services.AddIdentityServer()
	.AddAspNetIdentity<IdentityUser>()
	.AddConfigurationStore(options =>
	{
		options.ConfigureDbContext = b => b.UseSqlServer(defaultConnString, opt => opt.MigrationsAssembly(assembly));
	})
	.AddOperationalStore(options =>
	{
		options.ConfigureDbContext = b => b.UseSqlServer(defaultConnString, opt => opt.MigrationsAssembly(assembly));
	})
	.AddDeveloperSigningCredential();

// O código `builder.services.addcontrollerswithViewS ()` é usado para configurar os serviços em um aplicativo ASP.NET Core para suportar controladores e visualizações.
// No núcleo do ASP.NET, os serviços são registrados no contêiner de injeção de dependência, que permite que eles sejam facilmente acessados e usados em todo o aplicativo.O método `addControllersWithViewS ()` é um método de extensão que adiciona os serviços necessários para controladores e visualizações à coleta de serviços.
// Ao ligar para `builder.services.addcontrollerswithViews ()`, o aplicativo está configurado para suportar controladores, responsáveis por lidar com solicitações HTTP de entrada e retornar respostas apropriadas.Ele também configura o aplicativo para suportar visualizações, que são usadas para gerar respostas HTML a serem enviadas de volta ao cliente.
// Este método também registra outros serviços relacionados, como ligação ao modelo, validação e renderização de visualização, necessários para o funcionamento adequado de controladores e vistas.
// No geral, `builder.services.addcontrollerswithViewS ()` é uma etapa importante na configuração de um aplicativo ASP.NET Core para lidar com solicitações HTTP e gerar respostas HTML usando controladores e visualizações.
builder.Services.AddControllersWithViews();



var app = builder.Build();

#region Initialized Database
using (var serviceScope = app.Services.GetService<IServiceScopeFactory>().CreateScope())
{
	// Esta linha de código está usando o método `getRequiredService` da propriedade` ServiceProvider` do objeto `ServiceCope` para recuperar uma instância do` PersistGrantDbContext` Service.o `PersistGrantDbContext 'é obtido, a propriedade' Database`é acessado para realizar uma migração.O método `migrate` é chamado na propriedade` banco de dados` para aplicar qualquer migração pendente ao banco de dados associado ao `persistgrantDbContext`.
	serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

	// Esta linha de código está recuperando uma instância da classe 'ConfigurationDbContext` do provedor de serviços no `ServiceCope`.The' ServicesCope` é um objeto que representa um escopo para resolver e gerenciar dependências.É normalmente usado no contexto de injeção de dependência para controlar a vida útil dos objetos e gerenciar suas dependências.
	// O método `getRequiredService <t>` é um método genérico que recupera um serviço do tipo `t` do provedor de serviços.Nesse caso, está recuperando uma instância da classe `ConfigurationDbContext`.
	// Depois que a instância `ConfigurationDbContext` for recuperada, ela pode ser usada para interagir com os dados de configuração no aplicativo, como recuperar ou atualizar as definições de configuração.
	var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

	// O código `context.database.migrate ();` é usado no Entity Framework para aplicar qualquer migração de banco de dados pendente no banco de dados.
	// Ao usar a estrutura da entidade, os desenvolvedores podem criar e modificar esquemas de banco de dados usando migrações de código-primeiro.Essas migrações são representadas como classes que herdam da classe `Migration` e contêm instruções para criar ou modificar tabelas de banco de dados, colunas e outros objetos.
	// O método `migrate ()` é chamado em uma instância da classe `database`, que representa o banco de dados associado à classe` dbContext`.Este método verifica se existem migrações pendentes que não foram aplicadas ao banco de dados e as aplica.
	// chamando `context.database.migrate ();`, o aplicativo garante que o esquema do banco de dados esteja atualizado com as mais recentes migrações definidas no código.Isso geralmente é feito durante a inicialização do aplicativo ou como parte de um processo de implantação para garantir que o banco de dados esteja sincronizado com o código do aplicativo.
	context.Database.Migrate();

	if (!context.Clients.Any())
	{
		foreach (var client in Config.GetClients())
		{
			// Esta linha de código está adicionando um objeto cliente à coleção de clientes no contexto.O objeto cliente está sendo convertido em um objeto de entidade usando o método Toentity () antes de ser adicionado à coleção.
			context.Clients.Add(client.ToEntity());
		}
		context.SaveChanges();
	}

	if (!context.IdentityResources.Any())
	{
		foreach (var resource in Config.GetIdentityResources())
		{
			context.IdentityResources.Add(resource.ToEntity());
		}
		context.SaveChanges();
	}

	if (!context.ApiScopes.Any())
	{
		foreach (var resource in Config.ApiScopes.ToList())
		{
			context.ApiScopes.Add(resource.ToEntity());
		}

		context.SaveChanges();
	}

	if (!context.ApiResources.Any())
	{
		foreach (var resource in Config.GetApis())
		{
			context.ApiResources.Add(resource.ToEntity());
		}
		context.SaveChanges();
	}
}
#endregion

app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
	// O método `mapDefaultControlleroute ()` é usado para configurar a rota padrão para os controladores em um aplicativo ASP.NET Core.métodos.Isso significa que, se uma solicitação for feita para o aplicativo sem especificar uma rota específica, a rota padrão será usada para determinar qual controlador e ação devem lidar com a solicitação.
	// A rota padrão é normalmente definida como "{controlador}/{action}/{id?}", Onde "{controlador}" é substituído pelo nome do controlador, "{Action}" é substituído pelo nome do nome do nome doO método de ação e "{id?}" é um parâmetro opcional que pode ser usado para passar dados adicionais ao método de ação.
	// Por exemplo, se a rota padrão estiver definida como "Home/Index", uma solicitação para a URL raiz do aplicativo será tratada pelo método de ação "índice" no controlador "Homecontroller".
	// Observe que o método `mapDefaultControllerRoute ()` deve ser chamado no método `configure ()` da classe `startup`, normalmente no método` configureServices () ``.
	endpoints.MapDefaultControllerRoute();
});
app.Run();
```

```powershell
add-migration InitialAspNetIdentityMigration -c AspNetIdentityDbContext
```

```sh
dotnet ef migrations add InitialAspNetIdentityMigration -c AspNetIdentityDbContext
```

```powershell
database-update -c AspNetIdentityDbContext
```

```sh
dotnet ef database update -c AspNetIdentityDbContext
```

```powershell
add-migration InitialIdentityServerConfigurationDbMigration -c ConfigurationDbContext
```

```sh
dotnet ef migrations add InitialIdentityServerConfigurationDbMigration -c ConfigurationDbContext
```

```powershell
add-migration InitialIdentityServerPersistedGrantDbMigration -c PersistedGrantDbContext
```

```sh
dotnet ef migrations add InitialIdentityServerPersistedGrantDbMigration -c PersistedGrantDbContext
```

```powershell
database-update -c ConfigurationDbContext
```

```sh
dotnet ef database update -c ConfigurationDbContext
```

```powershell
database-update -c PersistedGrantDbContext
```

```sh
dotnet ef database update -c PersistedGrantDbContext
```

[https://localhost:7000/.well-known/openid-configuration](https://localhost:7000/.well-known/openid-configuration)

> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)
