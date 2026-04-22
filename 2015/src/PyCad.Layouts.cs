using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public string GetCurrentLayoutName()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(_db.CurrentSpaceId, OpenMode.ForRead);
                Layout layout = (Layout)tr.GetObject(currentSpace.LayoutId, OpenMode.ForRead);
                return layout.LayoutName;
            }
        }

        public Hashtable GetLayoutInfo(string layoutName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBDictionary dict = (DBDictionary)tr.GetObject(_db.LayoutDictionaryId, OpenMode.ForRead);
                if (!dict.Contains(layoutName))
                {
                    throw new ArgumentException("Layout non trovato: " + layoutName);
                }

                Layout layout = (Layout)tr.GetObject(dict.GetAt(layoutName), OpenMode.ForRead);
                Hashtable info = new Hashtable();
                info["name"] = layout.LayoutName;
                info["is_model"] = layout.ModelType;
                info["tab_order"] = layout.TabOrder;
                info["block_table_record_id"] = layout.BlockTableRecordId.ToString();
                return info;
            }
        }
    }
}
