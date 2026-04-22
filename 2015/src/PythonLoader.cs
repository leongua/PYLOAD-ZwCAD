using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
using ZwApp = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PYLOAD
{
    public class PythonLoader
    {
        private static ScriptEngine _engine;
        private static ScriptScope _scope;

        [CommandMethod("PYLOAD", CommandFlags.Modal)]
        public void ExposeAndRun()
        {
            Document doc = ZwApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            if (_engine == null)
            {
                _engine = Python.CreateEngine();
                _scope = _engine.CreateScope();
            }

            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var paths = _engine.GetSearchPaths();
            foreach (string candidate in GetIronPythonSearchPathCandidates(assemblyDir))
            {
                if (Directory.Exists(candidate) && !paths.Contains(candidate))
                {
                    paths.Add(candidate);
                }
            }
            _engine.SetSearchPaths(paths);

            string scriptPath = AskScriptPathOrDialog(ed);
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                return;
            }

            if (!File.Exists(scriptPath))
            {
                ed.WriteMessage("\n[PYLOAD] File non trovato: " + scriptPath);
                return;
            }

            if (!scriptPath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            {
                ed.WriteMessage("\n[PYLOAD] Il file deve avere estensione .py");
                return;
            }

            RunScript(doc, db, ed, scriptPath);
            ed.Regen();
        }

        private static string AskScriptPathOrDialog(Editor ed)
        {
            PromptStringOptions pso = new PromptStringOptions("\nPercorso script .py (Invio = dialog): ");
            pso.AllowSpaces = true;
            PromptResult pr = ed.GetString(pso);
            if (pr.Status == PromptStatus.Cancel)
            {
                return null;
            }

            string value = (pr.StringResult ?? string.Empty).Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Python files (*.py)|*.py" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    return ofd.FileName;
                }
            }

            return null;
        }

        private static void RunScript(Document doc, Database db, Editor ed, string scriptPath)
        {
            using (DocumentLock loc = doc.LockDocument())
            {
                try
                {
                    _engine.Runtime.LoadAssembly(Assembly.Load("ZwManaged"));
                    _engine.Runtime.LoadAssembly(Assembly.Load("ZwDatabaseMgd"));

                    PyCad cad = new PyCad(doc, db, ed);

                    _scope.SetVariable("doc", doc);
                    _scope.SetVariable("db", db);
                    _scope.SetVariable("ed", ed);
                    _scope.SetVariable("cad", cad);
                    _scope.SetVariable("script_path", scriptPath);
                    _scope.SetVariable("script_dir", Path.GetDirectoryName(scriptPath));

                    string code = File.ReadAllText(scriptPath, System.Text.Encoding.UTF8);
                    ScriptSource source = _engine.CreateScriptSourceFromString(code, SourceCodeKind.File);
                    source.Execute(_scope);
                }
                catch (System.Exception ex)
                {
                    string msg = _engine.GetService<ExceptionOperations>().FormatException(ex);
                    ed.WriteMessage("\n[PYLOAD TRACEBACK]:\n" + msg);
                }
            }
        }

        private static IEnumerable<string> GetIronPythonSearchPathCandidates(string assemblyDir)
        {
            string projectDir = Directory.GetParent(assemblyDir) != null ? Directory.GetParent(assemblyDir).FullName : assemblyDir;

            yield return Path.Combine(assemblyDir, "Lib");
            yield return assemblyDir;
            yield return Path.Combine(projectDir, "Lib");
            yield return Path.Combine(projectDir, "bin", "Release", "net462");
            yield return Path.Combine(projectDir, "bin", "Debug", "net462");
            yield return Path.Combine(projectDir, "bin", "Release", "net47");
            yield return Path.Combine(projectDir, "bin", "Debug", "net47");
            yield return Path.Combine(projectDir, "bin", "Release", "net48");
            yield return Path.Combine(projectDir, "bin", "Debug", "net48");
        }
    }
}
