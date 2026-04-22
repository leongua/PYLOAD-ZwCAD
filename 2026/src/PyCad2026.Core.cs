using System;
using System.Collections;
using System.Globalization;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        private readonly Document _doc;
        private readonly Database _db;
        private readonly Editor _ed;
        private readonly ArrayList _shellTranscript = new ArrayList();

        public PyCad2026(Document doc, Database db, Editor ed)
        {
            _doc = doc;
            _db = db;
            _ed = ed;
        }

        public void Msg(string text)
        {
            LogShell("out", "pyload", text);
            _ed.WriteMessage("\n" + text);
        }

        public PromptPointResult GetPoint(string message)
        {
            LogShell("prompt", "editor", message);
            PromptPointResult result = _ed.GetPoint("\n" + message);
            LogShell("result", "editor", FormatPromptResult(result));
            return result;
        }

        public void ClearShellTranscript()
        {
            _shellTranscript.Clear();
        }

        public string GetBuildMarker()
        {
            return "PYLOAD2026R-FIX8";
        }

        public string GetEntityHandle(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject obj = tr.GetObject(entityId, OpenMode.ForRead, false);
                return GetHandleString(obj);
            }
        }

        public ArrayList GetShellTranscript()
        {
            return new ArrayList(_shellTranscript);
        }

        public string GetShellTranscriptText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (object raw in _shellTranscript)
            {
                Hashtable item = raw as Hashtable;
                if (item == null) continue;
                sb.Append("[");
                sb.Append(item["direction"]);
                sb.Append("][");
                sb.Append(item["channel"]);
                sb.Append("] ");
                sb.Append(item["text"]);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public string GetLastShellLine()
        {
            if (_shellTranscript.Count == 0) return string.Empty;
            Hashtable item = _shellTranscript[_shellTranscript.Count - 1] as Hashtable;
            return item == null ? string.Empty : Convert.ToString(item["text"], CultureInfo.InvariantCulture);
        }

        public void RunLisp(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return;
            LogShell("in", "lisp", expression);
            _doc.SendStringToExecute(expression + " ", true, false, false);
        }

        public void Princ(string text, bool newLine)
        {
            string final = (newLine ? "\n" : string.Empty) + (text ?? string.Empty);
            LogShell("out", "princ", final);
            _ed.WriteMessage(final);
        }

        public void RegenNative()
        {
            _ed.Regen();
        }

        public void ZoomExtents()
        {
            LogShell("in", "command", "_.ZOOM _E");
            _doc.SendStringToExecute("_.ZOOM _E ", true, false, false);
        }

        internal ObjectId AddEntity(Entity entity)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(_db);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);
                ObjectId id = ms.AppendEntity(entity);
                tr.AddNewlyCreatedDBObject(entity, true);
                tr.Commit();
                return id;
            }
        }

        internal static Hashtable NewInfo()
        {
            return new Hashtable();
        }

        internal void EnsureLayerExists(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();
                    LayerTableRecord ltr = new LayerTableRecord();
                    ltr.Name = layerName;
                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                }
                tr.Commit();
            }
        }

        internal void LogShell(string direction, string channel, string text)
        {
            Hashtable item = new Hashtable();
            item["direction"] = direction;
            item["channel"] = channel;
            item["text"] = text ?? string.Empty;
            _shellTranscript.Add(item);
        }

        internal static double DegToRad(double value) { return value * Math.PI / 180.0; }
        internal static double RadToDeg(double value) { return value * 180.0 / Math.PI; }
        internal static string GetHandleString(DBObject obj) { return obj == null ? string.Empty : obj.Handle.ToString(); }

        private static string FormatPromptResult(PromptPointResult result)
        {
            if (result == null) return "<null>";
            string text = "Status=" + result.Status;
            if (result.Status == PromptStatus.OK)
            {
                text += string.Format(CultureInfo.InvariantCulture, " Value={0},{1},{2}", result.Value.X, result.Value.Y, result.Value.Z);
            }
            return text;
        }
    }
}
