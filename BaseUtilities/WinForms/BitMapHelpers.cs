﻿/*
 * Copyright © 2016 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */
using System;
using System.Drawing;

namespace BaseUtils
{
    public static class BitMapHelpers
    {
        public static Bitmap ReplaceColourInBitmap(Bitmap source, System.Drawing.Imaging.ColorMap[] remap)
        {
            Bitmap newmap = new Bitmap(source.Width, source.Height);

            System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes();
            ia.SetRemapTable(remap, System.Drawing.Imaging.ColorAdjustType.Bitmap);

            using (Graphics gr = Graphics.FromImage(newmap))
                gr.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

            return newmap;
        }

        public static Bitmap ScaleColourInBitmap(Bitmap source, System.Drawing.Imaging.ColorMatrix cm)
        {
            Bitmap newmap = new Bitmap(source.Width, source.Height);

            System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes();
            ia.SetColorMatrix(cm);

            using (Graphics gr = Graphics.FromImage(newmap))
                gr.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);

            return newmap;
        }

        public static Bitmap ScaleColourInBitmapSideBySide(Bitmap source, Bitmap source2, System.Drawing.Imaging.ColorMatrix cm)
        {
            Bitmap newmap = new Bitmap(source.Width + source2.Width, Math.Max(source.Height, source2.Height));

            System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes();
            ia.SetColorMatrix(cm);

            using (Graphics gr = Graphics.FromImage(newmap))
            {
                gr.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, ia);
                gr.DrawImage(source2, new Rectangle(source.Width, 0, source2.Width, source2.Height), 0, 0, source2.Width, source2.Height, GraphicsUnit.Pixel, ia);
            }

            return newmap;
        }

        public static void DrawTextCentreIntoBitmap(ref Bitmap img, string text, Font dp, Color c)
        {
            using (Graphics bgr = Graphics.FromImage(img))
            {
                SizeF sizef = bgr.MeasureString(text, dp);

                bgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Brush textb = new SolidBrush(c))
                    bgr.DrawString(text, dp, textb, img.Width / 2 - (int)((sizef.Width + 1) / 2), img.Height / 2 - (int)((sizef.Height + 1) / 2));

                bgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
            }
        }

        // if b != Transparent, a back box is drawn.
        // bitmap never bigger than maxsize
        // setting frmt allows you to word wrap etc into a bitmap, maximum of maxsize.  
        // no frmt means a single line across the bitmap unless there are \n in it.

        public static Bitmap DrawTextIntoAutoSizedBitmap(string text, Size maxsize, Font dp, Color c, Color b,
                                            float backscale = 1.0F, StringFormat frmt = null)
        {
            Bitmap t = new Bitmap(1, 1);

            using (Graphics bgr = Graphics.FromImage(t))
            {
                // if frmt, we measure the string within the maxsize bounding box.
                SizeF sizef = (frmt != null) ? bgr.MeasureString(text, dp, maxsize, frmt) : bgr.MeasureString(text, dp);
                //System.Diagnostics.Debug.WriteLine("Bit map auto size " + sizef);

                int width = Math.Min((int)(sizef.Width + 1), maxsize.Width);
                int height = Math.Min((int)(sizef.Height + 1), maxsize.Height);
                Bitmap img = new Bitmap(width, height);

                using (Graphics dgr = Graphics.FromImage(img))
                {
                    if (!b.IsFullyTransparent() && text.Length > 0)
                    {
                        Rectangle backarea = new Rectangle(0, 0, img.Width, img.Height);
                        using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, b, b.Multiply(backscale), 90))
                            dgr.FillRectangle(bb, backarea);

                        dgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;   // only worth doing this if we have filled it.. if transparent, antialias does not work
                    }

                    using (Brush textb = new SolidBrush(c))
                    {
                        if (frmt != null)
                            dgr.DrawString(text, dp, textb, new Rectangle(0, 0, width, height), frmt); // use the draw into rectangle with formatting function
                        else
                            dgr.DrawString(text, dp, textb, 0, 0);
                    }

                    dgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

                    return img;
                }
            }
        }

        // draw into fixed sized bitmap. 
        // centretext overrided frmt and just centres it
        // frmt provides full options and draws text into bitmap

        public static Bitmap DrawTextIntoFixedSizeBitmapC(string text, Size size, Font dp, Color c, Color b,
                                                    float backscale = 1.0F, bool centertext = false, StringFormat frmt = null)
        {
            Bitmap img = new Bitmap(size.Width, size.Height);

            using (Graphics dgr = Graphics.FromImage(img))
            {
                if (!b.IsFullyTransparent() && text.Length > 0)
                {
                    Rectangle backarea = new Rectangle(0, 0, img.Width, img.Height);
                    using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, b, b.Multiply(backscale), 90))
                        dgr.FillRectangle(bb, backarea);

                    dgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; // only if filled
                }

                using (Brush textb = new SolidBrush(c))
                {
                    if (centertext)
                    {
                        SizeF sizef = dgr.MeasureString(text, dp);
                        int w = (int)(sizef.Width + 1);
                        int h = (int)(sizef.Height + 1);
                        dgr.DrawString(text, dp, textb, size.Width / 2 - w / 2, size.Height / 2 - h / 2);
                    }
                    else if (frmt != null)
                        dgr.DrawString(text, dp, textb, new Rectangle(0, 0, size.Width, size.Height), frmt);
                    else
                        dgr.DrawString(text, dp, textb, 0, 0);
                }

                dgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

                return img;
            }
        }

        public static void FillBitmap(Bitmap img, Color c, float backscale = 1.0F)
        {
            using (Graphics dgr = Graphics.FromImage(img))
            {
                Rectangle backarea = new Rectangle(0, 0, img.Width, img.Height);
                using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, c, c.Multiply(backscale), 90))
                    dgr.FillRectangle(bb, backarea);
            }
        }

        // convert BMP to another format and return the bytes of that format

        public static byte[] ConvertTo(this Bitmap bmp, System.Drawing.Imaging.ImageFormat fmt)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bmp.Save(ms, fmt);
            Byte[] f = ms.ToArray();
            return f;
        }
    }
}
