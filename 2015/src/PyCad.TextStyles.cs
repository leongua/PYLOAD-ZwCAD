using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public string GetCurrentTextStyle()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                TextStyleTableRecord rec = (TextStyleTableRecord)tr.GetObject(_db.Textstyle, OpenMode.ForRead);
                return rec.Name;
            }
        }

        public ObjectId CreateTextStyle(string styleName, string fontFile, double textSize, double xScale, double obliqueAngleDegrees)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                if (table.Has(styleName))
                {
                    return table[styleName];
                }

                table.UpgradeOpen();
                TextStyleTableRecord rec = new TextStyleTableRecord();
                rec.Name = styleName;
                if (!string.IsNullOrWhiteSpace(fontFile))
                {
                    rec.FileName = fontFile;
                }
                rec.TextSize = textSize;
                rec.XScale = xScale;
                rec.ObliquingAngle = DegreesToRadians(obliqueAngleDegrees);
                ObjectId id = table.Add(rec);
                tr.AddNewlyCreatedDBObject(rec, true);
                tr.Commit();
                return id;
            }
        }

        public void RenameTextStyle(string oldName, string newName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                if (!table.Has(oldName))
                {
                    throw new ArgumentException("TextStyle non trovato: " + oldName);
                }
                if (table.Has(newName))
                {
                    throw new ArgumentException("Esiste gia un TextStyle con nome: " + newName);
                }

                TextStyleTableRecord rec = (TextStyleTableRecord)tr.GetObject(table[oldName], OpenMode.ForWrite);
                rec.Name = newName;
                tr.Commit();
            }
        }

        public void SetCurrentTextStyle(string styleName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                if (!table.Has(styleName))
                {
                    throw new ArgumentException("TextStyle non trovato: " + styleName);
                }
                _db.Textstyle = table[styleName];
                tr.Commit();
            }
        }

        public Hashtable GetTextStyleInfo(string styleName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                if (!table.Has(styleName))
                {
                    throw new ArgumentException("TextStyle non trovato: " + styleName);
                }

                TextStyleTableRecord rec = (TextStyleTableRecord)tr.GetObject(table[styleName], OpenMode.ForRead);
                Hashtable info = new Hashtable();
                info["name"] = rec.Name;
                info["font_file"] = rec.FileName;
                info["bigfont_file"] = rec.BigFontFileName;
                info["text_size"] = rec.TextSize;
                info["x_scale"] = rec.XScale;
                info["oblique_angle"] = rec.ObliquingAngle;
                info["is_shape_file"] = rec.IsShapeFile;
                return info;
            }
        }
    }
}
