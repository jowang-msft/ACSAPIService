<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <NuGetReference>NAudio</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>NAudio.Wave</Namespace>
  <Namespace>NAudio.Utils</Namespace>
</Query>

static string acsToken = "ACS_TOKEN";
static string requestUrl = "http://localhost:5000";

async Task Main()
{
	using (HttpClient client = new HttpClient())
	{
		client.Timeout = TimeSpan.FromMinutes(5);
		
		await TestMultipleCallsAsync(client, 20);
		
		await GetStreamAsync(client);
	}
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

async Task CallByAgentAsync(HttpClient client)
{
	string url = $"{requestUrl}/callout?token={acsToken}";
	HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
}

async Task GetStreamAsync(HttpClient client)
{
	try
	{
		string url = $"{requestUrl}/call2stream?token={acsToken}";

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