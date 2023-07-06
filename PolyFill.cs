using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A25 {
   class PolyFill {
      public void AddLine (int x0, int y0, int x1, int y1) => mLines.Add (new (new (x0, y0), new (x1, y1)));
      public void Fill (GrayBMP bmp, int color) {
         for (int i = 0; i < bmp.Height; i++) {
            var lPts = mLines.Select (a => ScanLnIntersection (a.Item1, a.Item2, bmp.Width, i)).Where (a => a.X is not double.NaN).ToList ();
            if (lPts.Count > 0 && lPts.Count % 2 == 0) 
               for (int j = 0; j < lPts.Count; j += 2) 
                  bmp.DrawHorizontalLine ((int)lPts[j].X, (int)lPts[j + 1].X, i, 255);            
         }
      }

      /// <summary> Find intersection point of a given line segment with a scan line at height lY </summary>
      /// <param name="p1">Start point of line</param>
      /// <param name="p2">End point of line</param>
      /// <param name="width">Width of the screen</param>
      /// <param name="lY">Height of the scan line</param>
      /// <returns></returns>
      static Point2 ScanLnIntersection (Point2 p1, Point2 p2, double width, double lY) {
         lY += 0.5;
         if (p1.Y < lY && p2.Y < lY || p1.Y > lY && p2.Y > lY) return new (double.NaN, double.NaN);
         double X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y;
         double dY = Y2 - Y1, dX = X2 - X1; 
         if (Math.Abs (dY) < DELTA && Math.Abs (dX) < DELTA) return new (double.NaN, double.NaN);
         double c1 = X1 * dY - Y1 * dX;
         double b2 = width;
         double c2 = -lY * width;
         double d = -dY * b2;
         Point2 pInt = new ((dX * c2 - b2 * c1) / d, c2 * dY / d);
         if (dX < 0) (X1, X2) = (X2, X1);
         if (dY < 0) (Y1, Y2) = (Y2, Y1);
         if (pInt.X >= X1 && pInt.X <= X2 && pInt.Y >= Y1 && pInt.Y<=Y2) return pInt;
         return new (double.NaN, double.NaN);
      }

      struct Point2 {
         public Point2 (double x, double y) { X = x; Y = y; }
         public double X, Y;
      }
      List<(Point2, Point2)> mLines = new ();
      const double DELTA = 0.1;
   }
}
