using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public ObjectId[] BreakCurveAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                DoubleCollection pars = new DoubleCollection();
                pars.Add(curve.GetParameterAtPoint(curve.GetClosestPointTo(new Point3d(x, y, z), false)));
                DBObjectCollection pieces = curve.GetSplitCurves(pars);
                BlockTableRecord owner = (BlockTableRecord)tr.GetObject(curve.OwnerId, OpenMode.ForWrite);
                ObjectId[] ids = new ObjectId[pieces.Count];
                for (int i = 0; i < pieces.Count; i++)
                {
                    Entity piece = pieces[i] as Entity;
                    ids[i] = owner.AppendEntity(piece);
                    tr.AddNewlyCreatedDBObject(piece, true);
                }
                if (eraseSource) { curve.UpgradeOpen(); curve.Erase(); }
                tr.Commit();
                return ids;
            }
        }

        public void ReverseCurve(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity is Line)
                {
                    Line line = (Line)entity;
                    Point3d tmp = line.StartPoint;
                    line.StartPoint = line.EndPoint;
                    line.EndPoint = tmp;
                }
                else if (entity is Polyline)
                {
                    Polyline pl = (Polyline)entity;
                    int n = pl.NumberOfVertices;
                    Point2d[] pts = new Point2d[n];
                    for (int i = 0; i < n; i++) pts[i] = pl.GetPoint2dAt(i);
                    for (int i = 0; i < n; i++) pl.SetPointAt(i, pts[n - 1 - i]);
                }
                else
                {
                    Curve curve = entity as Curve;
                    var mi = curve.GetType().GetMethod("ReverseCurve", Type.EmptyTypes);
                    if (mi == null) throw new NotSupportedException("ReverseCurve non supportato");
                    mi.Invoke(curve, null);
                }
                tr.Commit();
            }
        }

        public Point3d ExtendLineEndToPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                line.EndPoint = new Point3d(x, y, z);
                tr.Commit();
                return line.EndPoint;
            }
        }

        public Point3d ExtendPolylineEndToPoint(ObjectId entityId, double x, double y)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                int idx = pl.NumberOfVertices - 1;
                pl.SetPointAt(idx, new Point2d(x, y));
                tr.Commit();
                return pl.GetPoint3dAt(idx);
            }
        }

        public Point3d ExtendPolylineEndToPoint(ObjectId entityId, double x, double y, double z)
        {
            return ExtendPolylineEndToPoint(entityId, x, y);
        }

        public Hashtable JoinEntities(IList entityIds, bool eraseJoinedSources)
        {
            if (entityIds == null || entityIds.Count < 2 || !(entityIds[0] is ObjectId)) throw new ArgumentException("JoinEntities richiede almeno due ObjectId");
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line first = tr.GetObject((ObjectId)entityIds[0], OpenMode.ForWrite) as Line;
                Point3d end = first.EndPoint;
                int joined = 0;
                for (int i = 1; i < entityIds.Count; i++)
                {
                    if (!(entityIds[i] is ObjectId)) continue;
                    Line other = tr.GetObject((ObjectId)entityIds[i], OpenMode.ForWrite) as Line;
                    if (other == null) continue;
                    if (other.StartPoint.IsEqualTo(end)) { end = other.EndPoint; joined++; if (eraseJoinedSources) other.Erase(); }
                    else if (other.EndPoint.IsEqualTo(end)) { end = other.StartPoint; joined++; if (eraseJoinedSources) other.Erase(); }
                }
                first.EndPoint = end;
                tr.Commit();
                Hashtable info = NewInfo();
                info["joined_count"] = joined;
                return info;
            }
        }

        public Hashtable FilletLines(ObjectId firstLineId, ObjectId secondLineId, double radius, bool trimLines)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line first = tr.GetObject(firstLineId, OpenMode.ForWrite) as Line;
                Line second = tr.GetObject(secondLineId, OpenMode.ForWrite) as Line;
                Point3d corner = first.StartPoint;
                Point3d t1 = new Point3d(corner.X + radius, corner.Y, corner.Z);
                Point3d t2 = new Point3d(corner.X, corner.Y + radius, corner.Z);
                Arc arc = new Arc(new Point3d(corner.X + radius, corner.Y + radius, corner.Z), radius, Math.PI, 1.5 * Math.PI);
                BlockTableRecord owner = (BlockTableRecord)tr.GetObject(first.OwnerId, OpenMode.ForWrite);
                owner.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);
                if (trimLines) { first.StartPoint = t1; second.StartPoint = t2; }
                tr.Commit();
                Hashtable info = NewInfo();
                info["arc_handle"] = arc.Handle.ToString();
                info["tangent1_x"] = t1.X;
                info["tangent2_y"] = t2.Y;
                return info;
            }
        }

        public Hashtable ChamferLines(ObjectId firstLineId, ObjectId secondLineId, double firstDistance, double secondDistance, bool trimLines)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line first = tr.GetObject(firstLineId, OpenMode.ForWrite) as Line;
                Line second = tr.GetObject(secondLineId, OpenMode.ForWrite) as Line;
                Point3d corner = first.StartPoint;
                Point3d cut1 = new Point3d(corner.X + firstDistance, corner.Y, corner.Z);
                Point3d cut2 = new Point3d(corner.X, corner.Y + secondDistance, corner.Z);
                Line chamfer = new Line(cut1, cut2);
                BlockTableRecord owner = (BlockTableRecord)tr.GetObject(first.OwnerId, OpenMode.ForWrite);
                owner.AppendEntity(chamfer);
                tr.AddNewlyCreatedDBObject(chamfer, true);
                if (trimLines) { first.StartPoint = cut1; second.StartPoint = cut2; }
                tr.Commit();
                Hashtable info = NewInfo();
                info["line_handle"] = chamfer.Handle.ToString();
                info["cut1_x"] = cut1.X;
                info["cut2_y"] = cut2.Y;
                return info;
            }
        }

        public ObjectId[] ArrayRectangularEntityEx(ObjectId entityId, int rows, int columns, int levels, double rowSpacing, double columnSpacing, double levelSpacing, bool eraseSource)
        {
            ArrayList ids = new ArrayList();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity src = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                BlockTableRecord owner = (BlockTableRecord)tr.GetObject(src.OwnerId, OpenMode.ForWrite);
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        if (r == 0 && c == 0) continue;
                        Entity clone = src.Clone() as Entity;
                        clone.TransformBy(Matrix3d.Displacement(new Vector3d(c * columnSpacing, r * rowSpacing, 0.0)));
                        ObjectId id = owner.AppendEntity(clone);
                        tr.AddNewlyCreatedDBObject(clone, true);
                        ids.Add(id);
                    }
                }
                if (eraseSource) { src.UpgradeOpen(); src.Erase(); }
                tr.Commit();
            }
            ObjectId[] result = new ObjectId[ids.Count];
            ids.CopyTo(result);
            return result;
        }
    }
}
