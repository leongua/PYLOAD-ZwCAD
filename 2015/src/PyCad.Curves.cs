using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public Hashtable GetCircleInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = circle.Handle.ToString();
                info["type"] = circle.GetType().Name;
                info["layer"] = circle.Layer;
                info["color_index"] = circle.ColorIndex;
                info["center_x"] = circle.Center.X;
                info["center_y"] = circle.Center.Y;
                info["center_z"] = circle.Center.Z;
                info["radius"] = circle.Radius;
                info["diameter"] = circle.Radius * 2.0;
                info["circumference"] = 2.0 * Math.PI * circle.Radius;
                info["area"] = circle.Area;
                info["thickness"] = circle.Thickness;
                info["normal_x"] = circle.Normal.X;
                info["normal_y"] = circle.Normal.Y;
                info["normal_z"] = circle.Normal.Z;
                return info;
            }
        }

        public void SetCircleRadius(ObjectId entityId, double radius)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForWrite) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
                circle.Radius = radius;
                tr.Commit();
            }
        }

        public void SetCircleCenter(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForWrite) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
                circle.Center = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public double GetCircleDiameter(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
                return circle.Radius * 2.0;
            }
        }

        public double GetCircleCircumference(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
                return 2.0 * Math.PI * circle.Radius;
            }
        }

        public Point3d GetCircleStartPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
                return circle.StartPoint;
            }
        }

        public Point3d GetCircleEndPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
                return circle.EndPoint;
            }
        }

        public Point3d GetCirclePointAtAngleDegrees(ObjectId entityId, double angleDegrees)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }

                return circle.GetPointAtDist(circle.Radius * DegreesToRadians(angleDegrees));
            }
        }

        public void SetCircleDiameter(ObjectId entityId, double diameter)
        {
            SetCircleRadius(entityId, diameter * 0.5);
        }

        public void SetCircleThickness(ObjectId entityId, double thickness)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForWrite) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
                circle.Thickness = thickness;
                tr.Commit();
            }
        }

        public void SetCircleNormal(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForWrite) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
                circle.Normal = new Vector3d(x, y, z);
                tr.Commit();
            }
        }

        public ObjectId[] OffsetCircle(ObjectId entityId, double offsetDistance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Circle circle = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                if (circle == null)
                {
                    throw new ArgumentException("L'entita non e un Circle");
                }
            }

            return OffsetEntity(entityId, offsetDistance);
        }

        public Hashtable GetLineInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = line.Handle.ToString();
                info["type"] = line.GetType().Name;
                info["layer"] = line.Layer;
                info["color_index"] = line.ColorIndex;
                info["start_x"] = line.StartPoint.X;
                info["start_y"] = line.StartPoint.Y;
                info["start_z"] = line.StartPoint.Z;
                info["end_x"] = line.EndPoint.X;
                info["end_y"] = line.EndPoint.Y;
                info["end_z"] = line.EndPoint.Z;
                info["length"] = line.GetDistanceAtParameter(line.EndParam) - line.GetDistanceAtParameter(line.StartParam);
                info["angle"] = Math.Atan2(line.EndPoint.Y - line.StartPoint.Y, line.EndPoint.X - line.StartPoint.X);
                info["thickness"] = line.Thickness;
                info["normal_x"] = line.Normal.X;
                info["normal_y"] = line.Normal.Y;
                info["normal_z"] = line.Normal.Z;
                return info;
            }
        }

        public Point3d GetLineMidPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }

                return new Point3d(
                    (line.StartPoint.X + line.EndPoint.X) * 0.5,
                    (line.StartPoint.Y + line.EndPoint.Y) * 0.5,
                    (line.StartPoint.Z + line.EndPoint.Z) * 0.5);
            }
        }

        public Point3d GetLineStartPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }
                return line.StartPoint;
            }
        }

        public Point3d GetLineEndPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }
                return line.EndPoint;
            }
        }

        public double GetLineAngleDegrees(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }

                return Math.Atan2(line.EndPoint.Y - line.StartPoint.Y, line.EndPoint.X - line.StartPoint.X) * 180.0 / Math.PI;
            }
        }

        public void SetLineStartPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }
                line.StartPoint = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public void SetLineEndPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }
                line.EndPoint = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public void SetLineThickness(ObjectId entityId, double thickness)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }
                line.Thickness = thickness;
                tr.Commit();
            }
        }

        public void SetLineNormal(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }
                line.Normal = new Vector3d(x, y, z);
                tr.Commit();
            }
        }

        public ObjectId[] OffsetLine(ObjectId entityId, double offsetDistance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForRead) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }
            }

            return OffsetEntity(entityId, offsetDistance);
        }

        public Hashtable GetSplineInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = spline.Handle.ToString();
                info["type"] = spline.GetType().Name;
                info["layer"] = spline.Layer;
                info["color_index"] = spline.ColorIndex;
                info["degree"] = spline.Degree;
                info["is_closed"] = spline.Closed;
                info["is_periodic"] = spline.IsPeriodic;
                info["is_rational"] = spline.IsRational;
                info["start_param"] = spline.StartParam;
                info["end_param"] = spline.EndParam;
                info["length"] = spline.GetDistanceAtParameter(spline.EndParam) - spline.GetDistanceAtParameter(spline.StartParam);
                info["start_x"] = spline.StartPoint.X;
                info["start_y"] = spline.StartPoint.Y;
                info["start_z"] = spline.StartPoint.Z;
                info["end_x"] = spline.EndPoint.X;
                info["end_y"] = spline.EndPoint.Y;
                info["end_z"] = spline.EndPoint.Z;
                info["control_point_count"] = GetSplineCountValue(spline, "NumControlPoints", "NumberOfControlPoints");
                info["fit_point_count"] = GetSplineCountValue(spline, "NumFitPoints", "NumberOfFitPoints");
                return info;
            }
        }

        public Point3d GetSplineStartPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return spline.StartPoint;
            }
        }

        public Point3d GetSplineEndPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return spline.EndPoint;
            }
        }

        public bool IsSplineClosed(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return spline.Closed;
            }
        }

        public double GetSplineStartParameter(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return spline.StartParam;
            }
        }

        public double GetSplineEndParameter(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return spline.EndParam;
            }
        }

        public Point3d GetSplinePointAtParameter(ObjectId entityId, double parameter)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return spline.GetPointAtParameter(parameter);
            }
        }

        public double GetSplineParameterAtPoint(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return spline.GetParameterAtPoint(new Point3d(x, y, z));
            }
        }

        public double GetSplineParameterAtDistance(ObjectId entityId, double distance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return spline.GetParameterAtDistance(distance);
            }
        }

        public Point3d GetSplinePointAtPercent(ObjectId entityId, double percent01)
        {
            if (percent01 < 0.0 || percent01 > 1.0)
            {
                throw new ArgumentOutOfRangeException("percent01", "percent01 deve essere tra 0 e 1");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }

                double totalLength = spline.GetDistanceAtParameter(spline.EndParam) - spline.GetDistanceAtParameter(spline.StartParam);
                return spline.GetPointAtDist(totalLength * percent01);
            }
        }

        public Hashtable GetSplineStartTangent(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return VectorToHash(spline.GetFirstDerivative(spline.StartParam));
            }
        }

        public Hashtable GetSplineEndTangent(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return VectorToHash(spline.GetFirstDerivative(spline.EndParam));
            }
        }

        public int GetSplineControlPointCount(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return GetSplineCountValue(spline, "NumControlPoints", "NumberOfControlPoints");
            }
        }

        public int GetSplineFitPointCount(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
                return GetSplineCountValue(spline, "NumFitPoints", "NumberOfFitPoints");
            }
        }

        public ObjectId[] OffsetSpline(ObjectId entityId, double offsetDistance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Spline spline = tr.GetObject(entityId, OpenMode.ForRead) as Spline;
                if (spline == null)
                {
                    throw new ArgumentException("L'entita non e una Spline");
                }
            }

            return OffsetEntity(entityId, offsetDistance);
        }

        private static int GetSplineCountValue(Spline spline, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                System.Reflection.PropertyInfo prop = spline.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    object value = prop.GetValue(spline, null);
                    if (value != null)
                    {
                        return Convert.ToInt32(value);
                    }
                }
            }

            return 0;
        }
    }
}
