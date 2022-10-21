using Azure.Communication.Calling;
using Azure.WinRT.Communication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace CallApiService.Controllers
{

    [ApiController]
    public class TestController : ControllerBase
    {
        static CallClient s_callClient;

        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        private async Task<CallAgent> InitCallAgentAsync(string token)
        {
            if (s_callClient == null)
            {
                s_callClient = new CallClient();
            }

            var token_credential = new CommunicationTokenCredential(token);
            var callAgentOptions = new CallAgentOptions()
            {
                DisplayName = "Zheng Wang"
            };

            var callAgent = await s_callClient.CreateCallAgent(token_credential, callAgentOptions);

            return callAgent;
        }

        [HttpGet]
        [Route("/")]
        public async Task<CallResults> CallAsync(string token)
        {
            var results = new CallResults()
            {
                Date = DateTime.Now,
                HosterArchitecture = (IntPtr.Size == 4) ? "32-bit" : "64-bit",
                HostingProcess = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                WorkingFolder = System.IO.Directory.GetCurrentDirectory(),
                Journal = new List<string>()
            };

            results.Journal.Add($"{System.Environment.OSVersion.Version.Major}." +
                $"{System.Environment.OSVersion.Version.Minor}." +
                $"{System.Environment.OSVersion.Version.Build}");

            try
            {
                var callAgent = await InitCallAgentAsync(token);

                results.Journal.Add("Calling");

                var incomingAudioStream = new RawIncomingAudioStream(new RawIncomingAudioProperties(AudioSampleRate.SampleRate_48000, AudioChannelMode.ChannelMode_Stereo, AudioFormat.Pcm_16_Bit));
                var outgoingAudioStream = new RawOutgoingAudioStream(new RawOutgoingAudioProperties(AudioSampleRate.SampleRate_48000, AudioChannelMode.ChannelMode_Stereo, AudioFormat.Pcm_16_Bit, OutgoingAudioMsOfDataPerBlock.Ms_20));

                var call = await callAgent.StartCallAsync(
                    new ICommunicationIdentifier[1]
                    {
                        new CommunicationUserIdentifier("8:echo123")
                    },
                    new StartCallOptions()
                    {
                        AudioOptions = new AudioOptions() { 
                            IncomingAudioStream = incomingAudioStream,
                            OutgoingAudioStream = outgoingAudioStream
                        }
                    });

                call.OnStateChanged += (sender, args) =>
                {
                    results.Journal.Add(call.State.ToString());
                };

                await Task.Delay(30000);

                await call.HangUpAsync(new HangUpOptions());


                results.Journal.Add("Exiting RESTful call");

                callAgent.dispose();
            }
            catch (Exception ex)
            {
                results.Journal.Add(ex.ToString());
            }

            return results;
        }

        [HttpGet]
        [Route("streaming")]
        public async Task CallAndStreamAsync(string token)
        {
            var outputStream = this.Response.Body;

            try
            {
                var callAgent = await InitCallAgentAsync(token);

                var incomingAudioStream = new RawIncomingAudioStream(new RawIncomingAudioProperties(AudioSampleRate.SampleRate_48000, AudioChannelMode.ChannelMode_Stereo, AudioFormat.Pcm_16_Bit));
                var outgoingAudioStream = new RawOutgoingAudioStream(new RawOutgoingAudioProperties(AudioSampleRate.SampleRate_48000, AudioChannelMode.ChannelMode_Stereo, AudioFormat.Pcm_16_Bit, OutgoingAudioMsOfDataPerBlock.Ms_20));

                var call = await callAgent.StartCallAsync(
                    new ICommunicationIdentifier[1]
                    {
                        new CommunicationUserIdentifier("8:echo123")
                    },
                    new StartCallOptions()
                    {
                        AudioOptions = new AudioOptions()
                        {
                            IncomingAudioStream = incomingAudioStream,
                            OutgoingAudioStream = outgoingAudioStream
                        }
                    });

                call.OnStateChanged += (sender, args) =>
                {
                };

                incomingAudioStream.OnNewAudioBufferAvailable += (object sender, IncomingAudioEventArgs args) =>
                {
                    if (args.IncomingAudioBuffer.Data != null)
                    {
                        var bytes = args.IncomingAudioBuffer.Data.ToArray();
                        outputStream.WriteAsync(bytes, 0, bytes.Length).Wait();
                    }
                };

                outgoingAudioStream.OnAudioStreamReady += (sender, args) =>
                {
                };

                await Task.Delay(20000);

                await call.HangUpAsync(new HangUpOptions());

                callAgent.dispose();
            }
            catch (Exception ex)
            {
            }

            await outputStream.FlushAsync();
        }

        [HttpGet]
        [Route("streaming2")]
        public async Task CallAndStreamOutProcAsync(string token)
        {
            var outputStream = this.Response.Body;

            string pipeName = Guid.NewGuid().ToString();

            //var domain = AppDomain.CreateDomain(pipeName);
            //var loader = (ACSCallAgentDLL.ACSCallingAgent)domain.CreateInstanceAndUnwrap(typeof(ACSCallAgentDLL.ACSCallingAgent).Assembly.FullName, typeof(ACSCallAgentDLL.ACSCallingAgent).FullName);

            //await loader.DoWorkAsync(pipeName, token);

            //AppDomain.Unload(domain);

            using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1))
            {
                Process pipeClient = new Process();
                pipeClient.StartInfo.FileName = "ACSCallAgent.exe";
                pipeClient.StartInfo.Arguments = $"{pipeName} {token}";
                pipeClient.StartInfo.UseShellExecute = false;
                pipeClient.Start();

                //pipeServer.WaitForConnection();

                //var bytes = default(byte[]);
                //using (var memstream = new MemoryStream())
                //{
                //    pipeServer.CopyTo(memstream);
                //    bytes = memstream.ToArray();
                //    outputStream.WriteAsync(bytes, 0, bytes.Length).Wait();
                //}
            }

            //await outputStream.FlushAsync();
        }
    }
}
