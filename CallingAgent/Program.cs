using Azure.Communication.Calling;
using Azure.WinRT.Communication;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using WinRT;

namespace ACSCallAgent
{
    [ComImport, Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMemoryBufferByteAccess
    {
        unsafe void GetBuffer(byte** bytes, uint* capacity);
    }

    internal class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string fileName, IntPtr fileHandle, uint flags);

        static CancellationTokenSource cancel = new CancellationTokenSource();

        static long videoFrameCount = 0;
        static long audioCount = 0;

        static async Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                var pipeName = args[0];
                var token = args[1];
                Console.WriteLine($"{pipeName} - {token}");

                token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOmI2YWFkYTFmLTBiMWQtNDdhYy04NjZmLTkxYWFlMDBhMWQwMV8wMDAwMDAxMy1mMzE2LWZhNTMtYjhiYS1hNDNhMGQwMDhkZDAiLCJzY3AiOjE3OTIsImNzaSI6IjE2NjM1NDgzNDgiLCJleHAiOjE2NjM2MzQ3NDgsImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiJiNmFhZGExZi0wYjFkLTQ3YWMtODY2Zi05MWFhZTAwYTFkMDEiLCJpYXQiOjE2NjM1NDgzNDh9.VNspnFAPMK1lqvY0tIY5FNjxyW6yaJREIRND8gccYSY_S-k5BJML3ibBX8hnVHedjprwHXFzMjeh61wyxbrm13_zX9HWWrK_1xsiWP-uA9c7dQgPcEHHneMIWCUfLqhdSWYmysrROFyOejukMHBS7hs4ajUD3OKxBoCvt0xOAy7of9WfgbOe1OZbRDbnQBg9N2ANLMlMcmoCF0A74dPrYfhFON2la4RrySIk0c__LG-pDzkMnl1ydn7HUk_3kLVIupKQEM-ULBcqNm6R5pb9g0J0Te5StY5YwRtWpekN_CD3iwwrZvZttTaJYl76VL9wsZbeqQAg8UDEbeak9SZDKg";
                //ACS test token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOjk5NmQyMzA1LTkwYTEtNGIyYS05MmZkLWVlZmZmNDIxNWZmMl8wMDAwMDAxMy1lZjI2LTA0YTQtMmM4YS0wODQ4MjIwMDBjMmIiLCJzY3AiOjE3OTIsImNzaSI6IjE2NjM0ODIyMjQiLCJleHAiOjE2NjM1Njg2MjQsImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiI5OTZkMjMwNS05MGExLTRiMmEtOTJmZC1lZWZmZjQyMTVmZjIiLCJpYXQiOjE2NjM0ODIyMjR9.q2qBtLS13W - 4r_NBlhgFzKfYOskiCyHcG5LP0Su - Qmnzl3Y3HeHZu4OItvpLhHwVhVacb5uitiHj1ky44Dk7pnxFAi1szN7jXtgiNkU7HdJi76xN - _9A7a9leo - WiPnpWrlrZnHv5CW - YHRJ5U2dKOjEYRIGruRjCQtiYxkszelsYBFqDhAT9 - VnbiEzT6Zvgg1E0uJMb4OuMmAnUrNqvFHrX7iOyuLVEokZZWNWKwSRC - hEvcGvhZphG_Xj - RA1PLnRKNpKgHnfhqxstlLFgR7mjC19MPyusla1P1oAEsPGOKvXThLunu18D1XXb2yJ_h5UjoWITcMi5JAP7zvp1A";
                //{ "token":"eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNiIsIng1dCI6Im9QMWFxQnlfR3hZU3pSaXhuQ25zdE5PU2p2cyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOmI2YWFkYTFmLTBiMWQtNDdhYy04NjZmLTkxYWFlMDBhMWQwMV8wMDAwMDAxMy1mMzE2LWZhNTMtYjhiYS1hNDNhMGQwMDhkZDAiLCJzY3AiOjE3OTIsImNzaSI6IjE2NjM1NDgzNDgiLCJleHAiOjE2NjM2MzQ3NDgsImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiJiNmFhZGExZi0wYjFkLTQ3YWMtODY2Zi05MWFhZTAwYTFkMDEiLCJpYXQiOjE2NjM1NDgzNDh9.VNspnFAPMK1lqvY0tIY5FNjxyW6yaJREIRND8gccYSY_S-k5BJML3ibBX8hnVHedjprwHXFzMjeh61wyxbrm13_zX9HWWrK_1xsiWP-uA9c7dQgPcEHHneMIWCUfLqhdSWYmysrROFyOejukMHBS7hs4ajUD3OKxBoCvt0xOAy7of9WfgbOe1OZbRDbnQBg9N2ANLMlMcmoCF0A74dPrYfhFON2la4RrySIk0c__LG-pDzkMnl1ydn7HUk_3kLVIupKQEM-ULBcqNm6R5pb9g0J0Te5StY5YwRtWpekN_CD3iwwrZvZttTaJYl76VL9wsZbeqQAg8UDEbeak9SZDKg","expiresOn":"2022-09-20T00:45:48.260Z","user":{ "communicationUserId":"8:acs:b6aada1f-0b1d-47ac-866f-91aae00a1d01_00000013-f316-fa53-b8ba-a43a0d008dd0"} }

