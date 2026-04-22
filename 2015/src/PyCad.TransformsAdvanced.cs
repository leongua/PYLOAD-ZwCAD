using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ObjectId[] ArrayRectangularEntity(
            ObjectId entityId,
            int rows,
            int columns,
            int levels,
            double rowSpacing,
            double columnSpacing,
            double levelSpacing)
        {
            if (rows < 1 || columns < 1 || levels < 1)
            {
                throw new ArgumentException("rows, columns e levels devono essere >= 1");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity source = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (source == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                List<ObjectId> created = new List<ObjectId>();

                for (int level = 0; level < levels; level++)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        for (int col = 0; col < columns; col++)
                        {
                            if (level == 0 && row == 0 && col == 0)
                            {
                                continue;
                            }

                            Entity clone = source.Clone() as Entity;
                            if (clone == null)
                            {
                                continue;
                            }

                            Vector3d disp = new Vector3d(
                                col * columnSpacing,
                                row * rowSpacing,
                                level * levelSpacing);

                            clone.TransformBy(Matrix3d.Displacement(disp));
                            ObjectId id = ms.AppendEntity(clone);
                            tr.AddNewlyCreatedDBObject(clone, true);
                            created.Add(id);
                        }
                    }
                }

                tr.Commit();
                return created.ToArray();
            }
        }

        public ObjectId[] ArrayPolarEntity(
            ObjectId entityId,
            int itemCount,
            double centerX,
            double centerY,
            double centerZ,
            double fillAngleDegrees,
            bool rotateItems)
        {
            if (itemCount < 1)
            {
                throw new ArgumentException("itemCount deve essere >= 1");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity source = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (source == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Point3d center = new Point3d(centerX, centerY, centerZ);
                double fillAngleRadians = DegreesToRadians(fillAngleDegrees);
                double step = itemCount == 1 ? 0.0 : fillAngleRadians / itemCount;

                List<ObjectId> created = new List<ObjectId>();

                for (int i = 1; i < itemCount; i++)
                {
                    Entity clone = source.Clone() as Entity;
                    if (clone == null)
                    {
                        continue;
                    }

                    double angle = step * i;
                    clone.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, center));

                    if (!rotateItems)
                    {
                        clone.TransformBy(Matrix3d.Rotation(-angle, Vector3d.ZAxis, center));
                    }

                    ObjectId id = ms.AppendEntity(clone);
                    tr.AddNewlyCreatedDBObject(clone, true);
                    created.Add(id);
                }

                tr.Commit();
                return created.ToArray();
            }
        }

        public int ArrayRectangularEntities(
            IList entityIds,
            int rows,
            int columns,
            int levels,
            double rowSpacing,
            double columnSpacing,
            double levelSpacing)
        {
            int total = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    total += ArrayRectangularEntity(
                        (ObjectId)raw,
                        rows,
                        columns,
                        levels,
                        rowSpacing,
                        columnSpacing,
                        levelSpacing).Length;
                }
            }
            return total;
        }

        public int ArrayPolarEntities(
            IList entityIds,
            int itemCount,
            double centerX,
            double centerY,
            double centerZ,
            double fillAngleDegrees,
            bool rotateItems)
        {
            int total = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    total += ArrayPolarEntity(
                        (ObjectId)raw,
                        itemCount,
                        centerX,
                        centerY,
                        centerZ,
                        fillAngleDegrees,
                        rotateItems).Length;
                }
            }
            return total;
        }
    }
}
