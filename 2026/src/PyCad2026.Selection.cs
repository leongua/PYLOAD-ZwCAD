using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public PromptEntityResult SelectEntity(string message)
        {
            PromptEntityOptions peo = new PromptEntityOptions("\n" + message);
            return _ed.GetEntity(peo);
        }

        public ObjectId[] SelectAll()
        {
            PromptSelectionResult psr = _ed.SelectAll();
            if (psr.Status != PromptStatus.OK || psr.Value == null)
            {
                return new ObjectId[0];
            }

            return psr.Value.GetObjectIds();
        }

        public ObjectId[] GetSelectionByLayer(string layerName)
        {
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId msId = SymbolUtilityServices.GetBlockModelSpaceId(_db);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(msId, OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity != null && string.Equals(entity.Layer, layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.ToArray();
        }

        public ObjectId[] GetSelectionByType(string typeName)
        {
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId msId = SymbolUtilityServices.GetBlockModelSpaceId(_db);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(msId, OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    string runtimeType = entity.GetType().Name;
                    string dxfType = entity.GetRXClass().DxfName;
                    if (string.Equals(runtimeType, typeName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dxfType, typeName, StringComparison.OrdinalIgnoreCase))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.ToArray();
        }

        public ObjectId[] GetSelection(string layerName, string typeName)
        {
            List<ObjectId> ids = new List<ObjectId>();
            bool filterLayer = !string.IsNullOrWhiteSpace(layerName);
            bool filterType = !string.IsNullOrWhiteSpace(typeName);

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId msId = SymbolUtilityServices.GetBlockModelSpaceId(_db);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(msId, OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    if (filterLayer && !string.Equals(entity.Layer, layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (filterType)
                    {
                        string runtimeType = entity.GetType().Name;
                        string dxfType = entity.GetRXClass().DxfName;
                        if (!string.Equals(runtimeType, typeName, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(dxfType, typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    ids.Add(id);
                }
            }

            return ids.ToArray();
        }

        public ObjectId[] SelectWindow(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            PromptSelectionResult psr = _ed.SelectWindow(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2));
            if (psr.Status != PromptStatus.OK || psr.Value == null)
            {
                return new ObjectId[0];
            }

            return psr.Value.GetObjectIds();
        }

        public ObjectId[] SelectCrossingWindow(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            PromptSelectionResult psr = _ed.SelectCrossingWindow(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2));
            if (psr.Status != PromptStatus.OK || psr.Value == null)
            {
                return new ObjectId[0];
            }

            return psr.Value.GetObjectIds();
        }

        public ObjectId CopyEntity(ObjectId entityId, double dx, double dy, double dz)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity source = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (source == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                Entity clone = source.Clone() as Entity;
                BlockTableRecord owner = (BlockTableRecord)tr.GetObject(source.OwnerId, OpenMode.ForWrite);
                clone.TransformBy(Matrix3d.Displacement(new Vector3d(dx, dy, dz)));
                ObjectId id = owner.AppendEntity(clone);
                tr.AddNewlyCreatedDBObject(clone, true);
                tr.Commit();
                return id;
            }
        }

        public void MoveEntity(ObjectId entityId, double dx, double dy, double dz)
        {
            TransformEntity(entityId, Matrix3d.Displacement(new Vector3d(dx, dy, dz)));
        }

        public void RotateEntity(ObjectId entityId, double baseX, double baseY, double baseZ, double angleDegrees)
        {
            Matrix3d matrix = Matrix3d.Rotation(DegToRad(angleDegrees), Vector3d.ZAxis, new Point3d(baseX, baseY, baseZ));
            TransformEntity(entityId, matrix);
        }

        public void ScaleEntity(ObjectId entityId, double baseX, double baseY, double baseZ, double scaleFactor)
        {
            Matrix3d matrix = Matrix3d.Scaling(scaleFactor, new Point3d(baseX, baseY, baseZ));
            TransformEntity(entityId, matrix);
        }

        public ObjectId MirrorEntity(ObjectId entityId, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity source = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (source == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                Entity clone = source.Clone() as Entity;
                Line3d axis = new Line3d(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2));
                clone.TransformBy(Matrix3d.Mirroring(axis));

                BlockTableRecord owner = tr.GetObject(source.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                ObjectId newId = owner.AppendEntity(clone);
                tr.AddNewlyCreatedDBObject(clone, true);

                if (eraseSource)
                {
                    Entity writable = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                    if (writable != null && !writable.IsErased)
                    {
                        writable.Erase(true);
                    }
                }

                tr.Commit();
                return newId;
            }
        }

        public void EraseEntity(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                entity.Erase(true);
                tr.Commit();
            }
        }

        public ObjectId[] ExplodeEntity(ObjectId entityId, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                DBObjectCollection exploded = new DBObjectCollection();
                try
                {
                    entity.Explode(exploded);
                }
                catch
                {
                    return new ObjectId[0];
                }

                BlockTableRecord owner = tr.GetObject(entity.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                List<ObjectId> created = new List<ObjectId>();
                foreach (DBObject dbo in exploded)
                {
                    Entity child = dbo as Entity;
                    if (child == null)
                    {
                        continue;
                    }

                    ObjectId id = owner.AppendEntity(child);
                    tr.AddNewlyCreatedDBObject(child, true);
                    created.Add(id);
                }

                if (eraseSource)
                {
                    Entity writable = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                    if (writable != null && !writable.IsErased)
                    {
                        writable.Erase(true);
                    }
                }

                tr.Commit();
                return created.ToArray();
            }
        }

        public ObjectId[] OffsetEntity(ObjectId entityId, double offsetDistance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                Curve curve = entity as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non supporta offset: " + (entity == null ? "null" : entity.GetType().Name));
                }

                DBObjectCollection offsetObjects = curve.GetOffsetCurves(offsetDistance);
                BlockTableRecord owner = tr.GetObject(entity.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                List<ObjectId> created = new List<ObjectId>();
                foreach (DBObject dbo in offsetObjects)
                {
                    Entity child = dbo as Entity;
                    if (child == null)
                    {
                        continue;
                    }

                    ObjectId id = owner.AppendEntity(child);
                    tr.AddNewlyCreatedDBObject(child, true);
                    created.Add(id);
                }

                tr.Commit();
                return created.ToArray();
            }
        }

        public int MoveEntities(IList entityIds, double dx, double dy, double dz)
        {
            return TransformEntities(entityIds, Matrix3d.Displacement(new Vector3d(dx, dy, dz)));
        }

        public int RotateEntities(IList entityIds, double baseX, double baseY, double baseZ, double angleDegrees)
        {
            Matrix3d matrix = Matrix3d.Rotation(DegToRad(angleDegrees), Vector3d.ZAxis, new Point3d(baseX, baseY, baseZ));
            return TransformEntities(entityIds, matrix);
        }

        public int ScaleEntities(IList entityIds, double baseX, double baseY, double baseZ, double scaleFactor)
        {
            Matrix3d matrix = Matrix3d.Scaling(scaleFactor, new Point3d(baseX, baseY, baseZ));
            return TransformEntities(entityIds, matrix);
        }

        public ObjectId[] MirrorEntities(IList entityIds, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
        {
            if (entityIds == null)
            {
                return new ObjectId[0];
            }

            List<ObjectId> ids = new List<ObjectId>();
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    ids.Add(MirrorEntity((ObjectId)raw, x1, y1, z1, x2, y2, z2, eraseSource));
                }
            }

            return ids.ToArray();
        }

        public int ExplodeEntities(IList entityIds, bool eraseSource)
        {
            if (entityIds == null)
            {
                return 0;
            }

            int count = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    count += ExplodeEntity((ObjectId)raw, eraseSource).Length;
                }
            }

            return count;
        }

        public int OffsetEntities(IList entityIds, double offsetDistance)
        {
            if (entityIds == null)
            {
                return 0;
            }

            int count = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    count += OffsetEntity((ObjectId)raw, offsetDistance).Length;
                }
            }
            return count;
        }

        public int EraseEntities(IList entityIds)
        {
            if (entityIds == null)
            {
                return 0;
            }

            int count = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    EraseEntity((ObjectId)raw);
                    count++;
                }
            }

            return count;
        }

        private void TransformEntity(ObjectId entityId, Matrix3d matrix)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                entity.TransformBy(matrix);
                tr.Commit();
            }
        }

        private int TransformEntities(IList entityIds, Matrix3d matrix)
        {
            if (entityIds == null)
            {
                return 0;
            }

            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)raw, OpenMode.ForWrite) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    entity.TransformBy(matrix);
                    changed++;
                }

                tr.Commit();
            }

            return changed;
        }
    }
}
