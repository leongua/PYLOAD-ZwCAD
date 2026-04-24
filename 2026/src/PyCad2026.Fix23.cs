using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public void RunCommandNoiseFree(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText)) return;
            string normalized = commandText.EndsWith(" ", StringComparison.Ordinal) ? commandText : commandText + " ";
            _doc.SendStringToExecute(normalized, true, false, false);
        }

        public void RunCommandsNoiseFree(IList commandTexts)
        {
            if (commandTexts == null) return;
            StringBuilder batch = new StringBuilder();
            foreach (object raw in commandTexts)
            {
                if (raw == null) continue;
                string cmd = Convert.ToString(raw, CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(cmd)) continue;
                batch.Append(cmd.EndsWith(" ", StringComparison.Ordinal) ? cmd : cmd + " ");
            }
            if (batch.Length == 0) return;
            _doc.SendStringToExecute(batch.ToString(), true, false, false);
        }

        public void FlushCommandChannel()
        {
            FlushCommandChannel(2, 2);
        }

        public void FlushCommandChannel(int cancelCount, int enterCount)
        {
            int c = Math.Max(0, cancelCount);
            int e = Math.Max(0, enterCount);
            for (int i = 0; i < c; i++) _doc.SendStringToExecute("\x03", true, false, false);
            for (int i = 0; i < e; i++) _doc.SendStringToExecute(" ", true, false, false);
        }

        public void RunLispQuiet(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return;
            string text = expression.EndsWith(" ", StringComparison.Ordinal) ? expression : expression + " ";
            _doc.SendStringToExecute(text, true, false, false);
        }

        public void CommandSilent(IList commandArgs)
        {
            if (commandArgs == null || commandArgs.Count == 0) return;
            StringBuilder sb = new StringBuilder();
            sb.Append("(command");
            foreach (object raw in commandArgs)
            {
                string arg = raw == null ? string.Empty : Convert.ToString(raw, CultureInfo.InvariantCulture);
                sb.Append(" \"").Append(EscapeLispStringFix23(arg)).Append("\"");
            }
            sb.Append(")");
            RunLispQuiet(sb.ToString());
        }

        public string[] GetLayerNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(_db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt == null) return new string[0];
                List<string> names = new List<string>();
                foreach (ObjectId id in lt)
                {
                    LayerTableRecord ltr = tr.GetObject(id, OpenMode.ForRead, false) as LayerTableRecord;
                    if (ltr == null) continue;
                    names.Add(ltr.Name);
                }
                names.Sort(StringComparer.OrdinalIgnoreCase);
                return names.ToArray();
            }
        }

        public string[] GetLayoutTabNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary layouts = tr.GetObject(_db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (layouts == null) return new string[0];
                List<string> names = new List<string>();
                foreach (DBDictionaryEntry de in layouts)
                {
                    names.Add(de.Key);
                }
                names.Sort(StringComparer.OrdinalIgnoreCase);
                return names.ToArray();
            }
        }

        public Hashtable GetLayerEntityCounts()
        {
            return GetLayerEntityCounts(false);
        }

        public Hashtable GetLayerEntityCounts(bool onlyModelSpace)
        {
            Hashtable counts = NewInfo();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false) as BlockTableRecord;
                    if (btr == null || !btr.IsLayout) continue;
                    if (onlyModelSpace && !string.Equals(btr.Name, BlockTableRecord.ModelSpace, StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (ObjectId entId in btr)
                    {
                        Entity e = tr.GetObject(entId, OpenMode.ForRead, false) as Entity;
                        if (e == null || e.IsErased) continue;
                        string key = e.Layer ?? string.Empty;
                        counts[key] = (counts.ContainsKey(key) ? Convert.ToInt32(counts[key], CultureInfo.InvariantCulture) : 0) + 1;
                    }
                }
            }
            return counts;
        }

        public Hashtable GetDxfEntityCountsBySpace(string spaceName)
        {
            Hashtable counts = NewInfo();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = ResolveSpaceRecordFix23(tr, spaceName);
                if (btr == null) return counts;
                foreach (ObjectId entId in btr)
                {
                    Entity e = tr.GetObject(entId, OpenMode.ForRead, false) as Entity;
                    if (e == null || e.IsErased) continue;
                    string dxf = e.GetRXClass().DxfName;
                    counts[dxf] = (counts.ContainsKey(dxf) ? Convert.ToInt32(counts[dxf], CultureInfo.InvariantCulture) : 0) + 1;
                }
            }
            return counts;
        }

        public int MoveEntitiesToLayer(IList entityIds, string layerName)
        {
            if (entityIds == null || string.IsNullOrWhiteSpace(layerName)) return 0;
            EnsureLayerExists(layerName);
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId)) continue;
                    Entity e = tr.GetObject((ObjectId)raw, OpenMode.ForWrite, false) as Entity;
                    if (e == null || e.IsErased) continue;
                    e.Layer = layerName;
                    changed++;
                }
                tr.Commit();
            }
            return changed;
        }

        public int MoveEntitiesByDxfToLayer(string dxfType, string layerName, bool onlyModelSpace)
        {
            if (string.IsNullOrWhiteSpace(dxfType) || string.IsNullOrWhiteSpace(layerName)) return 0;
            EnsureLayerExists(layerName);
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false) as BlockTableRecord;
                    if (btr == null || !btr.IsLayout) continue;
                    if (onlyModelSpace && !string.Equals(btr.Name, BlockTableRecord.ModelSpace, StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (ObjectId entId in btr)
                    {
                        Entity e = tr.GetObject(entId, OpenMode.ForWrite, false) as Entity;
                        if (e == null || e.IsErased) continue;
                        if (!string.Equals(e.GetRXClass().DxfName, dxfType, StringComparison.OrdinalIgnoreCase)) continue;
                        e.Layer = layerName;
                        changed++;
                    }
                }
                tr.Commit();
            }
            return changed;
        }

        public int MoveEntitiesByDxfToLayer(IList dxfFilters, string layerName, bool onlyModelSpace)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return 0;
            Hashtable filters = NormalizeDxfPairs(dxfFilters);
            EnsureLayerExists(layerName);
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false) as BlockTableRecord;
                    if (btr == null || !btr.IsLayout) continue;
                    if (onlyModelSpace && !string.Equals(btr.Name, BlockTableRecord.ModelSpace, StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (ObjectId entId in btr)
                    {
                        Entity e = tr.GetObject(entId, OpenMode.ForWrite, false) as Entity;
                        if (e == null || e.IsErased) continue;
                        if (!MatchesFilters(e, filters, tr)) continue;
                        e.Layer = layerName;
                        changed++;
                    }
                }
                tr.Commit();
            }
            return changed;
        }

        public int BatchSetEntityVisibility(IList entityIds, bool visible)
        {
            if (entityIds == null) return 0;
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId)) continue;
                    Entity e = tr.GetObject((ObjectId)raw, OpenMode.ForWrite, false) as Entity;
                    if (e == null || e.IsErased) continue;
                    e.Visible = visible;
                    changed++;
                }
                tr.Commit();
            }
            return changed;
        }

        public Hashtable GetBlockReferenceCountsByName()
        {
            Hashtable counts = NewInfo();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead, false) as BlockTableRecord;
                    if (btr == null || !btr.IsLayout) continue;
                    foreach (ObjectId entId in btr)
                    {
                        BlockReference br = tr.GetObject(entId, OpenMode.ForRead, false) as BlockReference;
                        if (br == null || br.IsErased) continue;
                        string name = ResolveBlockReferenceNameFix23(tr, br);
                        if (string.IsNullOrWhiteSpace(name)) name = "<unnamed>";
                        counts[name] = (counts.ContainsKey(name) ? Convert.ToInt32(counts[name], CultureInfo.InvariantCulture) : 0) + 1;
                    }
                }
            }
            return counts;
        }

        public ObjectId[] GetObjectIdsByHandleStrings(IList handleStrings)
        {
            if (handleStrings == null) return new ObjectId[0];
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in handleStrings)
                {
                    string text = raw == null ? string.Empty : Convert.ToString(raw, CultureInfo.InvariantCulture);
                    Handle handle;
                    if (!TryParseHandleFix23(text, out handle)) continue;
                    try
                    {
                        ObjectId id = _db.GetObjectId(false, handle, 0);
                        if (!id.IsNull) ids.Add(id);
                    }
                    catch
                    {
                    }
                }
            }
            return ids.ToArray();
        }

        public Hashtable GetHandleMap(IList entityIds)
        {
            Hashtable map = NewInfo();
            if (entityIds == null) return map;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId)) continue;
                    ObjectId id = (ObjectId)raw;
                    DBObject obj = tr.GetObject(id, OpenMode.ForRead, false);
                    if (obj == null) continue;
                    string handle = GetHandleString(obj);
                    if (string.IsNullOrWhiteSpace(handle)) continue;
                    map[handle] = id;
                }
            }
            return map;
        }

        public string GetCurrentLayerName()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTableRecord ltr = tr.GetObject(_db.Clayer, OpenMode.ForRead, false) as LayerTableRecord;
                return ltr == null ? string.Empty : (ltr.Name ?? string.Empty);
            }
        }

        public void SetCurrentLayerName(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName)) throw new ArgumentException("layerName non valido");
            EnsureLayerExists(layerName);
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(_db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt == null || !lt.Has(layerName)) throw new ArgumentException("layerName non trovato");
                _db.Clayer = lt[layerName];
                tr.Commit();
            }
        }

        public Hashtable GetLayerState(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName)) throw new ArgumentException("layerName non valido");
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(_db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt == null || !lt.Has(layerName)) throw new ArgumentException("layerName non trovato");
                LayerTableRecord ltr = tr.GetObject(lt[layerName], OpenMode.ForRead) as LayerTableRecord;
                Hashtable info = NewInfo();
                info["name"] = ltr.Name;
                info["is_off"] = ltr.IsOff;
                info["is_frozen"] = ltr.IsFrozen;
                info["is_locked"] = ltr.IsLocked;
                info["is_plottable"] = ltr.IsPlottable;
                info["is_hidden"] = ltr.IsHidden;
                return info;
            }
        }

        public bool SetLayerState(string layerName, Hashtable values)
        {
            if (string.IsNullOrWhiteSpace(layerName)) throw new ArgumentException("layerName non valido");
            if (values == null || values.Count == 0) return false;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(_db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt == null || !lt.Has(layerName)) throw new ArgumentException("layerName non trovato");
                LayerTableRecord ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                bool changed = false;
                changed |= SetLayerFlagFix23(values, "is_off", v => ltr.IsOff = v);
                changed |= SetLayerFlagFix23(values, "is_frozen", v => ltr.IsFrozen = v);
                changed |= SetLayerFlagFix23(values, "is_locked", v => ltr.IsLocked = v);
                changed |= SetLayerFlagFix23(values, "is_plottable", v => ltr.IsPlottable = v);
                changed |= SetLayerFlagFix23(values, "is_hidden", v => ltr.IsHidden = v);
                if (changed) tr.Commit();
                return changed;
            }
        }

        public Hashtable SetLayerStatesBatch(Hashtable layerStateMap)
        {
            Hashtable info = NewInfo();
            int changed = 0;
            int failed = 0;
            if (layerStateMap == null)
            {
                info["changed"] = 0;
                info["failed"] = 0;
                return info;
            }

            foreach (DictionaryEntry de in layerStateMap)
            {
                string layer = de.Key == null ? string.Empty : Convert.ToString(de.Key, CultureInfo.InvariantCulture);
                Hashtable values = de.Value as Hashtable;
                try
                {
                    if (SetLayerState(layer, values)) changed++;
                }
                catch
                {
                    failed++;
                }
            }

            info["changed"] = changed;
            info["failed"] = failed;
            return info;
        }

        private static bool TryParseHandleFix23(string text, out Handle handle)
        {
            handle = new Handle(0);
            string s = (text ?? string.Empty).Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            long value;
            if (!long.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)) return false;
            handle = new Handle(value);
            return true;
        }

        private static string EscapeLispStringFix23(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static bool SetLayerFlagFix23(Hashtable values, string key, Action<bool> setter)
        {
            if (!values.ContainsKey(key)) return false;
            bool flag = Convert.ToBoolean(values[key], CultureInfo.InvariantCulture);
            setter(flag);
            return true;
        }

        private BlockTableRecord ResolveSpaceRecordFix23(Transaction tr, string spaceName)
        {
            BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
            string wanted = string.IsNullOrWhiteSpace(spaceName) ? BlockTableRecord.ModelSpace : spaceName.Trim();
            foreach (ObjectId id in bt)
            {
                BlockTableRecord btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null || !btr.IsLayout) continue;
                if (string.Equals(btr.Name, wanted, StringComparison.OrdinalIgnoreCase)) return btr;
            }

            DBDictionary layouts = tr.GetObject(_db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
            if (layouts != null && layouts.Contains(wanted))
            {
                Layout lo = tr.GetObject(layouts.GetAt(wanted), OpenMode.ForRead) as Layout;
                if (lo != null)
                {
                    BlockTableRecord byLayout = tr.GetObject(lo.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    if (byLayout != null) return byLayout;
                }
            }
            return null;
        }

        private static string ResolveBlockReferenceNameFix23(Transaction tr, BlockReference br)
        {
            if (br == null) return string.Empty;
            try
            {
                BlockTableRecord btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead, false) as BlockTableRecord;
                return btr == null ? string.Empty : (btr.Name ?? string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
