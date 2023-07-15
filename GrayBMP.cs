// GrayBMP.cs - Contains the GrayBMP class (implementation of grayscale bitmp on top
// of a WPF WriteableBitmap class)
// ---------------------------------------------------------------------------------------
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace GrayBMP;
using static System.Math;

#region class GrayBitmap -------------------------------------------------------
/// <summary>Implements a writeable grayscale bitmap</summary>
class GrayBMP {
   #region Constructor --------------------------------------
   /// <summary>Constructs a grayscale (8 bits-per-pixel) bitmap of given size</summary>
   public GrayBMP (double width, double height) {
      mBmp = new WriteableBitmap (mWidth = (int)width, mHeight = (int)height, 96, 96, PixelFormats.Gray8, null);
      mStride = mBmp.BackBufferStride;
      mBuffer = mBmp.BackBuffer;
   }
   #endregion

   #region Properties ---------------------------------------
   /// <summary>The underlying WriteableBitmap</summary>
   public WriteableBitmap Bitmap => mBmp;

   /// <summary>Pointer to the bitmap's buffer - you can obtain this only after a Begin</summary>
   public nint Buffer {
      get {
         if (mcLocks == 0) Fatal ("Buffer access outside Begin() / End()");
         return mBuffer;
      }
   }

   /// <summary>Height of the bitmap, in pixels</summary>
   public int Height => mHeight;

   /// <summary>The back-buffer stride for this bitmap</summary>
   public int Stride => mStride;

   /// <summary>Width of the bitmap, in pixels</summary>
   public int Width => mWidth;
   #endregion

   #region Methods -----------------------------------------
   /// <summary>Call Begin before you obtain the Buffer to update the bitmap</summary>
   public nint Begin () {
      if (mcLocks++ == 0) {
         mBmp.Lock ();
         mX0 = mY0 = int.MaxValue;
         mX1 = mY1 = int.MinValue;
      }
      return mBmp.BackBuffer;
   }

   /// <summary>Clear the bitmap to a given shade of gray</summary>
   public void Clear (int gray) {
      Begin ();
      unsafe {
         var ptr = (byte*)Buffer;
         System.Runtime.CompilerServices.Unsafe.InitBlock (ref *ptr, (byte)gray, (uint)(mHeight * mStride));
         Dirty (0, 0, mWidth - 1, mHeight - 1);
      }
      End ();
   }

   /// <summary>Tags a pixel as dirty</summary>
   public void Dirty (int x, int y) {
      if (x < mX0) mX0 = x; if (x > mX1) mX1 = x;
      if (y < mY0) mY0 = y; if (y > mY1) mY1 = y;
   }

   /// <summary>Tags a rectangle as dirty (x1, x2, y1, y2 need not be 'ordered')</summary>
   public void Dirty (int x1, int y1, int x2, int y2) {
      Dirty (x1, y1); Dirty (x2, y2);
   }
   /// <summary>
   /// Tags the entire bitmap as dirty
   /// </summary>
   public void Dirty () 
      => Dirty (0, 0, Width - 1, Height - 1);

   /// <summary>Draws a line between the given endpoints, with the given shade of gray</summary>
   public void DrawLine (int x1, int y1, int x2, int y2, int gray) {
      if (y1 == y2) { DrawHorizontalLine (x1, x2, y1, gray); return; }
      Begin ();
      int dx = Abs (x2 - x1), dy = -Abs (y2 - y1), error = dx + dy;
      int stepX = x1 < x2 ? 1 : -1, stepY = y1 < y2 ? 1 : -1;
      int stepYPtr = stepY * mStride;
      Check (x1, y1); Check (x2, y2); Dirty (x1, y1, x2, y2);
      byte bGray = (byte)gray;

      unsafe {
         byte* ptr = (byte*)(Buffer + y1 * mStride + x1);
         while (true) {
            *ptr = bGray;
            if (x1 == x2 && y1 == y2) break;
            int delta = 2 * error;
            if (delta >= dy) {
               if (x1 == x2) break;
               error += dy;
               x1 += stepX; ptr += stepX;
            }
            if (delta <= dx) {
               if (y1 == y2) break;
               error += dx;
               y1 += stepY; ptr += stepYPtr;
            }
         }
      }
      End ();
   }

