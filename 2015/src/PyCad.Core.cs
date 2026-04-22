using System;
using System.IO;
using System.Collections;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;

namespace PYLOAD
{
    public partial class PyCad
    {
        private readonly Document _doc;
        private readonly Database _db;
        private readonly Editor _ed;
        private readonly ArrayList _shellTranscript = new ArrayList();

        public PyCad(Document doc, Database db, Editor ed)
        {
            _doc = doc;
            _db = db;
            _ed = ed;
        }

        public Document doc { get { return _doc; } }
        public Database db { get { return _db; } }
        public Editor ed { get { return _ed; } }
        public string DrawingPath { get { return _doc.Name; } }
        public string DrawingName { get { return Path.GetFileName(_doc.Name); } }
        public string DrawingDirectory { get { return Path.GetDirectoryName(_doc.Name); } }
        public string DatabaseFilename { get { return _db.Filename; } }
        public bool HasFullDrawingPath { get { return !string.IsNullOrWhiteSpace(_db.Filename); } }
        public bool IsDrawingSaved { get { return !string.IsNullOrWhiteSpace(_db.Filename); } }

        public void Msg(string text)
        {
            LogShell("out", "pyload", text);
            _ed.WriteMessage("\n" + text + "\n");
        }

        public void Regen()
        {
            _ed.Regen();
        }

        public PromptPointResult GetPoint(string message)
        {
            string prompt = "\n" + message;
            LogShell("prompt", "editor", message);
            PromptPointResult result = _ed.GetPoint(prompt);
            LogShell("result", "editor", FormatPromptResult(result));
            return result;
        }

        public PromptResult GetString(string message)
        {
            PromptStringOptions pso = new PromptStringOptions("\n" + message);
            pso.AllowSpaces = true;
            LogShell("prompt", "editor", message);
            PromptResult result = _ed.GetString(pso);
            LogShell("result", "editor", FormatPromptResult(result));
            return result;
        }

        public PromptDoubleResult GetDouble(string message)
        {
            PromptDoubleOptions pdo = new PromptDoubleOptions("\n" + message);
            LogShell("prompt", "editor", message);
            PromptDoubleResult result = _ed.GetDouble(pdo);
            LogShell("result", "editor", FormatPromptResult(result));
            return result;
        }

        public PromptIntegerResult GetInteger(string message)
        {
            PromptIntegerOptions pio = new PromptIntegerOptions("\n" + message);
            LogShell("prompt", "editor", message);
            PromptIntegerResult result = _ed.GetInteger(pio);
            LogShell("result", "editor", FormatPromptResult(result));
            return result;
        }

        public PromptResult GetKeyword(string message, string keywords)
        {
            PromptKeywordOptions pko = new PromptKeywordOptions("\n" + message, keywords);
            pko.AllowNone = false;
            LogShell("prompt", "editor", message + " [" + keywords + "]");
            PromptResult result = _ed.GetKeywords(pko);
            LogShell("result", "editor", FormatPromptResult(result));
            return result;
        }

        public ArrayList GetShellTranscript()
        {
            ArrayList copy = new ArrayList();
            foreach (object item in _shellTranscript)
            {
                copy.Add(item);
            }
            return copy;
        }

        public string GetShellTranscriptText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (object raw in _shellTranscript)
            {
                Hashtable item = raw as Hashtable;
                if (item == null)
                {
                    continue;
                }

                sb.Append("[");
                sb.Append(Convert.ToString(item["direction"]));
                sb.Append("][");
                sb.Append(Convert.ToString(item["channel"]));
                sb.Append("] ");
                sb.Append(Convert.ToString(item["text"]));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void ClearShellTranscript()
        {
            _shellTranscript.Clear();
        }

        public string GetLastShellLine()
        {
            if (_shellTranscript.Count == 0)
            {
                return string.Empty;
            }

            Hashtable item = _shellTranscript[_shellTranscript.Count - 1] as Hashtable;
            if (item == null)
            {
                return string.Empty;
            }

            return Convert.ToString(item["text"]);
        }

        private void LogShell(string direction, string channel, string text)
        {
            Hashtable item = new Hashtable();
            item["direction"] = direction;
            item["channel"] = channel;
            item["text"] = text ?? string.Empty;
            item["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _shellTranscript.Add(item);
        }

        private static string FormatPromptResult(PromptResult result)
        {
            if (result == null)
            {
                return "<null>";
            }

            string text = "Status=" + result.Status;
            if (!string.IsNullOrWhiteSpace(result.StringResult))
            {
                text += " Value=" + result.StringResult;
            }
            return text;
        }

        private static string FormatPromptResult(PromptPointResult result)
        {
            if (result == null)
            {
                return "<null>";
            }

            string text = "Status=" + result.Status;
            if (result.Status == PromptStatus.OK)
            {
                text += string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    " Value={0},{1},{2}",
                    result.Value.X, result.Value.Y, result.Value.Z);
            }
            return text;
        }

        private static string FormatPromptResult(PromptDoubleResult result)
        {
            if (result == null)
            {
                return "<null>";
            }

            string text = "Status=" + result.Status;
            if (result.Status == PromptStatus.OK)
            {
                text += " Value=" + result.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            return text;
        }

        private static string FormatPromptResult(PromptIntegerResult result)
        {
            if (result == null)
            {
                return "<null>";
            }

            string text = "Status=" + result.Status;
            if (result.Status == PromptStatus.OK)
            {
                text += " Value=" + result.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            return text;
        }
    }
}