                try
                {
                    string audioFile = string.IsNullOrEmpty(pipeName) ? Guid.NewGuid().ToString() : pipeName;
                    using (WaveFileWriter writer = new WaveFileWriter($"C:\\CallStreams\\audio-{audioFile}.wav", new WaveFormat(48000, 16, 2)))
                    {
                        var callClient = new CallClient();

                        var deviceManager = await callClient.GetDeviceManager();

                        var token_credential = new CommunicationTokenCredential(token);
                        var callAgentOptions = new CallAgentOptions()
                        {
                            DisplayName = "Zheng Wang"
                        };

                        Call call = null;
                        var callAgent = await callClient.CreateCallAgent(token_credential, callAgentOptions);

                        callAgent.OnIncomingCall += (object sender, IncomingCall incomingcall) =>
                        {
                            Console.WriteLine("OnIncomingCall");
                        };

                        // HACK: Keep reference to CallsUpdatedEventArgs to prevent a bug that releases callsUpdatedEventsArgs
                        var callsUpdatedEventsArgs = new List<CallsUpdatedEventArgs>();
                        callAgent.OnCallsUpdated += (object sender, CallsUpdatedEventArgs args) =>
                        {
                            callsUpdatedEventsArgs.Add(args);
                        };

                        // Configure audio stream
                        var incomingAudioStream = new RawIncomingAudioStream(new RawIncomingAudioProperties(AudioSampleRate.SampleRate_48000, AudioChannelMode.ChannelMode_Stereo, AudioFormat.Pcm_16_Bit));
                        var outgoingAudioStream = new RawOutgoingAudioStream(new RawOutgoingAudioProperties(AudioSampleRate.SampleRate_48000, AudioChannelMode.ChannelMode_Stereo, AudioFormat.Pcm_16_Bit, OutgoingAudioMsOfDataPerBlock.Ms_20));

                        incomingAudioStream.OnNewAudioBufferAvailable += (object sender, IncomingAudioEventArgs args) =>
                        {
                            OnHandleIncomingAudio(sender, args, writer);
                        };

                        outgoingAudioStream.OnAudioStreamReady += async (sender, args) =>
                        {
                            await OnPrepareAndSendOutgoingAudioStream(sender, args, callClient, callAgent, outgoingAudioStream);
                        };

                        
                        
                        // Configure outgoing video

                        //var localVideoStream = new LocalVideoStream[1];
                        //IReadOnlyList<VideoDeviceInfo> cameras = deviceManager.Cameras;
                        //if (cameras.Count > 0)
                        //{
                        //    VideoDeviceInfo videoDeviceInfo = cameras[0];
                        //    localVideoStream[0] = new LocalVideoStream(videoDeviceInfo);
                        //}

                        var videoFormat = new VideoFormat() { Width = 1280, Height = 720, PixelFormat = PixelFormat.Bgrx, VideoFrameKind = VideoFrameKind.VideoSoftware, FramesPerSecond = 30, Stride1 = 1280 * 4 };
                        var rawoutgoingVideoStreamOptions = new RawOutgoingVideoStreamOptions();
                        rawoutgoingVideoStreamOptions.SetVideoFormats(new VideoFormat[] { videoFormat });
                        rawoutgoingVideoStreamOptions.OnVideoFrameSenderChanged += async (object sender, VideoFrameSenderChangedEventArgs args) =>
                        {
                            await OnPrepareAndSendVideoFrames(sender, args);
                        };
                        var virtualRawoutgoingVideoStream = new VirtualRawOutgoingVideoStream(rawoutgoingVideoStreamOptions);

                        var videoOptions = new VideoOptions(new OutgoingVideoStream[] { virtualRawoutgoingVideoStream /*, localVideoStream[0] */});

                        Console.WriteLine("Calling...");

                        var startCallOptions = new StartCallOptions()
                        {
                            AudioOptions = new AudioOptions()
                            {
                                IncomingAudioStream = incomingAudioStream,
                                OutgoingAudioStream = outgoingAudioStream
                            },
                            VideoOptions = videoOptions
                        };

                        call = await callAgent.StartCallAsync(
                            new List<ICommunicationIdentifier>() {
                                //new CommunicationUserIdentifier("8:echo123"),
                                new CommunicationUserIdentifier("8:acs:b6aada1f-0b1d-47ac-866f-91aae00a1d01_00000013-f316-6cb7-b8ba-a43a0d008dc4")
                            },
                            startCallOptions);

                        call.OnStateChanged += (object sender, PropertyChangedEventArgs args) =>
                        {
                            Call call = sender as Call;
                            if (call != null)
                            {
                                Console.WriteLine(call.State);
                            }
                        };

                        await Task.Delay(60000);

                        Console.WriteLine($"Total: {audioCount}, {videoFrameCount}");

                        // BUG: This is to keep a reference to VideoFormats and prevent it from being released.
                        if (startCallOptions.VideoOptions != null)
                        {
                            var format = rawoutgoingVideoStreamOptions.VideoFormats;
                            Console.WriteLine(format.ToString());
                        }

                        cancel.Cancel();

                        if (call.State != CallState.Disconnected)
                        {
                            try
                            {
                                await call.HangUpAsync(new HangUpOptions());
                            }
                            catch { }
                        }

                        callAgent.dispose();
                        //callAgent = null;
                        callClient.dispose();
                        //callClient = null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private static async Task OnPrepareAndSendVideoFrames(object sender, VideoFrameSenderChangedEventArgs args)
        {
            // TODO: Lock on cancel
            cancel.Cancel();
            cancel = new CancellationTokenSource();
            var videFrameSender = args.VideoFrameSender;
            await GenerateFramesAsync(videFrameSender);
            Console.WriteLine(videFrameSender.VideoFormat.ToString());
        }

        private static async Task OnPrepareAndSendOutgoingAudioStream(object sender, PropertyChangedEventArgs args, CallClient callClient, CallAgent callAgent, RawOutgoingAudioStream outgoingAudioStream)
        {
            RawOutgoingAudioProperties properties = outgoingAudioStream.RawOutgoingAudioProperties;
            int currentSampleNumber = 0;
            OutgoingAudioBuffer buffer;
            new Thread(async () =>
            {
                try
                {
                    DateTime nextDeliverTime = DateTime.Now;
                    while (true)
                    {
                        if ((callAgent != null) && (callClient != null))
                        {
                            nextDeliverTime = nextDeliverTime.AddMilliseconds(20);
                            buffer = new OutgoingAudioBuffer(properties);
                            List<byte> data = new List<byte>();
                            currentSampleNumber = GenerateToneData(currentSampleNumber, 440, data, buffer.DataSize / (int)properties.ChannelMode, (int)properties.ChannelMode, (int)properties.SampleRate);
                            buffer.Data = data;
                            outgoingAudioStream.SendOutgoingAudioBuffer(buffer);
                            audioCount++;
                            TimeSpan wait = nextDeliverTime - DateTime.Now;
                            if (wait > TimeSpan.Zero)
                            {
                                await Task.Delay(wait);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            })
            { IsBackground = true }.Start();
        }

        private static void OnHandleIncomingAudio(object sender, IncomingAudioEventArgs args, WaveFileWriter writer)
        {
            if (args.IncomingAudioBuffer.Data != null)
            {
                var bytes = args.IncomingAudioBuffer.Data.ToArray();
                writer.WriteData(bytes, 0, bytes.Length);
            }
        }

        static int GenerateToneData(
            int currentSampleNumber,
            int frequency,
            List<byte> sampleBuffer,
            int samplesToGenerate,
            int channelCount,
            int samplesPerSecond)
        {
            sampleBuffer.Clear();
            double ReferenceSoundPressure = 20.0;   // dB
            double ReferenceAudioToneLevel = -20.0; // dB

            int AudioMaxLevel = byte.MaxValue;
            double maxLevel =
                (double)AudioMaxLevel * Math.Pow(10.0, ReferenceAudioToneLevel / ReferenceSoundPressure);

            var endSampleNumber = currentSampleNumber + samplesToGenerate;
            for (; currentSampleNumber < endSampleNumber; ++currentSampleNumber)
            {
                for (var channel = 0; channel != channelCount; ++channel)
                {
                    var timeInFractionalSeconds = ((double)(currentSampleNumber)) / samplesPerSecond;
                    double amplitude = Math.Sin(2 * Math.PI * frequency * timeInFractionalSeconds);
                    if (Double.IsNaN(amplitude))
                        amplitude = 0; // casting NaN to int is undefined behavior
                    Random rnd = new Random();
                    var noise = rnd.NextDouble() * 0.2 - 0.1;
                    amplitude = Math.Max(Math.Min(amplitude, 1.0), -1.0) + noise;
                    //assert(-1.0 <= amplitude && amplitude <= 1.0);
                    var sample = (byte)(maxLevel * amplitude);
                    sampleBuffer.Add(sample);
                }
            }
            return currentSampleNumber;
        }

        static async Task GenerateFramesAsync(VideoFrameSender videoFrameSender)
        {
            if (videoFrameSender == null) return;

            var ticks = videoFrameSender.TimestampInTicks;

            Random rand = new Random();

            SoftwareBasedVideoFrameSender videoFrame = videoFrameSender as SoftwareBasedVideoFrameSender;
            int w = videoFrame.VideoFormat.Width;
            int h = videoFrame.VideoFormat.Height;
            int delayBetweenFrames = (int)(1000.0 / videoFrame.VideoFormat.FramesPerSecond);
            uint bufferSize = (uint)(w * h) * 4;

            // HACK: Prevent FrameConfirmation from getting released
            var frameConfirmations = new List<FrameConfirmation>();
            while (!cancel.IsCancellationRequested)
            {
                using (var mb = new MemoryBuffer(bufferSize))
                {
                    using (var mbr = mb.CreateReference())
                    {
                        if (mbr != null)
                        {
                            var mba = mbr.As<IMemoryBufferByteAccess>();
                            if (mba != null)
                            {
                                unsafe
                                {
                                    byte* destBytes = null;
                                    uint destCapacity;
                                    mba.GetBuffer(&destBytes, &destCapacity);
                                    byte r = (byte)rand.Next(1, 255);
                                    byte g = (byte)rand.Next(1, 255);
                                    byte b = (byte)rand.Next(1, 255);
                                    for (int y = 0; y < h; ++y)
                                        for (int x = 0; x < w * 4; x += 4)
                                        {
                                            destBytes[(w * 4 * y) + x] = (byte)(y % b);                 // b
                                            destBytes[(w * 4 * y) + x + 1] = (byte)(y % g);             // g
                                            destBytes[(w * 4 * y) + x + 2] = (byte)(y % r);             // r
                                            destBytes[(w * 4 * y) + x + 3] = 0;
                                        }
                                }

                                var frameConfirmation = await videoFrame.SendFrameAsync(mb, ticks);
                                videoFrameCount++;
                                await Task.Delay(delayBetweenFrames);

                                if (frameConfirmation != null)
                                {
                                    frameConfirmations.Add(frameConfirmation);
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("End of frame generator thread.");
        }
    }
}