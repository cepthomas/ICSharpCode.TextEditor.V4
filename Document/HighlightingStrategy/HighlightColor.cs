// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
    /// <summary>
    /// A color used for highlighting
    /// </summary>
    public class HighlightColor
    {
        #region Properties
        public bool HasForeground { get { return Color != Color.Transparent; } }

        public bool HasBackground { get { return BackgroundColor != Color.Transparent; } }

        public bool Bold { get; } = false;

        public bool Italic { get; } = false;

        public Color BackgroundColor { get; } = Color.Transparent;

        public Color Color { get; } = Color.Transparent;
        #endregion

        #region Lifecycle
        public HighlightColor(XmlElement el, HighlightColor defaultColor)
        {
            if (el.Attributes["bold"] != null)
            {
                Bold = bool.Parse(el.Attributes["bold"].InnerText);
            }
            else
            {
                Bold = defaultColor.Bold;
            }

            if (el.Attributes["italic"] != null)
            {
                Italic = bool.Parse(el.Attributes["italic"].InnerText);
            }
            else
            {
                Italic = defaultColor.Italic;
            }

            if (el.Attributes["color"] != null)
            {
                string c = el.Attributes["color"].InnerText;
                if (c[0] == '#')
                {
                    Color = ParseColor(c);
                }
                else if (c.StartsWith("SystemColors."))
                {
                    Color = ParseColorString(c.Substring("SystemColors.".Length));
                }
                else
                {
 //?                   ColorX = (Color)(Color.GetType()).InvokeMember(c, BindingFlags.GetProperty, null, Color, new object[0]);
                    Color = defaultColor.Color;
                }
                //HasForeground = true;
            }
            else
            {
                Color = defaultColor.Color;
            }

            if (el.Attributes["bgcolor"] != null)
            {
                string c = el.Attributes["bgcolor"].InnerText;
                if (c[0] == '#')
                {
                    BackgroundColor = ParseColor(c);
                }
                else if (c.StartsWith("SystemColors."))
                {
                    BackgroundColor = ParseColorString(c.Substring("SystemColors.".Length));
                }
                else
                {
//?                    BackgroundColorX = (Color)(Color.GetType()).InvokeMember(c, BindingFlags.GetProperty, null, Color, new object[0]);
                    BackgroundColor = defaultColor.BackgroundColor;
                }
        //        HasBackground = true;
            }
            else
            {
                BackgroundColor = defaultColor.BackgroundColor;
            }
        }

        public HighlightColor(XmlElement el)// : this(el, DEF_HL_COL)
        {
            if (el.Attributes["bold"] != null)
            {
                Bold = bool.Parse(el.Attributes["bold"].InnerText);
            }

            if (el.Attributes["italic"] != null)
            {
                Italic = bool.Parse(el.Attributes["italic"].InnerText);
            }

            if (el.Attributes["color"] != null)
            {
                string c = el.Attributes["color"].InnerText;
                if (c[0] == '#')
                {
                    Color = ParseColor(c);
                }
                else if (c.StartsWith("SystemColors."))
                {
                    Color = ParseColorString(c.Substring("SystemColors.".Length));
                }
                else
                {
                    Color = (Color)(Color.GetType()).InvokeMember(c, BindingFlags.GetProperty, null, Color, new object[0]);
                }
  //              HasForeground = true;
            }
            else
            {
                Color = Color.Transparent; // to set it to the default value.
            }

            if (el.Attributes["bgcolor"] != null)
            {
                string c = el.Attributes["bgcolor"].InnerText;
                if (c[0] == '#')
                {
                    BackgroundColor = ParseColor(c);
                }
                else if (c.StartsWith("SystemColors."))
                {
                    BackgroundColor = ParseColorString(c.Substring("SystemColors.".Length));
                }
                else
                {
                    BackgroundColor = (Color)(Color.GetType()).InvokeMember(c, BindingFlags.GetProperty, null, Color, new object[0]);
                }
//                HasBackground = true;
            }
        }

        public HighlightColor(Color color, bool bold = false, bool italic = false)
        {
            //HasForeground = true;
            Color = color;
            Bold = bold;
            Italic = italic;
        }

        public HighlightColor(Color color, Color backgroundcolor, bool bold = false, bool italic = false)
        {
            //HasForeground = true;
            //HasBackground = true;
            Color = color;
            BackgroundColor = backgroundcolor;
            Bold = bold;
            Italic = italic;
        }

        //public HighlightColor(string systemColor, string systemBackgroundColor, bool bold, bool italic)
        //{
        //    HasForeground = true;
        //    HasBackground = true;

        //    Color = ParseColorString(systemColor);
        //    BackgroundColor = ParseColorString(systemBackgroundColor);

        //    Bold = bold;
        //    Italic = italic;
        //}

        //public HighlightColor(string systemColor, bool bold, bool italic)
        //{
        //    HasForeground = true;

        //    Color = ParseColorString(systemColor);

        //    Bold = bold;
        //    Italic = italic;
        //}

        public HighlightColor()
        {

        }
        #endregion

        #region Private functions
        Color ParseColorString(string colorName)
        {
            string[] cNames = colorName.Split('*');
            PropertyInfo myPropInfo = typeof(SystemColors).GetProperty(cNames[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            Color c = (Color)myPropInfo.GetValue(null, null);

            if (cNames.Length == 2)
            {
                // hack : can't figure out how to parse doubles with '.' (culture info might set the '.' to ',')
                double factor = double.Parse(cNames[1]) / 100;
                c = Color.FromArgb((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));
            }

            return c;
        }

        static Color ParseColor(string c)
        {
            int a = 255;
            int offset = 0;
            if (c.Length > 7)
            {
                offset = 2;
                a = int.Parse(c.Substring(1,2), NumberStyles.HexNumber);
            }

            int r = int.Parse(c.Substring(1 + offset,2), NumberStyles.HexNumber);
            int g = int.Parse(c.Substring(3 + offset,2), NumberStyles.HexNumber);
            int b = int.Parse(c.Substring(5 + offset,2), NumberStyles.HexNumber);
            return Color.FromArgb(a, r, g, b);
        }
        #endregion
    }
}