   /// <summary>
   /// Draws a horizontal line between the two given end-points (with given shade of gray)
   /// </summary>
   public void DrawHorizontalLine (int x1, int x2, int y, int gray) {
      Begin ();
      Check (x1, y); Check (x2, y); Dirty (x1, y, x2, y);
      byte bGray = (byte)gray;
      if (x1 > x2) (x1, x2) = (x2, x1);
      unsafe {
         byte* ptr = (byte*)(Buffer + y * Stride + x1);
         for (int i = x2 - x1; i >= 0; i--)
            *ptr++ = bGray;
      }
      End ();
   }

   /// <summary>
   /// Draws a polygon centered on the line (between the two given end-points) of given width
   /// </summary>
   public void DrawThickLine (int x0, int y0, int x1, int y1, int width, int color) {
      if (x0 > x1) (x0, y0, x1, y1) = (x1, y1, x0, y0);
      double dx = x1 - x0, dy = y1 - y0;
      double length = Sqrt (dx * dx + dy * dy);
      double iLength = width / (2 * length);
      double xDelta = dy * iLength;
      double hXDelta = xDelta * 0.5;
      double yDelta = dx * iLength;
      double hYDelta = yDelta * 0.5;
      double sF = 0.86602 * iLength + 1; 
      var midVec = (x: dx * sF, y: dy * sF);

      int X0 = (int)(x0 - xDelta + 0.5), Y0 = (int)(y0 + yDelta + 0.5);
      int X1 = (int)(x1 - xDelta + 0.5), Y1 = (int)(y1 + yDelta + 0.5);
      int X2 = (int)(x0 + xDelta + 0.5), Y2 = (int)(y0 - yDelta + 0.5);
      int X3 = (int)(x1 + xDelta + 0.5), Y3 = (int)(y1 - yDelta + 0.5);

      double XMid = x1 - midVec.x, YMid = y1 - midVec.y;
      int X4 = (int)(XMid - hXDelta + 0.5), Y4 = (int)(YMid + hYDelta + 0.5);
      int X5 = (int)(XMid + hXDelta + 0.5), Y5 = (int)(YMid - hYDelta + 0.5);
      XMid = x0 + midVec.x; YMid = y0 + midVec.y;
      int X6 = (int)(XMid - hXDelta + 0.5), Y6 = (int)(YMid + hYDelta + 0.5);
      int X7 = (int)(XMid + hXDelta + 0.5), Y7 = (int)(YMid - hYDelta + 0.5);
      mPF.Reset ();
      mPF.AddLine (X0, Y0, X1, Y1);
      mPF.AddLine (X2, Y2, X3, Y3);

      mPF.AddLine (X1, Y1, X6, Y6);
      mPF.AddLine (X6, Y6, X7, Y7);
      mPF.AddLine (X7, Y7, X3, Y3);
      mPF.AddLine (X0, Y0, X4, Y4);
      mPF.AddLine (X4, Y4, X5, Y5);
      mPF.AddLine (X5, Y5, X2, Y2);
      mPF.Fill (this, 0);
   }
   PolyFillFast mPF = new ();
   /// <summary>Call End after finishing the update of the bitmap</summary>
   public void End () {
      if (--mcLocks == 0) {
         if (mcLocks < 0) Fatal ("Unexpected call to GrayBitmap.End()");
         if (mX1 >= mX0 && mY1 >= mY0)
            mBmp.AddDirtyRect (new Int32Rect (mX0, mY0, mX1 - mX0 + 1, mY1 - mY0 + 1));
         mBmp.Unlock (); 
      }
   }

   /// <summary>Set a given pixel to a shade of gray</summary>
   public void SetPixel (int x, int y, int gray) {
      Check (x, y); Dirty (x, y);
      var ptr = Begin () + y * mStride + x;
      unsafe { *(byte*)ptr = (byte)gray; };
      End ();
   }

   /// <summary>Set a given pixel to a shade of gray</summary>
   void SetPixelFast (int x, int y, int gray) {
      var ptr = Buffer + y * mStride + x;
      unsafe { *(byte*)ptr = (byte)gray; };
   }
   #endregion

   #region Implementation ----------------------------------
   void Check (int x, int y) {
      if (x < 0 || x >= mWidth || y < 0 || y >= mHeight)
         Fatal ($"Pixel location out of range: ({x},{y})");
   }

   // Helper to throw an exception on invalid usage
   void Fatal (string message)
      => throw new InvalidOperationException (message);

   readonly int mWidth, mHeight, mStride;
   readonly WriteableBitmap mBmp;
   readonly nint mBuffer;
   int mX0, mY0, mX1, mY1;    // The 'dirty rectangle'
   int mcLocks;               // Number of unmatched Begin() calls
   #endregion
}
#endregion
