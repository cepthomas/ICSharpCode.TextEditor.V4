using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Newtonsoft.Json;
using ICSharpCode.TextEditor.Actions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.TextEditor.Common
{
    /// <summary>
    /// Dynamic aspect.
    /// </summary>
    public class Action
    {
        public IEditAction EditAction { get; set; } = null;

        public string Name { get; set; } = "???";

        // otherwise builtin.
        public bool UserAction { get; set; } = false;

        public List<(string, object)> Arguments { get; set; } = new List<(string, object)>();
    }

    [Serializable]
    public class ControlSpec
    {
        public Keys Chord1 { get; set; } = Keys.None;

        public Keys Chord2 { get; set; } = Keys.None;


        public string Menu { get; set; } = "";
        public string SubMenu { get; set; } = "";
        public bool ContextMenu { get; set; } = false;

        // TODO2 Toolbar icons.


        // any derived from IEditAction incl scripts.
        public string ActionName { get; set; }

        public ControlSpec()
        {
            Chord1 = Keys.A & Keys.Control;
            Chord2 = Keys.B;

            Menu = "file";// ToolStripMenuItem";
            SubMenu = "justATest";// ToolStripMenuItem";
            ContextMenu = false;

            ActionName = "ShiftTab";
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

            //// Create dummy.
            //ctlmap = new ControlMap();
            //for (int i = 0; i < 3; i++)
            //{
            //    ctlmap.ControlSpecs.Add(new ControlSpec());
            //}
            //ctlmap._fn = fname;
            //ctlmap.Save();

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
        /// <summary>Mapping between keystrokes and actions.</summary>
        Dictionary<(Keys, Keys), Action> _keyActions = new Dictionary<(Keys, Keys), Action>();

        /// <summary>All the loaded actions, with key = name.</summary>
        Dictionary<string, IEditAction> _actions = new Dictionary<string, IEditAction>();
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userMapFile"></param>
        /// <param name="userActionFiles">aka scripts/plugins/etc</param>
        public void LoadMaps(string userMapFile, List<string> userActionFiles)
        {
            /////// Load actions //////////

            // First the builtin actions.
            Type ti = typeof(IEditAction);
            Assembly assy = Assembly.GetAssembly(ti);

            foreach (Type t in assy.GetTypes())
            {
                if (ti.IsAssignableFrom(t) && !t.IsAbstract)
                {
                    //var inst = Activator.CreateInstance(i);
                    // Actions don't have default constructors so use this:
                    var inst = FormatterServices.GetUninitializedObject(t);

                    _actions.Add(t.Name, inst as IEditAction);
                }
            }

            // Now the user actions.
            foreach(string uaf in userActionFiles)
            {
                CompileUserAction(uaf);
            }

            ///////////// Load mappings /////////////////

            // Load the default control map definitions.
            ControlMap ctrlMap = ControlMap.Load(@".\Resources\ctlmap.default");

            // Load the user control map definitions.
            ControlMap userMap = ControlMap.Load(userMapFile);

            if (userMap != null)
            {
                // Copy/overlay into defMap. TODO0 Duplicates?
                userMap.ControlSpecs.ForEach(cs => ctrlMap.ControlSpecs.Add(cs));
            }
            else
            {
                throw new Exception($"Failed to load: {userMapFile}");
            }

            ///////////// Bind mappings /////////////////

            // Bind the action implementations to the input configs.
            foreach (ControlSpec asp in ctrlMap.ControlSpecs)
            {
                if(_actions.ContainsKey(asp.ActionName))
                {
                    Action act = new Action
                    {
                        Name = asp.ActionName,
                        UserAction = false,
                        EditAction = _actions[asp.ActionName]
                    };

                    // Key binding?
                    if (asp.Chord1 != Keys.None)
                    {
                        //act.
                        var key = (asp.Chord1, asp.Chord2);
                        if(!_keyActions.ContainsKey(key))
                        {
                            _keyActions.Add(key, act);
                        }
                        else
                        {
                            // TODO1 overwrite - notify user?
                            _keyActions[key] = act;
                        }
                    }

                    // Menu binding? TODO1
                    if (asp.Menu != "")
                    {
                        if (asp.SubMenu != "")
                        {
                        }

                        if (asp.ContextMenu)
                        {
                        }
                    }
                }
                else
                {
                    throw new Exception($"Missing action to bind: {asp.ActionName}");
                }
            }
        }

        public IEditAction GetEditAction(Keys chord1, Keys chord2 = Keys.None)
        {
            var key = (chord1, chord2);

            return _keyActions.ContainsKey(key) ? _keyActions[key].EditAction : null;
        }

        void CompileUserAction(string fn)
        {
            string sc = File.ReadAllText(fn);


            var syntaxTree = CSharpSyntaxTree.ParseText(sc);

            CSharpCompilation compilation = CSharpCompilation.Create(
                "assemblyName",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                if (!emitResult.Success)
                {
                    // emitResult.Diagnostics
                }
            }



            //var syntaxTree = CSharpSyntaxTree.ParseText(sc);

            //CSharpCompilation compilation = CSharpCompilation.Create(
            //    "assemblyName",
            //    new[] { syntaxTree },
            //    new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            //    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            //using (var dllStream = new MemoryStream())
            //using (var pdbStream = new MemoryStream())
            //{
            //    var emitResult = compilation.Emit(dllStream, pdbStream);
            //    if (!emitResult.Success)
            //    {
            //        // emitResult.Diagnostics
            //    }
            //}






            ScriptOptions sopt = ScriptOptions.Default;
            sopt = sopt.AddReferences("ICSharpCode.TextEditor.Document", "ICSharpCode.TextEditor.Common", "ICSharpCode.TextEditor.Actions");

            var scr = CSharpScript.Create(sc, sopt);

            string sret;
            var compErrs = scr.Compile();

            if (compErrs.Length > 0)
            {
                sret = string.Join(Environment.NewLine, compErrs);
                throw new Exception($"User action errors in {fn}: {sret}");
            }
            else
            {
                // Get the interface and add to _actions.

                Type t = scr.GetType();
                Type ti = typeof(IEditAction);

                if (ti.IsAssignableFrom(t) && !t.IsAbstract)
                {
                    var inst = FormatterServices.GetUninitializedObject(t);
                    _actions.Add(t.Name, inst as IEditAction);
                }
                else
                {
                    throw new Exception($"IEditAction not implemented in {fn}");
                }
            }
        }

        void CompileUserAction_orig(string fileName)
        {
            string sc =
            @"
            public class ScriptedClass
            {
                public string HelloWorld {get;set;}
                public ScriptedClass()
                {
                    HelloWorld = ""Hello Roslyn!"";
                }
            }";

            ScriptState<object> scriptState = null;

            ScriptOptions sopt = ScriptOptions.Default;
            //sopt = sopt.AddReferences("System", "System.Linq");

            var scr = CSharpScript.Create(sc, sopt);

            string sret;
            var compErrs = scr.Compile();
            if (compErrs.Length > 0)
            {
                sret = string.Join(Environment.NewLine, compErrs);
            }
            else
            {
                var task = scr.RunAsync();
                scriptState = task.Result;

                task = scriptState.ContinueWithAsync("new ScriptedClass().HelloWorld");

                sret = task.Result.ReturnValue.ToString();
            }

            Console.WriteLine(sret);
        }
    }
}
