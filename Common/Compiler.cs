using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Newtonsoft.Json;
using ICSharpCode.TextEditor.Actions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Drawing;
using System.Diagnostics;

namespace ICSharpCode.TextEditor.Common
{
    public class Compiler
    {
        public Assembly CompiledAssembly { get; set; } = null;

        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Use Roslyn to create an assembly.
        /// </summary>
        /// <param name="fn">File to compile. Also used for assembly name.</param>
        /// <returns></returns>
        public bool Compile(string fn)
        {
            CompiledAssembly = null;
            Errors.Clear();

            string sc = File.ReadAllText(fn);
            string newAssyName = Path.GetFileNameWithoutExtension(fn);

            // Assemble references.
            var mr = new List<MetadataReference>();

            // Remarks:
            //     Performance considerations:
            //     It is recommended to use Microsoft.CodeAnalysis.AssemblyMetadata.CreateFromFile(System.String)
            //     API when creating multiple references to the same assembly. Reusing Microsoft.CodeAnalysis.AssemblyMetadata
            //     object allows for sharing data across these references.

            //var myAssy = Assembly.GetExecutingAssembly();
            //var refAssys = myAssy.GetReferencedAssemblies();

            // Add reference to almost everything we have loaded now.
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<string> ignore = new List<string> { "Dex9.exe", "Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.CSharp.dll" };
            foreach (var lassy in loadedAssemblies)
            {
                string loc = lassy.Location;

                if(ignore.TrueForAll(i => !loc.Contains(i)))
                {
                    Debug.WriteLine(loc);
                    AssemblyMetadata amd = AssemblyMetadata.CreateFromFile(loc);
                    mr.Add(amd.GetReference());
                }
            }

            // Parse the source.
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sc);
            CSharpCompilationOptions opts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            CSharpCompilation compilation = CSharpCompilation.Create(newAssyName, new[] { syntaxTree }, mr, opts);

            // Compile the source.
            using (var memoryStream = new MemoryStream())
            {
                var result = compilation.Emit(memoryStream);

                if (result.Success)
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    CompiledAssembly = Assembly.Load(memoryStream.ToArray());
                }
                else
                {
                    foreach (var diag in result.Diagnostics)
                    {
                        Errors.Add(FormatDiagnostic(diag, fn));
                    }
                }
            }

            return Errors.Count == 0 && CompiledAssembly != null;
        }

        /// <summary>
        /// Utility formatter.
        /// </summary>
        /// <param name="diag"></param>
        /// <returns></returns>
        string FormatDiagnostic(Diagnostic diag, string fn)
        {
            return $"{fn} {diag}";
        }
    }
}