using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public string GetCurrentDimensionStyle()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DimStyleTableRecord rec = (DimStyleTableRecord)tr.GetObject(_db.Dimstyle, OpenMode.ForRead);
                return rec.Name;
            }
        }

        public void SetCurrentDimensionStyle(string styleName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DimStyleTable table = (DimStyleTable)tr.GetObject(_db.DimStyleTableId, OpenMode.ForRead);
                if (!table.Has(styleName))
                {
                    throw new ArgumentException("DimStyle non trovato: " + styleName);
                }
                _db.Dimstyle = table[styleName];
                tr.Commit();
            }
        }

        public ObjectId CreateDimensionStyle(string styleName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DimStyleTable table = (DimStyleTable)tr.GetObject(_db.DimStyleTableId, OpenMode.ForRead);
                if (table.Has(styleName))
                {
                    return table[styleName];
                }

                table.UpgradeOpen();
                DimStyleTableRecord rec = new DimStyleTableRecord();
                rec.Name = styleName;
                ObjectId id = table.Add(rec);
                tr.AddNewlyCreatedDBObject(rec, true);
                tr.Commit();
                return id;
            }
        }

        public void RenameDimensionStyle(string oldName, string newName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DimStyleTable table = (DimStyleTable)tr.GetObject(_db.DimStyleTableId, OpenMode.ForRead);
                if (!table.Has(oldName))
                {
                    throw new ArgumentException("DimStyle non trovato: " + oldName);
                }
                if (table.Has(newName))
                {
                    throw new ArgumentException("Esiste gia un DimStyle con nome: " + newName);
                }

                DimStyleTableRecord rec = (DimStyleTableRecord)tr.GetObject(table[oldName], OpenMode.ForWrite);
                rec.Name = newName;
                tr.Commit();
            }
        }

        public Hashtable GetDimensionStyleInfo(string styleName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DimStyleTable table = (DimStyleTable)tr.GetObject(_db.DimStyleTableId, OpenMode.ForRead);
                if (!table.Has(styleName))
                {
                    throw new ArgumentException("DimStyle non trovato: " + styleName);
                }

                DimStyleTableRecord rec = (DimStyleTableRecord)tr.GetObject(table[styleName], OpenMode.ForRead);
                Hashtable info = new Hashtable();
                info["name"] = rec.Name;
                info["dimscale"] = rec.Dimscale;
                info["dimtxt"] = rec.Dimtxt;
                info["dimasz"] = rec.Dimasz;
                info["dimexo"] = rec.Dimexo;
                info["dimdli"] = rec.Dimdli;
                info["dimdec"] = rec.Dimdec;
                return info;
            }
        }
    }
}
