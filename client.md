> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)

# Client

```sh
# ASP.NET Core Web App
dotnet new mvc -f net6.0 -n Client
dotnet sln add Client
```

Client/Properties/launchSettings.json

```json
{
	"profiles": {
		"Client": {
			"commandName": "Project",
			"dotnetRunMessages": true,
			"launchBrowser": true,
			"applicationUrl": "https://localhost:7088",
			"environmentVariables": {
				"ASPNETCORE_ENVIRONMENT": "Development"
			}
		}
	}
}
```

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
	<PropertyGroup>
		<TargetFramework>net6.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="IdentityModel" Version="6.0.0" />
		<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="6.0.11" />
		<PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="6.0.10" />
	</ItemGroup>
</Project>
```

```sh
cd Client
mkdir Models
touch Models/ResponseModel.cs
```

Client/Models/ResponseModel.cs

```csharp
namespace Client.Models
{
    public class ResponseModel<T>
    {
        public T Data { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
    }
}
```

```sh
touch Models/User.cs
```

Client/Models/User.cs

```csharp
namespace Client.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Address { get; set; }
        public string Contact { get; set; }
    }
}
```

```sh
mkdir Services
touch Services/IdentitySettings.cs
```

Client/Services/IdentitySettings.cs

```csharp
public class IdentitySettings
{
	public string DiscoveryUrl { get; set; }
	public string ClientName { get; set; }
	public string ClientPassword { get; set; }
	public string UseHttps { get; set; }
}
```

```sh
touch Services/ITokenService.cs
```

Client/Services/ITokenService.cs

```csharp
using IdentityModel.Client;

namespace Client.Services
{
	public interface ITokenService
	{
		Task<TokenResponse> GetToken(string scope);
	}
}
```

```sh
touch Services/TokenService.cs
```

Client/Services/TokenService.cs

```csharp
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace Client.Services
{
	public class TokenService : ITokenService
	{
		private readonly IOptions<IdentitySettings> _identitySettings;
		private readonly DiscoveryDocumentResponse _documentResponse;
		private readonly HttpClient _httpClient;

		public TokenService(IOptions<IdentitySettings> identitySettings)
		{
			_identitySettings = identitySettings;
			_httpClient = new HttpClient();
			_documentResponse = _httpClient.GetDiscoveryDocumentAsync
				 (_identitySettings.Value.DiscoveryUrl).Result;

			if (_documentResponse.IsError)
			{
				throw new Exception("Unable to get discovery document", _documentResponse.Exception);
			}

		}

		public async Task<TokenResponse> GetToken(string scope)
		{
			var tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
			{
				Address = _documentResponse.TokenEndpoint,
				ClientId = _identitySettings.Value.ClientName,
				ClientSecret = _identitySettings.Value.ClientPassword,
				Scope = scope
			});

			if (tokenResponse.IsError)
			{
				throw new Exception("Unable to get token", tokenResponse.Exception);
			}

			return tokenResponse;
		}
	}
}
```

Client/appsettings.json

```json
{
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft.AspNetCore": "Warning"
		}
	},
	"AllowedHosts": "*",
	"apiUrl": "https://localhost:7222",
	"applicationUrl": "https://localhost:7088",
	"IdentitySettings": {
		"DiscoveryUrl": "https://localhost:7000",
		"ClientName": "client",
		"ClientPassword": "secret",
		"UseHttps": true
	},
	"InteractiveServiceSettings": {
		"AuthorityUrl": "https://localhost:7000",
		"ClientId": "mvc",
		"ClientSecret": "secret",
		"Scopes": ["api1"]
	}
}
```

Client/Controllers/HomeController.cs

```csharp
using Client.Models;
using Client.Services;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers
{
	public class HomeController : Controller
	{
		private readonly ITokenService _service;
		private readonly IConfiguration _config;
		private readonly HttpClient httpClient;
		public HomeController(ITokenService service, IConfiguration config)
		{
			httpClient = new HttpClient();
			_service = service;
			_config = config;
		}

		public async Task<IActionResult> Index()
		{
			return View();
		}

		[Authorize]
		public async Task<IActionResult> Privacy()
		{
			var tokenResult = await _service.GetToken("api1");
			httpClient.SetBearerToken(tokenResult.AccessToken);

			var result = await httpClient.GetAsync(_config["apiUrl"] + "/api/User");
			if (result.IsSuccessStatusCode)
			{
				var user = await result.Content.ReadFromJsonAsync<ResponseModel<List<User>>>();
				return View(user.Data);
			}
			List<User> u = new List<User>();

			return View(u);
		}
	}
}
```

```sh
touch Controllers/AccountController.cs
```

Client/Controllers/AccountController.cs

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers
{
	public class AccountController : Controller
	{
		private readonly IConfiguration _config;

		public AccountController(IConfiguration config)
		{
			_config = config;
		}
		public IActionResult Login(string redirectUri)
		{
			if (string.IsNullOrWhiteSpace(redirectUri))
			{
				redirectUri = Url.Content("~/");
			}

			if (HttpContext.User.Identity.IsAuthenticated)
			{
				Response.Redirect(redirectUri);
			}

			return Challenge(new AuthenticationProperties
			{
				RedirectUri = redirectUri,
			},
			OpenIdConnectDefaults.AuthenticationScheme);
		}

		public IActionResult LogOut(string redirectUri)
		{
			return SignOut(new AuthenticationProperties
			{
				RedirectUri = _config["applicationUrl"]
			},
			OpenIdConnectDefaults.AuthenticationScheme,
			CookieAuthenticationDefaults.AuthenticationScheme);
		}

		public IActionResult Register(string redirectUri)
		{
			return RedirectPermanent(_config["InteractiveServiceSettings:AuthorityUrl"] + "/Account/Register");
		}
	}
}
```

