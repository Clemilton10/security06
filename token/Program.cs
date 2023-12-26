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