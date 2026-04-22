using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ArrayList GetIntersections(ObjectId firstEntityId, ObjectId secondEntityId)
        {
            return GetIntersectionsEx(firstEntityId, secondEntityId, "both");
        }

        public ArrayList GetIntersectionsEx(ObjectId firstEntityId, ObjectId secondEntityId, string mode)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity first = tr.GetObject(firstEntityId, OpenMode.ForRead) as Entity;
                Entity second = tr.GetObject(secondEntityId, OpenMode.ForRead) as Entity;
                if (first == null || second == null)
                {
                    throw new ArgumentException("Gli ObjectId devono identificare due Entity");
                }

                Point3dCollection pts = new Point3dCollection();
                bool reverseOperands;
                Intersect intersectMode = ParseIntersectMode(mode, out reverseOperands);
                if (reverseOperands)
                {
                    second.IntersectWith(first, intersectMode, pts, 0, 0);
                }
                else
                {
                    first.IntersectWith(second, intersectMode, pts, 0, 0);
                }

                ArrayList result = new ArrayList();
                foreach (Point3d pt in pts)
                {
                    Hashtable item = new Hashtable();
                    item["x"] = pt.X;
                    item["y"] = pt.Y;
                    item["z"] = pt.Z;
                    result.Add(item);
                }

                return result;
            }
        }

        public int CountIntersections(ObjectId firstEntityId, ObjectId secondEntityId, string mode)
        {
            return GetIntersectionsEx(firstEntityId, secondEntityId, mode).Count;
        }

        public Point3d GetCurveStartPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una Curve");
                }
                return curve.StartPoint;
            }
        }

        public Point3d GetCurveEndPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una Curve");
                }
                return curve.EndPoint;
            }
        }

        public Point3d GetCurveMidPoint(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una Curve");
                }

                double startDist = curve.GetDistanceAtParameter(curve.StartParam);
                double endDist = curve.GetDistanceAtParameter(curve.EndParam);
                double midDist = (startDist + endDist) * 0.5;
                return curve.GetPointAtDist(midDist);
            }
        }

        public ArrayList GetCurveSamplePoints(ObjectId entityId, int divisions)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una Curve");
                }
                if (divisions < 1)
                {
                    throw new ArgumentException("divisions deve essere >= 1");
                }

                double startDist = curve.GetDistanceAtParameter(curve.StartParam);
                double endDist = curve.GetDistanceAtParameter(curve.EndParam);
                double total = endDist - startDist;

                ArrayList result = new ArrayList();
                for (int i = 0; i <= divisions; i++)
                {
                    double ratio = (double)i / divisions;
                    double dist = startDist + (total * ratio);
                    Point3d pt = curve.GetPointAtDist(dist);

                    Hashtable item = new Hashtable();
                    item["index"] = i;
                    item["x"] = pt.X;
                    item["y"] = pt.Y;
                    item["z"] = pt.Z;
                    item["distance"] = dist;
                    result.Add(item);
                }

                return result;
            }
        }

        public bool IsPointOnCurve(ObjectId entityId, double x, double y, double z, double tolerance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una Curve");
                }

                Point3d input = new Point3d(x, y, z);
                Point3d closest = curve.GetClosestPointTo(input, false);
                return input.DistanceTo(closest) <= Math.Abs(tolerance);
            }
        }

        public double GetEntityArea(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                return GetAreaInternal(entity);
            }
        }

        public double GetEntityPerimeter(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }
                return GetPerimeterInternal(entity);
            }
        }

        public Hashtable GetEntityMetrics(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                Hashtable metrics = new Hashtable();
                metrics["type"] = entity.GetType().Name;
                metrics["has_area"] = HasArea(entity);
                metrics["has_perimeter"] = HasPerimeter(entity);
                metrics["is_closed"] = IsClosedEntity(entity);
                metrics["area"] = HasArea(entity) ? GetAreaInternal(entity) : 0.0;
                metrics["perimeter"] = HasPerimeter(entity) ? GetPerimeterInternal(entity) : 0.0;
                return metrics;
            }
        }

        public double SumEntityAreas(IList entityIds)
        {
            double total = 0.0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)raw, OpenMode.ForRead) as Entity;
                    if (entity != null && HasArea(entity))
                    {
                        total += GetAreaInternal(entity);
                    }
                }
            }
            return total;
        }

        public double SumEntityPerimeters(IList entityIds)
        {
            double total = 0.0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)raw, OpenMode.ForRead) as Entity;
                    if (entity != null && HasPerimeter(entity))
                    {
                        total += GetPerimeterInternal(entity);
                    }
                }
            }
            return total;
        }

        public Hashtable BuildMetricsSummary(IList entityIds)
        {
            Hashtable summary = new Hashtable();
            Hashtable byTypeArea = new Hashtable();
            Hashtable byTypePerimeter = new Hashtable();
            double totalArea = 0.0;
            double totalPerimeter = 0.0;
            int countArea = 0;
            int countPerimeter = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)raw, OpenMode.ForRead) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    string typeName = entity.GetType().Name;

                    if (HasArea(entity))
                    {
                        double area = GetAreaInternal(entity);
                        totalArea += area;
                        countArea++;
                        IncrementHashDouble(byTypeArea, typeName, area);
                    }

                    if (HasPerimeter(entity))
                    {
                        double perimeter = GetPerimeterInternal(entity);
                        totalPerimeter += perimeter;
                        countPerimeter++;
                        IncrementHashDouble(byTypePerimeter, typeName, perimeter);
                    }
                }
            }

            summary["total_area"] = totalArea;
            summary["total_perimeter"] = totalPerimeter;
            summary["count_with_area"] = countArea;
            summary["count_with_perimeter"] = countPerimeter;
            summary["by_type_area"] = byTypeArea;
            summary["by_type_perimeter"] = byTypePerimeter;
            return summary;
        }

        private static Intersect ParseIntersectMode(string mode, out bool reverseOperands)
        {
            reverseOperands = false;
            string value = (mode ?? string.Empty).Trim().ToLowerInvariant();
            switch (value)
            {
                case "":
                case "both":
                case "onboth":
                case "onbothoperands":
                    return Intersect.OnBothOperands;
                case "extend_this":
                case "extendthis":
                case "this":
                    return Intersect.ExtendThis;
                case "extend_other":
                case "other":
                    reverseOperands = true;
                    return Intersect.ExtendThis;
                case "extend_both":
                case "extendboth":
                    return Intersect.ExtendBoth;
                default:
                    throw new ArgumentException("Modo intersezione non supportato: " + mode);
            }
        }

        private static bool HasArea(Entity entity)
        {
            return entity is Circle || entity is Arc || entity is Polyline || entity is Ellipse;
        }

        private static bool HasPerimeter(Entity entity)
        {
            return entity is Curve;
        }

        private static bool IsClosedEntity(Entity entity)
        {
            if (entity is Circle)
            {
                return true;
            }

            if (entity is Arc)
            {
                return false;
            }

            Polyline pl = entity as Polyline;
            if (pl != null)
            {
                return pl.Closed;
            }

            Ellipse ellipse = entity as Ellipse;
            if (ellipse != null)
            {
                return IsFullEllipse(ellipse);
            }

            Curve curve = entity as Curve;
            if (curve != null)
            {
                return curve.Closed;
            }

            return false;
        }

        private static double GetAreaInternal(Entity entity)
        {
            Circle circle = entity as Circle;
            if (circle != null)
            {
                return circle.Area;
            }

            Arc arc = entity as Arc;
            if (arc != null)
            {
                double angle = NormalizeAngle(arc.EndAngle - arc.StartAngle);
                return 0.5 * arc.Radius * arc.Radius * (angle - Math.Sin(angle));
            }

            Polyline pl = entity as Polyline;
            if (pl != null)
            {
                if (!pl.Closed)
                {
                    throw new InvalidOperationException("La Polyline non e chiusa: area non disponibile");
                }
                return pl.Area;
            }

            Ellipse ellipse = entity as Ellipse;
            if (ellipse != null)
            {
                if (!IsFullEllipse(ellipse))
                {
                    throw new InvalidOperationException("La Ellipse non e completa: area non disponibile");
                }

                double a = ellipse.MajorAxis.Length;
                double b = a * ellipse.RadiusRatio;
                return Math.PI * a * b;
            }

            throw new InvalidOperationException("Area non disponibile per " + entity.GetType().Name);
        }

        private static double GetPerimeterInternal(Entity entity)
        {
            Curve curve = entity as Curve;
            if (curve != null)
            {
                double length = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam);
                Arc arc = curve as Arc;
                if (arc != null)
                {
                    return length + arc.StartPoint.DistanceTo(arc.EndPoint);
                }
                return length;
            }

            throw new InvalidOperationException("Perimetro non disponibile per " + entity.GetType().Name);
        }

        private static bool IsFullEllipse(Ellipse ellipse)
        {
            double delta = Math.Abs(ellipse.EndAngle - ellipse.StartAngle);
            return delta < 0.000001 || Math.Abs(delta - (Math.PI * 2.0)) < 0.000001;
        }

        private static double NormalizeAngle(double angleRadians)
        {
            double result = angleRadians;
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

        private static void IncrementHashDouble(Hashtable ht, string key, double value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "<empty>";
            }

            if (ht.ContainsKey(key))
            {
                ht[key] = Convert.ToDouble(ht[key]) + value;
            }
            else
            {
                ht[key] = value;
            }
        }
    }
}
