using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Diagnostics;

namespace FaceBubbles
{
    class FaceImage 
    {
     public   WriteableBitmap faceBitmap;
   

        public FaceImage(byte[] croppedData, int croppedImageStride, int faceImageSize)
            {
                faceBitmap = new WriteableBitmap(faceImageSize, faceImageSize, 96, 96, PixelFormats.Bgra32, null);
                faceBitmap.WritePixels(new Int32Rect(0, 0, faceImageSize, faceImageSize),croppedData,faceImageSize *((PixelFormats.Bgra32.BitsPerPixel + 7) / 8),0);
            }

    }
}
