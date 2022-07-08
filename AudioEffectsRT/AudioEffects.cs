using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Foundation;
using System.Diagnostics;
using Windows.System.Threading;
using AudioEffectsLib;
using Windows.Storage.Streams;

namespace AudioEffectsRT
{
    public sealed class FFTAudioEffect : IBasicAudioEffect
    {
        public static event EventHandler<object> SpectrumDataReady;

        public static Windows.Media.Playback.MediaPlayer player { get; set; }

        AudioEncodingProperties encProps;
        AudioSpectrum spectrum;

        public FFTAudioEffect()
        {
            spectrum = new AudioSpectrum(48000,32,2);
        }

        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var encodingProps = new List<AudioEncodingProperties>();

                AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 2, 32);
                encodingProps1.Subtype = MediaEncodingSubtypes.Float;
                AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 2, 32);
                encodingProps2.Subtype = MediaEncodingSubtypes.Float;
                //AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 2, 16);
                //encodingProps1.Subtype = MediaEncodingSubtypes.Pcm;
                //AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 2, 16);
                //encodingProps2.Subtype = MediaEncodingSubtypes.Pcm;
                //AudioEncodingProperties encodingProps3 = AudioEncodingProperties.CreateAac(44100, 2, 16);
                //encodingProps3.Subtype = MediaEncodingSubtypes.Pcm;
                //AudioEncodingProperties encodingProps4 = AudioEncodingProperties.CreateAac(48000, 2, 16);
                //encodingProps4.Subtype = MediaEncodingSubtypes.Pcm;

                encodingProps.Add(encodingProps1);
                encodingProps.Add(encodingProps2);
                //encodingProps.Add(encodingProps3);
                //encodingProps.Add(encodingProps4);
                //encodingProps[2] = AudioEncodingProperties.CreateAac(44100, 2, 16);
                //encodingProps[2].Subtype = MediaEncodingSubtypes.Pcm;
                //encodingProps[3] = AudioEncodingProperties.CreateAac(48000, 2, 16);
                //encodingProps[3].Subtype = MediaEncodingSubtypes.Pcm;

                return encodingProps;

            }
        }

        public bool TimeIndependent { get { return false; } }

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            encProps = encodingProperties;
            spectrum.SampleRate = (int)encodingProperties.SampleRate;
            spectrum.BitsPerSample = (int)encodingProperties.BitsPerSample;
            spectrum.Channels = (int)encodingProperties.ChannelCount;

            spectrum.Start();
        }

        string audioWType = "";

        public void SetProperties(IPropertySet configuration)
        {
            spectrum.Attack = (double)configuration["Attack"];
            spectrum.Decay = (double)configuration["Decay"];
            spectrum.Bands = (int)configuration["Bands"];
            spectrum.FreqMax = (double)configuration["FreqMin"];
            spectrum.FreqMax = (double)configuration["FreqMax"];
            spectrum.Sensitivity = (double)configuration["Sensitivity"];
            spectrum.UseFFT = true;
            spectrum.Window = AudioEffectsLib.FastFourierTransform.WFWindow.HammingWindow;
            spectrum.FFTSize = 8192;
            spectrum.FFTBufferSize = 32768;
            spectrum.Channel = 0;
            spectrum.SpectrumDataAvailable += Spectrum_SpectrumDataAvailable;

            audioWType = (string)configuration["AudioType"];

            spectrum.Start();
        }

        private void Spectrum_SpectrumDataAvailable(object sender, AudioVisEventArgs e)
        {
            SpectrumDataReady?.Invoke(sender, e.AudioData);
        }

        TimeSpan lastFrameTime = TimeSpan.Zero;

        unsafe public void ProcessFrame(ProcessAudioFrameContext context)
        {
                
            AudioFrame inputFrame = context.InputFrame;
            AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read);
            IMemoryBufferReference inputReference = inputBuffer.CreateReference();
            
            byte* audioData;
            uint capacity;

            ((IMemoryBufferByteAccess)inputReference).GetBuffer(out audioData, out capacity);

            //Debug.WriteLine("AUDIOFRAME: " + capacity);

            int audioBufferSize = 0;
            if (audioWType == "PCM" || audioWType == "Float" || audioWType == "MP3")
            {
                audioBufferSize = (int)capacity;
            }
            else if (audioWType == "AAC" || audioWType == "OPUS" || audioWType == "Vorbis")
            {
                audioBufferSize = (int)(1024 * encProps.ChannelCount * (encProps.BitsPerSample / 8));
            }
            else
            {
                audioBufferSize = (int)capacity;
            }

            int realbuffersize;
            for (realbuffersize = audioBufferSize-1; audioData[realbuffersize] == 0; realbuffersize--)
            { 

            }

            Debug.WriteLine("Removed " + (audioBufferSize-realbuffersize) + " bytes of zero padding");

            realbuffersize += 1;
            byte[] data = new byte[realbuffersize];

            for (int i = 0; i < realbuffersize; i++)
                {
                    data[i] = audioData[i];
                }

            TimeSpan dtplayer = inputFrame.RelativeTime.Value - player.PlaybackSession.Position;
            //TimeSpan dtlastFrame = inputFrame.RelativeTime.Value - lastFrameTime;
            //lastFrameTime = inputFrame.RelativeTime.Value;
            //Debug.WriteLine("rt: " + dtlastFrame.TotalSeconds);
            //double cap = (dtlastFrame.TotalSeconds * encProps.SampleRate) * encProps.ChannelCount * (encProps.BitsPerSample / 8);
            //lastFrameTime = inputFrame.RelativeTime.Value;
            //Debug.WriteLine("RT:" + inputFrame.RelativeTime.Value.TotalSeconds + "s, Time until play: " + dtplayer.TotalMilliseconds + "ms, Time since last frame: " + dtlastFrame.TotalMilliseconds + "ms");

            ThreadPoolTimer tppt = ThreadPoolTimer.CreateTimer((source) =>
            {
                (spectrum.AudioClient as AudioStreamWaveSource).Write(data, 0, realbuffersize);
            },dtplayer);
            

        }

        public void Close(MediaEffectClosedReason reason)
        {
            // Dispose of effect resources
            spectrum.Stop();
            Debug.WriteLine("Close: " + reason.ToString());
        }

        public void DiscardQueuedFrames()
        {
            // Reset contents of the samples buffer
        }

        public bool UseInputFrameForOutput { get { return true; } }
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public sealed class EchoAudioEffect : IBasicAudioEffect
    // </SnippetImplementIBasicAudioEffect>
    {




        // <SnippetSupportedEncodingProperties>
        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var supportedEncodingProperties = new List<AudioEncodingProperties>();
                //AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 2, 16);
                //encodingProps1.Subtype = "OPUS";
                //AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 2, 16);
                //encodingProps2.Subtype = "OPUS";
                AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 2, 16);
                encodingProps1.Subtype = MediaEncodingSubtypes.Float;
                AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 2, 16);
                encodingProps2.Subtype = MediaEncodingSubtypes.Float;

                supportedEncodingProperties.Add(encodingProps1);
                supportedEncodingProperties.Add(encodingProps2);

                return supportedEncodingProperties;

            }
        }
        // </SnippetSupportedEncodingProperties>

        // <SnippetDeclareEchoBuffer>
        private float[] echoBuffer;
        private int currentActiveSampleIndex;
        private AudioEncodingProperties currentEncodingProperties;
        // </SnippetDeclareEchoBuffer>

        // <SnippetSetEncodingProperties>
        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            currentEncodingProperties = encodingProperties;
            echoBuffer = new float[encodingProperties.SampleRate]; // exactly one second delay
            currentActiveSampleIndex = 0;
        }
        // </SnippetSetEncodingProperties>

        // <SnippetSetProperties>
        IPropertySet configuration;
        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;
        }
        // </SnippetSetProperties>

        // <SnippetMixProperty>
        public float Mix
        {
            get
            {
                object val;
                if (configuration != null && configuration.TryGetValue("Mix", out val))
                {
                    return (float)val;
                }
                return .5f;
            }
        }
        // </SnippetMixProperty>

        // <SnippetProcessFrame>
        unsafe public void ProcessFrame(ProcessAudioFrameContext context)
        {
            AudioFrame inputFrame = context.InputFrame;
            AudioFrame outputFrame = context.OutputFrame;

            using (AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read),
                                outputBuffer = outputFrame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference inputReference = inputBuffer.CreateReference(),
                                            outputReference = outputBuffer.CreateReference())
            {
                byte* inputDataInBytes;
                byte* outputDataInBytes;
                uint inputCapacity;
                uint outputCapacity;

                ((IMemoryBufferByteAccess)inputReference).GetBuffer(out inputDataInBytes, out inputCapacity);
                ((IMemoryBufferByteAccess)outputReference).GetBuffer(out outputDataInBytes, out outputCapacity);

                float* inputDataInFloat = (float*)inputDataInBytes;
                float* outputDataInFloat = (float*)outputDataInBytes;

                float inputData;
                float echoData;

                // Process audio data
                int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                for (int i = 0; i < dataInFloatLength; i++)
                {
                    inputData = inputDataInFloat[i] * (1.0f - this.Mix);
                    echoData = echoBuffer[currentActiveSampleIndex] * this.Mix;
                    outputDataInFloat[i] = inputData + echoData;
                    echoBuffer[currentActiveSampleIndex] = inputDataInFloat[i];
                    currentActiveSampleIndex++;

                    if (currentActiveSampleIndex == echoBuffer.Length)
                    {
                        // Wrap around (after one second of samples)
                        currentActiveSampleIndex = 0;
                    }
                }
            }
        }
        // </SnippetProcessFrame>


        // <SnippetClose>
        public void Close(MediaEffectClosedReason reason)
        {
            // Dispose of effect resources
            echoBuffer = null;
        }
        // </SnippetClose>

        // <SnippetDiscardQueuedFrames>
        public void DiscardQueuedFrames()
        {
            // Reset contents of the samples buffer
            Array.Clear(echoBuffer, 0, echoBuffer.Length - 1);
            currentActiveSampleIndex = 0;
        }
        // </SnippetDiscardQueuedFrames>

        // <SnippetTimeIndependent>
        public bool TimeIndependent { get { return true; } }
        // </SnippetTimeIndependent>

        // <SnippetUseInputFrameForOutput>
        public bool UseInputFrameForOutput { get { return false; } }
        // </SnippetUseInputFrameForOutput>

    }

}
