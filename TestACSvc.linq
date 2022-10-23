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

static string acsToken = "AUTH_TOKEN";
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