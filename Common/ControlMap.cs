using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

using Newtonsoft.Json;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Common
{
    [Serializable]
    public class ControlSpec
    {
        /// <summary>Any derived from IEditAction incl scripts.</summary>
        public string ActionName { get; set; }

        [JsonIgnore]
        public EditAction EditAction { get; set; } = null;

        /// <summary>First key combo.</summary>
        [JsonIgnore]
        public Keys Key { get; set; } = Keys.None;

        /// <summary>Second key combo or if None, just use the first one.</summary>
        [JsonIgnore]
        public Keys Key2 { get; set; } = Keys.None;

        /// <summary>Make it readable/editable.</summary>
        [JsonProperty("Key")]
        public string KeySerializable { get { return Utils.SerializeKey(Key); } set { Key = Utils.DeserializeKey(value); } }

        /// <summary>Make it readable/editable.</summary>
        [JsonProperty("Key2")]
        public string Key2Serializable { get { return Utils.SerializeKey(Key2); } set { Key2 = Utils.DeserializeKey(value); } }

        /// <summary>Main menu to add to.</summary>
        public string Menu { get; set; } = "";

        /// <summary>Drop down item name.</summary>
        public string SubMenu { get; set; } = "";

        public override string ToString()
        {
            return $"Key:{Key} Action:{ActionName}";
        }
    }


    [Serializable]
    public class ControlMap
    {
        #region Properties
        /// <summary>Contents of the file.</summary>
        public List<ControlSpec> ControlSpecs { get; set; } = new List<ControlSpec>();
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "";
        #endregion

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static ControlMap Load(string fname)
        {
            ControlMap ctlmap;

            if (File.Exists(fname))
            {
                string json = File.ReadAllText(fname);
                ctlmap = JsonConvert.DeserializeObject<ControlMap>(json);
            }
            else
            {
                ctlmap = new ControlMap();
            }

            if (ctlmap != null)
            {
                ctlmap._fn = fname;
            }

            return ctlmap;
        }
        #endregion
    }

    public class ControlMapManager
    {
        #region Properties
        /// <summary>Contents of the file.</summary>
        public List<ControlSpec> ControlSpecs { get { return _ctrlMap.ControlSpecs; } } // TODO1 better way?
        #endregion

        #region Fields
        /// <summary>The encapsulated data.</summary>
        ControlMap _ctrlMap = new ControlMap();

        /// <summary>Mapping between keystrokes and actions.</summary>
        Dictionary<(Keys, Keys), ControlSpec> _keyMap = new Dictionary<(Keys, Keys), ControlSpec>();

        /// <summary>All the loaded actions, with key = name.</summary>
        Dictionary<string, EditAction> _actions = new Dictionary<string, EditAction>();
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userMapFile"></param>
        /// <param name="userActionFiles">aka scripts/plugins/etc</param>
        public List<string> LoadMaps(string defaultMapFile, string userMapFile, List<string> userActionFiles)
        {
            List<string> errors = new List<string>();

            /////// Load actions //////////

            // First the builtin actions.
            Type ti = typeof(EditAction);
            Assembly assy = Assembly.GetAssembly(ti);

            foreach (Type t in assy.GetTypes())
            {
                if (ti.IsAssignableFrom(t) && !t.IsAbstract)
                {
                    //var inst = Activator.CreateInstance(i);
                    // Actions don't have default constructors so use this:
                    var inst = FormatterServices.GetUninitializedObject(t) as EditAction;
                    inst.UserAction = false;

                    _actions.Add(t.Name, inst);
                }
            }

            // Now the user actions.
            foreach(string uaf in userActionFiles)
            {
                // This adds to _actions.
                var uerrs = CompileUserAction(uaf);
                errors.AddRange(uerrs);
            }

            ///////////// Load mappings /////////////////

            // Load the default control map definitions.
            _ctrlMap = ControlMap.Load(defaultMapFile);

            // Load the user control map definitions.
            ControlMap userMap = ControlMap.Load(userMapFile);

            if (userMap != null)
            {
                // Copy into ctrlMap. If overlay is intended it will be dealt with next.
                userMap.ControlSpecs.ForEach(cs => _ctrlMap.ControlSpecs.Add(cs));
            }
            else
            {
                errors.Add($"Failed to load: {userMapFile}");
            }

            ///////////// Bind mappings /////////////////

            // Bind the action implementations to the input configs.
            foreach (ControlSpec asp in _ctrlMap.ControlSpecs)
            {
                if(_actions.ContainsKey(asp.ActionName))
                {
                    asp.EditAction = _actions[asp.ActionName];

                    // Key binding?
                    if (asp.Key != Keys.None)
                    {
                        var key = (asp.Key, asp.Key2);
                        if(!_keyMap.ContainsKey(key))
                        {
                            _keyMap.Add(key, asp);
                        }
                        else
                        {
                            // Presumably a user overwrite. TODO2 notify user?
                            _keyMap[key] = asp;
                        }
                    }
                }
                else
                {
                    errors.Add($"Missing action to bind: {asp.ActionName}");
                }
            }

            return errors;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chord1"></param>
        /// <param name="chord2"></param>
        /// <returns></returns>
        public EditAction GetEditAction(Keys chord1, Keys chord2 = Keys.None)
        {
            var key = (chord1, chord2);

            return _keyMap.ContainsKey(key) ? _keyMap[key].EditAction : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fn"></param>
        List<string> CompileUserAction(string fn)
        {
            List<string> errors = new List<string>();

            Compiler comp = new Compiler();

            if(comp.Compile(fn))
            {
                // Locate our interface.
                Type tif = typeof(EditAction);

                // Load the resulting assembly into the domain?
                //Assembly assembly = Assembly.Load(result);

                // Find our interface.
                bool found = false;
                foreach (Type t in comp.CompiledAssembly.GetTypes())
                {
                    if (tif.IsAssignableFrom(t) && !t.IsAbstract)
                    {
                        // Actions don't have default constructors so we can't use Activator.CreateInstance(i). Use this instead:
                        var inst = FormatterServices.GetUninitializedObject(t) as EditAction;
                        inst.UserAction = true;
                        _actions.Add(t.Name, inst);
                        found = true;
                    }
                }

                if(!found)
                {
                    errors.Add($"Missing IEditAction in {fn}");
                }
            }
            else // failure
            {
                errors.AddRange(comp.Errors);
            }

            return errors;
        }
    }
}
