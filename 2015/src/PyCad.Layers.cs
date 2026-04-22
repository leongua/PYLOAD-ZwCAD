using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PYLOAD
{
    public partial class PyCad
    {
        public bool LayerExists(string layerName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                return lt.Has(layerName);
            }
        }

        public ObjectId EnsureLayer(string layerName, short colorIndex)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(layerName))
                {
                    return lt[layerName];
                }

                lt.UpgradeOpen();
                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = layerName;
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
                ObjectId id = lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
                tr.Commit();
                return id;
            }
        }

        public string[] ListLayers()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (ObjectId id in lt)
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    names.Add(ltr.Name);
                }
                return names.ToArray();
            }
        }

        public string GetCurrentLayer()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(_db.Clayer, OpenMode.ForRead);
                return ltr.Name;
            }
        }

        public void SetCurrentLayer(string layerName)
        {
            EnsureLayer(layerName, 7);
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                _db.Clayer = lt[layerName];
                tr.Commit();
            }
        }

        public void SetLayerColor(string layerName, short colorIndex)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
                tr.Commit();
            }
        }

        public void RenameLayer(string oldName, string newName)
        {
            if (string.Equals(oldName, "0", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Il layer 0 non puo essere rinominato");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(oldName))
                {
                    throw new ArgumentException("Layer non trovato: " + oldName);
                }
                if (lt.Has(newName))
                {
                    throw new ArgumentException("Esiste gia un layer con nome: " + newName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[oldName], OpenMode.ForWrite);
                ltr.Name = newName;
                tr.Commit();
            }
        }

        public void DeleteLayer(string layerName)
        {
            if (string.Equals(layerName, "0", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Il layer 0 non puo essere eliminato");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                ObjectId layerId = lt[layerName];
                if (_db.Clayer == layerId)
                {
                    throw new ArgumentException("Non puoi eliminare il layer corrente");
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);
                ltr.Erase(true);
                tr.Commit();
            }
        }

        public void TurnLayerOn(string layerName)
        {
            SetLayerState(layerName, false);
        }

        public void TurnLayerOff(string layerName)
        {
            if (string.Equals(GetCurrentLayer(), layerName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Non puoi spegnere il layer corrente");
            }

            SetLayerState(layerName, true);
        }

        public bool IsLayerOn(string layerName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
                return !ltr.IsOff;
            }
        }

        public void LockLayer(string layerName)
        {
            SetLayerLocked(layerName, true);
        }

        public void UnlockLayer(string layerName)
        {
            SetLayerLocked(layerName, false);
        }

        public bool IsLayerLocked(string layerName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
                return ltr.IsLocked;
            }
        }

        public void FreezeLayer(string layerName)
        {
            if (string.Equals(GetCurrentLayer(), layerName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Non puoi congelare il layer corrente");
            }

            SetLayerFrozen(layerName, true);
        }

        public void ThawLayer(string layerName)
        {
            SetLayerFrozen(layerName, false);
        }

        public bool IsLayerFrozen(string layerName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
                return ltr.IsFrozen;
            }
        }

        public void SetLayerLineWeight(string layerName, int lineWeight)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                ltr.LineWeight = (LineWeight)lineWeight;
                tr.Commit();
            }
        }

        public Hashtable GetLayerInfo(string layerName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
                Hashtable info = new Hashtable();
                info["name"] = ltr.Name;
                info["is_off"] = ltr.IsOff;
                info["is_locked"] = ltr.IsLocked;
                info["is_frozen"] = ltr.IsFrozen;
                info["color_index"] = ltr.Color.ColorIndex;
                info["lineweight"] = (int)ltr.LineWeight;
                return info;
            }
        }

        public string[] ListLinetypes()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LinetypeTable ltt = (LinetypeTable)tr.GetObject(_db.LinetypeTableId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (ObjectId id in ltt)
                {
                    LinetypeTableRecord rec = (LinetypeTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    names.Add(rec.Name);
                }
                return names.ToArray();
            }
        }

        private void SetLayerState(string layerName, bool isOff)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                ltr.IsOff = isOff;
                tr.Commit();
            }
        }

        private void SetLayerLocked(string layerName, bool isLocked)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                ltr.IsLocked = isLocked;
                tr.Commit();
            }
        }

        private void SetLayerFrozen(string layerName, bool isFrozen)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    throw new ArgumentException("Layer non trovato: " + layerName);
                }

                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                ltr.IsFrozen = isFrozen;
                tr.Commit();
            }
        }
    }
}
