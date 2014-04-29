// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace FaceBubbles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using ShaderEffectsLibrary;
    using System.Windows.Media.Animation;
    using System.Diagnostics;
    using System.Threading;



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       
        
        private static readonly int Bgra32BytesPerPixel = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private byte[] colorImageData;
        private ColorImageFormat currentColorImageFormat = ColorImageFormat.Undefined;
        private List<Bubble> bubbles = new List<Bubble>();
        private byte[] invisibleColorData;
        private int faceImageSize = 192;
        private const int AudioPollingInterval = 50;
        private const int SamplesPerMillisecond = 16;
        private const int BytesPerSample = 2;
        private readonly byte[] audioBuffer = new byte[AudioPollingInterval * SamplesPerMillisecond * BytesPerSample];
        Stream audioStream;
        public MainWindow()

        {

            this.Background = Brushes.Black;
            InitializeComponent();
            invisibleColorData = new byte[faceImageSize * 4 * faceImageSize];
            for (int i = 0; i < invisibleColorData.Length; i++)
            {
                invisibleColorData[i] = 0;
            }          
            var faceTrackingViewerBinding = new Binding("Kinect") { Source = sensorChooser };
            faceTrackingViewer.SetBinding(FaceTrackingViewer.KinectProperty, faceTrackingViewerBinding);
            sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            sensorChooser.Start();

        }

        void MainWindow_FinishedRecording(object sender, RoutedEventArgs e)
        {

        }


        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs kinectChangedEventArgs)
        {
            KinectSensor oldSensor = kinectChangedEventArgs.OldSensor;
            KinectSensor newSensor = kinectChangedEventArgs.NewSensor;
            if (oldSensor != null)
            {/*
                oldSensor.AudioSource.Stop();
                oldSensor.AllFramesReady -= KinectSensorOnAllFramesReady;
                oldSensor.ColorStream.Disable();
                oldSensor.DepthStream.Disable();
                oldSensor.DepthStream.Range = DepthRange.Default;
                oldSensor.SkeletonStream.Disable();
                oldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                oldSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
           */ }

            if (newSensor != null)
            {      
                  
                try
                {   
                    newSensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
                    newSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    try
                    {
                        // This will throw on non Kinect For Windows devices.
                        newSensor.DepthStream.Range = DepthRange.Near;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                      newSensor.DepthStream.Range = DepthRange.Default;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                   newSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    newSensor.SkeletonStream.Enable();
                    newSensor.AllFramesReady += KinectSensorOnAllFramesReady;

                   audioStream = newSensor.AudioSource.Start();
                    
                }
                catch (InvalidOperationException)
                {
                    // This exception can be thrown when we are trying to
                    // enable streams on a device that has gone away.  This
                    // can occur,IMAGE say, in app shutdown scenarios when the sensor
                    // goes away between the time it changed status and the
                    // time we get the sensor changed notification.
                    //
                    // Behavior here is to just eat the exception and assume
                    // another notification will come along if a sensor
                    // comes back.
                }
            }
        }

      
        private void WindowClosed(object sender, EventArgs e)
        {
            sensorChooser.Stop();   
         
        }

        private void KinectSensorOnAllFramesReady(object sender, AllFramesReadyEventArgs allFramesReadyEventArgs)
        {
           
            using (var colorImageFrame = allFramesReadyEventArgs.OpenColorImageFrame())
            {              
                if (colorImageFrame == null)
                {
                    return;
                }
                if (faceTrackingViewer.FaceBeingTracked)
                {


                    
                        var haveNewFormat = this.currentColorImageFormat != colorImageFrame.Format;
                        if (haveNewFormat)
                        {
                            this.currentColorImageFormat = colorImageFrame.Format;
                            this.colorImageData = new byte[colorImageFrame.PixelDataLength];
                         
                        }


                        colorImageFrame.CopyPixelDataTo(this.colorImageData);

                        //-----                       
                        int colorImageStride = 1280 * 4;
                        int croppedImageStride = faceImageSize * 4;
                        byte[] croppedData = new byte[croppedImageStride * faceImageSize];
                        int Xoffset = (int)(faceTrackingViewer.faceLeft) * 4;
                        int Yoffset = (int)(faceTrackingViewer.faceTop);
                        {
                            int b = 1;
                            for (int i = 0; i < faceImageSize * 4 * faceImageSize - 3; i += 4)
                            {
                                if (i == (faceImageSize * 4 * b))
                                {
                                    b++;
                                }

                                int j = (Xoffset + colorImageStride * (b - 1) + colorImageStride * Yoffset + i - (faceImageSize * 4) * (b - 1));

                                if (j > 0 && j < (colorImageData.Length - 2))
                                {
                                    croppedData[i] = colorImageData[j];
                                    croppedData[i + 1] = colorImageData[j + 1];
                                    croppedData[i + 2] = colorImageData[j + 2];
                                    croppedData[i + 3] = 0xFF;
                                }
                            }
                        }

                        //-----
                        //   this.faceImage.WritePixels(new Int32Rect(0, 0, faceImageSize, faceImageSize),croppedData,faceImageSize * Bgra32BytesPerPixel,0); 


                        FaceImage faceImageFrame = new FaceImage(croppedData, croppedImageStride, faceImageSize);

                        for (int i = 0; i < bubbles.Count; i++)
                        {
                            bubbles[i].addFaceImage(faceImageFrame);
                        }
              
                        
                    }
                else
                    { 
                    //this.faceImage = new WriteableBitmap(faceImageSize, faceImageSize, 96, 96, PixelFormats.Bgra32, null);
                    //this.faceImage.WritePixels(new Int32Rect(0, 0, faceImageSize, faceImageSize), invisibleColorData, faceImageSize * Bgra32BytesPerPixel, 0);
                    }
            }
           updateBubbles();
        }



        private void updateBubbles()
        {
            for (int i = 0; i < bubbles.Count; i++)
            {
                Bubble b = bubbles[i];
                b.update();
                if (b.expired == true)
                {
                  //  bubbles.Remove(b);
                    this.MainGrid.Children.Remove(b.faceImage);
                    this.MainGrid.Children.Remove(b);
                    b = null;
                }
            }
       }

       int bubbleTurn = 0; // make only one bubble, or use 0;
       private int wheelInterval = 1;


   
       private void mouseWheeled(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {


           makeBubbles();
           
        }

     
       private void makeBubbles()
       {
           if (bubbleTurn >= wheelInterval)
           {
               bubbleTurn = 0;
               Bubble bubble = new Bubble(audioStream);
               bubble.BeginAnimation(Image.WidthProperty, new DoubleAnimation(10.0,200.0, TimeSpan.FromSeconds(25)));
               bubble.BeginAnimation(Image.OpacityProperty, new DoubleAnimation(0.5, 1, TimeSpan.FromSeconds(15)));
               bubble.faceImage.BeginAnimation(Image.WidthProperty, new DoubleAnimation(10.0, 200.0, TimeSpan.FromSeconds(25)));
               bubble.faceImage.BeginAnimation(Image.OpacityProperty, new DoubleAnimation(0.5, 1, TimeSpan.FromSeconds(15)));
               bubble.effect.BeginAnimation(Monochrome.FilterColorProperty, new ColorAnimation(Colors.White, Colors.Black, TimeSpan.FromSeconds(21)));
               bubble.circularVignette.BeginAnimation(CircularVignette.FilterColorProperty, new ColorAnimation(Colors.Blue, Colors.Orange, TimeSpan.FromSeconds(11)));
               MainGrid.Children.Add(bubble.faceImage);
               // hiding bubble to see face image
               MainGrid.Children.Add(bubble);
               bubbles.Add(bubble);
           }
           else
           {
               bubbleTurn++;
           }
       }

        private void EventTrigger_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }
    }
}
