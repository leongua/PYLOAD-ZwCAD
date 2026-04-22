using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public bool GroupExists(string groupName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = (DBDictionary)tr.GetObject(_db.GroupDictionaryId, OpenMode.ForRead);
                return dict.Contains(groupName);
            }
        }

        public ObjectId CreateGroup(string groupName, string description)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = (DBDictionary)tr.GetObject(_db.GroupDictionaryId, OpenMode.ForRead);
                if (dict.Contains(groupName))
                {
                    return dict.GetAt(groupName);
                }

                dict.UpgradeOpen();
                Group group = new Group(description ?? string.Empty, true);
                ObjectId id = dict.SetAt(groupName, group);
                tr.AddNewlyCreatedDBObject(group, true);
                tr.Commit();
                return id;
            }
        }

        public void DeleteGroup(string groupName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = (DBDictionary)tr.GetObject(_db.GroupDictionaryId, OpenMode.ForRead);
                if (!dict.Contains(groupName))
                {
                    throw new ArgumentException("Group non trovato: " + groupName);
                }

                Group group = (Group)tr.GetObject(dict.GetAt(groupName), OpenMode.ForWrite);
                group.Erase(true);
                tr.Commit();
            }
        }

        public void AddEntitiesToGroup(string groupName, IList entityIds)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = (DBDictionary)tr.GetObject(_db.GroupDictionaryId, OpenMode.ForRead);
                if (!dict.Contains(groupName))
                {
                    throw new ArgumentException("Group non trovato: " + groupName);
                }

                Group group = (Group)tr.GetObject(dict.GetAt(groupName), OpenMode.ForWrite);
                ObjectIdCollection ids = new ObjectIdCollection();
                foreach (object raw in entityIds)
                {
                    if (raw is ObjectId)
                    {
                        ids.Add((ObjectId)raw);
                    }
                }
                if (ids.Count > 0)
                {
                    group.Append(ids);
                }
                tr.Commit();
            }
        }

        public ObjectId[] GetGroupEntityIds(string groupName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = (DBDictionary)tr.GetObject(_db.GroupDictionaryId, OpenMode.ForRead);
                if (!dict.Contains(groupName))
                {
                    throw new ArgumentException("Group non trovato: " + groupName);
                }

                Group group = (Group)tr.GetObject(dict.GetAt(groupName), OpenMode.ForRead);
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in group.GetAllEntityIds())
                {
                    ids.Add(id);
                }
                return ids.ToArray();
            }
        }

        public Hashtable GetGroupInfo(string groupName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = (DBDictionary)tr.GetObject(_db.GroupDictionaryId, OpenMode.ForRead);
                if (!dict.Contains(groupName))
                {
                    throw new ArgumentException("Group non trovato: " + groupName);
                }

                Group group = (Group)tr.GetObject(dict.GetAt(groupName), OpenMode.ForRead);
                Hashtable info = new Hashtable();
                info["name"] = groupName;
                info["description"] = group.Description;
                info["is_selectable"] = group.Selectable;
                info["count"] = group.NumEntities;
                return info;
            }
        }
    }
}
