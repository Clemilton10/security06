> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)

# Biblioteca de classes

```sh
# Biblioteca de Classes
dotnet new classlib -n DataAccess -f net6.0
dotnet sln add DataAccess
```

DataAccess/DataAccess.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<TargetFramework>net6.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="7.0.0" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="7.0.0">
			<PrivateAssets>all</PrivateAssets>
			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
		</PackageReference>
	</ItemGroup>
</Project>
```

```sh
cd DataAccess
mkdir Data
touch Data/AppDBContext.cs
```

DataAccess/Data/AppDBContext.cs

```csharp
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Data
{
	public class AppDBContext : DbContext
	{
		public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
		{

		}
		public DbSet<User> users { get; set; }
	}
}
```

```sh
mkdir Entities
touch Entities/User.cs
```

DataAccess/Entities/User.cs

```csharp
namespace DataAccess.Entities
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
rm -rf Class1.cs
```

> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)
