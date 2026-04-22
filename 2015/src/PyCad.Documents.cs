using System;
using System.Collections.Generic;
using System.IO;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwApp = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PYLOAD
{
    public partial class PyCad
    {
        public string[] GetOpenDrawings()
        {
            List<string> names = new List<string>();
            foreach (Document doc in ZwApp.DocumentManager)
            {
                names.Add(doc.Name);
            }
            return names.ToArray();
        }

        public bool SaveDrawing()
        {
            _doc.Database.SaveAs(_doc.Name, DwgVersion.Current);
            return true;
        }

        public bool SaveDrawingAs(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath non valido");
            }

            string fullPath = Path.GetFullPath(filePath);
            _doc.Database.SaveAs(fullPath, DwgVersion.Current);
            return true;
        }

        public bool NewDrawing()
        {
            ZwApp.DocumentManager.Add(string.Empty);
            return true;
        }

        public bool OpenDrawing(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath non valido");
            }

            string fullPath = Path.GetFullPath(filePath);
            ZwApp.DocumentManager.Open(fullPath, false);
            return true;
        }

        public bool SwitchDrawing(string drawingNameOrPath)
        {
            if (string.IsNullOrWhiteSpace(drawingNameOrPath))
            {
                return false;
            }

            foreach (Document doc in ZwApp.DocumentManager)
            {
                if (string.Equals(doc.Name, drawingNameOrPath, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetFileName(doc.Name), drawingNameOrPath, StringComparison.OrdinalIgnoreCase))
                {
                    ZwApp.DocumentManager.MdiActiveDocument = doc;
                    return true;
                }
            }

            return false;
        }
    }
}
