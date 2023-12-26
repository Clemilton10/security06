> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)

# Get Token

```sh
dotnet new console -n token -f net6.0
dotnet sln add token
```

token/token.csproj

```csharp
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<OutputType>Exe</OutputType>
		<TargetFramework>net6.0</TargetFramework>
	</PropertyGroup>
	<ItemGroup>
		<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
	</ItemGroup>
</Project>
```

```sh
cd token
touch IAccessToken.cs
```

token/IAccessToken.cs

```csharp
namespace mytoken
{
	internal class IAccessToken
	{
		public string access_token { get; set; }
		public string expires_in { get; set; }
		public string token_type { get; set; }
		public string scope { get; set; }
	}
}
```

```sh
touch IResponse.cs
```

token/IResponse.cs

```csharp
namespace mytoken
{
	internal class IResponse
	{
		public int Id { get; set; }
		public string UserName { get; set; }
		public string Address { get; set; }
		public string Contact { get; set; }
	}
}
```

```sh
touch IFirstResponse.cs
```

token/IFirstResponse.cs

```csharp
using System.Collections.Generic;

namespace mytoken
{
	internal class IFirstResponse
	{
		public List<IResponse>? data { get; set; }
		public int? code { get; set; }
		public string? message { get; set; }
	}
}
```

token/Program.cs

```csharp
using mytoken;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
	private
	static async Task Main()
	{
		string tokenEndpoint = "https://localhost:7000/connect/token";
		string grantType = "client_credentials";
		string clientId = "client";
		string clientSecret = "secret";
		string scope = "api1";

		string requestBody = $"grant_type={grantType}&scope={scope}&client_id={clientId}&client_secret={clientSecret}";
		using (var httpClient = new HttpClient())
		{
			var content = new StringContent(
				requestBody,
				System.Text.Encoding.UTF8,
				"application/x-www-form-urlencoded"
			);

			var rp = await httpClient.PostAsync(tokenEndpoint, content);

			if (rp.IsSuccessStatusCode)
			{
				var rs = await rp.Content.ReadAsStringAsync();
				if (rs != null)
				{
					var obj = JsonConvert.DeserializeObject<IAccessToken>(rs);
					if (obj != null)
					{
						//Console.WriteLine(obj.access_token);
						httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", obj.access_token);
						tokenEndpoint = "https://localhost:7222/Api/User";
						rp = await httpClient.GetAsync(tokenEndpoint);
						if (rp.IsSuccessStatusCode)
						{
							rs = await rp.Content.ReadAsStringAsync();
							if (rs != null)
							{
								Console.WriteLine(rs);
								var obj2 = JsonConvert.DeserializeObject<IFirstResponse>(rs);
								if (obj2 != null)
								{
									foreach (var obx in obj2.data)
									{
										Console.WriteLine($"UserName: {obx.UserName}");
										Console.WriteLine($"Address: {obx.Address}");
										Console.WriteLine($"Contact: {obx.Contact}");
										Console.WriteLine("");
									}
								}
							}
						}
						else
						{
							Console.WriteLine($"Erro na solicitação: {rp.StatusCode}");
						}
					}
				}
			}
			else
			{
				Console.WriteLine($"Erro na solicitação: {rp.StatusCode}");
			}
		}
	}
}
```

> [Identity Server](./is4.md) | [Biblioteca](./biblioteca.md) | [API](./api.md) | [Get Token](./token.md) | [Client](./client.md)
