using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public double GetCurveLength(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
            }
        }

        public Point3d GetPointAtDist(ObjectId entityId, double distance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return curve.GetPointAtDist(distance);
            }
        }

        public double GetDistAtPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return curve.GetDistAtPoint(new Point3d(x, y, z));
            }
        }

        public Point3d GetClosestPointTo(ObjectId entityId, double x, double y, double z, bool extend)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return curve.GetClosestPointTo(new Point3d(x, y, z), extend);
            }
        }

        public Hashtable GetCurveFirstDerivativeAtParameter(ObjectId entityId, double parameter)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return VectorToHash(curve.GetFirstDerivative(parameter));
            }
        }

        public Hashtable GetCurveFirstDerivativeAtPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return VectorToHash(curve.GetFirstDerivative(new Point3d(x, y, z)));
            }
        }

        public Hashtable GetCurveSecondDerivativeAtParameter(ObjectId entityId, double parameter)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return VectorToHash(curve.GetSecondDerivative(parameter));
            }
        }

        public Hashtable GetCurveSecondDerivativeAtPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return VectorToHash(curve.GetSecondDerivative(new Point3d(x, y, z)));
            }
        }

        public ObjectId[] SplitCurveByParameters(ObjectId entityId, IList parameters)
        {
            DoubleCollection values = new DoubleCollection();
            foreach (object raw in parameters)
            {
                values.Add(Convert.ToDouble(raw));
            }
            return SplitCurveByParametersInternal(entityId, values);
        }

        public ObjectId[] SplitCurveByPoints(ObjectId entityId, IList coordinates)
        {
            if (coordinates == null || coordinates.Count < 3 || coordinates.Count % 3 != 0)
            {
                throw new ArgumentException("coordinates deve contenere triple x,y,z");
            }

            Point3dCollection points = new Point3dCollection();
            for (int i = 0; i < coordinates.Count; i += 3)
            {
                points.Add(new Point3d(
                    Convert.ToDouble(coordinates[i]),
                    Convert.ToDouble(coordinates[i + 1]),
                    Convert.ToDouble(coordinates[i + 2])));
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                DBObjectCollection parts = curve.GetSplitCurves(points);
                return AppendSplitParts(tr, parts);
            }
        }

        public Point3d GetPointAtParameter(ObjectId entityId, double parameter)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return curve.GetPointAtParameter(parameter);
            }
        }

        public double GetParameterAtPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return curve.GetParameterAtPoint(new Point3d(x, y, z));
            }
        }

        public double GetParameterAtDistance(ObjectId entityId, double distance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                return curve.GetParameterAtDistance(distance);
            }
        }

        public int GetPolylineVertexCount(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                return pl.NumberOfVertices;
            }
        }

        public ArrayList GetPolylineVertices(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ArrayList pts = new ArrayList();
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    Point2d pt = pl.GetPoint2dAt(i);
                    Hashtable item = new Hashtable();
                    item["index"] = i;
                    item["x"] = pt.X;
                    item["y"] = pt.Y;
                    item["z"] = 0.0;
                    pts.Add(item);
                }
                return pts;
            }
        }

        public Hashtable GetPolylineInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = pl.Handle.ToString();
                info["type"] = pl.GetType().Name;
                info["layer"] = pl.Layer;
                info["color_index"] = pl.ColorIndex;
                info["vertex_count"] = pl.NumberOfVertices;
                info["segment_count"] = GetPolylineSegmentCountInternal(pl);
                info["closed"] = pl.Closed;
                info["elevation"] = pl.Elevation;
                info["thickness"] = pl.Thickness;
                info["normal_x"] = pl.Normal.X;
                info["normal_y"] = pl.Normal.Y;
                info["normal_z"] = pl.Normal.Z;
                info["length"] = pl.GetDistanceAtParameter(pl.EndParam) - pl.GetDistanceAtParameter(pl.StartParam);
                info["area"] = pl.Closed ? pl.Area : 0.0;
                return info;
            }
        }

        public Point3d GetPolylineVertexAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }
                if (index < 0 || index >= pl.NumberOfVertices)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                Point2d pt = pl.GetPoint2dAt(index);
                return new Point3d(pt.X, pt.Y, 0.0);
            }
        }

        public bool IsPolylineClosed(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                return pl.Closed;
            }
        }

        public void SetPolylineClosed(ObjectId entityId, bool closed)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                pl.Closed = closed;
                tr.Commit();
            }
        }

        public double GetPolylineArea(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                return pl.Area;
            }
        }

        public int GetPolylineSegmentCount(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                return GetPolylineSegmentCountInternal(pl);
            }
        }

        public string GetPolylineSegmentType(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateSegmentIndex(pl, index);
                return pl.GetSegmentType(index).ToString();
            }
        }

        public Hashtable GetLineSegment2dAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateSegmentIndex(pl, index);
                LineSegment2d seg = pl.GetLineSegment2dAt(index);
                Hashtable info = new Hashtable();
                info["start_x"] = seg.StartPoint.X;
                info["start_y"] = seg.StartPoint.Y;
                info["end_x"] = seg.EndPoint.X;
                info["end_y"] = seg.EndPoint.Y;
                info["length"] = seg.Length;
                return info;
            }
        }

        public Hashtable GetArcSegment2dAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateSegmentIndex(pl, index);
                CircularArc2d seg = pl.GetArcSegment2dAt(index);
                Hashtable info = new Hashtable();
                info["center_x"] = seg.Center.X;
                info["center_y"] = seg.Center.Y;
                info["radius"] = seg.Radius;
                info["start_angle"] = seg.StartAngle;
                info["end_angle"] = seg.EndAngle;
                info["length"] = Math.Abs(NormalizePolylineArcAngle(seg.EndAngle - seg.StartAngle)) * seg.Radius;
                return info;
            }
        }

        public double GetBulgeAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateVertexIndex(pl, index);
                return pl.GetBulgeAt(index);
            }
        }

        public void SetBulgeAt(ObjectId entityId, int index, double value)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateVertexIndex(pl, index);
                pl.SetBulgeAt(index, value);
                tr.Commit();
            }
        }

        public double GetStartWidthAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateVertexIndex(pl, index);
                return pl.GetStartWidthAt(index);
            }
        }

        public void SetStartWidthAt(ObjectId entityId, int index, double value)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateVertexIndex(pl, index);
                pl.SetStartWidthAt(index, value);
                tr.Commit();
            }
        }

        public double GetEndWidthAt(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateVertexIndex(pl, index);
                return pl.GetEndWidthAt(index);
            }
        }

        public void SetEndWidthAt(ObjectId entityId, int index, double value)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                ValidateVertexIndex(pl, index);
                pl.SetEndWidthAt(index, value);
                tr.Commit();
            }
        }

        public void SetPolylineElevation(ObjectId entityId, double elevation)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                pl.Elevation = elevation;
                tr.Commit();
            }
        }

        public void SetPolylineThickness(ObjectId entityId, double thickness)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                pl.Thickness = thickness;
                tr.Commit();
            }
        }

        public void SetPolylineNormal(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                pl.Normal = new Vector3d(x, y, z);
                tr.Commit();
            }
        }

        public Point3d GetPolylinePointAtPercent(ObjectId entityId, double percent)
        {
            if (percent < 0.0 || percent > 1.0)
            {
                throw new ArgumentOutOfRangeException("percent");
            }

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
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                if (index < 0 || index > pl.NumberOfVertices)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                pl.AddVertexAt(index, new Point2d(x, y), 0.0, 0.0, 0.0);
                tr.Commit();
            }
        }

        public void RemovePolylineVertex(ObjectId entityId, int index)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }
                if (pl.NumberOfVertices <= 2)
                {
                    throw new ArgumentException("Una polyline deve mantenere almeno 2 vertici");
                }
                if (index < 0 || index >= pl.NumberOfVertices)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                pl.RemoveVertexAt(index);
                tr.Commit();
            }
        }

        private static int GetPolylineSegmentCountInternal(Polyline pl)
        {
            return pl.Closed ? pl.NumberOfVertices : Math.Max(0, pl.NumberOfVertices - 1);
        }

        private ObjectId[] SplitCurveByParametersInternal(ObjectId entityId, DoubleCollection parameters)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                DBObjectCollection parts = curve.GetSplitCurves(parameters);
                return AppendSplitParts(tr, parts);
            }
        }

        private ObjectId[] AppendSplitParts(Transaction tr, DBObjectCollection parts)
        {
            BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            ArrayList created = new ArrayList();

            foreach (DBObject dbo in parts)
            {
                Entity entity = dbo as Entity;
                if (entity == null)
                {
                    continue;
                }

                ObjectId id = ms.AppendEntity(entity);
                tr.AddNewlyCreatedDBObject(entity, true);
                created.Add(id);
            }

            tr.Commit();
            ObjectId[] result = new ObjectId[created.Count];
            for (int i = 0; i < created.Count; i++)
            {
                result[i] = (ObjectId)created[i];
            }
            return result;
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
            int segmentCount = GetPolylineSegmentCountInternal(pl);
            if (index < 0 || index >= segmentCount)
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }

        private static double NormalizePolylineArcAngle(double angle)
        {
            double result = angle;
            while (result < 0.0)
            {
                result += Math.PI * 2.0;
            }
            while (result >= Math.PI * 2.0)
            {
                result -= Math.PI * 2.0;
            }
            return result;
        }
    }
}
