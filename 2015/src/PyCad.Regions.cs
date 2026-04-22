using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ObjectId CreateRegionFromEntity(ObjectId entityId)
        {
            ObjectId[] ids = CreateRegionsFromEntities(new ArrayList { entityId });
            if (ids.Length == 0)
            {
                throw new InvalidOperationException("Nessuna region creata dall'entita specificata");
            }
            return ids[0];
        }

        public ObjectId[] CreateRegionsFromEntities(IList entityIds)
        {
            if (entityIds == null || entityIds.Count == 0)
            {
                throw new ArgumentException("entityIds non puo essere vuoto");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObjectCollection curves = new DBObjectCollection();
                foreach (object raw in entityIds)
                {
                    ObjectId id = (ObjectId)raw;
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    DBObject clone = entity.Clone() as DBObject;
                    if (clone != null)
                    {
                        curves.Add(clone);
                    }
                }

                DBObjectCollection regions = Region.CreateFromCurves(curves);

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                ArrayList ids = new ArrayList();
                foreach (DBObject dbo in regions)
                {
                    Region region = dbo as Region;
                    if (region == null)
                    {
                        continue;
                    }

                    ObjectId id = ms.AppendEntity(region);
                    tr.AddNewlyCreatedDBObject(region, true);
                    ids.Add(id);
                }

                tr.Commit();

                ObjectId[] result = new ObjectId[ids.Count];
                for (int i = 0; i < ids.Count; i++)
                {
                    result[i] = (ObjectId)ids[i];
                }
                return result;
            }
        }

        public Hashtable GetRegionInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Region region = tr.GetObject(entityId, OpenMode.ForRead) as Region;
                if (region == null)
                {
                    throw new ArgumentException("L'entita non e una Region");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = region.Handle.ToString();
                info["type"] = region.GetType().Name;
                info["layer"] = region.Layer;
                info["color_index"] = region.ColorIndex;
                info["area"] = region.Area;
                info["perimeter"] = GetRegionPerimeter(region);
                info["normal_x"] = region.Normal.X;
                info["normal_y"] = region.Normal.Y;
                info["normal_z"] = region.Normal.Z;

                Extents3d? ext = TryGetExtents(region);
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

        public ObjectId BooleanRegions(ObjectId regionId, ObjectId otherRegionId, string operation)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Region region = tr.GetObject(regionId, OpenMode.ForWrite) as Region;
                Region other = tr.GetObject(otherRegionId, OpenMode.ForWrite) as Region;
                if (region == null || other == null)
                {
                    throw new ArgumentException("Entrambe le entita devono essere Region");
                }

                region.BooleanOperation(ParseBooleanOperation(operation), other);
                tr.Commit();
                return regionId;
            }
        }

        public ObjectId[] ExplodeRegion(ObjectId regionId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Region region = tr.GetObject(regionId, OpenMode.ForRead) as Region;
                if (region == null)
                {
                    throw new ArgumentException("L'entita non e una Region");
                }

                DBObjectCollection pieces = new DBObjectCollection();
                region.Explode(pieces);

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                ArrayList ids = new ArrayList();
                foreach (DBObject dbo in pieces)
                {
                    Entity entity = dbo as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    ObjectId id = ms.AppendEntity(entity);
                    tr.AddNewlyCreatedDBObject(entity, true);
                    ids.Add(id);
                }

                tr.Commit();

                ObjectId[] result = new ObjectId[ids.Count];
                for (int i = 0; i < ids.Count; i++)
                {
                    result[i] = (ObjectId)ids[i];
                }
                return result;
            }
        }

        public void SetRegionNormal(ObjectId regionId, double x, double y, double z)
        {
            throw new NotSupportedException("Region.Normal e di sola lettura nella tua API ZWCAD 2015");
        }

        private static BooleanOperationType ParseBooleanOperation(string operation)
        {
            string op = operation == null ? string.Empty : operation.Trim().ToLowerInvariant();
            switch (op)
            {
                case "union":
                case "unite":
                case "add":
                    return BooleanOperationType.BoolUnite;
                case "subtract":
                case "sub":
                case "difference":
                    return BooleanOperationType.BoolSubtract;
                case "intersect":
                case "intersection":
                    return BooleanOperationType.BoolIntersect;
                default:
                    throw new ArgumentException("operation deve essere union|subtract|intersect");
            }
        }

        private static double GetRegionPerimeter(Region region)
        {
            DBObject clone = region.Clone() as DBObject;
            Region temp = clone as Region;
            if (temp == null)
            {
                throw new InvalidOperationException("Impossibile clonare la Region per il calcolo del perimetro");
            }

            try
            {
                DBObjectCollection pieces = new DBObjectCollection();
                temp.Explode(pieces);

                double total = 0.0;
                foreach (DBObject dbo in pieces)
                {
                    Curve curve = dbo as Curve;
                    if (curve != null)
                    {
                        total += curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
                    }

                    IDisposable disposable = dbo as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                return total;
            }
            finally
            {
                temp.Dispose();
            }
        }
    }
}
