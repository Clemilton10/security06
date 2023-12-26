> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)

# Api

```sh
# ASP.NET Core Web API
dotnet new webapi -f net6.0 -n API
dotnet sln add API
```

📄 API/API.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
	<PropertyGroup>
		<TargetFramework>net6.0</TargetFramework>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
		<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.0" />
		<PackageReference Include="IdentityServer4.AccessTokenValidation" Version="3.0.1" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="7.0.2" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="7.0.2">
			<PrivateAssets>all</PrivateAssets>
			<IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
		</PackageReference>
	</ItemGroup>
	<ItemGroup>
	  <ProjectReference Include="..\DataAccess\DataAccess.csproj" />
	</ItemGroup>
</Project>
```

📄 API/Properties/launchSettings.json

```json
{
	"profiles": {
		"API": {
			"commandName": "Project",
			"dotnetRunMessages": true,
			"launchBrowser": true,
			"launchUrl": "swagger",
			"applicationUrl": "https://localhost:7222",
			"environmentVariables": {
				"ASPNETCORE_ENVIRONMENT": "Development"
			}
		}
	}
}
```

📄 API/appsettings.json

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

```sh
cd API
dotnet add reference ../DataAccess/DataAccess.csproj
```

Isso gerará o seguinte xml no API/API.csproj

```xml
<ItemGroup>
	<ProjectReference Include="..\DataAccess\DataAccess.csproj" />
</ItemGroup>
```

📄 API/Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDBContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

> Obs.: Extraia o Migrations para criar a base

```sh
cd ../DataAccess
# Se for no Console do gerenciador de pacotes não se esqueça de selecionar o DataAccess
# PM> add-migration InnitialDbContextMigration
# caso contrário
dotnet ef migrations add InnitialDbContext --startup-project ../API
# PM> update-database
dotnet ef database update --startup-project ../API

# Listar as migrations
dotnet ef migrations list --startup-project ../API

# executar
dotnet ef database update 20221125022849_InnitialDbContextMigration --startup-project ../API
dotnet ef database update 20221125032152_AddUser --startup-project ../API
```

📄 API/Program.cs

```csharp
builder.Services.AddAuthentication("Bearer")
	.AddJwtBearer("Bearer", options =>
	{
		options.Authority = "https://localhost:7000";
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateAudience = false
		};
	});
```

```sh
cd ../API
mkdir Model
```

```sh
touch Model/ResponseModel.cs
```

📄 API/Model/ResponseModel.cs

```csharp
public class ResponseModel<T>
{
	public T Data { get; set; }
	public int Code { get; set; }
	public string Message { get; set; }
}
```

```sh
touch Model/UserModel.cs
```

📄 API/Model/UserModel.cs

```csharp
namespace API.Models
{
	public class UserModel
	{
		public int Id { get; set; }
		public string UserName { get; set; }
		public string Address { get; set; }
		public string Contact { get; set; }
	}
}
```

```sh
mkdir Service
```

```sh
touch Service/IUserService.cs
```

📄 API/Service/IUserService.cs

```csharp
using API.Models;

namespace API.Services
{
	public interface IUserService
	{
		Task<ResponseModel<List<UserModel>>> GetUsers();
	}
}
```

```sh
touch Service/UserService.cs
```

📄 API/Service/UserService.cs

```csharp
using API.Models;
using AutoMapper;
using DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
	public class UserService : IUserService
	{
		private readonly AppDBContext _dBContext;
		private readonly IMapper _mapper;

		public UserService(AppDBContext dBContext, IMapper mapper)
		{
			_dBContext = dBContext;
			_mapper = mapper;
		}

		public async Task<ResponseModel<List<UserModel>>> GetUsers()
		{
			ResponseModel<List<UserModel>> response = new ResponseModel<List<UserModel>>();
			response.Data = _mapper.Map<List<UserModel>>(await _dBContext.users.ToListAsync());
			response.Code = 200;
			return response;
		}
	}
}
```

📄 API/Program.cs

```csharp
builder.Services.AddScoped<IUserService, UserService>();
```

```sh
mkdir Mapper
```

```sh
touch Mapper/MappingProfile.cs
```

📄 API/Mapper/MappingProfile.cs

```csharp
using API.Models;
using AutoMapper;
using DataAccess.Entities;

namespace API.Mapper
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<UserModel, User>().ReverseMap();
		}
	}
}
```

📄 API/Program.cs

```csharp
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
```

```sh
touch Controllers/UserController.cs
```

📄 API/Controllers/UserController.cs

```csharp
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class UserController : ControllerBase
	{
		private readonly IUserService _service;

		public UserController(IUserService service)
		{
			_service = service;
		}

		[HttpGet]
		public async Task<IActionResult> GetUsers()
		{
			var user = await _service.GetUsers();
			return Ok(user);
		}
	}
}
```

```sh
rm -rf Controllers/WeatherForecastController.cs
rm -rf WeatherForecast.cs
```

📄 API/Program.cs

```csharp
using API.Services;
using DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
//using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Adicione serviços ao contêiner.
builder.Services.AddControllers();

builder.Services.AddAuthentication("Bearer")
	.AddJwtBearer("Bearer", options =>
	{
		options.Authority = "https://localhost:7000";
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateAudience = false
		};
	});

builder.Services.AddDbContext<AppDBContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)

# Swagger Authentication

API/Program.cs

```csharp
builder.Services.AddSwaggerGen(option =>
{
	option.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
	option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Please enter a valid token",
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		BearerFormat = "JWT",
		Scheme = "Bearer"
	});
	option.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type=ReferenceType.SecurityScheme,
					Id="Bearer"
				}
			},
			new string[]{}
		}
	});
});
```

Ficando assim o Program.cs

```csharp
using API.Services;
using DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
//using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Adicione serviços ao contêiner.
builder.Services.AddControllers();

builder.Services.AddAuthentication("Bearer")
	.AddJwtBearer("Bearer", options =>
	{
		options.Authority = "https://localhost:7000";
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateAudience = false
		};
	});

builder.Services.AddDbContext<AppDBContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Saiba mais sobre como configurar swagger/openapi em https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
	option.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
	option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Please enter a valid token",
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		BearerFormat = "JWT",
		Scheme = "Bearer"
	});
	option.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type=ReferenceType.SecurityScheme,
					Id="Bearer"
				}
			},
			new string[]{}
		}
	});
});

var app = builder.Build();

// Configure o pipeline de solicitação HTTP.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)
