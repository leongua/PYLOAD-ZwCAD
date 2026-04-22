using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public Hashtable GetArcInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForRead) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = arc.Handle.ToString();
                info["type"] = arc.GetType().Name;
                info["layer"] = arc.Layer;
                info["color_index"] = arc.ColorIndex;
                info["center_x"] = arc.Center.X;
                info["center_y"] = arc.Center.Y;
                info["center_z"] = arc.Center.Z;
                info["radius"] = arc.Radius;
                info["start_angle"] = arc.StartAngle;
                info["end_angle"] = arc.EndAngle;
                info["total_angle"] = NormalizeArcAngle(arc.EndAngle - arc.StartAngle);
                info["start_angle_deg"] = arc.StartAngle * 180.0 / Math.PI;
                info["end_angle_deg"] = arc.EndAngle * 180.0 / Math.PI;
                info["total_angle_deg"] = NormalizeArcAngle(arc.EndAngle - arc.StartAngle) * 180.0 / Math.PI;
                info["arc_length"] = arc.GetDistanceAtParameter(arc.EndParam) - arc.GetDistanceAtParameter(arc.StartParam);
                info["area"] = arc.Area;
                info["thickness"] = arc.Thickness;
                info["start_x"] = arc.StartPoint.X;
                info["start_y"] = arc.StartPoint.Y;
                info["start_z"] = arc.StartPoint.Z;
                info["end_x"] = arc.EndPoint.X;
                info["end_y"] = arc.EndPoint.Y;
                info["end_z"] = arc.EndPoint.Z;
                info["normal_x"] = arc.Normal.X;
                info["normal_y"] = arc.Normal.Y;
                info["normal_z"] = arc.Normal.Z;
                return info;
            }
        }

        public double GetArcLength(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForRead) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                return arc.GetDistanceAtParameter(arc.EndParam) - arc.GetDistanceAtParameter(arc.StartParam);
            }
        }

        public double GetArcArea(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForRead) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                return arc.Area;
            }
        }

        public Point3d GetArcStartPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForRead) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                return arc.StartPoint;
            }
        }

        public Point3d GetArcEndPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForRead) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                return arc.EndPoint;
            }
        }

        public Point3d GetArcMidPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForRead) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }

                double midDist = (arc.GetDistanceAtParameter(arc.StartParam) + arc.GetDistanceAtParameter(arc.EndParam)) * 0.5;
                return arc.GetPointAtDist(midDist);
            }
        }

        public double GetArcTotalAngle(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForRead) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                return NormalizeArcAngle(arc.EndAngle - arc.StartAngle);
            }
        }

        public double GetArcTotalAngleDegrees(ObjectId entityId)
        {
            return GetArcTotalAngle(entityId) * 180.0 / Math.PI;
        }

        public void SetArcRadius(ObjectId entityId, double radius)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForWrite) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                arc.Radius = radius;
                tr.Commit();
            }
        }

        public void SetArcAngles(ObjectId entityId, double startAngleRadians, double endAngleRadians)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForWrite) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                arc.StartAngle = startAngleRadians;
                arc.EndAngle = endAngleRadians;
                tr.Commit();
            }
        }

        public void SetArcAnglesDegrees(ObjectId entityId, double startAngleDegrees, double endAngleDegrees)
        {
            SetArcAngles(entityId, DegreesToRadians(startAngleDegrees), DegreesToRadians(endAngleDegrees));
        }

        public void SetArcCenter(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForWrite) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                arc.Center = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public void SetArcThickness(ObjectId entityId, double thickness)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForWrite) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                arc.Thickness = thickness;
                tr.Commit();
            }
        }

        public void SetArcNormal(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForWrite) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
                arc.Normal = new Vector3d(x, y, z);
                tr.Commit();
            }
        }

        public ObjectId[] OffsetArc(ObjectId entityId, double offsetDistance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Arc arc = tr.GetObject(entityId, OpenMode.ForRead) as Arc;
                if (arc == null)
                {
                    throw new ArgumentException("L'entita non e un Arc");
                }
            }

            return OffsetEntity(entityId, offsetDistance);
        }

        private static double NormalizeArcAngle(double angle)
        {
            while (angle < 0.0)
            {
                angle += Math.PI * 2.0;
            }
            while (angle >= Math.PI * 2.0)
            {
                angle -= Math.PI * 2.0;
            }
            return angle;
        }
    }
}
