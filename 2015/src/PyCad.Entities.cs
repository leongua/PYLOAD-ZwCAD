using System;
using System.Collections;
using ZwSoft.ZwCAD.Colors;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public string GetEntityHandle(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                return entity.Handle.ToString();
            }
        }

        public string GetEntityTypeName(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                return entity.GetType().Name;
            }
        }

        public string GetEntityOwnerId(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                return entity.OwnerId.ToString();
            }
        }

        public bool IsEntityVisible(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                return entity.Visible;
            }
        }

        public void SetEntityColor(ObjectId entityId, short colorIndex)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                entity.ColorIndex = colorIndex;
                tr.Commit();
            }
        }

        public void SetEntityLayer(ObjectId entityId, string layerName)
        {
            EnsureLayer(layerName, 7);
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                entity.Layer = layerName;
                tr.Commit();
            }
        }

        public void SetEntityLineWeight(ObjectId entityId, int lineWeight)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                entity.LineWeight = (LineWeight)lineWeight;
                tr.Commit();
            }
        }

        public void SetEntityLinetype(ObjectId entityId, string linetypeName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LinetypeTable ltt = (LinetypeTable)tr.GetObject(_db.LinetypeTableId, OpenMode.ForRead);
                if (!ltt.Has(linetypeName))
                {
                    throw new ArgumentException("Linetype non trovato: " + linetypeName);
                }

                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                entity.Linetype = linetypeName;
                tr.Commit();
            }
        }

        public Hashtable SetEntitiesColorByLayer(IList entityIds)
        {
            Hashtable result = new Hashtable();
            int changed = 0;
            int skipped = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < entityIds.Count; i++)
                {
                    object raw = entityIds[i];
                    if (!(raw is ObjectId))
                    {
                        skipped++;
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)raw, OpenMode.ForWrite) as Entity;
                    if (entity == null)
                    {
                        skipped++;
                        continue;
                    }

                    entity.ColorIndex = 256;
                    changed++;
                }

                tr.Commit();
            }

            result["changed"] = changed;
            result["skipped"] = skipped;
            result["total"] = entityIds == null ? 0 : entityIds.Count;
            return result;
        }

        public int SetEntitiesLineWeight(IList entityIds, int lineWeight)
        {
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < entityIds.Count; i++)
                {
                    if (!(entityIds[i] is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)entityIds[i], OpenMode.ForWrite) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    entity.LineWeight = (LineWeight)lineWeight;
                    changed++;
                }
                tr.Commit();
            }
            return changed;
        }

        public int SetEntitiesLinetype(IList entityIds, string linetypeName)
        {
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                LinetypeTable ltt = (LinetypeTable)tr.GetObject(_db.LinetypeTableId, OpenMode.ForRead);
                if (!ltt.Has(linetypeName))
                {
                    throw new ArgumentException("Linetype non trovato: " + linetypeName);
                }

                for (int i = 0; i < entityIds.Count; i++)
                {
                    if (!(entityIds[i] is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)entityIds[i], OpenMode.ForWrite) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    entity.Linetype = linetypeName;
                    changed++;
                }
                tr.Commit();
            }
            return changed;
        }

        public Hashtable GetBoundingBox(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                Extents3d? ext = TryGetExtents(entity);
                if (!ext.HasValue)
                {
                    throw new InvalidOperationException("BoundingBox non disponibile per l'entita");
                }

                Point3d min = ext.Value.MinPoint;
                Point3d max = ext.Value.MaxPoint;

                Hashtable box = new Hashtable();
                box["min_x"] = min.X;
                box["min_y"] = min.Y;
                box["min_z"] = min.Z;
                box["max_x"] = max.X;
                box["max_y"] = max.Y;
                box["max_z"] = max.Z;
                box["size_x"] = max.X - min.X;
                box["size_y"] = max.Y - min.Y;
                box["size_z"] = max.Z - min.Z;
                box["center_x"] = (min.X + max.X) * 0.5;
                box["center_y"] = (min.Y + max.Y) * 0.5;
                box["center_z"] = (min.Z + max.Z) * 0.5;
                return box;
            }
        }

        public void SetEntityVisible(ObjectId entityId, bool visible)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                entity.Visible = visible;
                tr.Commit();
            }
        }

        public Hashtable GetEntityCommonInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = entity.Handle.ToString();
                info["type"] = entity.GetType().Name;
                info["layer"] = entity.Layer;
                info["owner_id"] = entity.OwnerId.ToString();
                info["color_index"] = entity.ColorIndex;
                info["linetype"] = entity.Linetype;
                info["lineweight"] = (int)entity.LineWeight;
                info["is_erased"] = entity.IsErased;
                info["is_visible"] = entity.Visible;

                Extents3d? ext = TryGetExtents(entity);
                if (ext.HasValue)
                {
                    info["min_x"] = ext.Value.MinPoint.X;
                    info["min_y"] = ext.Value.MinPoint.Y;
                    info["min_z"] = ext.Value.MinPoint.Z;
                    info["max_x"] = ext.Value.MaxPoint.X;
                    info["max_y"] = ext.Value.MaxPoint.Y;
                    info["max_z"] = ext.Value.MaxPoint.Z;
                }

                return info;
            }
        }

        public Hashtable GetEntityInfo(ObjectId entityId)
        {
            return GetEntityCommonInfo(entityId);
        }

        public Hashtable GetEntitiesCommonInfo(IList entityIds)
        {
            Hashtable result = new Hashtable();
            int index = 0;
            foreach (object raw in entityIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }
                result[index++] = GetEntityCommonInfo((ObjectId)raw);
            }
            return result;
        }
    }
}
