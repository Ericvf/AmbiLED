using System;
using Color = System.Drawing.Color;

namespace Ambiled.Core
{
    /// <summary>
    /// http://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/ControlPaint.cs,2764
    /// </summary>
    public struct HLSColor
    {
        private const int Range = 240;
        private const int HLSMax = Range;
        private const int RGBMax = 255;
        private const int Undefined = HLSMax * 2 / 3;

        public int hue;
        public int saturation;
        public int luminosity;

        public HLSColor(int r, int g, int b)
        {
            int max, min;
            int sum, dif;
            int Rdelta, Gdelta, Bdelta;

            max = Math.Max(Math.Max(r, g), b);
            min = Math.Min(Math.Min(r, g), b);
            sum = max + min;

            luminosity = (((sum * HLSMax) + RGBMax) / (2 * RGBMax));

            dif = max - min;
            if (dif == 0)
            {
                saturation = 0;
                hue = Undefined;
            }
            else
            {
                if (luminosity <= (HLSMax / 2))
                    saturation = (int)(((dif * (int)HLSMax) + (sum / 2)) / sum);
                else
                    saturation = (int)((int)((dif * (int)HLSMax) + (int)((2 * RGBMax - sum) / 2)) / (2 * RGBMax - sum));

                Rdelta = (int)((((max - r) * (int)(HLSMax / 6)) + (dif / 2)) / dif);
                Gdelta = (int)((((max - g) * (int)(HLSMax / 6)) + (dif / 2)) / dif);
                Bdelta = (int)((((max - b) * (int)(HLSMax / 6)) + (dif / 2)) / dif);

                if ((int)r == max)
                    hue = Bdelta - Gdelta;
                else if ((int)g == max)
                    hue = (HLSMax / 3) + Rdelta - Bdelta;
                else
                    hue = ((2 * HLSMax) / 3) + Gdelta - Rdelta;

                if (hue < 0)
                    hue += HLSMax;
                if (hue > HLSMax)
                    hue -= HLSMax;
            }
        }

        public static Color ColorFromHLS(int hue, int luminosity, int saturation)
        {
            byte r, g, b;
            int magic1, magic2;

            if (saturation == 0)
            {
                r = g = b = (byte)((luminosity * RGBMax) / HLSMax);
            }
            else
            {
                if (luminosity <= (HLSMax / 2))
                    magic2 = (int)((luminosity * ((int)HLSMax + saturation) + (HLSMax / 2)) / HLSMax);
                else
                    magic2 = luminosity + saturation - (int)(((luminosity * saturation) + (int)(HLSMax / 2)) / HLSMax);

                magic1 = 2 * luminosity - magic2;

                r = (byte)(((HueToRGB(magic1, magic2, (int)(hue + (int)(HLSMax / 3))) * (int)RGBMax + (HLSMax / 2))) / (int)HLSMax);
                g = (byte)(((HueToRGB(magic1, magic2, hue) * (int)RGBMax + (HLSMax / 2))) / HLSMax);
                b = (byte)(((HueToRGB(magic1, magic2, (int)(hue - (int)(HLSMax / 3))) * (int)RGBMax + (HLSMax / 2))) / (int)HLSMax);
            }

            return Color.FromArgb(r, g, b);
        }

        private static int HueToRGB(int n1, int n2, int hue)
        {
            if (hue < 0)
                hue += HLSMax;

            if (hue > HLSMax)
                hue -= HLSMax;

            if (hue < (HLSMax / 6))
                return (n1 + (((n2 - n1) * hue + (HLSMax / 12)) / (HLSMax / 6)));
            if (hue < (HLSMax / 2))
                return (n2);
            if (hue < ((HLSMax * 2) / 3))
                return (n1 + (((n2 - n1) * (((HLSMax * 2) / 3) - hue) + (HLSMax / 12)) / (HLSMax / 6)));
            else
                return (n1);
        }
    }

}
