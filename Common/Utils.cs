using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Common
{

    public class Utils
    {
        /// <summary>
        /// Convert key and modifiers into e.g. "F2 SCA".
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string SerializeKey(Keys key)
        {
            //string skey = key.ToString();

            Keys bkey = (Keys)((int)key & 0xFF);

            string skey = bkey.ToString();
            string smod = "";

            if ((key & Keys.Control) > 0)
            {
                smod += "C";
            }
            if ((key & Keys.Alt) > 0)
            {
                smod += "A";
            }
            if ((key & Keys.Shift) > 0)
            {
                smod += "S";
            }

            if (smod.Length > 0)
            {
                skey += $" {smod}";
            }

            return skey;
        }

        /// <summary>
        /// See SerializeKey().
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Key or None if invalid.</returns>
        public static Keys DeserializeKey(string s)
        {
            Keys key = Keys.None;
            //Enum.TryParse(s, true, out key);

            var parts = s.Split(' ');

            if (parts.Length > 0)
            {
                var skey = parts[0];
                var smod = parts.Length > 1 ? parts[1] : "";

                if (Enum.TryParse(skey, true, out key))
                {
                    foreach (char c in smod)
                    {
                        switch (c)
                        {
                            case 'C': key |= Keys.Control; break;
                            case 'A': key |= Keys.Alt; break;
                            case 'S': key |= Keys.Shift; break;
                            default: key = Keys.None; break;
                        }
                    }
                }
                // else invalid
            }

            return key;
        }
    }
}