Client/Views/Home/Privacy.cshtml

```csharp
@model IEnumerable<Client.Models.User>

@{
	ViewData["Title"] = "Privacy";
}

<h1>Users Table</h1>


<table class="table">
	<thead>
		<tr>
			<th>
				@Html.DisplayNameFor(model => model.Id)
			</th>
			<th>
				@Html.DisplayNameFor(model => model.UserName)
			</th>
			<th>
				@Html.DisplayNameFor(model => model.Address)
			</th>
			<th>
				@Html.DisplayNameFor(model => model.Contact)
			</th>
		</tr>
	</thead>
	<tbody>
		@foreach (var item in Model)
		{
			<tr>
				<td>
					@Html.DisplayFor(modelItem => item.Id)
				</td>
				<td>
					@Html.DisplayFor(modelItem => item.UserName)
				</td>
				<td>
					@Html.DisplayFor(modelItem => item.Address)
				</td>
				<td>
					@Html.DisplayFor(modelItem => item.Contact)
				</td>
			</tr>
		}
	</tbody>
</table>
```

Client/Views/Shared/-Layout.cshtml

```csharp
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>@ViewData["Title"] - Client</title>
	<link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
	<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
	<link rel="stylesheet" href="~/Client.styles.css" asp-append-version="true" />
</head>
<body>
	<header>
		<nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
			<div class="container-fluid">
				<a class="navbar-brand" asp-area="" asp-controller="Home" asp-action="Index">Client</a>
				<button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse" aria-controls="navbarSupportedContent"
						aria-expanded="false" aria-label="Toggle navigation">
					<span class="navbar-toggler-icon"></span>
				</button>
				<div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
					<ul class="navbar-nav flex-grow-1">
						<li class="nav-item">
							<a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Index">Home</a>
						</li>
						<li class="nav-item">
							<a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Privacy">Privacy</a>
						</li>
						@if (User.Identity.IsAuthenticated)
						{
							<li class="nav-item">
								<a class="nav-link text-dark" asp-area="" asp-controller="Account" asp-action="LogOut">LogOut</a>
							</li>
						}
						else
						{
							<li class="nav-item">
								<a class="nav-link text-dark" asp-area="" asp-controller="Account" asp-action="Register">Register</a>
							</li>
						}

					</ul>
				</div>
			</div>
		</nav>
	</header>
	<div class="container">
		<main role="main" class="pb-3">
			@RenderBody()
		</main>
	</div>

	<footer class="border-top footer text-muted">
		<div class="container">
			&copy; 2022 - Client - <a asp-area="" asp-controller="Home" asp-action="Privacy">Privacy</a>
		</div>
	</footer>
	<script src="~/lib/jquery/dist/jquery.min.js"></script>
	<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
	<script src="~/js/site.js" asp-append-version="true"></script>
	@await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Client/Views/Home/Index.cshtml

```csharp
@{
    ViewData["Title"] = "Home";
}
<h1>@ViewData["Title"]</h1>

<p>Use this page to detail your site's privacy policy.</p>
```

> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)
