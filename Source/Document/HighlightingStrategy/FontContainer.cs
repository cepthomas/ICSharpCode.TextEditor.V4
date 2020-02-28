// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// This class is used to generate bold, italic and bold/italic fonts out of a base font.
    /// </summary>
    public class FontContainer //TODO2 this is a bit messed up - maybe refactor.
    {
        Font _defaultFont;
        static float _twipsPerPixelY = 0;

        /// <value>
        /// The scaled, regular version of the base font
        /// </value>
        public Font RegularFont { get; private set; }

        /// <value>
        /// The scaled, bold version of the base font
        /// </value>
        public Font BoldFont { get; private set; }

        /// <value>
        /// The scaled, italic version of the base font
        /// </value>
        public Font ItalicFont { get; private set; }

        /// <value>
        /// The scaled, bold/italic version of the base font
        /// </value>
        public Font BoldItalicFont { get; private set; }

        public static float TwipsPerPixelY
        {
            get
            {
                if (_twipsPerPixelY == 0)
                {
                    using (Bitmap bmp = new Bitmap(1,1))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            _twipsPerPixelY = 1440 / g.DpiY;
                        }
                    }
                }
                return _twipsPerPixelY;
            }
        }

        /// <value>
        /// The base font
        /// </value>
        public Font DefaultFont
        {
            get
            {
                return _defaultFont;
            }
            set
            {
                _defaultFont = value;
                // 1440 twips is one inch
                float pixelSize = (float)Math.Round(value.SizeInPoints * 20 / TwipsPerPixelY);
                RegularFont = new Font(value.FontFamily, pixelSize * TwipsPerPixelY / 20f, FontStyle.Regular);
                BoldFont = new Font(RegularFont, FontStyle.Bold);
                ItalicFont = new Font(RegularFont, FontStyle.Italic);
                BoldItalicFont = new Font(RegularFont, FontStyle.Bold | FontStyle.Italic);
            }
        }

        //public static Font ParseFont(string font)
        //{
        //    string[] descr = font.Split(new char[] {',', '='});
        //    return new Font(descr[1], Single.Parse(descr[3]));
        //}

        public FontContainer(Font defaultFont)
        {
            DefaultFont = defaultFont;
        }
    }
}
