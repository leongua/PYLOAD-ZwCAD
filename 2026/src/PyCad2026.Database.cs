using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public ObjectId GetModelSpaceRecordId()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                return bt[BlockTableRecord.ModelSpace];
            }
        }

        public ObjectId CreateNamedDictionary(string dictionaryPath)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId id = EnsureDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath);
                tr.Commit();
                return id;
            }
        }

        public void SetNamedXRecord(string dictionaryPath, string key, IList typedValues)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId dictId = EnsureDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath);
                DBDictionary dict = tr.GetObject(dictId, OpenMode.ForWrite) as DBDictionary;
                SetXRecordDataInternal(tr, dict, key, typedValues);
                tr.Commit();
            }
        }

        public void SetNamedStringMap(string dictionaryPath, string key, Hashtable values)
        {
            SetNamedXRecord(dictionaryPath, key, BuildStringMapTypedValues(values));
        }

        public void SetEntityStringMap(ObjectId entityId, string subDictionaryPath, string key, Hashtable values)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject obj = tr.GetObject(entityId, OpenMode.ForWrite);
                if (obj.ExtensionDictionary.IsNull) obj.CreateExtensionDictionary();
                ObjectId dictId = EnsureDictionaryPath(tr, obj.ExtensionDictionary, subDictionaryPath);
                DBDictionary dict = tr.GetObject(dictId, OpenMode.ForWrite) as DBDictionary;
                SetXRecordDataInternal(tr, dict, key, BuildStringMapTypedValues(values));
                tr.Commit();
            }
        }

        public ObjectId[] CloneObjectsToOwner(IList objectIds, ObjectId ownerId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord owner = tr.GetObject(ownerId, OpenMode.ForWrite) as BlockTableRecord;
                if (owner == null) throw new NotSupportedException("CloneObjectsToOwner 2026R supporta solo BlockTableRecord");
                ArrayList ids = new ArrayList();
                foreach (object raw in objectIds)
                {
                    if (!(raw is ObjectId)) continue;
                    Entity src = tr.GetObject((ObjectId)raw, OpenMode.ForRead) as Entity;
                    if (src == null) continue;
                    Entity clone = src.Clone() as Entity;
                    ObjectId id = owner.AppendEntity(clone);
                    tr.AddNewlyCreatedDBObject(clone, true);
                    ids.Add(id);
                }
                tr.Commit();
                ObjectId[] result = new ObjectId[ids.Count];
                ids.CopyTo(result);
                return result;
            }
        }

        public void CopyXRecordBetweenDictionaries(ObjectId sourceDictionaryId, string sourceKey, ObjectId targetDictionaryId, string targetKey, bool overwrite)
        {
            Hashtable data = GetXRecordData(sourceDictionaryId, sourceKey);
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary target = tr.GetObject(targetDictionaryId, OpenMode.ForWrite) as DBDictionary;
                SetXRecordDataInternal(tr, target, targetKey, data["values"] as IList);
                tr.Commit();
            }
        }

        public Hashtable GetXRecordData(ObjectId dictionaryId, string key)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
                Hashtable info = NewInfo();
                ArrayList values = new ArrayList();
                info["values"] = values;
                info["count"] = 0;
                if (dict == null || !dict.Contains(key)) return info;
                Xrecord xrec = tr.GetObject(dict.GetAt(key), OpenMode.ForRead) as Xrecord;
                if (xrec != null && xrec.Data != null)
                {
                    foreach (TypedValue tv in xrec.Data)
                    {
                        Hashtable item = NewInfo();
                        item["type_code"] = tv.TypeCode;
                        item["value"] = tv.Value;
                        values.Add(item);
                    }
                    info["count"] = values.Count;
                }
                return info;
            }
        }

        public ArrayList ListNamedDictionaryTree(string dictionaryPath, int maxDepth)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId root = ResolveDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath);
                ArrayList items = new ArrayList();
                WalkDictionaryTree(tr, root, NormalizeDictionaryPath(dictionaryPath), 0, maxDepth, items);
                return items;
            }
        }

        private static string NormalizeDictionaryPath(string path) { return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().Replace("\\", "/").Trim('/'); }

        private static ObjectId ResolveDictionaryPath(Transaction tr, ObjectId rootId, string path)
        {
            string[] parts = NormalizeDictionaryPath(path).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ObjectId current = rootId;
            foreach (string part in parts)
            {
                DBDictionary dict = tr.GetObject(current, OpenMode.ForRead) as DBDictionary;
                if (dict == null || !dict.Contains(part)) throw new ArgumentException("Dictionary path non trovato: " + path);
                current = dict.GetAt(part);
            }
            return current;
        }

        private static ObjectId EnsureDictionaryPath(Transaction tr, ObjectId rootId, string path)
        {
            string[] parts = NormalizeDictionaryPath(path).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ObjectId current = rootId;
            foreach (string part in parts)
            {
                DBDictionary dict = tr.GetObject(current, OpenMode.ForWrite) as DBDictionary;
                if (!dict.Contains(part))
                {
                    DBDictionary child = new DBDictionary();
                    current = dict.SetAt(part, child);
                    tr.AddNewlyCreatedDBObject(child, true);
                }
                else current = dict.GetAt(part);
            }
            return current;
        }

        private static IList BuildStringMapTypedValues(Hashtable values)
        {
            ArrayList items = new ArrayList();
            if (values == null) return items;
            foreach (DictionaryEntry de in values)
            {
                Hashtable k = new Hashtable(); k["type_code"] = 1000; k["value"] = Convert.ToString(de.Key); items.Add(k);
                Hashtable v = new Hashtable(); v["type_code"] = 1000; v["value"] = Convert.ToString(de.Value); items.Add(v);
            }
            return items;
        }

        private static void SetXRecordDataInternal(Transaction tr, DBDictionary dict, string key, IList typedValues)
        {
            ResultBuffer rb = new ResultBuffer();
            if (typedValues != null)
            {
                foreach (object raw in typedValues)
                {
                    Hashtable item = raw as Hashtable;
                    if (item == null) continue;
                    rb.Add(new TypedValue(Convert.ToInt16(item["type_code"]), item["value"]));
                }
            }
            Xrecord xrec;
            if (dict.Contains(key)) xrec = tr.GetObject(dict.GetAt(key), OpenMode.ForWrite) as Xrecord;
            else
            {
                xrec = new Xrecord();
                dict.SetAt(key, xrec);
                tr.AddNewlyCreatedDBObject(xrec, true);
            }
            xrec.Data = rb;
        }

        private static void WalkDictionaryTree(Transaction tr, ObjectId dictId, string path, int depth, int maxDepth, ArrayList items)
        {
            DBDictionary dict = tr.GetObject(dictId, OpenMode.ForRead) as DBDictionary;
            if (dict == null) return;
            foreach (DBDictionaryEntry entry in dict)
            {
                Hashtable item = new Hashtable();
                item["path"] = string.IsNullOrWhiteSpace(path) ? entry.Key : (path + "/" + entry.Key);
                item["depth"] = depth;
                item["id"] = entry.Value;
                items.Add(item);
                if (depth < maxDepth) WalkDictionaryTree(tr, entry.Value, Convert.ToString(item["path"]), depth + 1, maxDepth, items);
            }
        }
    }
}
