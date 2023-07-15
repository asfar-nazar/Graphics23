// PolyFillWin.cs - Demo window for testing the PolyFill class
// ---------------------------------------------------------------------------------------
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Reflection;
using System.IO;
using System.Windows.Threading;

namespace GrayBMP;

class PolyFillWin : Window {
   public PolyFillWin () {
      Width = 900; Height = 600;
      Left = 200; Top = 50; WindowStyle = WindowStyle.None;

      mBmp = new GrayBMP (Width * mScale, Height * mScale);
      mBmpS = new GrayBMP (Width, Height);
      Image image = new () {
         Stretch = Stretch.Fill,
         HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top,
         Source = mBmpS.Bitmap
      };
      
      RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.HighQuality);
      RenderOptions.SetEdgeMode (image, EdgeMode.Unspecified);
      Content = image;

      mDwg = LoadDrawing ();
      DispatcherTimer timer = new () {
         Interval = TimeSpan.FromMilliseconds (500), IsEnabled = true,
      };
      timer.Tick += NextFrame;
   }
   readonly GrayBMP mBmp;
   readonly GrayBMP mBmpS;
   readonly int mScale = 16;

   void NextFrame (object s, EventArgs e) {
      using (new BlockTimer ("Leaf")) {
         var ptr = mBmp.Begin ();
         DrawLeaf ();
         var ptr1 = mBmpS.Begin ();
         mBmpS.Clear (192);
         DownScaleImage (ptr, ptr1, mScale);
         mBmp.End ();
         mBmpS.End ();
      }
   }

   void DrawLeaf () {
      mBmp.Begin ();
      mBmp.Clear (192);
      mPF.Reset ();
      foreach (var line in mDwg.Lines) {
         var ((x0, y0), (x1, y1)) = (line.A.Round (), line.B.Round ());
         mPF.AddLine (x0, y0, x1, y1);
      }
      mPF.Fill (mBmp, 255);

      foreach (var line in mDwg.Lines) {
         var ((x0, y0), (x1, y1)) = (line.A.Round (), line.B.Round ());
         mBmp.DrawThickLine (x0, y0, x1, y1, mScale, 0);
      }
      mBmp.End ();
   }
   PolyFillFast mPF = new ();

   void DownScaleImage (nint ptr, nint ptr1, int mScale) {
      unsafe {       
         for (int i = 0; i < mBmpS.Width; i++) {
            for (int j = 0; j < mBmpS.Height; j++) {
               var dest = ptr1 + j * mBmpS.Stride + i;
               int value = 0;
               int sRow = i * 16, sCol = j * 16;
               for (int k = sRow; k < sRow + 16; k++) {
                  for (int l = sCol; l < sCol + 16; l++) {
                     value += *(byte*)(ptr + l * mBmp.Stride + k);
                  }
               }
               *(byte*)dest = (byte)(value / 256);
            }
         }

      }
   }

   Drawing LoadDrawing () {
      Drawing dwg = new ();
      using (var stm = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("GrayBMP.Data.leaf-fill.txt"))
      using (var sr = new StreamReader (stm)) {
         for (; ; ) {
            string line = sr.ReadLine (); if (line == null) break;
            double[] w = line.Split ().Select (double.Parse).Select (a => a * mScale).ToArray ();
            Point2 a = new (w[0], w[1]), b = new (w[2], w[3]);
            dwg.AddLine (new Line (a, b));
         }
      }
      return dwg;
   }
   Drawing mDwg;
}