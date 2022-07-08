using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAudio;
using CoreAudio.Interfaces;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using NAudio.Win8.Wave.WaveOutputs;
using NAudio.Wave;

namespace AudioVisualizerUWP
{
    class AudioCapture
    {
        IAudioClient client;
        IAudioCaptureClient capture;
        //MMDevice device;
        object device;


        EventWaitHandle onDataReady;

        public AudioCapture()
        {

            try
            {
                //MMDeviceEnumerator denum = new MMDeviceEnumerator();
                //device = denum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw e;
            }

            Debug.WriteLine("2");
            client = null;
            Debug.WriteLine("3");
            WAVEFORMATEX wfx;
            Debug.WriteLine("4");
            err(client.GetMixFormat(out wfx));
            Debug.WriteLine("5");
            if (wfx.nChannels == 8)
            {
                Debug.WriteLine("8 channels detected, using 2");
                wfx.nChannels = 2;
                wfx.nBlockAlign = Convert.ToUInt16((2 * wfx.wBitsPerSample) / 8);
                wfx.nAvgBytesPerSec = wfx.nSamplesPerSec * wfx.nBlockAlign;
            }
            Debug.WriteLine("6");
            err(client.Initialize(
                AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, 
                AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_LOOPBACK | AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
                0, 
                0, 
                wfx, 
                Guid.Empty));
            Debug.WriteLine("7");
            onDataReady = new EventWaitHandle(false, EventResetMode.AutoReset);
            IntPtr pHandle = onDataReady.GetSafeWaitHandle().DangerousGetHandle();

            client.SetEventHandle(pHandle);

            object objCapture;
            client.GetService(ref IIDs.IID_IAudioCaptureClient, out objCapture);
            capture = (IAudioCaptureClient)objCapture;
        }

        public void StartCapture()
        {
            client.Start();
            capturing = true;
            Task.Run((Action)Update);
        }

        public void StopCapture()
        {
            capturing = false;
        }

        bool capturing = false;

        //This function runs in a loop to capture the audio and call events
        private void Update()
        {
            uint nFrames, nFramesNext;
            byte[] buffer;
            _AUDCLNT_BUFFERFLAGS flags;

            while (capturing)
            {
                Debug.WriteLine("Were waiting");
                //Wait until data is ready
                onDataReady.WaitOne();

                Debug.WriteLine("ye 1");
                //How much frames to read
                err(capture.GetNextPacketSize(out nFramesNext));
                Debug.WriteLine("ye 2");

                if (nFramesNext != 0)
                {
                    //No frames to read
                    continue;
                }

                Debug.WriteLine("ye 3");
                while (capture.GetBuffer(out buffer, out nFrames, out flags, 0, 0) == AUDCLNT_RETURNFLAGS.S_OK)
                {
                    //nframes to read
                    //Process data
                    Debug.WriteLine("YE we got " + nFrames + " Frames to read!!!");
                }

            }

            client.Stop();
        }

        private void err(AUDCLNT_RETURNFLAGS returnflag)
        {
            if (returnflag != AUDCLNT_RETURNFLAGS.S_OK)
            {
                Debug.WriteLine("Error: " + returnflag.ToString());
                throw new Exception(returnflag.ToString());
            }
        }
    }
}
