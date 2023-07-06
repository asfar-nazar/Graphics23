using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace A25;

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

   /// <summary>Tags a pixel as dirty</summary>
   public void Dirty (int x, int y) {
      if (x < mX0) mX0 = x; if (x > mX1) mX1 = x;
      if (y < mY0) mY0 = y; if (y > mY1) mY1 = y;
   }

   /// <summary>Tags a rectangle as dirty (x1, x2, y1, y2 need not be 'ordered')</summary>
   public void Dirty (int x1, int y1, int x2, int y2) {
      Dirty (x1, y1); Dirty (x2, y2);
   }

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
   
   /// <summary>Clear the bmp with the given color</summary>
   public void Clear (int gray) {
      Begin ();
      unsafe {
         byte* ptr = (byte*)Buffer;
         System.Runtime.CompilerServices.Unsafe.InitBlock (ref *ptr, (byte)gray, (uint)(mHeight * mStride));
      }
      End ();
   }

   /// <summary>Draw a line b/w two points (x0, y0) and (x1, y1)</summary>
   /// <param name="gray">Color of the line</param>
   public void DrawLine (int x0, int y0, int x1, int y1, int gray) {
      Begin ();
      Check (x0, y0); Check (x1, y1);
      Dirty (x0, y0); Dirty (x1, y1);
      int dx = Math.Abs (x0 - x1), dy = -Math.Abs (y0 - y1);
      int stepX = x0 > x1 ? -1 : 1, stepY = y0 < y1 ? -1 : 1;
      int stepYPtr = mStride * stepY, error = dx + dy;
      byte bGray = (byte)gray; 
      unsafe {
         byte* ptr = (byte * )(Buffer + y0 * mStride + x0);
         while (true) {
            *ptr = bGray;
            if (x1 == x0 && y0 == y1) break;
            int delta = 2 * error;
            if (delta >= dy) {
               if (x0 == x1) break;
               x0 += stepX; ptr += stepX;
            }
            if (delta <= dx) { 
            if (y0 == y1) break;
            y0 += stepY; ptr += stepYPtr;
            }
         }
      };
      End ();
   }

   /// <summary>Draw a horizontal line b/w two points (x0, y) and (x1, y)</summary>
   /// <param name="gray">Color of the line</param>
   public void DrawHorizontalLine (int x0, int x1, int y, int gray) {
      Begin (); Check (x0, y); Check (x1, y);
      Dirty (x0, y); Dirty (x1, y);
      if (x1 < x0) (x1, x0) = (x0, x1);
      byte bGray = (byte)gray;
      unsafe {
         byte* ptr = (byte*)(Buffer + y * mStride + x0);
         System.Runtime.CompilerServices.Unsafe.InitBlock (ref *ptr, (byte)gray, (uint)(x1 - x0));
      };
      End ();
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
