using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ObjectId[] BreakCurveAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
        {
            return SplitCurveKeepParts(entityId, BuildPointSplitParameters(entityId, new Point3d(x, y, z)), keepMode: "all", eraseSource: eraseSource);
        }

        public ObjectId[] BreakCurveAtDistance(ObjectId entityId, double distance, bool eraseSource)
        {
            return SplitCurveKeepParts(entityId, BuildDistanceSplitParameters(entityId, new[] { distance }), keepMode: "all", eraseSource: eraseSource);
        }

        public ObjectId[] BreakCurveAtTwoPoints(ObjectId entityId, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
        {
            return SplitCurveKeepParts(
                entityId,
                BuildPointSplitParameters(entityId, new Point3d(x1, y1, z1), new Point3d(x2, y2, z2)),
                keepMode: "outer",
                eraseSource: eraseSource);
        }

        public ObjectId[] BreakCurveAtTwoDistances(ObjectId entityId, double firstDistance, double secondDistance, bool eraseSource)
        {
            return SplitCurveKeepParts(
                entityId,
                BuildDistanceSplitParameters(entityId, new[] { firstDistance, secondDistance }),
                keepMode: "outer",
                eraseSource: eraseSource);
        }

        public ObjectId TrimCurveStartAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
        {
            return SplitCurveKeepSingle(entityId, BuildPointSplitParameters(entityId, new Point3d(x, y, z)), "last", eraseSource);
        }

        public ObjectId TrimCurveEndAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
        {
            return SplitCurveKeepSingle(entityId, BuildPointSplitParameters(entityId, new Point3d(x, y, z)), "first", eraseSource);
        }

        public ObjectId TrimCurveStartAtDistance(ObjectId entityId, double distance, bool eraseSource)
        {
            return SplitCurveKeepSingle(entityId, BuildDistanceSplitParameters(entityId, new[] { distance }), "last", eraseSource);
        }

        public ObjectId TrimCurveEndAtDistance(ObjectId entityId, double distance, bool eraseSource)
        {
            return SplitCurveKeepSingle(entityId, BuildDistanceSplitParameters(entityId, new[] { distance }), "first", eraseSource);
        }

        public ObjectId KeepCurveSegmentBetweenPoints(ObjectId entityId, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
        {
            return SplitCurveKeepSingle(
                entityId,
                BuildPointSplitParameters(entityId, new Point3d(x1, y1, z1), new Point3d(x2, y2, z2)),
                "middle",
                eraseSource);
        }

        public ObjectId KeepCurveSegmentBetweenDistances(ObjectId entityId, double firstDistance, double secondDistance, bool eraseSource)
        {
            return SplitCurveKeepSingle(
                entityId,
                BuildDistanceSplitParameters(entityId, new[] { firstDistance, secondDistance }),
                "middle",
                eraseSource);
        }

        public ObjectId[] RemoveCurveSegmentBetweenPoints(ObjectId entityId, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
        {
            return SplitCurveKeepParts(
                entityId,
                BuildPointSplitParameters(entityId, new Point3d(x1, y1, z1), new Point3d(x2, y2, z2)),
                keepMode: "outer",
                eraseSource: eraseSource);
        }

        public ObjectId[] RemoveCurveSegmentBetweenDistances(ObjectId entityId, double firstDistance, double secondDistance, bool eraseSource)
        {
            return SplitCurveKeepParts(
                entityId,
                BuildDistanceSplitParameters(entityId, new[] { firstDistance, secondDistance }),
                keepMode: "outer",
                eraseSource: eraseSource);
        }

        public void ReverseCurve(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForWrite) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                MethodInfo mi = curve.GetType().GetMethod("ReverseCurve", Type.EmptyTypes);
                if (mi != null)
                {
                    mi.Invoke(curve, null);
                    tr.Commit();
                    return;
                }

                if (curve is Line)
                {
                    ReverseLineInternal((Line)curve);
                    tr.Commit();
                    return;
                }

                if (curve is Polyline)
                {
                    ReversePolylineInternal((Polyline)curve);
                    tr.Commit();
                    return;
                }

                if (curve is Arc)
                {
                    ReverseArcInternal((Arc)curve);
                    tr.Commit();
                    return;
                }

                throw new NotSupportedException("ReverseCurve non supportato da " + curve.GetType().Name);
            }
        }

        public int ReverseCurves(IList entityIds)
        {
            int changed = 0;
            foreach (object raw in entityIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }

                ReverseCurve((ObjectId)raw);
                changed++;
            }

            return changed;
        }

        public Hashtable JoinEntities(ObjectId primaryEntityId, IList otherEntityIds, bool eraseJoinedSources)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity primary = tr.GetObject(primaryEntityId, OpenMode.ForWrite) as Entity;
                if (primary == null)
                {
                    throw new ArgumentException("primaryEntityId non identifica una Entity");
                }

                int requestedCount = 0;
                int joinedCount = 0;
                bool usedFallback = false;

                MethodInfo joinMethod = primary.GetType().GetMethod("JoinEntities", new[] { typeof(DBObjectCollection) });
                List<ObjectId> otherIds = new List<ObjectId>();
                foreach (object raw in otherEntityIds)
                {
                    if (raw is ObjectId && (ObjectId)raw != primaryEntityId)
                    {
                        otherIds.Add((ObjectId)raw);
                    }
                }
                requestedCount = otherIds.Count;

                if (joinMethod != null)
                {
                    DBObjectCollection others = new DBObjectCollection();
                    foreach (ObjectId otherId in otherIds)
                    {
                        Entity other = tr.GetObject(otherId, OpenMode.ForWrite) as Entity;
                        if (other != null)
                        {
                            others.Add(other);
                        }
                    }

                    try
                    {
                        object rawResult = joinMethod.Invoke(primary, new object[] { others });
                        joinedCount = ExtractJoinResultCount(rawResult);
                    }
                    catch
                    {
                        joinedCount = 0;
                    }
                }

                if (joinedCount == 0)
                {
                    joinedCount = TryJoinEntitiesFallback(primary, otherIds, tr);
                    usedFallback = joinedCount > 0;
                }

                bool erased = false;
                if (eraseJoinedSources && joinedCount == requestedCount && joinedCount > 0)
                {
                    foreach (ObjectId otherId in otherIds)
                    {
                        Entity other = tr.GetObject(otherId, OpenMode.ForWrite) as Entity;
                        if (other != null && !other.IsErased)
                        {
                            other.Erase(true);
                        }
                    }
                    erased = true;
                }

                Hashtable info = new Hashtable();
                info["primary_id"] = primaryEntityId.ToString();
                info["primary_handle"] = primary.Handle.ToString();
                info["primary_type"] = primary.GetType().Name;
                info["requested_count"] = requestedCount;
                info["joined_count"] = joinedCount;
                info["erased_joined_sources"] = erased;
                info["used_fallback"] = usedFallback;
                info["success"] = joinedCount > 0;

                tr.Commit();
                return info;
            }
        }

        public Hashtable JoinAllEntities(IList entityIds, bool eraseJoinedSources)
        {
            if (entityIds == null || entityIds.Count < 2 || !(entityIds[0] is ObjectId))
            {
                throw new ArgumentException("entityIds deve contenere almeno 2 ObjectId");
            }

            ArrayList others = new ArrayList();
            for (int i = 1; i < entityIds.Count; i++)
            {
                others.Add(entityIds[i]);
            }

            return JoinEntities((ObjectId)entityIds[0], others, eraseJoinedSources);
        }

        public Point3d ExtendLineStartToPoint(ObjectId entityId, double x, double y, double z)
        {
            return ExtendLineInternal(entityId, new Point3d(x, y, z), true);
        }

        public Point3d ExtendLineEndToPoint(ObjectId entityId, double x, double y, double z)
        {
            return ExtendLineInternal(entityId, new Point3d(x, y, z), false);
        }

        public Point3d ExtendPolylineStartToPoint(ObjectId entityId, double x, double y)
        {
            return ExtendPolylineInternal(entityId, new Point2d(x, y), true);
        }

        public Point3d ExtendPolylineEndToPoint(ObjectId entityId, double x, double y)
        {
            return ExtendPolylineInternal(entityId, new Point2d(x, y), false);
        }

        private ObjectId[] SplitCurveKeepParts(ObjectId entityId, DoubleCollection parameters, string keepMode, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve source = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (source == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                if ((keepMode == "outer" || keepMode == "middle" || keepMode == "first" || keepMode == "last") && IsClosedCurveForModify(source))
                {
                    throw new NotSupportedException("Operazione supportata solo su curve aperte");
                }

                List<Entity> parts = ExtractSplitParts(source.GetSplitCurves(parameters));
                List<Entity> keep = SelectSplitParts(parts, keepMode);
                ObjectId[] created = AppendSplitPartsToOwner(tr, source.OwnerId, keep);
                DisposeUnselectedSplitParts(parts, keep);

                if (eraseSource)
                {
                    Entity writable = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                    if (writable != null && !writable.IsErased)
                    {
                        writable.Erase(true);
                    }
                }

                tr.Commit();
                return created;
            }
        }

        private ObjectId SplitCurveKeepSingle(ObjectId entityId, DoubleCollection parameters, string keepMode, bool eraseSource)
        {
            ObjectId[] ids = SplitCurveKeepParts(entityId, parameters, keepMode, eraseSource);
            if (ids.Length != 1)
            {
                throw new InvalidOperationException("L'operazione non ha prodotto una singola curva");
            }

            return ids[0];
        }

        private DoubleCollection BuildPointSplitParameters(ObjectId entityId, params Point3d[] points)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                List<double> values = new List<double>();
                foreach (Point3d point in points)
                {
                    Point3d onCurve = curve.GetClosestPointTo(point, false);
                    values.Add(curve.GetParameterAtPoint(onCurve));
                }

                return BuildSortedUniqueParameters(values);
            }
        }

        private DoubleCollection BuildDistanceSplitParameters(ObjectId entityId, IEnumerable<double> distances)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non e una curva");
                }

                List<double> values = new List<double>();
                foreach (double distance in distances)
                {
                    values.Add(curve.GetParameterAtDistance(distance));
                }

                return BuildSortedUniqueParameters(values);
            }
        }

        private static DoubleCollection BuildSortedUniqueParameters(IEnumerable<double> rawValues)
        {
            const double tol = 1e-9;
            List<double> values = new List<double>(rawValues);
            values.Sort();

            DoubleCollection result = new DoubleCollection();
            double? last = null;
            foreach (double value in values)
            {
                if (!last.HasValue || Math.Abs(value - last.Value) > tol)
                {
                    result.Add(value);
                    last = value;
                }
            }

            if (result.Count == 0)
            {
                throw new ArgumentException("Nessun parametro di split valido");
            }

            return result;
        }

        private static List<Entity> ExtractSplitParts(DBObjectCollection parts)
        {
            List<Entity> entities = new List<Entity>();
            foreach (DBObject dbo in parts)
            {
                Entity entity = dbo as Entity;
                if (entity != null)
                {
                    entities.Add(entity);
                }
            }

            if (entities.Count == 0)
            {
                throw new InvalidOperationException("Lo split non ha prodotto curve valide");
            }

            return entities;
        }

        private static List<Entity> SelectSplitParts(List<Entity> parts, string keepMode)
        {
            switch (keepMode)
            {
                case "all":
                    return parts;
                case "first":
                    if (parts.Count < 2) throw new InvalidOperationException("Impossibile trimmare la fine della curva");
                    return new List<Entity> { parts[0] };
                case "last":
                    if (parts.Count < 2) throw new InvalidOperationException("Impossibile trimmare l'inizio della curva");
                    return new List<Entity> { parts[parts.Count - 1] };
                case "middle":
                    if (parts.Count < 3) throw new InvalidOperationException("Impossibile isolare il segmento centrale");
                    return new List<Entity> { parts[1] };
                case "outer":
                    if (parts.Count < 3) throw new InvalidOperationException("Impossibile rimuovere il tratto centrale");
                    return new List<Entity> { parts[0], parts[parts.Count - 1] };
                default:
                    throw new ArgumentException("keepMode non supportato");
            }
        }

        private static void DisposeUnselectedSplitParts(List<Entity> allParts, List<Entity> selectedParts)
        {
            HashSet<Entity> keep = new HashSet<Entity>(selectedParts);
            foreach (Entity entity in allParts)
            {
                if (!keep.Contains(entity))
                {
                    entity.Dispose();
                }
            }
        }

        private static ObjectId[] AppendSplitPartsToOwner(Transaction tr, ObjectId ownerId, IEnumerable<Entity> entities)
        {
            BlockTableRecord owner = tr.GetObject(ownerId, OpenMode.ForWrite) as BlockTableRecord;
            if (owner == null)
            {
                throw new InvalidOperationException("Owner della curva non valido");
            }

            List<ObjectId> ids = new List<ObjectId>();
            foreach (Entity entity in entities)
            {
                ObjectId id = owner.AppendEntity(entity);
                tr.AddNewlyCreatedDBObject(entity, true);
                ids.Add(id);
            }

            return ids.ToArray();
        }

        private static bool IsClosedCurveForModify(Curve curve)
        {
            if (curve is Circle)
            {
                return true;
            }

            Polyline pl = curve as Polyline;
            if (pl != null)
            {
                return pl.Closed;
            }

            Ellipse ell = curve as Ellipse;
            if (ell != null)
            {
                return Math.Abs((ell.EndAngle - ell.StartAngle) - (Math.PI * 2.0)) < 1e-6;
            }

            PropertyInfo pi = curve.GetType().GetProperty("Closed");
            if (pi != null && pi.PropertyType == typeof(bool))
            {
                return (bool)pi.GetValue(curve, null);
            }

            return false;
        }

        private static int ExtractJoinResultCount(object rawResult)
        {
            if (rawResult == null)
            {
                return 0;
            }

            ICollection col = rawResult as ICollection;
            if (col != null)
            {
                return col.Count;
            }

            PropertyInfo countProp = rawResult.GetType().GetProperty("Count");
            if (countProp != null)
            {
                object value = countProp.GetValue(rawResult, null);
                if (value != null)
                {
                    return Convert.ToInt32(value);
                }
            }

            return 0;
        }

        private Point3d ExtendLineInternal(ObjectId entityId, Point3d target, bool atStart)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(entityId, OpenMode.ForWrite) as Line;
                if (line == null)
                {
                    throw new ArgumentException("L'entita non e una Line");
                }

                Point3d extended = line.GetClosestPointTo(target, true);
                if (atStart)
                {
                    line.StartPoint = extended;
                }
                else
                {
                    line.EndPoint = extended;
                }

                tr.Commit();
                return extended;
            }
        }

        private Point3d ExtendPolylineInternal(ObjectId entityId, Point2d target, bool atStart)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline;
                if (pl == null)
                {
                    throw new ArgumentException("L'entita non e una Polyline 2D");
                }

                int segmentIndex = atStart ? 0 : GetPolylineSegmentCountInternal(pl) - 1;
                if (segmentIndex < 0)
                {
                    throw new InvalidOperationException("La polyline non ha segmenti");
                }

                if (pl.GetSegmentType(segmentIndex) != SegmentType.Line)
                {
                    throw new NotSupportedException("L'estensione della polyline e supportata solo se il segmento iniziale/finale e lineare");
                }

                LineSegment2d seg = pl.GetLineSegment2dAt(segmentIndex);
                Point2d extended = ProjectPointOnInfiniteLine(seg.StartPoint, seg.EndPoint, target);
                int vertexIndex = atStart ? 0 : pl.NumberOfVertices - 1;
                pl.SetPointAt(vertexIndex, extended);
                tr.Commit();
                return new Point3d(extended.X, extended.Y, 0.0);
            }
        }

        private static Point2d ProjectPointOnInfiniteLine(Point2d a, Point2d b, Point2d p)
        {
            Vector2d ab = b - a;
            if (ab.Length <= 1e-9)
            {
                throw new InvalidOperationException("Segmento di riferimento degenerato");
            }

            Vector2d ap = p - a;
            double t = ap.DotProduct(ab) / ab.DotProduct(ab);
            return a + (ab * t);
        }

        private static void ReverseLineInternal(Line line)
        {
            Point3d start = line.StartPoint;
            line.StartPoint = line.EndPoint;
            line.EndPoint = start;
        }

        private static void ReverseArcInternal(Arc arc)
        {
            double start = arc.StartAngle;
            arc.StartAngle = arc.EndAngle;
            arc.EndAngle = start;
        }

        private static void ReversePolylineInternal(Polyline pl)
        {
            int count = pl.NumberOfVertices;
            if (count < 2)
            {
                return;
            }

            Point2d[] points = new Point2d[count];
            double[] bulges = new double[count];
            double[] startWidths = new double[count];
            double[] endWidths = new double[count];

            for (int i = 0; i < count; i++)
            {
                points[i] = pl.GetPoint2dAt(i);
                bulges[i] = pl.GetBulgeAt(i);
                startWidths[i] = pl.GetStartWidthAt(i);
                endWidths[i] = pl.GetEndWidthAt(i);
            }

            bool closed = pl.Closed;
            for (int newIndex = 0; newIndex < count; newIndex++)
            {
                int oldIndex = count - 1 - newIndex;
                int sourceSegmentIndex;
                if (closed)
                {
                    sourceSegmentIndex = (oldIndex - 1 + count) % count;
                }
                else
                {
                    sourceSegmentIndex = oldIndex - 1;
                }

                double bulge = 0.0;
                double sw = 0.0;
                double ew = 0.0;
                if (sourceSegmentIndex >= 0)
                {
                    bulge = -bulges[sourceSegmentIndex];
                    sw = endWidths[sourceSegmentIndex];
                    ew = startWidths[sourceSegmentIndex];
                }

                pl.SetPointAt(newIndex, points[oldIndex]);
                pl.SetBulgeAt(newIndex, bulge);
                pl.SetStartWidthAt(newIndex, sw);
                pl.SetEndWidthAt(newIndex, ew);
            }

            pl.Closed = closed;
        }

        private static int TryJoinEntitiesFallback(Entity primary, IList<ObjectId> otherIds, Transaction tr)
        {
            Polyline primaryPolyline = primary as Polyline;
            if (primaryPolyline == null || primaryPolyline.Closed)
            {
                return 0;
            }

            int joined = 0;
            foreach (ObjectId otherId in otherIds)
            {
                Entity other = tr.GetObject(otherId, OpenMode.ForRead) as Entity;
                if (other == null)
                {
                    continue;
                }

                if (TryAppendToPolyline(primaryPolyline, other))
                {
                    joined++;
                }
            }

            return joined;
        }

        private static bool TryAppendToPolyline(Polyline primary, Entity other)
        {
            Polyline otherPolyline = other as Polyline;
            if (otherPolyline != null && !otherPolyline.Closed)
            {
                return TryAppendPolyline(primary, otherPolyline);
            }

            Line otherLine = other as Line;
            if (otherLine != null)
            {
                return TryAppendLine(primary, otherLine);
            }

            return false;
        }

        private static bool TryAppendLine(Polyline primary, Line other)
        {
            Point2d pStart = primary.GetPoint2dAt(0);
            Point2d pEnd = primary.GetPoint2dAt(primary.NumberOfVertices - 1);
            Point2d lStart = new Point2d(other.StartPoint.X, other.StartPoint.Y);
            Point2d lEnd = new Point2d(other.EndPoint.X, other.EndPoint.Y);

            if (PointsEqual2d(pEnd, lStart))
            {
                primary.AddVertexAt(primary.NumberOfVertices, lEnd, 0.0, 0.0, 0.0);
                return true;
            }

            if (PointsEqual2d(pEnd, lEnd))
            {
                primary.AddVertexAt(primary.NumberOfVertices, lStart, 0.0, 0.0, 0.0);
                return true;
            }

            if (PointsEqual2d(pStart, lEnd))
            {
                PrependVertex(primary, lStart);
                return true;
            }

            if (PointsEqual2d(pStart, lStart))
            {
                PrependVertex(primary, lEnd);
                return true;
            }

            return false;
        }

        private static bool TryAppendPolyline(Polyline primary, Polyline other)
        {
            Point2d pStart = primary.GetPoint2dAt(0);
            Point2d pEnd = primary.GetPoint2dAt(primary.NumberOfVertices - 1);
            Point2d oStart = other.GetPoint2dAt(0);
            Point2d oEnd = other.GetPoint2dAt(other.NumberOfVertices - 1);

            if (PointsEqual2d(pEnd, oStart))
            {
                AppendPolylineVertices(primary, other, false);
                return true;
            }

            if (PointsEqual2d(pEnd, oEnd))
            {
                AppendPolylineVertices(primary, other, true);
                return true;
            }

            if (PointsEqual2d(pStart, oEnd))
            {
                PrependPolylineVertices(primary, other, false);
                return true;
            }

            if (PointsEqual2d(pStart, oStart))
            {
                PrependPolylineVertices(primary, other, true);
                return true;
            }

            return false;
        }

        private static void AppendPolylineVertices(Polyline primary, Polyline other, bool reverseOther)
        {
            int count = other.NumberOfVertices;
            for (int i = 1; i < count; i++)
            {
                int sourceIndex = reverseOther ? (count - 1 - i) : i;
                Point2d pt = other.GetPoint2dAt(sourceIndex);
                primary.AddVertexAt(primary.NumberOfVertices, pt, 0.0, 0.0, 0.0);
            }
        }

        private static void PrependPolylineVertices(Polyline primary, Polyline other, bool reverseOther)
        {
            List<Point2d> points = new List<Point2d>();
            int count = other.NumberOfVertices;
            for (int i = 1; i < count; i++)
            {
                int sourceIndex = reverseOther ? i : (count - 1 - i);
                points.Add(other.GetPoint2dAt(sourceIndex));
            }

            for (int i = points.Count - 1; i >= 0; i--)
            {
                PrependVertex(primary, points[i]);
            }
        }

        private static void PrependVertex(Polyline pl, Point2d pt)
        {
            pl.AddVertexAt(0, pt, 0.0, 0.0, 0.0);
        }

        private static bool PointsEqual2d(Point2d a, Point2d b)
        {
            return a.GetDistanceTo(b) <= 1e-6;
        }
    }
}
