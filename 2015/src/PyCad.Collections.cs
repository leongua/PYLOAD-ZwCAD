using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ObjectId[] GetModelSpaceEntityIds()
        {
            return GetBlockTableRecordEntityIds(BlockTableRecord.ModelSpace);
        }

        public ObjectId[] GetPaperSpaceEntityIds()
        {
            return GetBlockTableRecordEntityIds(BlockTableRecord.PaperSpace);
        }

        public string[] GetLayoutNames()
        {
            return ReadDictionaryNames(_db.LayoutDictionaryId);
        }

        public string[] GetTextStyleNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (ObjectId id in table)
                {
                    TextStyleTableRecord rec = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    names.Add(rec.Name);
                }
                return names.ToArray();
            }
        }

        public string[] GetDimensionStyleNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DimStyleTable table = (DimStyleTable)tr.GetObject(_db.DimStyleTableId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (ObjectId id in table)
                {
                    DimStyleTableRecord rec = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    names.Add(rec.Name);
                }
                return names.ToArray();
            }
        }

        public string[] GetGroupNames()
        {
            return ReadDictionaryNames(_db.GroupDictionaryId);
        }

        public string[] GetDictionaryNames()
        {
            return ReadDictionaryNames(_db.NamedObjectsDictionaryId);
        }

        public string[] GetRegisteredApplicationNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                RegAppTable table = (RegAppTable)tr.GetObject(_db.RegAppTableId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (ObjectId id in table)
                {
                    RegAppTableRecord rec = (RegAppTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    names.Add(rec.Name);
                }
                return names.ToArray();
            }
        }

        public string[] GetUcsNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                UcsTable table = (UcsTable)tr.GetObject(_db.UcsTableId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (ObjectId id in table)
                {
                    UcsTableRecord rec = (UcsTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    names.Add(rec.Name);
                }
                return names.ToArray();
            }
        }

        public string[] GetViewNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ViewTable table = (ViewTable)tr.GetObject(_db.ViewTableId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (ObjectId id in table)
                {
                    ViewTableRecord rec = (ViewTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    names.Add(rec.Name);
                }
                return names.ToArray();
            }
        }

        public Hashtable GetCollectionsSummary()
        {
            Hashtable info = new Hashtable();
            info["modelspace_count"] = GetModelSpaceEntityIds().Length;
            info["paperspace_count"] = GetPaperSpaceEntityIds().Length;
            info["blocks_count"] = GetBlockNames().Length;
            info["layers_count"] = ListLayers().Length;
            info["linetypes_count"] = ListLinetypes().Length;
            info["layouts_count"] = GetLayoutNames().Length;
            info["textstyles_count"] = GetTextStyleNames().Length;
            info["dimstyles_count"] = GetDimensionStyleNames().Length;
            info["groups_count"] = GetGroupNames().Length;
            info["dictionaries_count"] = GetDictionaryNames().Length;
            info["regapps_count"] = GetRegisteredApplicationNames().Length;
            info["ucs_count"] = GetUcsNames().Length;
            info["views_count"] = GetViewNames().Length;
            info["documents_count"] = GetOpenDrawings().Length;
            return info;
        }

        private ObjectId[] GetBlockTableRecordEntityIds(string recordName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[recordName], OpenMode.ForRead);
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in btr)
                {
                    ids.Add(id);
                }
                return ids.ToArray();
            }
        }

        private string[] ReadDictionaryNames(ObjectId dictionaryId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = (DBDictionary)tr.GetObject(dictionaryId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (DBDictionaryEntry entry in dict)
                {
                    names.Add(entry.Key);
                }
                return names.ToArray();
            }
        }
    }
}
