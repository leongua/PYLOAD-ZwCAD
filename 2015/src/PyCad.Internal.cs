using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        private ObjectId AddEntity(Entity entity)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ObjectId id = btr.AppendEntity(entity);
                tr.AddNewlyCreatedDBObject(entity, true);
                tr.Commit();
                return id;
            }
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private static Extents3d? TryGetExtents(Entity entity)
        {
            try
            {
                return entity.GeometricExtents;
            }
            catch
            {
                return null;
            }
        }
    }
}
