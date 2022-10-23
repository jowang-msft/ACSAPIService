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
        static CancellationTokenSource cancel = new CancellationTokenSource();

        static long videoFrameCount = 0;
        static long audioCount = 0;

        static async Task Main(string[] args)
        {
            if (args.Length > 0)
            {
                var pipeName = args[0];
                var token = args[1];
                var callee = args[2];
                Console.WriteLine($"{pipeName} - {token} - {callee}");

                try
                {
                    //string audioFile = string.IsNullOrEmpty(pipeName) ? Guid.NewGuid().ToString() : pipeName;
                    string audioFile = Guid.NewGuid().ToString();
                    using (WaveFileWriter writer = new WaveFileWriter($"C:\\CallStreams\\audio-{audioFile}.wav", new WaveFormat(48000, 16, 2)))
                    {
                        var callClient = new CallClient();

                        var deviceManager = await callClient.GetDeviceManager();

                        var tokenCredential = new CommunicationTokenCredential(token);
                        var callAgentOptions = new CallAgentOptions()
                        {
                            DisplayName = callee
                        };

                        var callAgent = await callClient.CreateCallAgent(tokenCredential, callAgentOptions);
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
                        incomingAudioStream.OnNewAudioBufferAvailable += async (object sender, IncomingAudioEventArgs args) =>
                        {
                            await OnHandleIncomingAudioAsync(sender, args, writer);
                        };

                        var outgoingAudioStream = new RawOutgoingAudioStream(new RawOutgoingAudioProperties(AudioSampleRate.SampleRate_48000, AudioChannelMode.ChannelMode_Stereo, AudioFormat.Pcm_16_Bit, OutgoingAudioMsOfDataPerBlock.Ms_20));
                        outgoingAudioStream.OnAudioStreamReady += async (sender, args) =>
                        {
                            await OnPrepareAndSendOutgoingAudioStreamAsync(sender, args);
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

                        var call = await callAgent.StartCallAsync(
                            new List<ICommunicationIdentifier>() {
                                new CommunicationUserIdentifier(callee)
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

                        await Task.Delay(30000);

                        Console.WriteLine($"Total: {audioCount}, {videoFrameCount}");

                        // BUG: This is to keep a reference to VideoFormats and prevent it from being released.
                        if (startCallOptions.VideoOptions != null)
                        {
                            //var format = rawoutgoingVideoStreamOptions.VideoFormats;
                            //Console.WriteLine(format.ToString());
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
                        callClient.dispose();
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

        private static async Task OnPrepareAndSendOutgoingAudioStreamAsync(object sender, PropertyChangedEventArgs args)
        {
            RawOutgoingAudioStream rawOutgoingAudioStream = sender as RawOutgoingAudioStream;

            if (rawOutgoingAudioStream != null)
            {
                var properties = rawOutgoingAudioStream.RawOutgoingAudioProperties;
                int currentSampleNumber = 0;
                new Thread(async () =>
                {
                    try
                    {
                        DateTime nextDeliverTime = DateTime.Now;
                        OutgoingAudioBuffer buffer;
                        while (true)
                        {
                            nextDeliverTime = nextDeliverTime.AddMilliseconds(20);
                            buffer = new OutgoingAudioBuffer(properties);
                            List<byte> data = new List<byte>();
                            currentSampleNumber = GenerateToneData(currentSampleNumber, 440, data, buffer.DataSize / (int)properties.ChannelMode, (int)properties.ChannelMode, (int)properties.SampleRate);
                            buffer.Data = data;
                            var buffer2 = new OutgoingAudioBuffer(properties) { Data = data };

                            rawOutgoingAudioStream.SendOutgoingAudioBuffer(buffer2);
                            audioCount++;
                            TimeSpan wait = nextDeliverTime - DateTime.Now;
                            if (wait > TimeSpan.Zero)
                            {
                                await Task.Delay(wait);
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
        }

        private static async Task OnHandleIncomingAudioAsync(object sender, IncomingAudioEventArgs args, WaveFileWriter writer)
        {
            if (args?.IncomingAudioBuffer?.Data != null)
            {
                var bytes = args.IncomingAudioBuffer.Data.ToArray();
                writer.WriteData(bytes, 0, bytes.Length);
            }
        }

        private static int GenerateToneData(
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