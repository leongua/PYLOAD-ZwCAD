using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwApp = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PYLOAD
{
    public partial class PyCad
    {
        public void ZoomExtents()
        {
            RunLispCommand("_.ZOOM", "_E");
        }

        public void RunCommand(string commandText)
        {
            RunCommand(commandText, true, false, false);
        }

        public void RunCommand(string commandText, bool activate, bool wrapUpInactiveDoc, bool echoCommand)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                return;
            }

            string normalized = NormalizeCommand(commandText);
            LogShell("in", "command", normalized.TrimEnd());
            _doc.SendStringToExecute(normalized, activate, wrapUpInactiveDoc, echoCommand);
        }

        public void RunCommands(IList commandTexts)
        {
            if (commandTexts == null)
            {
                return;
            }

            StringBuilder batch = new StringBuilder();
            foreach (object raw in commandTexts)
            {
                if (raw == null)
                {
                    continue;
                }

                string cmd = Convert.ToString(raw);
                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    batch.Append(NormalizeCommand(cmd));
                }
            }

            if (batch.Length > 0)
            {
                LogShell("in", "command-batch", batch.ToString().Trim());
                _doc.SendStringToExecute(batch.ToString(), true, false, false);
            }
        }

        public void SendEnter()
        {
            LogShell("in", "command", "<ENTER>");
            _doc.SendStringToExecute(" ", true, false, false);
        }

        public void CancelActiveCommand()
        {
            LogShell("in", "command", "<CANCEL>");
            _doc.SendStringToExecute("\x03\x03", true, false, false);
        }

        public void RegenNative()
        {
            RunLispCommand("_.REGEN");
        }

        public void ZoomAll()
        {
            RunLispCommand("_.ZOOM", "_A");
        }

        public void ZoomPrevious()
        {
            RunLispCommand("_.ZOOM", "_P");
        }

        public void ZoomWindow(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            RunLispCommand("_.ZOOM", "_W", FormatPoint(x1, y1, z1), FormatPoint(x2, y2, z2));
        }

        public void ZoomCenter(double x, double y, double z, double height)
        {
            RunLispCommand("_.ZOOM", "_C", FormatPoint(x, y, z), FormatNumber(height));
        }

        public void ZoomObject(ObjectId entityId)
        {
            string handle = GetEntityHandle(entityId);
            string lisp = string.Format(CultureInfo.InvariantCulture, "(command \"_.ZOOM\" \"_O\" (handent \"{0}\"))", EscapeLispString(handle));
            RunLisp(lisp);
        }

        public void Audit(bool fixErrors)
        {
            RunCommands(new ArrayList { "_.AUDIT", fixErrors ? "_Y" : "_N" });
        }

        public void AuditInteractive()
        {
            RunCommand("_.AUDIT");
        }

        public void SaveCommand()
        {
            RunCommand("_QSAVE");
        }

        public void OpenScript(string scriptFilePath)
        {
            if (string.IsNullOrWhiteSpace(scriptFilePath))
            {
                throw new ArgumentException("scriptFilePath non valido");
            }

            string escaped = scriptFilePath.Replace("\"", "\"\"");
            RunCommand("_SCRIPT \"" + escaped + "\"");
        }

        public void RunScriptFile(string scriptFilePath)
        {
            OpenScript(scriptFilePath);
        }

        public object GetVar(string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new ArgumentException("variableName non valido");
            }

            MethodInfo mi = typeof(ZwApp).GetMethod("GetSystemVariable", BindingFlags.Public | BindingFlags.Static);
            if (mi == null)
            {
                throw new NotSupportedException("GetSystemVariable non disponibile in questa API ZWCAD");
            }

            object value = mi.Invoke(null, new object[] { variableName });
            LogShell("out", "sysvar", variableName + "=" + Convert.ToString(value, CultureInfo.InvariantCulture));
            return value;
        }

        public string GetVarString(string variableName)
        {
            object value = GetVar(variableName);
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public int GetVarInt(string variableName)
        {
            object value = GetVar(variableName);
            return value == null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public double GetVarDouble(string variableName)
        {
            object value = GetVar(variableName);
            return value == null ? 0.0 : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        public bool GetVarBool(string variableName)
        {
            object value = GetVar(variableName);
            if (value == null)
            {
                return false;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            if (value is string)
            {
                string s = ((string)value).Trim();
                if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || s == "1")
                {
                    return true;
                }
                if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) || s == "0")
                {
                    return false;
                }
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
        }

        public Hashtable GetVars(IList variableNames)
        {
            Hashtable result = new Hashtable();
            if (variableNames == null)
            {
                return result;
            }

            foreach (object raw in variableNames)
            {
                string name = raw == null ? string.Empty : Convert.ToString(raw, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result[name] = GetVar(name);
                }
            }
            return result;
        }

        public void SetVar(string variableName, object value)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new ArgumentException("variableName non valido");
            }

            MethodInfo mi = typeof(ZwApp).GetMethod("SetSystemVariable", BindingFlags.Public | BindingFlags.Static);
            if (mi == null)
            {
                throw new NotSupportedException("SetSystemVariable non disponibile in questa API ZWCAD");
            }

            LogShell("in", "sysvar", variableName + "=" + Convert.ToString(value, CultureInfo.InvariantCulture));
            mi.Invoke(null, new object[] { variableName, value });
        }

        public void SetVars(IDictionary variables)
        {
            if (variables == null)
            {
                return;
            }

            foreach (DictionaryEntry item in variables)
            {
                string name = item.Key == null ? string.Empty : Convert.ToString(item.Key, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    SetVar(name, item.Value);
                }
            }
        }

        public void RunLisp(string lispExpression)
        {
            if (string.IsNullOrWhiteSpace(lispExpression))
            {
                return;
            }

            string expr = lispExpression.Trim();
            string wrapped = expr.EndsWith("(princ)", StringComparison.OrdinalIgnoreCase)
                ? expr
                : "(progn " + expr + " (princ))";

            if (ShouldRunLispDirect(wrapped))
            {
                RunCommand(wrapped);
                return;
            }

            RunLispViaTempScript(wrapped);
        }

        public void LoadLispFile(string lspFilePath)
        {
            if (string.IsNullOrWhiteSpace(lspFilePath))
            {
                throw new ArgumentException("lspFilePath non valido");
            }

            LoadLispFileCore(lspFilePath);
        }

        public void RunLispFile(string lspFilePath)
        {
            LoadLispFile(lspFilePath);
        }

        public void CallLisp(string functionName, IList args)
        {
            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new ArgumentException("functionName non valido");
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            sb.Append(functionName.Trim());
            if (args != null)
            {
                foreach (object raw in args)
                {
                    sb.Append(" ");
                    sb.Append(FormatLispAtom(raw));
                }
            }
            sb.Append(")");
            RunLisp(sb.ToString());
        }

        public void Princ(string text)
        {
            Princ(text, false);
        }

        public void Princ(string text, bool appendNewLine)
        {
            string value = text ?? string.Empty;
            if (appendNewLine)
            {
                value = Environment.NewLine + value;
            }

            LogShell("out", "princ", value);
            _ed.WriteMessage(value);
        }

        public void Command(IList commandArgs)
        {
            if (commandArgs == null || commandArgs.Count == 0)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("(command");
            foreach (object raw in commandArgs)
            {
                string arg = raw == null ? string.Empty : Convert.ToString(raw, CultureInfo.InvariantCulture);
                sb.Append(" \"");
                sb.Append(EscapeLispString(arg));
                sb.Append("\"");
            }
            sb.Append(")");
            RunLisp(sb.ToString());
        }

        public string ExportShellTranscript(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath non valido");
            }

            string fullPath = Path.GetFullPath(filePath);
            string content = GetShellTranscriptText();

            try
            {
                File.WriteAllText(fullPath, content, Encoding.UTF8);
                LogShell("out", "transcript", fullPath);
                return fullPath;
            }
            catch (UnauthorizedAccessException)
            {
                string fallback = BuildUniqueFallbackPath(fullPath);
                File.WriteAllText(fallback, content, Encoding.UTF8);
                LogShell("out", "transcript", fallback);
                return fallback;
            }
            catch (IOException)
            {
                string fallback = BuildUniqueFallbackPath(fullPath);
                File.WriteAllText(fallback, content, Encoding.UTF8);
                LogShell("out", "transcript", fallback);
                return fallback;
            }
        }

        private static string NormalizeCommand(string commandText)
        {
            return commandText.EndsWith(" ", StringComparison.Ordinal) ? commandText : commandText + " ";
        }

        private static bool ShouldRunLispDirect(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return true;
            }

            if (expression.IndexOf('\r') >= 0 || expression.IndexOf('\n') >= 0)
            {
                return false;
            }

            return expression.Length <= 220;
        }

        private void RunLispViaTempScript(string lispExpression)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "PYLOAD_LISP");
            Directory.CreateDirectory(tempDir);

            string lspPath = Path.Combine(tempDir, "pyload_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture) + ".lsp");
            File.WriteAllText(lspPath, lispExpression + Environment.NewLine, Encoding.UTF8);

            LogShell("in", "lisp-file", lspPath);
            LoadLispFileCore(lspPath);
        }

        private void RunLispCommand(params string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("(command");
            foreach (string arg in args)
            {
                sb.Append(" \"");
                sb.Append(EscapeLispString(arg ?? string.Empty));
                sb.Append("\"");
            }
            sb.Append(")");
            RunLisp(sb.ToString());
        }

        private void LoadLispFileCore(string lspFilePath)
        {
            string fullPath = Path.GetFullPath(lspFilePath);
            string escaped = EscapeLispString(fullPath);
            RunCommand("(load \"" + escaped + "\")");
        }

        private static string EscapeLispString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string FormatPoint(double x, double y, double z)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", x, y, z);
        }

        private static string FormatNumber(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatLispAtom(object value)
        {
            if (value == null)
            {
                return "nil";
            }

            if (value is bool)
            {
                return (bool)value ? "T" : "nil";
            }

            if (value is string)
            {
                return BuildLispStringExpression((string)value);
            }

            if (value is char)
            {
                return BuildLispStringExpression(value.ToString());
            }

            if (value is IFormattable)
            {
                return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            }

            return "\"" + EscapeLispString(Convert.ToString(value, CultureInfo.InvariantCulture)) + "\"";
        }

        private static string BuildLispStringExpression(string value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            List<string> parts = new List<string>();
            StringBuilder chunk = new StringBuilder();

            Action flushChunk = () =>
            {
                if (chunk.Length > 0)
                {
                    parts.Add("\"" + EscapeLispString(chunk.ToString()) + "\"");
                    chunk.Length = 0;
                }
            };

            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '\r':
                        flushChunk();
                        parts.Add("(chr 13)");
                        break;
                    case '\n':
                        flushChunk();
                        parts.Add("(chr 10)");
                        break;
                    case '\t':
                        flushChunk();
                        parts.Add("(chr 9)");
                        break;
                    default:
                        chunk.Append(ch);
                        break;
                }
            }

            flushChunk();

            if (parts.Count == 0)
            {
                return "\"\"";
            }

            if (parts.Count == 1)
            {
                return parts[0];
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("(strcat");
            foreach (string part in parts)
            {
                sb.Append(" ");
                sb.Append(part);
            }
            sb.Append(")");
            return sb.ToString();
        }

        private static string BuildUniqueFallbackPath(string originalPath)
        {
            string directory = Path.GetDirectoryName(originalPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            string suffix = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            return Path.Combine(directory, fileNameWithoutExtension + "_" + suffix + extension);
        }
    }
}
