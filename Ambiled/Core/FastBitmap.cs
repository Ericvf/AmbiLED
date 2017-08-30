using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Ambiled.Core
{
    public unsafe class FastBitmap
    {
        /// <summary>
        /// Bitmap start address
        /// </summary>
        byte* bitmapPixelPointer = null;

        /// <summary>
        /// Bitmap data
        /// </summary>
        BitmapData bitmapData = null;

        /// <summary>
        /// The original bitmap
        /// </summary>
        Bitmap bitmap;

        /// <summary>
        /// Size of the bitmap
        /// </summary>
        int width;

        /// <summary>
        /// Gets the original bitmap
        /// </summary>
        public Bitmap Bitmap
        {
            get
            {
                return (bitmap);
            }
        }

        /// <summary>
        /// Returns the pixel size
        /// </summary>
        private Point PixelSize
        {
            get
            {
                GraphicsUnit unit = GraphicsUnit.Pixel;
                RectangleF bounds = bitmap.GetBounds(ref unit);

                return new Point((int)bounds.Width, (int)bounds.Height);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bitmap"></param>
        public FastBitmap(Bitmap bitmap)
        {
            this.bitmap = new Bitmap(bitmap);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public FastBitmap(int width, int height)
        {
            this.bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        }

        /// <summary>
        /// Returns a RGB pointer address for the given pixel offset
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private RGB* PixelAt(int x, int y)
        {
            return (RGB*)(bitmapPixelPointer + y * width + x * sizeof(RGB));
        }

        /// <summary>
        /// Returns the pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public RGB GetPixel(int x, int y)
        {
            RGB returnValue = *PixelAt(x, y);
            return returnValue;
        }

        public RGB GetPixel(int x, int y, float r, float g, float b)
        {
            RGB returnValue = *PixelAt(x, y);
            //returnValue.R = (byte)(returnValue.R * r);
            //returnValue.G = (byte)(returnValue.G * g);
            //returnValue.B = (byte)(returnValue.B * b);
            return returnValue;
        }
        /// <summary>
        /// Sets the pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, RGB color)
        {
            RGB* pixel = PixelAt(x, y);
            *pixel = color;
        }


        /// <summary>
        /// Locks the Bitmap for fast consume
        /// </summary>
        public void LockBitmap()
        {
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = bitmap.GetBounds(ref unit);
            Rectangle bounds = new Rectangle((int)boundsF.X, (int)boundsF.Y, (int)boundsF.Width, (int)boundsF.Height);

            // Figure out the number of bytes in a row. This is rounded up to be a multiple of 4
            // bytes, since a scan line in an image must always be a multiple of 4 bytes in length. 
            width = (int)boundsF.Width * sizeof(RGB);
            if (width % 4 != 0) width = 4 * (width / 4 + 1);

            // Lock the bits
            bitmapData = bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            // Save the start address for fast consuming
            bitmapPixelPointer = (Byte*)bitmapData.Scan0.ToPointer();
        }

        /// <summary>
        /// Unlocks the bitmap
        /// </summary>
        public void UnlockBitmap()
        {
            bitmap.UnlockBits(bitmapData);
            bitmapData = null;
            bitmapPixelPointer = null;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            bitmap.Dispose();
        }
    }


    public class HSL
    {
        /// <summary>
        /// Private members
        /// </summary>
        private float h = 0;
        private float s = 0;
        private float l = 0;

        /// <summary>
        /// Gets or sets the Hue
        /// </summary>
        public float Hue
        {
            get
            {
                return h;
            }
            set
            {
                h = (float)(Math.Abs(value) % 360);
            }
        }

        /// <summary>
        /// Gets or sets the Saturation
        /// </summary>
        public float Saturation
        {
            get
            {
                return s;
            }
            set
            {
                s = (float)Math.Max(Math.Min(1.0, value), 0.0);
            }
        }

        /// <summary>
        /// Gets or sets the luminance
        /// </summary>
        public float Luminance
        {
            get
            {
                return l;
            }
            set
            {
                l = (float)Math.Max(Math.Min(1.0, value), 0.0);
            }
        }

        /// <summary>
        /// Initializes the HSL instance
        /// </summary>
        public HSL(float hue, float saturation, float luminance)
        {
            Hue = hue;
            Saturation = saturation;
            Luminance = luminance;
        }

        public HSL()
        {

        }

        /// <summary>
        /// Returns the RGB colors
        /// </summary>
        public RGB RGB
        {
            get
            {
                double r = 0, g = 0, b = 0;

                double temp1, temp2;

                double normalisedH = h / 360.0;

                if (l == 0)
                {
                    r = g = b = 0;
                }
                else
                {
                    if (s == 0)
                    {
                        r = g = b = l;
                    }
                    else
                    {
                        temp2 = ((l <= 0.5) ? l * (1.0 + s) : l + s - (l * s));

                        temp1 = 2.0 * l - temp2;

                        double[] t3 = new double[] { normalisedH + 1.0 / 3.0, normalisedH, normalisedH - 1.0 / 3.0 };

                        double[] clr = new double[] { 0, 0, 0 };

                        for (int i = 0; i < 3; ++i)
                        {
                            if (t3[i] < 0)
                                t3[i] += 1.0;

                            if (t3[i] > 1)
                                t3[i] -= 1.0;

                            if (6.0 * t3[i] < 1.0)
                                clr[i] = temp1 + (temp2 - temp1) * t3[i] * 6.0;
                            else if (2.0 * t3[i] < 1.0)
                                clr[i] = temp2;
                            else if (3.0 * t3[i] < 2.0)
                                clr[i] = (temp1 + (temp2 - temp1) * ((2.0 / 3.0) - t3[i]) * 6.0);
                            else
                                clr[i] = temp1;

                        }

                        r = clr[0];
                        g = clr[1];
                        b = clr[2];
                    }

                }

                return new RGB((byte)(255 * r), (byte)(255 * g), (byte)(255 * b));
            }
        }

        //private static byte toRGB(float rm1, float rm2, float rh)
        //{
        //    if (rh > 360) rh -= 360;
        //    else if (rh < 0) rh += 360;

        //    if (rh < 60) rm1 = rm1 + (rm2 - rm1) * rh / 60;
        //    else if (rh < 180) rm1 = rm2;
        //    else if (rh < 240) rm1 = rm1 + (rm2 - rm1) * (240 - rh) / 60;

        //    return (byte)(rm1 * 255);
        //}

        public static HSL FromRGB(byte red, byte green, byte blue)
        {
            return FromRGB(Color.FromArgb(red, green, blue));
        }


        public static HSL FromARGB(byte a, byte red, byte green, byte blue)
        {
            return FromRGB(Color.FromArgb(a, red, green, blue));
        }

        public static HSL FromRGB(Color c)
        {
            return new HSL(c.GetHue(), c.GetSaturation(), c.GetBrightness());
        }
    }


    public struct RGB
    {
        public byte R;
        public byte G;
        public byte B;

        public RGB(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public void SetLuminance(float factor)
        {
            var f = factor;
            if (f <= 1.0f)
                Blend(new RGB(), f);
            else
            {
                HSL hsl = HSL.FromRGB(R, G, B);
                hsl.Luminance *= factor;
                var c = hsl.RGB;

                R = c.R;
                G = c.G;
                B = c.B;
            }
        }

        public void SetHue(float factor)
        {
            HSL hsl = HSL.FromRGB(R, G, B);
            hsl.Hue += factor * 360;
            var c = hsl.RGB;
            R = c.R;
            G = c.G;
            B = c.B;
        }

        public void SetSaturation(float factor)
        {
            HSL hsl = HSL.FromRGB(R, G, B);
            hsl.Saturation *= factor;
            var c = hsl.RGB;
            R = c.R;
            G = c.G;
            B = c.B;
        }

        public void Blend(RGB p, float f)
        {
            R = (byte)interpolate(R, p.R, f);
            G = (byte)interpolate(G, p.G, f);
            B = (byte)interpolate(B, p.B, f);
        }

        byte interpolate(byte a, byte b, float p)
        {
            return (byte)((a * p) + b * (1 - p));
            //if (a == b)
            //    return b;

            //if (a > b)
            //    return a--;
            //else
            //    return a++;
            //    return (byte)((a * p) + b * (1 - p)); 
            //int a1 = a * 100;
            //int b1 = b * 100;
            //var x = (a1 * p) + b1 * (1 - p);
            //return (byte)(x / 100);

        }

    }
}
