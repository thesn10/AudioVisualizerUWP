using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Numerics;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Media.Audio;
using Windows.Storage;
using Windows.Media.Effects;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Shapes;
using NAudio.Wave;
using Windows.Storage.Pickers;
using AudioEffectsLib;
using AudioEffectsRT;

namespace AudioVisualizerUWP
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        List<Rectangle> lstBands;
        MediaPlayer player;

        public MainPage()
        {
            this.InitializeComponent();
           
            
            polywidth = polyline.Width;
            polyheight = polyline.Height;

            //lstBands = GenerateBands(480, 1, 1);

            txtFFTSize.Text = (8192).ToString();
            txtFFTBuffer.Text = (32768).ToString();
            txtBandNum.Text = (50).ToString();
            txtAttack.Text = (0).ToString();
            txtDecay.Text = (65).ToString();
            txtFreqMin.Text = (20).ToString();
            txtFreqMax.Text = (500).ToString();
            txtSensitivity.Text = (46).ToString();
            
            PropertySet set = new PropertySet();
            set.Add("Attack", 0d);
            set.Add("Decay", 65d);
            set.Add("FreqMin", 20d);
            set.Add("FreqMax", 500d);
            set.Add("Sensitivity", 46.8124123737559d);
            set.Add("Bands", 50);

            capture = new WasapiCaptureRT();
            capture.WaveFormat = new WaveFormat(48000, 16, 2);
            capture.AudioDataAvailable += Capture_AudioDataAvailable;

            player = new MediaPlayer();
            //player.RealTimePlayback = true;
            FFTAudioEffect.player = player;
            FFTAudioEffect.SpectrumDataReady += Capture_AudioDataAvailable;


            lstBands = GenerateBands(50, 10, 5);

        }

        private async void Itm_AudioTracksChanged(MediaPlaybackItem sender, IVectorChangedEventArgs args)
        {
            var encprop = sender.AudioTracks[(int)args.Index].GetEncodingProperties();
            Debug.WriteLine("Bitrate: " + encprop.Bitrate + ", SampleRate: " + encprop.SampleRate + ", BitsPerSample: " + encprop.BitsPerSample + ", Type: " + encprop.Type + ", Subtype: " + encprop.Subtype + ", Channels: " + encprop.ChannelCount);
            //Debug.WriteLine("BT: " + sender.Source.
        }

        public List<Rectangle> GenerateBands(int amount, double width, double gapwidth)
        {
            if (lstBands != null)
            {
                foreach (Rectangle rect in lstBands)
                {
                    rootp.Children.Remove(rect);
                }
                lstBands.Clear();
                GC.Collect();
            }

            List<Rectangle> lstRects = new List<Rectangle>();
            for (int i = 0; i < amount; i++)
            {
                Rectangle rect = new Rectangle();
                rect.Height = 0;
                rect.Width = width;

                rect.HorizontalAlignment = HorizontalAlignment.Left;
                rect.VerticalAlignment = VerticalAlignment.Bottom;

                double twidth = width + gapwidth;

                rect.Margin = new Thickness(100 + (twidth * i), 0, 0, -800);

                AcrylicBrush awb = (AcrylicBrush)Application.Current.Resources["SystemControlAccentAcrylicWindowAccentMediumHighBrush"];
                SolidColorBrush scb = new SolidColorBrush(Colors.Green);

                rect.Fill = awb;
                rootp.Children.Add(rect);

                lstRects.Add(rect);
            }

            return lstRects;
        }

        double polywidth;
        double polyheight;

        WasapiCaptureRT capture;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            double bperp = 500 / int.Parse(txtBandNum.Text);
            double bw = (double)bperp / 3;
            Debug.WriteLine("bp: " + bperp + ", bw: " + bw);

            lstBands = GenerateBands(int.Parse(txtBandNum.Text), bperp, (double)bperp / 2);
            polyline.Width = int.Parse(txtBandNum.Text);

            if (audioSource == "File")
            {


                //var lib = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                //var file = await (await lib.Folders.First(x => x.DisplayName == "Musik").GetFolderAsync("other")).GetFileAsync("ApoRed - Range Rover Mansory (Official Video).wav");

                PropertySet set = new PropertySet();
                set.Add("Attack", double.Parse(txtAttack.Text));
                set.Add("Decay", double.Parse(txtDecay.Text));
                set.Add("FreqMin", double.Parse(txtFreqMin.Text));
                set.Add("FreqMax", double.Parse(txtFreqMax.Text));
                set.Add("Sensitivity", 46.8124123737559d);
                set.Add("Bands", int.Parse(txtBandNum.Text));

                //var mediasource = MediaSource.CreateFromUri(new Uri(@"https://r5---sn-4g5e6nsk.googlevideo.com/videoplayback?fvip=5&signature=AE4F49C5C9CEF4926E486F48AD963A5AAD1C38EB.C579F690E19F61A15498359B606344D4AC7A63C6&ei=epKKXOWCNcXY1gLnqY7gDA&itag=251&sparams=clen%2Cdur%2Cei%2Cgir%2Cid%2Cinitcwndbps%2Cip%2Cipbits%2Citag%2Ckeepalive%2Clmt%2Cmime%2Cmm%2Cmn%2Cms%2Cmv%2Cpl%2Crequiressl%2Csource%2Cexpire&key=yt6&source=youtube&requiressl=yes&mime=audio%2Fwebm&pl=26&keepalive=yes&lmt=1537590053592089&expire=1552606938&initcwndbps=967500&id=o-AIBXJyfxyLblgHrzOT3w0Stw93cjZbSghkm1Ut989wBQ&ipbits=0&mm=31%2C26&mn=sn-4g5e6nsk%2Csn-h0jeen7r&c=WEB&gir=yes&ms=au%2Conr&mt=1552585171&mv=m&dur=182.021&clen=3018604&ip=84.169.118.196"));//MediaSource.CreateFromStorageFile(musicFile);
                MediaPlaybackItem itm = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(musicFile));//mediasource);
                itm.AudioTracksChanged += Itm_AudioTracksChanged;
                //Debug.WriteLine("BT: " + itm.Source.MediaStreamSource.BufferTime);
                await itm.Source.OpenAsync();

                set.Add("AudioType", itm.AudioTracks.First().GetEncodingProperties().Subtype);

                player.RemoveAllEffects();
                player.AddAudioEffect(typeof(FFTAudioEffect).FullName, false, set);

                player.Source = itm;
                player.Play();
                
            }
            else if (audioSource == "System")
            {
                capture.StopRecording();

                capture.FFTBufferSize = int.Parse(txtFFTBuffer.Text);
                capture.FFTSize = int.Parse(txtFFTSize.Text);
                capture.FFTEngine = cbFFTEngine.SelectedIndex == 0 ? FFTEngine.KissFFT : FFTEngine.LomontFFT;
                capture.Bands = int.Parse(txtBandNum.Text);
                capture.FreqMin = int.Parse(txtFreqMin.Text);
                capture.FreqMax = int.Parse(txtFreqMax.Text);
                capture.Attack = int.Parse(txtAttack.Text);
                capture.Decay = int.Parse(txtDecay.Text);
                capture.Sensitivity = int.Parse(txtSensitivity.Text);//46.8124123737559d;

                switch (cbWaveWindow.SelectedIndex)
                {
                    case 0:
                        capture.Window = WFWindow.None;
                        break;
                    case 1:
                        capture.Window = WFWindow.HammingWindow;
                        break;
                    case 2:
                        capture.Window = WFWindow.HannWindow;
                        break;
                    case 3:
                        capture.Window = WFWindow.BlackmannHarrisWindow;
                        break;
                }
                capture.StartRecording();
                Debug.WriteLine("sda");
            }
        }

        int exec = 0;

        bool usebars = false;

        private void Capture_AudioDataAvailable(object sender, object e)
        {
            try
            {

                Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate ()
                {
                    double[] audioData = new double[1];
                    double elapsedTime = 0;
                    try
                    {
                        if (audioSource == "File")
                        {
                            audioData = (double[])e;
                        }
                        else if (audioSource == "System")
                        {
                            audioData = (e as AudioVisEventArgs).AudioData;
                            elapsedTime = (e as AudioVisEventArgs).elapsedTime;
                        }
                    }
                    catch
                    {
                        return;
                    }

                    if (exec % 10 == 0)
                    {
                        lblElapsed.Text = "ElapsedTime: " + elapsedTime + "ms";
                    }
                    for (int i = 0; i < audioData.Length; i += 1)
                    {
                        //double logamp = 100 + (100 * Math.Log10(e.AudioData[i] / 100));
                        double height = audioData[i] * 300;// *10000;//(e.AudioData[i] * polyheight) / (capture.WaveFormat.SampleRate *100);
                        //Debug.WriteLine(height);
                        //Debug.WriteLine(height);
                        //height = 30* Math.Log(height);

                        if (height < 0)
                        {
                            height = 0;
                            //height = Math.Abs(height);
                        }
                        else if (height > 300)
                        {
                            height = 300;
                            //height = Math.Abs(height);
                        }

                        try
                        {
                            if (!tsDmode.IsOn) {
                                lstBands[i].Height = height;
                            }
                            else
                            {
                                if (i < polyline.Points.Count)
                                {
                                    polyline.Points[i] = new Point(i, 200 - height);
                                }
                                else
                                {
                                    polyline.Points.Add(new Point(i, 200 - height));
                                }
                            }
                        }
                        catch { }
                    }
                    exec++;
                });
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

            }
        }

        private void FFTAudioEffect_OnDataReady(object data)
        {
            Debug.WriteLine("DataReady!");
        }

        private void TsFFT_Toggled(object sender, RoutedEventArgs e)
        {
            if (capture != null)
            {
                capture.UseFFT = tsFFT.IsOn;
            }
        }

        private void TsLog_Toggled(object sender, RoutedEventArgs e)
        {
            if (capture != null)
            {
                capture.UseLogScale = tsLog.IsOn;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            polyline.Points.Clear();
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine(int.Parse(txtFFTBuffer.Text));
            //capture.RecordingStopped += Capture_Restart;
            capture.StopRecording();
            Debug.WriteLine("Stopped");
            //lstBands = GenerateBands(capture.Bands, 1, 1);
        }

        string audioSource = "System";
        StorageFile musicFile;

        private async void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton btn = (RadioButton)sender;
            switch ((string)btn.Tag)
            {
                case "System":
                    break;
                case "File":
                    FileOpenPicker picker = new FileOpenPicker();
                    picker.FileTypeFilter.Add(".wav");
                    picker.FileTypeFilter.Add(".webm");
                    picker.FileTypeFilter.Add(".m4a");
                    picker.FileTypeFilter.Add(".mp3");
                    musicFile = await picker.PickSingleFileAsync();
                    break;
            }
            audioSource = (string)btn.Tag;


        }
    }
}
