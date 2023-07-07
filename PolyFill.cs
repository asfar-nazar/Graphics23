using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections;

namespace GrayBMP {

   class PolyFill {
      public void AddLine (int x0, int y0, int x1, int y1) {
         if ((x0, y0) == (x1, y1)) return;
         mLines.Add (new (new (x0, y0), new (x1, y1))); 
      }

      /// <summary> Draw all mLines to the bmp and fill all closed polylines with the given color </summary>
      public void Fill (GrayBMP bmp, int color) {
         for (int i = 0; i < bmp.Height; i++) {
            var iPts = mLines.Select (a => ScanLnIntersection (a.Item1, a.Item2, i)).Where (a => a is not double.NaN).OrderBy (a => a).ToList ();
            if (iPts.Count > 0 && iPts.Count % 2 == 0)
               for (int j = 0; j < iPts.Count; j += 2)
                  bmp.DrawHorizontalLine ((int)iPts[j], (int)iPts[j + 1], i, 255);
         }
      }

      public void FillM (GrayBMP bmp, int color) {
         ConcurrentQueue<int> Queue = new (Enumerable.Range (0, bmp.Height));
         bmp.Begin ();
         int threads = Environment.ProcessorCount; ;
         var mCEV = new CountdownEvent (threads);
         for (int i = 0; i < threads; i++)
            new Thread (FillS).Start ();
         mCEV.Wait ();
         bmp.Dirty (0, 0, bmp.Width - 1, bmp.Height - 1);
         bmp.End ();

         void FillS () {
            while (Queue.TryDequeue (out int y)) {
               var iPts = mLines.Select (a => ScanLnIntersection (a.Item1, a.Item2, y)).Where (a => a is not double.NaN).OrderBy (a => a).ToList ();
               if (iPts.Count > 0 && iPts.Count % 2 == 0)
                  for (int j = 0; j < iPts.Count; j += 2)
                     DrawHorizontalLine ((int)iPts[j], (int)iPts[j + 1], y, 255);
         
            }mCEV.Signal ();
         }

         void DrawHorizontalLine (int x1, int x2, int y, int gray) {
            if (x2 < x1) (x2, x1) = (x1, x2);
            byte bGray = (byte)gray;
            unsafe {
               byte* ptr = (byte*)(bmp.Buffer + y * bmp.Stride + x1);
               System.Runtime.CompilerServices.Unsafe.InitBlock (ref *ptr, (byte)gray, (uint)(x2 - x1));
            };
         }
      }

      /// <summary> Find intersection point of a given line segment with a scan line at height lY </summary>
      /// <param name="p1">Start point of line</param>
      /// <param name="p2">End point of line</param>
      /// <param name="sY">Height of the scan line</param>
      static double ScanLnIntersection (Point2 p1, Point2 p2, double sY) {
         double X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y;
         double dY = Y2 - Y1, dX = X2 - X1;
         // If line is above or below the scan line no intersection is possible
         if (Y1 < sY && Y2 < sY || Y1 > sY && Y2 > sY) return double.NaN;
         // No intersection if the line is parallel to scan line
         if (dY == 0) return double.NaN;
         sY += 0.5;
         double xS = (dX != 0 ? (sY - Y1) * dX / dY : 0) + X1;
         if (dY < 0) (Y1, Y2) = (Y2, Y1);
         // If the point of intersection is above or below the line segment -> not an intersection
         if (sY < Y1 || sY > Y2) return double.NaN;
         return xS;
      }

      struct Point2 {
         public Point2 (double x, double y) { X = x; Y = y; }
         public double X, Y;
      }
      List<(Point2, Point2)> mLines = new ();
      const double DELTA = 0.1;
   }
}
