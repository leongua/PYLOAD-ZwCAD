using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ObjectId GetNamedObjectsDictionaryId()
        {
            return _db.NamedObjectsDictionaryId;
        }

        public ObjectId GetModelSpaceRecordId()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                return bt[BlockTableRecord.ModelSpace];
            }
        }

        public ObjectId GetPaperSpaceRecordId()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                return bt[BlockTableRecord.PaperSpace];
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

        public Hashtable GetDictionaryInfo(ObjectId dictionaryId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
                if (dict == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica un DBDictionary");
                }

                Hashtable info = new Hashtable();
                info["id"] = dictionaryId.ToString();
                info["handle"] = dict.Handle.ToString();
                info["count"] = dict.Count;
                info["owner_id"] = dict.OwnerId.ToString();
                return info;
            }
        }

        public Hashtable GetNamedDictionaryInfo(string dictionaryPath)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId id = ResolveDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath, false);
                return GetDictionaryInfoInternal(tr, id);
            }
        }

        public ArrayList GetDictionaryEntries(ObjectId dictionaryId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
                if (dict == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica un DBDictionary");
                }

                ArrayList items = new ArrayList();
                foreach (DBDictionaryEntry entry in dict)
                {
                    DBObject dbo = tr.GetObject(entry.Value, OpenMode.ForRead);
                    Hashtable item = new Hashtable();
                    item["key"] = entry.Key;
                    item["id"] = entry.Value.ToString();
                    item["handle"] = dbo.Handle.ToString();
                    item["type"] = dbo.GetType().Name;
                    item["dxf_name"] = dbo.GetRXClass().DxfName;
                    items.Add(item);
                }
                return items;
            }
        }

        public ObjectId[] GetDictionaryEntryIds(ObjectId dictionaryId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
                if (dict == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica un DBDictionary");
                }

                List<ObjectId> ids = new List<ObjectId>();
                foreach (DBDictionaryEntry entry in dict)
                {
                    ids.Add(entry.Value);
                }
                return ids.ToArray();
            }
        }

        public ArrayList GetNamedDictionaryEntries(string dictionaryPath)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId id = ResolveDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath, false);
                return GetDictionaryEntriesInternal(tr, id);
            }
        }

        public bool DictionaryContains(ObjectId dictionaryId, string key)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
                if (dict == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica un DBDictionary");
                }

                return dict.Contains(key);
            }
        }

        public bool NamedDictionaryContains(string dictionaryPath, string key)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId id = ResolveDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath, false);
                DBDictionary dict = tr.GetObject(id, OpenMode.ForRead) as DBDictionary;
                return dict != null && dict.Contains(key);
            }
        }

        public void DeleteDictionaryEntry(ObjectId dictionaryId, string key, bool eraseObject)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForWrite) as DBDictionary;
                if (dict == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica un DBDictionary");
                }
                if (!dict.Contains(key))
                {
                    tr.Commit();
                    return;
                }

                DBObject obj = tr.GetObject(dict.GetAt(key), OpenMode.ForWrite);
                dict.Remove(key);
                if (eraseObject && obj != null && !obj.IsErased)
                {
                    obj.Erase(true);
                }
                tr.Commit();
            }
        }

        public void DeleteNamedDictionaryEntry(string dictionaryPath, string key, bool eraseObject)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId id = ResolveDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath, false);
                DBDictionary dict = tr.GetObject(id, OpenMode.ForWrite) as DBDictionary;
                if (dict != null && dict.Contains(key))
                {
                    DBObject obj = tr.GetObject(dict.GetAt(key), OpenMode.ForWrite);
                    dict.Remove(key);
                    if (eraseObject && obj != null && !obj.IsErased)
                    {
                        obj.Erase(true);
                    }
                }
                tr.Commit();
            }
        }

        public void SetXRecordData(ObjectId dictionaryId, string key, IList typedValues)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForWrite) as DBDictionary;
                if (dict == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica un DBDictionary");
                }

                Xrecord record;
                if (dict.Contains(key))
                {
                    record = tr.GetObject(dict.GetAt(key), OpenMode.ForWrite) as Xrecord;
                    if (record == null)
                    {
                        throw new InvalidOperationException("La chiave esiste ma non punta a un Xrecord");
                    }
                }
                else
                {
                    record = new Xrecord();
                    dict.SetAt(key, record);
                    tr.AddNewlyCreatedDBObject(record, true);
                }

                record.Data = BuildResultBuffer(typedValues);
                tr.Commit();
            }
        }

        public Hashtable GetXRecordData(ObjectId dictionaryId, string key)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
                if (dict == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica un DBDictionary");
                }

                Hashtable info = new Hashtable();
                info["key"] = key;
                ArrayList values = new ArrayList();
                info["values"] = values;

                if (!dict.Contains(key))
                {
                    info["count"] = 0;
                    return info;
                }

                Xrecord record = tr.GetObject(dict.GetAt(key), OpenMode.ForRead) as Xrecord;
                if (record == null || record.Data == null)
                {
                    info["count"] = 0;
                    return info;
                }

                foreach (TypedValue tv in record.Data)
                {
                    Hashtable item = new Hashtable();
                    item["type_code"] = tv.TypeCode;
                    item["value"] = tv.Value;
                    values.Add(item);
                }

                info["count"] = values.Count;
                return info;
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

        public Hashtable GetNamedXRecord(string dictionaryPath, string key)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId dictId = ResolveDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath, false);
                return GetXRecordDataInternal(tr, dictId, key);
            }
        }

        public void SetNamedStringMap(string dictionaryPath, string key, Hashtable values)
        {
            SetNamedXRecord(dictionaryPath, key, BuildStringMapTypedValues(values));
        }

        public Hashtable GetNamedStringMap(string dictionaryPath, string key)
        {
            Hashtable xrec = GetNamedXRecord(dictionaryPath, key);
            return ParseStringMapFromXRecord(xrec);
        }

        public void DeleteNamedXRecord(string dictionaryPath, string key, bool eraseObject)
        {
            DeleteNamedDictionaryEntry(dictionaryPath, key, eraseObject);
        }

        public ObjectId EnsureEntityExtensionDictionary(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                if (entity.ExtensionDictionary.IsNull)
                {
                    entity.CreateExtensionDictionary();
                }

                tr.Commit();
                return entity.ExtensionDictionary;
            }
        }

        public Hashtable GetExtensionDictionaryInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                Hashtable info = new Hashtable();
                info["has_extension_dictionary"] = !entity.ExtensionDictionary.IsNull;
                if (!entity.ExtensionDictionary.IsNull)
                {
                    DBDictionary dict = tr.GetObject(entity.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
                    info["dictionary_id"] = entity.ExtensionDictionary.ToString();
                    info["count"] = dict == null ? 0 : dict.Count;
                }
                return info;
            }
        }

        public ArrayList GetEntityExtensionDictionaryEntriesAtPath(ObjectId entityId, string subDictionaryPath)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null || entity.ExtensionDictionary.IsNull)
                {
                    return new ArrayList();
                }

                ObjectId dictId = ResolveDictionaryPath(tr, entity.ExtensionDictionary, subDictionaryPath, false);
                return GetDictionaryEntriesInternal(tr, dictId);
            }
        }

        public ArrayList GetEntityExtensionDictionaryEntries(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                if (entity.ExtensionDictionary.IsNull)
                {
                    return new ArrayList();
                }

                return GetDictionaryEntriesInternal(tr, entity.ExtensionDictionary);
            }
        }

        public void SetEntityXRecord(ObjectId entityId, string subDictionaryPath, string key, IList typedValues)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                if (entity.ExtensionDictionary.IsNull)
                {
                    entity.CreateExtensionDictionary();
                }

                ObjectId dictId = EnsureDictionaryPath(tr, entity.ExtensionDictionary, subDictionaryPath);
                DBDictionary dict = tr.GetObject(dictId, OpenMode.ForWrite) as DBDictionary;
                SetXRecordDataInternal(tr, dict, key, typedValues);
                tr.Commit();
            }
        }

        public Hashtable GetEntityXRecord(ObjectId entityId, string subDictionaryPath, string key)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                if (entity.ExtensionDictionary.IsNull)
                {
                    Hashtable empty = new Hashtable();
                    empty["key"] = key;
                    empty["count"] = 0;
                    empty["values"] = new ArrayList();
                    return empty;
                }

                ObjectId dictId = ResolveDictionaryPath(tr, entity.ExtensionDictionary, subDictionaryPath, false);
                return GetXRecordDataInternal(tr, dictId, key);
            }
        }

        public void SetEntityStringMap(ObjectId entityId, string subDictionaryPath, string key, Hashtable values)
        {
            SetEntityXRecord(entityId, subDictionaryPath, key, BuildStringMapTypedValues(values));
        }

        public Hashtable GetEntityStringMap(ObjectId entityId, string subDictionaryPath, string key)
        {
            Hashtable xrec = GetEntityXRecord(entityId, subDictionaryPath, key);
            return ParseStringMapFromXRecord(xrec);
        }

        public void CopyXRecordBetweenNamedDictionaries(string sourceDictionaryPath, string sourceKey, string targetDictionaryPath, string targetKey, bool overwrite)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId sourceId = ResolveDictionaryPath(tr, _db.NamedObjectsDictionaryId, sourceDictionaryPath, false);
                ObjectId targetId = EnsureDictionaryPath(tr, _db.NamedObjectsDictionaryId, targetDictionaryPath);
                tr.Commit();
                CopyXRecordBetweenDictionaries(sourceId, sourceKey, targetId, targetKey, overwrite);
            }
        }

        public ArrayList ListNamedDictionaryTree(string dictionaryPath, int maxDepth)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId rootId = ResolveDictionaryPath(tr, _db.NamedObjectsDictionaryId, dictionaryPath, false);
                ArrayList items = new ArrayList();
                WalkDictionaryTree(tr, rootId, NormalizeDictionaryPath(dictionaryPath), 0, Math.Max(0, maxDepth), items);
                return items;
            }
        }

        public void DeleteEntityXRecord(ObjectId entityId, string subDictionaryPath, string key, bool eraseObject)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null || entity.ExtensionDictionary.IsNull)
                {
                    tr.Commit();
                    return;
                }

                ObjectId dictId = ResolveDictionaryPath(tr, entity.ExtensionDictionary, subDictionaryPath, false);
                DBDictionary dict = tr.GetObject(dictId, OpenMode.ForWrite) as DBDictionary;
                if (dict != null && dict.Contains(key))
                {
                    DBObject dbo = tr.GetObject(dict.GetAt(key), OpenMode.ForWrite);
                    dict.Remove(key);
                    if (eraseObject && dbo != null && !dbo.IsErased)
                    {
                        dbo.Erase(true);
                    }
                }
                tr.Commit();
            }
        }

        public ObjectId[] CloneObjectsToOwner(IList objectIds, ObjectId ownerId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject owner = tr.GetObject(ownerId, OpenMode.ForRead);
                if (owner == null)
                {
                    throw new ArgumentException("ownerId non valido");
                }
                List<ObjectId> created = new List<ObjectId>();
                BlockTableRecord ownerBtr = owner as BlockTableRecord;
                if (ownerBtr != null)
                {
                    ownerBtr.UpgradeOpen();
                    foreach (object raw in objectIds)
                    {
                        if (!(raw is ObjectId))
                        {
                            continue;
                        }

                        DBObject source = tr.GetObject((ObjectId)raw, OpenMode.ForRead);
                        Entity entity = source as Entity;
                        if (entity == null)
                        {
                            continue;
                        }

                        Entity clone = entity.Clone() as Entity;
                        if (clone == null)
                        {
                            continue;
                        }

                        ObjectId newId = ownerBtr.AppendEntity(clone);
                        tr.AddNewlyCreatedDBObject(clone, true);
                        created.Add(newId);
                    }
                }
                else
                {
                    throw new NotSupportedException("CloneObjectsToOwner al momento supporta clone sicuro solo verso BlockTableRecord");
                }

                tr.Commit();
                return created.ToArray();
            }
        }

        public void CopyXRecordBetweenDictionaries(ObjectId sourceDictionaryId, string sourceKey, ObjectId targetDictionaryId, string targetKey, bool overwrite)
        {
            Hashtable data = GetXRecordData(sourceDictionaryId, sourceKey);
            int count = Convert.ToInt32(data["count"]);
            if (count == 0)
            {
                return;
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary target = tr.GetObject(targetDictionaryId, OpenMode.ForWrite) as DBDictionary;
                if (target == null)
                {
                    throw new ArgumentException("targetDictionaryId non identifica un DBDictionary");
                }
                if (target.Contains(targetKey))
                {
                    if (!overwrite)
                    {
                        tr.Commit();
                        return;
                    }

                    DBObject old = tr.GetObject(target.GetAt(targetKey), OpenMode.ForWrite);
                    target.Remove(targetKey);
                    if (old != null && !old.IsErased)
                    {
                        old.Erase(true);
                    }
                }

                SetXRecordDataInternal(tr, target, targetKey, (IList)data["values"]);
                tr.Commit();
            }
        }

        private static ResultBuffer BuildResultBuffer(IList typedValues)
        {
            List<TypedValue> values = new List<TypedValue>();
            foreach (object raw in typedValues)
            {
                Hashtable item = raw as Hashtable;
                if (item == null)
                {
                    continue;
                }

                short code = Convert.ToInt16(item["type_code"]);
                object value = item["value"];
                values.Add(new TypedValue(code, value));
            }

            return new ResultBuffer(values.ToArray());
        }

        private void SetXRecordDataInternal(Transaction tr, DBDictionary dict, string key, IList typedValues)
        {
            Xrecord record;
            if (dict.Contains(key))
            {
                record = tr.GetObject(dict.GetAt(key), OpenMode.ForWrite) as Xrecord;
                if (record == null)
                {
                    throw new InvalidOperationException("La chiave esiste ma non punta a un Xrecord");
                }
            }
            else
            {
                record = new Xrecord();
                dict.SetAt(key, record);
                tr.AddNewlyCreatedDBObject(record, true);
            }

            record.Data = BuildResultBuffer(typedValues);
        }

        private Hashtable GetXRecordDataInternal(Transaction tr, ObjectId dictionaryId, string key)
        {
            DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
            if (dict == null)
            {
                throw new ArgumentException("dictionaryId non identifica un DBDictionary");
            }

            Hashtable info = new Hashtable();
            info["key"] = key;
            ArrayList values = new ArrayList();
            info["values"] = values;

            if (!dict.Contains(key))
            {
                info["count"] = 0;
                return info;
            }

            Xrecord record = tr.GetObject(dict.GetAt(key), OpenMode.ForRead) as Xrecord;
            if (record == null || record.Data == null)
            {
                info["count"] = 0;
                return info;
            }

            foreach (TypedValue tv in record.Data)
            {
                Hashtable item = new Hashtable();
                item["type_code"] = tv.TypeCode;
                item["value"] = tv.Value;
                values.Add(item);
            }

            info["count"] = values.Count;
            return info;
        }

        private Hashtable GetDictionaryInfoInternal(Transaction tr, ObjectId dictionaryId)
        {
            DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
            if (dict == null)
            {
                throw new ArgumentException("L'ObjectId non identifica un DBDictionary");
            }

            Hashtable info = new Hashtable();
            info["id"] = dictionaryId.ToString();
            info["handle"] = dict.Handle.ToString();
            info["count"] = dict.Count;
            info["owner_id"] = dict.OwnerId.ToString();
            return info;
        }

        private ObjectId EnsureDictionaryPath(Transaction tr, ObjectId rootDictionaryId, string dictionaryPath)
        {
            return ResolveDictionaryPath(tr, rootDictionaryId, dictionaryPath, true);
        }

        private ObjectId ResolveDictionaryPath(Transaction tr, ObjectId rootDictionaryId, string dictionaryPath, bool createMissing)
        {
            if (string.IsNullOrWhiteSpace(dictionaryPath))
            {
                return rootDictionaryId;
            }

            string[] parts = dictionaryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            ObjectId currentId = rootDictionaryId;
            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                DBDictionary current = tr.GetObject(currentId, createMissing ? OpenMode.ForWrite : OpenMode.ForRead) as DBDictionary;
                if (current == null)
                {
                    throw new InvalidOperationException("Percorso dizionario non valido");
                }

                if (!current.Contains(part))
                {
                    if (!createMissing)
                    {
                        throw new ArgumentException("Dizionario non trovato: " + dictionaryPath);
                    }

                    DBDictionary child = new DBDictionary();
                    current.SetAt(part, child);
                    tr.AddNewlyCreatedDBObject(child, true);
                    currentId = child.ObjectId;
                }
                else
                {
                    currentId = current.GetAt(part);
                }
            }

            return currentId;
        }

        private static string NormalizeDictionaryPath(string dictionaryPath)
        {
            if (string.IsNullOrWhiteSpace(dictionaryPath))
            {
                return string.Empty;
            }

            return dictionaryPath.Replace('\\', '/').Trim('/');
        }

        private ArrayList GetDictionaryEntriesInternal(Transaction tr, ObjectId dictionaryId)
        {
            DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
            ArrayList items = new ArrayList();
            if (dict == null)
            {
                return items;
            }

            foreach (DBDictionaryEntry entry in dict)
            {
                DBObject dbo = tr.GetObject(entry.Value, OpenMode.ForRead);
                Hashtable item = new Hashtable();
                item["key"] = entry.Key;
                item["id"] = entry.Value.ToString();
                item["handle"] = dbo.Handle.ToString();
                item["type"] = dbo.GetType().Name;
                item["dxf_name"] = dbo.GetRXClass().DxfName;
                items.Add(item);
            }
            return items;
        }

        private void WalkDictionaryTree(Transaction tr, ObjectId dictionaryId, string path, int depth, int maxDepth, ArrayList items)
        {
            DBDictionary dict = tr.GetObject(dictionaryId, OpenMode.ForRead) as DBDictionary;
            if (dict == null)
            {
                return;
            }

            foreach (DBDictionaryEntry entry in dict)
            {
                DBObject dbo = tr.GetObject(entry.Value, OpenMode.ForRead);
                string nextPath = string.IsNullOrEmpty(path) ? entry.Key : (path + "/" + entry.Key);

                Hashtable item = new Hashtable();
                item["path"] = nextPath;
                item["depth"] = depth;
                item["id"] = entry.Value.ToString();
                item["type"] = dbo.GetType().Name;
                item["dxf_name"] = dbo.GetRXClass().DxfName;
                items.Add(item);

                if (depth < maxDepth && dbo is DBDictionary)
                {
                    WalkDictionaryTree(tr, entry.Value, nextPath, depth + 1, maxDepth, items);
                }
            }
        }

        private static ArrayList BuildStringMapTypedValues(Hashtable values)
        {
            ArrayList items = new ArrayList();
            foreach (DictionaryEntry entry in values)
            {
                Hashtable keyItem = new Hashtable();
                keyItem["type_code"] = 1000;
                keyItem["value"] = Convert.ToString(entry.Key);
                items.Add(keyItem);

                Hashtable valueItem = new Hashtable();
                valueItem["type_code"] = 1000;
                valueItem["value"] = Convert.ToString(entry.Value);
                items.Add(valueItem);
            }
            return items;
        }

        private static Hashtable ParseStringMapFromXRecord(Hashtable xrec)
        {
            Hashtable map = new Hashtable();
            map["key"] = xrec["key"];
            ArrayList values = xrec["values"] as ArrayList;
            Hashtable pairs = new Hashtable();
            if (values != null)
            {
                for (int i = 0; i + 1 < values.Count; i += 2)
                {
                    Hashtable k = values[i] as Hashtable;
                    Hashtable v = values[i + 1] as Hashtable;
                    if (k == null || v == null)
                    {
                        continue;
                    }
                    pairs[Convert.ToString(k["value"])] = Convert.ToString(v["value"]);
                }
            }
            map["pairs"] = pairs;
            map["count"] = pairs.Count;
            return map;
        }
    }
}
