using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
//using System.Windows.Forms;
using System.Windows.Controls;
using ShaderEffectsLibrary;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Threading;
using SlimDX.DirectSound;
using NAudio.Wave;

namespace FaceBubbles
{
    

    class Bubble : Image
    {        
        SlimDX.DirectSound.DirectSound device;
        SlimDX.DirectSound.SecondarySoundBuffer sBuffer;
        SlimDX.Multimedia.WaveFormat waveFormat = new SlimDX.Multimedia.WaveFormat();
        SlimDX.DirectSound.SoundBufferDescription bufferDescription;
        float amountOfTimeToRecord = .5f;
        private event RoutedEventHandler FinishedRecording;
     //   private const int AudioPollingInterval = 50;
     //   private const int SamplesPerMillisecond = 8;
     //   private const int BytesPerSample = 2;
       // private readonly byte[] audioBuffer = new byte[AudioPollingInterval * SamplesPerMillisecond * BytesPerSample];
        public Image faceImage = new Image();
        BitmapImage image = new BitmapImage(new Uri("C:\\Users\\diana_000\\Documents\\Github\\FaceBubbles\\FaceBubbles\\Images\\bubble.png"));
        double left = 0.0;
        double top = 450.0;        
        Random random;
        int updates = 5;
        double valueTop;
        double valueLeft;
        private int turns;
        List<FaceImage> faceImages;
        int lastFrame = 0;
        bool ping = true;
        bool pong = false;
        Stopwatch stopwatch;
        private int valueLifeSpan;
        private bool StopAddingFaces = true;
        Stream audioStream;
        int startRecordTime = 0;
        

        public Bubble(Stream audioStream)
            

      {                 
///
            /// 
            // Wave Format
          device = new DirectSound(SlimDX.DirectSound.DirectSoundGuid.DefaultPlaybackDevice);         
          waveFormat.FormatTag = SlimDX.Multimedia.WaveFormatTag.Pcm;
          waveFormat.BitsPerSample = 16;
          waveFormat.BlockAlignment = 2;
          waveFormat.Channels = 1;
          waveFormat.SamplesPerSecond = 16000;
          waveFormat.AverageBytesPerSecond = 32000;
            ///
            ///
          bufferDescription = new SlimDX.DirectSound.SoundBufferDescription();
          bufferDescription.Format = waveFormat;
          bufferDescription.Flags = BufferFlags.ControlVolume;
          bufferDescription.SizeInBytes = (int)(amountOfTimeToRecord *2* waveFormat.AverageBytesPerSecond);
          sBuffer = new SlimDX.DirectSound.SecondarySoundBuffer(device, bufferDescription);
          this.audioStream = audioStream;
            ///
            ///
          stopwatch = new Stopwatch();
          stopwatch.Start();
          faceImages = new List<FaceImage>();
          effect = new Monochrome();
          circularVignette = new CircularVignette();
          random = new Random();
          valueLifeSpan = random.Next(50000, 100000);
          left = random.NextDouble() * 1024 - 512;
          turns = (int)(random.NextDouble() * 100);
          this.expired = false;
          this.Margin = new Thickness(left, top, 0, 0);
          this.Source = image;
          this.faceImage.Margin = new Thickness(left, top, 0, 0);
          this.Width = 50;
          this.faceImage.Width = 50;
          this.Effect = effect;
          this.faceImage.Effect = circularVignette;
          FinishedRecording += new RoutedEventHandler(MainWindow_FinishedRecording);
          IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
          device.SetCooperativeLevel(handle, CooperativeLevel.Priority);
          startRecording();
      }


        void MainWindow_FinishedRecording(object sender, RoutedEventArgs e)
        {
          //  Console.WriteLine("length of recording: "+(stopwatch.Elapsed.Seconds - startRecordTime));
            StopAddingFaces = true;
            Dispatcher.BeginInvoke(new ThreadStart(Play));
        }


        private void Play()
        {
                sBuffer.Volume = -4000;
                sBuffer.Play(0, PlayFlags.Looping);            
        }


       private void startRecording()
        {
               var t = new Thread(new ParameterizedThreadStart((RecordAudio)));
                t.Start();            
        }


       private void RecordAudio(object foo)
       {
           var recordingLength = (int)(amountOfTimeToRecord * 2 * 16000);
           byte[] buffer = new byte[1024];
           using (MemoryStream inputStream = new MemoryStream())
           {
              WriteWavHeader(inputStream, recordingLength);
              StopAddingFaces = false;

                   int count, totalCount = 0;
                   while ((count = audioStream.Read(buffer, 0, buffer.Length)) > 0 && totalCount < recordingLength)
                   {
                       inputStream.Write(buffer, 0, count);
                       sBuffer.Write(buffer, totalCount, LockFlags.None);

                       totalCount += count;
                   }
                   startRecordTime = stopwatch.Elapsed.Seconds;    
                   using (MemoryStream outputStream = new MemoryStream())
                   {
                       WriteWavHeader(outputStream, recordingLength);
                       inputStream.Position = 0;
                       ReverseWaveFile(inputStream, outputStream, totalCount, recordingLength);
                   }

               if (FinishedRecording != null)
                   FinishedRecording(null, null);             
           }
       }


