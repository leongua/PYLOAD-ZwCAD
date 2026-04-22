using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Runtime;
using ZwApp = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PYLOAD2026R
{
    public class PythonLoader2026R
    {
        private static ScriptEngine _engine;
        private static ScriptScope _scope;

        [CommandMethod("PYLOAD2026R", CommandFlags.Modal)]
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
            List<string> paths = new List<string>(_engine.GetSearchPaths());
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
                ed.WriteMessage("\n[PYLOAD2026R] File non trovato: " + scriptPath);
                return;
            }

            using (DocumentLock loc = doc.LockDocument())
            {
                try
                {
                    _engine.Runtime.LoadAssembly(Assembly.Load("ZwManaged"));
                    _engine.Runtime.LoadAssembly(Assembly.Load("ZwDatabaseMgd"));

                    PyCad2026 cad = new PyCad2026(doc, db, ed);
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
                    ed.WriteMessage("\n[PYLOAD2026R TRACEBACK]:\n" + msg);
                }
            }
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
                return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
            }
        }

        private static IEnumerable<string> GetIronPythonSearchPathCandidates(string assemblyDir)
        {
            string projectDir = Directory.GetParent(assemblyDir) != null ? Directory.GetParent(assemblyDir).FullName : assemblyDir;
            yield return Path.Combine(assemblyDir, "Lib");
            yield return assemblyDir;
            yield return Path.Combine(projectDir, "Lib");
            yield return Path.Combine(projectDir, "bin", "Release", "net47");
            yield return Path.Combine(projectDir, "bin", "Debug", "net47");
            yield return Path.Combine(projectDir, "bin", "Release", "net48");
            yield return Path.Combine(projectDir, "bin", "Debug", "net48");
        }
    }
}
