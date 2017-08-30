using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ambiled.Core
{
    public class CaptureUtils
    {
        #region User32.EnumWindows methods

        /// <summary>
        /// Represents a WindowHandle
        /// </summary>
        public class WindowHandle
        {
            public string Title { get; set; }
            public IntPtr Handle { get; set; }

            public WindowHandle()
            {

            }

            public override string ToString()
            {
                return string.Format("{0}", this.Title);
            }
        }

        /// <summary>
        /// Returns the list of all WindowHandles
        /// </summary>
        /// <returns></returns>
        public static List<WindowHandle> GetAllWindows()
        {
            // Create the return list
            var windows = new List<WindowHandle>();

            // Do the pinvoke that will enumerate the lambda for every window
            User32.EnumWindows((IntPtr handle, int param) =>
            {
                if ((User32.GetWindowLongA(handle, User32.GWL_STYLE) & User32.TARGETWINDOW) != User32.TARGETWINDOW)
                    return true;

                // Create a stringbuilder that will contain the title (required for GetWindowText)
                StringBuilder sb = new StringBuilder(100);

                // Retrieve the title of the window 
                User32.GetWindowText(handle, sb, sb.Capacity);
                string title = sb.ToString();

                if (!string.IsNullOrEmpty(title))
                {
                    // Add a WindowHandle instance to the list
                    windows.Add(new WindowHandle()
                    {
                        Title = title,
                        Handle = handle
                    });
                }
                // Return true to continue the enumeration
                return true;
            }, 0);

            return windows;
        }

        /// <summary>
        /// Returns a single window that has a match with Title and namePart
        /// </summary>
        /// <param name="namePart"></param>
        /// <returns></returns>
        public static WindowHandle GetWindowByTitle(string namePart)
        {
            var windows = GetAllWindows();
            return windows.SingleOrDefault(w => w.Title.Contains(namePart));
        }

        /// <summary>
        /// Returns a "the handle to the windows desktop
        /// </summary>
        /// <param name="namePart"></param>
        /// <returns></returns>
        public static WindowHandle GetDesktopHandle()
        {
            return new WindowHandle()
            {
                Title = "Windows Desktop",
                Handle = User32.GetDesktopWindow()
            };
        }

        /// <summary>
        /// Returns a "the handle to the windows desktop
        /// </summary>
        /// <param name="namePart"></param>
        /// <returns></returns>
        public static IntPtr GetDesktopHandlePtr()
        {
            return User32.GetDesktopWindow();
        }

        #endregion

        public static Point CaptureHandleSize(IntPtr handle)
        {
            // Use pinvoke to find the window size
            var windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);

            // Determine width and height of the window
            int windowWidth = windowRect.right - windowRect.left;
            int windowHeight = windowRect.bottom - windowRect.top;

            return new Point(windowWidth, windowHeight);
        }

        public static void CaptureHandleCrop(IntPtr handle, bool hsbs, bool hou, out int cropX, out int cropY)
        {

            // Use pinvoke to find the window size
            var windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);

            // Determine width and height of the window
            int windowWidth = windowRect.right - windowRect.left;
            int windowHeight = windowRect.bottom - windowRect.top;

            if (hsbs)
            {
                windowRect.right -= windowWidth / 2;
                windowWidth = windowRect.right - windowRect.left;
            }
            else if (hou)
            {
                windowRect.bottom -= windowHeight / 2;
                windowHeight = windowRect.bottom - windowRect.top;
            }

            cropX = 0;
            cropY = 0;

            int x = windowWidth / 2;
            int h = windowHeight / 2;

            IntPtr hdcSrc = User32.GetDC(handle);

            int y = 0;
            for (y = 0; y < h; y++)
            {
                var px = Gdi32.GetPixel(hdcSrc, x, y);
                var r = (byte)(px & 0x000000FF);
                var g = (byte)(px & 0x0000FF00) >> 8;
                var b = (byte)(px & 0x00FF0000) >> 16;

                if (r + g + b > 5)
                {
                    cropY = y;
                    break;
                }
            }

            User32.ReleaseDC(handle, hdcSrc);
            GC.Collect();
        }

        public static System.Drawing.Image CaptureWindowStretchBlt(IntPtr handle, int width = 0, int height = 0, int cropX = 0, int cropY = 0,
            bool hsbs = false, bool hou = false, float cropscale = 1, int cols = 0, int rows = 0)
        {
            // Get the client context (without client area)
            IntPtr hdcSrc = User32.GetDC(handle);

            // Use pinvoke to find the window size
            var windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);

            // Determine width and height of the window
            int windowWidth = windowRect.right - windowRect.left;
            int windowHeight = windowRect.bottom - windowRect.top;

            if (hsbs)
            {
                windowRect.right -= windowWidth / 2;
                windowWidth = windowRect.right - windowRect.left;
            }
            else if (hou)
            {
                windowRect.bottom -= windowHeight / 2;
                windowHeight = windowRect.bottom - windowRect.top;
            }

            int dWidth = width > 0 ? width : windowWidth;
            int dHeight = height > 0 ? height : windowHeight;

            int cropHeight = windowHeight - (cropY * 2);
            int cropWidth = windowWidth - (cropX * 2);

            // Get the required memory space for calling Dlt functions
            IntPtr hdcDest = Gdi32.CreateCompatibleDC(hdcSrc);


            bool sameSize = cols == 0 || rows == 0;
            if (!sameSize)
            {
                IntPtr hdcDest2 = Gdi32.CreateCompatibleDC(hdcSrc);

                //IntPtr hBitmap1 = Gdi32.CreateCompatibleBitmap(hdcSrc, windowWidth, windowHeight); // for BitBlt
                IntPtr hBitmap1 = Gdi32.CreateCompatibleBitmap(hdcSrc, cols, rows);
                IntPtr hBitmap2 = Gdi32.CreateCompatibleBitmap(hdcSrc, dWidth, dHeight);

                Gdi32.SelectObject(hdcDest, hBitmap1);
                Gdi32.SetStretchBltMode(hdcDest, Gdi32.StretchMode.STRETCH_ORSCANS);
                Gdi32.StretchBlt(hdcDest, 0, 0, cols, rows, hdcSrc, cropX, cropY, cropWidth, cropHeight, Gdi32.TernaryRasterOperations.SRCCOPY);
                //Gdi32.BitBlt(hdcDest, 0, 0, windowWidth, windowHeight, hdcSrc, cropX, cropY, (int)Gdi32.TernaryRasterOperations.SRCCOPY);

                Gdi32.SelectObject(hdcDest2, hBitmap2);
                Gdi32.SetStretchBltMode(hdcDest2, Gdi32.StretchMode.STRETCH_DELETESCANS);
                Gdi32.StretchBlt(hdcDest2, 0, 0, dWidth, dHeight, hdcDest, 0, 0, cols, rows, Gdi32.TernaryRasterOperations.SRCCOPY);

                var screenCapture1 = System.Drawing.Image.FromHbitmap(hBitmap2);

                // Release handles
                Gdi32.DeleteDC(hdcDest);
                Gdi32.DeleteDC(hdcDest2);

                // Release handles
                User32.ReleaseDC(handle, hdcSrc);

                // Cleanup
                Gdi32.DeleteObject(hBitmap1);
                Gdi32.DeleteObject(hBitmap2);

                GC.Collect();

                return screenCapture1;
            }
            else
            {
                IntPtr hBitmap1 = Gdi32.CreateCompatibleBitmap(hdcSrc, dWidth, dHeight);
                Gdi32.SelectObject(hdcDest, hBitmap1);
                Gdi32.SetStretchBltMode(hdcDest, Gdi32.StretchMode.STRETCH_HALFTONE);
                Gdi32.StretchBlt(hdcDest, 0, 0, dWidth, dHeight, hdcSrc, cropX, cropY, cropWidth, cropHeight, Gdi32.TernaryRasterOperations.SRCCOPY);
                //Gdi32.BitBlt(hdcDest, 0, 0, windowWidth, windowHeight, hdcSrc, cropX, cropY, (int)Gdi32.TernaryRasterOperations.SRCCOPY);

                var screenCapture1 = System.Drawing.Image.FromHbitmap(hBitmap1);
                // Release handles
                Gdi32.DeleteDC(hdcDest);
                User32.ReleaseDC(handle, hdcSrc);
                Gdi32.DeleteObject(hBitmap1);

                GC.Collect();

                return screenCapture1;

            }
        }

    }

    public static class User32
    {
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public int Width
            {
                get
                {
                    return right - left;
                }
            }
            public int Height
            {
                get
                {
                    return bottom - top;
                }
            }
        }

        public static readonly int GWL_STYLE = -16;
        public static readonly ulong WS_VISIBLE = 0x10000000L;
        public static readonly ulong WS_BORDER = 0x00800000L;
        public static readonly ulong TARGETWINDOW = WS_BORDER | WS_VISIBLE;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        public static string GetWindowText(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            User32.GetWindowText(hWnd, sb, 256);
            return sb.ToString();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(int hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(int hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        public static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);
        public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        [DllImport("user32.dll")]
        public static extern void GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int x, int y);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);
        public static IntPtr GetParentWindow(IntPtr hWnd)
        {
            IntPtr parenthWnd = User32.GetParent(hWnd);
            IntPtr returnhWnd = hWnd;
            while (parenthWnd != IntPtr.Zero)
            {
                returnhWnd = parenthWnd;
                parenthWnd = User32.GetParent(parenthWnd);
            }

            return returnhWnd;
        }

        [DllImport("User32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point pt);

        [DllImport("user32.dll")]
        public static extern int InvalidateRect(IntPtr hWnd, IntPtr lpRect, int bErase);

        [DllImport("user32.dll")]
        public static extern int UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);


        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);
    }

    /// <summary>
    /// Encapsulates all the platform invokes for Gdi32.dll
    /// </summary>
    public static class Gdi32
    {
        /// <summary>
        /// Contains StretchModes for the StretchBlt and BitBlt methods
        /// </summary>
        public enum TernaryRasterOperations
        {
            SRCCOPY = 0x00CC0020, /* dest = source*/
            SRCPAINT = 0x00EE0086, /* dest = source OR dest*/
            SRCAND = 0x008800C6, /* dest = source AND dest*/
            SRCINVERT = 0x00660046, /* dest = source XOR dest*/
            SRCERASE = 0x00440328, /* dest = source AND (NOT dest )*/
            NOTSRCCOPY = 0x00330008, /* dest = (NOT source)*/
            NOTSRCERASE = 0x001100A6, /* dest = (NOT src) AND (NOT dest) */
            MERGECOPY = 0x00C000CA, /* dest = (source AND pattern)*/
            MERGEPAINT = 0x00BB0226, /* dest = (NOT source) OR dest*/
            PATCOPY = 0x00F00021, /* dest = pattern*/
            PATPAINT = 0x00FB0A09, /* dest = DPSnoo*/
            PATINVERT = 0x005A0049, /* dest = pattern XOR dest*/
            DSTINVERT = 0x00550009, /* dest = (NOT dest)*/
            BLACKNESS = 0x00000042, /* dest = BLACK*/
            WHITENESS = 0x00FF0062, /* dest = WHITE*/
        };

        /// <summary>
        /// Contains StretchModes for the SetStretchBltMode method
        /// </summary>
        public enum StretchMode
        {
            STRETCH_ANDSCANS = 1,
            STRETCH_ORSCANS = 2,
            STRETCH_DELETESCANS = 3,
            STRETCH_HALFTONE = 4,
        }

        [DllImport("gdi32.dll")]
        public static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll")]
        public static extern bool SetStretchBltMode(IntPtr hdc, StretchMode iStretchMode);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
    }
}