       private void ReverseWaveFile(MemoryStream inputFile, MemoryStream outputFile, int totalCount, int recordingLength )
       {
           using (WaveFileReader reader = new WaveFileReader(inputFile))
           {
               int blockAlign = reader.WaveFormat.BlockAlign;
               using (WaveFileWriter writer = new WaveFileWriter(outputFile, reader.WaveFormat))
               {
                   byte[] buffer = new byte[blockAlign];
                   long samples = reader.Length / blockAlign;
                   for (long sample = samples - 1; sample >= 0; sample--)
                   {
                       reader.Position = sample * blockAlign;
                       reader.Read(buffer, 0, blockAlign);
                       writer.Write(buffer, 0, blockAlign);
                   }
                        outputFile.Position = 0;
                        int count = 0;
                        buffer = new byte[1024];
                           while ((count = outputFile.Read(buffer, 0, buffer.Length)) > 0 && totalCount < (recordingLength*2))
                           {                             
                               sBuffer.Write(buffer, totalCount, LockFlags.None);
                               totalCount += count;
                           }
               }
           }
           
       }
  

     static void WriteWavHeader(Stream stream, int dataLength)
       {
           //We need to use a memory stream because the BinaryWriter will close the underlying stream when it is closed
           using (var memStream = new MemoryStream(64))
           {
               int cbFormat = 18; //sizeof(WAVEFORMATEX)
               WAVEFORMATEX format = new WAVEFORMATEX()
               {
                   wFormatTag = 1,
                   nChannels = 1,
                   nSamplesPerSec = 16000,
                   nAvgBytesPerSec = 32000,
                   nBlockAlign = 2,
                   wBitsPerSample = 16,
                   cbSize = 0
               };

               using (var bw = new BinaryWriter(memStream))
               {
                   //RIFF header
                   WriteString(memStream, "RIFF");
                   bw.Write(dataLength + cbFormat + 4); //File size - 8
                   WriteString(memStream, "WAVE");
                   WriteString(memStream, "fmt ");
                   bw.Write(cbFormat);

                   //WAVEFORMATEX
                   bw.Write(format.wFormatTag);
                   bw.Write(format.nChannels);
                   bw.Write(format.nSamplesPerSec);
                   bw.Write(format.nAvgBytesPerSec);
                   bw.Write(format.nBlockAlign);
                   bw.Write(format.wBitsPerSample);
                   bw.Write(format.cbSize);

                   //data header
                   WriteString(memStream, "data");
                   bw.Write(dataLength);
                   memStream.WriteTo(stream);
               }
           }
       }

       static void WriteString(Stream stream, string s)
       {
           byte[] bytes = Encoding.ASCII.GetBytes(s);
           stream.Write(bytes, 0, bytes.Length);
       }

       struct WAVEFORMATEX
       {
           public ushort wFormatTag;
           public ushort nChannels;
           public uint nSamplesPerSec;
           public uint nAvgBytesPerSec;
           public ushort nBlockAlign;
           public ushort wBitsPerSample;
           public ushort cbSize;
       }

      internal void update()
      {
              if(sBuffer.Volume <= -40)
              sBuffer.Volume += 40;
          
          if (stopwatch.ElapsedMilliseconds >= valueLifeSpan || top <= -400 || left >= 640)
          {
              pop();
              return;
          }
                    
          if (updates >= turns)
          {
              updates = 0;
              valueTop = random.NextDouble();
              valueLeft = random.NextDouble();
              
          }

          updates += 2;
          left += valueLeft*-20+10;
          top -= valueTop * -10 + 9;
          Thickness margin = new Thickness(left,top,0,0);
          this.Margin = margin;
          this.faceImage.Margin = margin;

          if (faceImages.Count != 0)
          {             
                if (ping)
                {
                    if (lastFrame >= faceImages.Count - 1)
                    {
                        lastFrame = faceImages.Count - 1;
                        pong = true;

                        ping = false;
                    }
                    else
                    {
                        lastFrame += 1;
                    }
                }
                else
                if (pong)
                {
                    if (lastFrame <= 0)
                    {
                        lastFrame = 0;
                        ping = true;

                        pong = false;
                    }
                    else
                    {
                        lastFrame -= 1;
                    }
                }             
            
              this.faceImage.Source = faceImages[lastFrame].faceBitmap;
          }
      }

      private void pop()
      {
          sBuffer.Stop();
          if(sBuffer.Status != BufferStatus.Playing)
          expired = true;
      }

      public void addFaceImage(FaceImage faceImage)
      {
          if (StopAddingFaces)
              return;
          faceImages.Add(faceImage);
      }

      public bool expired { get; set; }

      public Monochrome effect { get; set; }
      public CircularVignette circularVignette { get; set; }
    }

    class StreamFrame
    {
        public byte[] buffer = new byte[1024];
        public int offset = 0;
        public int count = 0;
        public int index = 0;

        public StreamFrame(byte[] buffer, int offset, int count, int index)
        {
            this.buffer = buffer;
            this.offset = offset;
            this.count = count;
            this.index = index;
        }

    }
}


