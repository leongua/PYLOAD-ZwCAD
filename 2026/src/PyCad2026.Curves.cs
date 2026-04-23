using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public Hashtable GetCircleInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle c = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                if (c == null) throw new ArgumentException("L'entita non e un Circle");
                Hashtable info = NewInfo();
                info["id"] = entityId.ToString();
                info["handle"] = c.Handle.ToString();
                info["layer"] = c.Layer;
                info["radius"] = c.Radius;
                info["diameter"] = c.Radius * 2.0;
                info["circumference"] = 2.0 * Math.PI * c.Radius;
                info["center_x"] = c.Center.X;
                info["center_y"] = c.Center.Y;
                info["center_z"] = c.Center.Z;
                info["normal_x"] = c.Normal.X;
                info["normal_y"] = c.Normal.Y;
                info["normal_z"] = c.Normal.Z;
                info["thickness"] = c.Thickness;
                return info;
            }
        }

        public void SetCircleRadius(ObjectId entityId, double radius)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle c = tr.GetObject(entityId, OpenMode.ForWrite) as Circle;
                if (c == null) throw new ArgumentException("L'entita non e un Circle");
                c.Radius = radius;
                tr.Commit();
            }
        }

        public void SetCircleCenter(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle c = tr.GetObject(entityId, OpenMode.ForWrite) as Circle;
                if (c == null) throw new ArgumentException("L'entita non e un Circle");
                c.Center = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public void SetCircleThickness(ObjectId entityId, double thickness)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle c = tr.GetObject(entityId, OpenMode.ForWrite) as Circle;
                if (c == null) throw new ArgumentException("L'entita non e un Circle");
                c.Thickness = thickness;
                tr.Commit();
            }
        }

        public void SetCircleNormal(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle c = tr.GetObject(entityId, OpenMode.ForWrite) as Circle;
                if (c == null) throw new ArgumentException("L'entita non e un Circle");
                c.Normal = new Vector3d(x, y, z);
                tr.Commit();
            }
        }

        public Point3d GetLineStartPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null) throw new ArgumentException("L'entita non e una Line");
                return line.StartPoint;
            }
        }

        public Point3d GetLineEndPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null) throw new ArgumentException("L'entita non e una Line");
                return line.EndPoint;
            }
        }

        public double GetLineAngleDegrees(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null) throw new ArgumentException("L'entita non e una Line");
                Vector3d direction = line.EndPoint - line.StartPoint;
                return RadToDeg(Math.Atan2(direction.Y, direction.X));
            }
        }

        public void SetLineStartPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null) throw new ArgumentException("L'entita non e una Line");
                line.StartPoint = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public void SetLineEndPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null) throw new ArgumentException("L'entita non e una Line");
                line.EndPoint = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public void SetLineThickness(ObjectId entityId, double thickness)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null) throw new ArgumentException("L'entita non e una Line");
                line.Thickness = thickness;
                tr.Commit();
            }
        }

        public void SetLineNormal(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null) throw new ArgumentException("L'entita non e una Line");
                line.Normal = new Vector3d(x, y, z);
                tr.Commit();
            }
        }

        public double GetCurveLength(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
            }
        }

        public Point3d GetPointAtDist(ObjectId entityId, double distance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return curve.GetPointAtDist(distance);
            }
        }

        public double GetDistAtPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return curve.GetDistAtPoint(new Point3d(x, y, z));
            }
        }

        public Point3d GetClosestPointTo(ObjectId entityId, double x, double y, double z, bool extend)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return curve.GetClosestPointTo(new Point3d(x, y, z), extend);
            }
        }

        public Point3d GetPointAtParameter(ObjectId entityId, double parameter)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return curve.GetPointAtParameter(parameter);
            }
        }

        public double GetParameterAtPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return curve.GetParameterAtPoint(new Point3d(x, y, z));
            }
        }

        public double GetParameterAtDistance(ObjectId entityId, double distance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return curve.GetParameterAtDistance(distance);
            }
        }

        public Hashtable GetCurveFirstDerivativeAtParameter(ObjectId entityId, double parameter)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return VectorToHash(curve.GetFirstDerivative(parameter));
            }
        }

        public Hashtable GetCurveSecondDerivativeAtParameter(ObjectId entityId, double parameter)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                return VectorToHash(curve.GetSecondDerivative(parameter));
            }
        }

        public ObjectId[] SplitCurveByParameters(ObjectId entityId, IList parameters)
        {
            DoubleCollection values = new DoubleCollection();
            if (parameters != null)
            {
                foreach (object raw in parameters)
                {
                    values.Add(Convert.ToDouble(raw));
                }
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");
                DBObjectCollection parts = curve.GetSplitCurves(values);
                return AppendSplitParts(tr, curve.OwnerId, parts);
            }
        }

        public int GetPolylineVertexCount(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                return pl.NumberOfVertices;
            }
        }

        public ArrayList GetPolylineVertices(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                ArrayList pts = new ArrayList();
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    Point2d pt = pl.GetPoint2dAt(i);
                    Hashtable item = NewInfo();
                    item["index"] = i;
                    item["x"] = pt.X;
                    item["y"] = pt.Y;
                    item["z"] = 0.0;
                    pts.Add(item);
                }
                return pts;
            }
        }

        public Point3d GetPolylineVertexAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                if (index < 0 || index >= pl.NumberOfVertices) throw new ArgumentOutOfRangeException("index");
                Point2d pt = pl.GetPoint2dAt(index);
                return new Point3d(pt.X, pt.Y, 0.0);
            }
        }

        public bool IsPolylineClosed(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                return pl.Closed;
            }
        }

        public void SetPolylineClosed(ObjectId entityId, bool closed)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                pl.Closed = closed;
                tr.Commit();
            }
        }

        public double GetPolylineArea(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                return pl.Area;
            }
        }

        public int GetPolylineSegmentCount(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                return pl.Closed ? pl.NumberOfVertices : Math.Max(0, pl.NumberOfVertices - 1);
            }
        }

        public string GetPolylineSegmentType(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                ValidateSegmentIndex(pl, index);
                return pl.GetSegmentType(index).ToString();
            }
        }

        public double GetBulgeAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                ValidateVertexIndex(pl, index);
                return pl.GetBulgeAt(index);
            }
        }

        public double GetStartWidthAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                ValidateVertexIndex(pl, index);
                return pl.GetStartWidthAt(index);
            }
        }

        public double GetEndWidthAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                ValidateVertexIndex(pl, index);
                return pl.GetEndWidthAt(index);
            }
        }

        public void SetPolylineNormal(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                pl.Normal = new Vector3d(x, y, z);
                tr.Commit();
            }
        }

        public Point3d GetPolylinePointAtPercent(ObjectId entityId, double percent)
        {
            if (percent < 0.0 || percent > 1.0) throw new ArgumentOutOfRangeException("percent");
            double length = GetCurveLength(entityId);
            return GetPointAtDist(entityId, length * percent);
        }

        public double GetPolylineLengthToVertex(ObjectId entityId, int index)
        {
            Point3d pt = GetPolylineVertexAt(entityId, index);
            return GetDistAtPoint(entityId, pt.X, pt.Y, pt.Z);
        }

        public void AddPolylineVertex(ObjectId entityId, int index, double x, double y)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                if (index < 0 || index > pl.NumberOfVertices) throw new ArgumentOutOfRangeException("index");
                pl.AddVertexAt(index, new Point2d(x, y), 0.0, 0.0, 0.0);
                tr.Commit();
            }
        }

        public void RemovePolylineVertex(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                if (pl.NumberOfVertices <= 2) throw new ArgumentException("Una polyline deve mantenere almeno 2 vertici");
                ValidateVertexIndex(pl, index);
                pl.RemoveVertexAt(index);
                tr.Commit();
            }
        }

        private static Hashtable VectorToHash(Vector3d vector)
        {
            Hashtable info = new Hashtable();
            info["x"] = vector.X;
            info["y"] = vector.Y;
            info["z"] = vector.Z;
            info["length"] = vector.Length;
            return info;
        }

        private static void ValidateVertexIndex(Polyline pl, int index)
        {
            if (index < 0 || index >= pl.NumberOfVertices)
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }

        private static void ValidateSegmentIndex(Polyline pl, int index)
        {
            int segmentCount = pl.Closed ? pl.NumberOfVertices : Math.Max(0, pl.NumberOfVertices - 1);
            if (index < 0 || index >= segmentCount)
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }

        private ObjectId[] AppendSplitParts(Transaction tr, ObjectId ownerId, DBObjectCollection parts)
        {
            BlockTableRecord owner = tr.GetObject(ownerId, OpenMode.ForWrite) as BlockTableRecord;
            ArrayList created = new ArrayList();
            foreach (DBObject dbo in parts)
            {
                Entity entity = dbo as Entity;
                if (entity == null)
                {
                    continue;
                }

                ObjectId id = owner.AppendEntity(entity);
                tr.AddNewlyCreatedDBObject(entity, true);
                created.Add(id);
            }

            tr.Commit();
            ObjectId[] result = new ObjectId[created.Count];
            created.CopyTo(result);
            return result;
        }
    }
}
