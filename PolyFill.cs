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

      /// <summary> Find intersection point of a given line segment with a scan line at height lY </summary>
      /// <param name="p1">Start point of line</param>
      /// <param name="p2">End point of line</param>
      /// <param name="sY">Height of the scan line</param>
      static double ScanLnIntersection (Point2 p1, Point2 p2, double sY) {
         double X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y;
         double dY = Y2 - Y1, dX = X2 - X1;
         if (dY == 0) return double.NaN;
         sY += 0.5;
         double xS = (dX != 0 ? (sY - Y1) * dX / dY : 0) + X1;
         if (dY < 0) (Y1, Y2) = (Y2, Y1);
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
