<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>NAudio</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>NAudio.Utils</Namespace>
  <Namespace>NAudio.Wave</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

//{"token":"eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOmI2YWFkYTFmLTBiMWQtNDdhYy04NjZmLTkxYWFlMDBhMWQwMV8wMDAwMDAxNC05ZGVhLTA5YTQtZWRiZS1hNDNhMGQwMDA4ZDEiLCJzY3AiOjE3OTIsImNzaSI6IjE2NjY0MTQzMDYiLCJleHAiOjE2NjY1MDA3MDYsImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiJiNmFhZGExZi0wYjFkLTQ3YWMtODY2Zi05MWFhZTAwYTFkMDEiLCJyZXNvdXJjZUxvY2F0aW9uIjoidW5pdGVkc3RhdGVzIiwiaWF0IjoxNjY2NDE0MzA2fQ.Bv6KAibBbxzi4VJQYh0paNRplUfUtqRRDG1agqQ2asER1ZHwtogU995Jl0jD0d_k0VRkAs4OC97vK8H0gjhA7i92ODyXMQv7SMrpQAboRnN9CowWTRKZNX-F9vRVWUtJWk_mOnn8-2ASBYd8glhhEoiQzqz6BwpQcsc7rluwax7iFNjvtF7pLT74X8E5f65gATUzR5cyoIDe-sKqSyQA4AczsSkFQdzt_ZAVLB6NIjfKlf90W99TY0a_nd6-h5nd-1_1hjhwxPCXsj8KdZMAWocKHfsVov-03bVRxqBWHfwD3LY6DtelUfX8AKb4Vmklfcx8CX3oPV5LY3av1xiE3Q","expiresOn":"2022-10-23T04:51:46.966Z","user":{"communicationUserId":"8:acs:b6aada1f-0b1d-47ac-866f-91aae00a1d01_00000014-9dea-09a4-edbe-a43a0d0008d1"}}
static string acsToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOmI2YWFkYTFmLTBiMWQtNDdhYy04NjZmLTkxYWFlMDBhMWQwMV8wMDAwMDAxNC05ZGVhLTA5YTQtZWRiZS1hNDNhMGQwMDA4ZDEiLCJzY3AiOjE3OTIsImNzaSI6IjE2NjY0MTQzMDYiLCJleHAiOjE2NjY1MDA3MDYsImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiJiNmFhZGExZi0wYjFkLTQ3YWMtODY2Zi05MWFhZTAwYTFkMDEiLCJyZXNvdXJjZUxvY2F0aW9uIjoidW5pdGVkc3RhdGVzIiwiaWF0IjoxNjY2NDE0MzA2fQ.Bv6KAibBbxzi4VJQYh0paNRplUfUtqRRDG1agqQ2asER1ZHwtogU995Jl0jD0d_k0VRkAs4OC97vK8H0gjhA7i92ODyXMQv7SMrpQAboRnN9CowWTRKZNX-F9vRVWUtJWk_mOnn8-2ASBYd8glhhEoiQzqz6BwpQcsc7rluwax7iFNjvtF7pLT74X8E5f65gATUzR5cyoIDe-sKqSyQA4AczsSkFQdzt_ZAVLB6NIjfKlf90W99TY0a_nd6-h5nd-1_1hjhwxPCXsj8KdZMAWocKHfsVov-03bVRxqBWHfwD3LY6DtelUfX8AKb4Vmklfcx8CX3oPV5LY3av1xiE3Q";
static string callee = "8:echo123";
static string requestUrl = "http://localhost:5000";

async Task Main()
{
	using (HttpClient client = new HttpClient())
	{
		client.DefaultRequestHeaders.Add("ACSTOKEN", acsToken);
		client.Timeout = TimeSpan.FromMinutes(5);
		
		//await CallAsync(client);

		// This is not working -only one live sesson is support per process
		//Task.WaitAll(CallAsync(client), CallAsync(client));
		// This works - calls are serialized.
		//await CallAsync(client);
		//await CallAsync(client);

		await CallByAgentAsync(client);
		
		//await TestMultipleCallsAsync(client, 20);
		//
		//await GetStreamAsync(client);
	}
}

async Task CallAsync(HttpClient client)
{
	string url = $"{requestUrl}/call?callee={callee}";
	HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
}

async Task CallByAgentAsync(HttpClient client)
{
	string url = $"{requestUrl}/callout?callee={callee}";
	HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
}

async Task TestMultipleCallsAsync(HttpClient client, int maxCalls)
{
	var calls = new List<Task>();
	for (int i = 0; i < maxCalls; i++)
	{
		calls.Add(GetStreamAsync(client));
	}

	await Task.WhenAll(calls);
}

async Task GetStreamAsync(HttpClient client)
{
	try
	{
		string url = $"{requestUrl}/call2stream?callee={callee}";

		using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
			using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
			{
				string fileToWriteTo = Path.GetTempFileName();

				var bytes = default(byte[]);
				using (var memstream = new MemoryStream())
				{
					streamToReadFrom.CopyTo(memstream);
					bytes = memstream.ToArray();
					using (WaveFileWriter writer = new WaveFileWriter(fileToWriteTo, new WaveFormat(48000, 16, 2)))
					{
						writer.WriteData(bytes, 0, bytes.Length);
					}
				}
			}
	}
	catch
	{ }
}