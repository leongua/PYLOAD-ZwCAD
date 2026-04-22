using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public Hashtable FilletLines(ObjectId firstLineId, ObjectId secondLineId, double radius, bool trimLines)
        {
            if (radius < 0.0)
            {
                throw new ArgumentException("radius deve essere >= 0");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line first = tr.GetObject(firstLineId, OpenMode.ForWrite) as Line;
                Line second = tr.GetObject(secondLineId, OpenMode.ForWrite) as Line;
                if (first == null || second == null)
                {
                    throw new ArgumentException("FilletLines supporta solo due Line");
                }

                Point2d intersection = IntersectInfiniteLines(ToPoint2d(first.StartPoint), ToPoint2d(first.EndPoint), ToPoint2d(second.StartPoint), ToPoint2d(second.EndPoint));
                bool trimFirstStart;
                bool trimSecondStart;
                Vector2d dir1 = GetLineDirectionAwayFromIntersection(first, intersection, out trimFirstStart);
                Vector2d dir2 = GetLineDirectionAwayFromIntersection(second, intersection, out trimSecondStart);

                double dot = Clamp(dir1.DotProduct(dir2), -1.0, 1.0);
                double angle = Math.Acos(dot);
                if (angle <= 1e-6 || Math.Abs(Math.PI - angle) <= 1e-6)
                {
                    throw new InvalidOperationException("Le linee sono parallele o degeneri per il fillet");
                }

                double offset = radius <= 1e-9 ? 0.0 : radius / Math.Tan(angle * 0.5);
                Point2d tangent1 = intersection + (dir1 * offset);
                Point2d tangent2 = intersection + (dir2 * offset);

                Point2d center2d = intersection;
                if (radius > 1e-9)
                {
                    Vector2d bisector = dir1 + dir2;
                    if (bisector.Length <= 1e-9)
                    {
                        throw new InvalidOperationException("Impossibile calcolare il bisettore del fillet");
                    }

                    bisector = bisector.GetNormal();
                    double centerDistance = radius / Math.Sin(angle * 0.5);
                    center2d = intersection + (bisector * centerDistance);
                }

                if (trimLines)
                {
                    SetLineEndpoint(first, trimFirstStart, tangent1);
                    SetLineEndpoint(second, trimSecondStart, tangent2);
                }

                Arc arc = new Arc(
                    new Point3d(center2d.X, center2d.Y, first.StartPoint.Z),
                    radius,
                    Math.Atan2(tangent1.Y - center2d.Y, tangent1.X - center2d.X),
                    Math.Atan2(tangent2.Y - center2d.Y, tangent2.X - center2d.X));
                EnsureMinorArc(arc);

                BlockTableRecord owner = tr.GetObject(first.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                ObjectId arcId = owner.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);

                Hashtable info = new Hashtable();
                info["arc_id"] = arcId.ToString();
                info["arc_handle"] = arc.Handle.ToString();
                info["center_x"] = center2d.X;
                info["center_y"] = center2d.Y;
                info["tangent1_x"] = tangent1.X;
                info["tangent1_y"] = tangent1.Y;
                info["tangent2_x"] = tangent2.X;
                info["tangent2_y"] = tangent2.Y;
                info["trimmed"] = trimLines;

                tr.Commit();
                return info;
            }
        }

        public Hashtable ChamferLines(ObjectId firstLineId, ObjectId secondLineId, double firstDistance, double secondDistance, bool trimLines)
        {
            if (firstDistance < 0.0 || secondDistance < 0.0)
            {
                throw new ArgumentException("Le distanze del chamfer devono essere >= 0");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Line first = tr.GetObject(firstLineId, OpenMode.ForWrite) as Line;
                Line second = tr.GetObject(secondLineId, OpenMode.ForWrite) as Line;
                if (first == null || second == null)
                {
                    throw new ArgumentException("ChamferLines supporta solo due Line");
                }

                Point2d intersection = IntersectInfiniteLines(ToPoint2d(first.StartPoint), ToPoint2d(first.EndPoint), ToPoint2d(second.StartPoint), ToPoint2d(second.EndPoint));
                bool trimFirstStart;
                bool trimSecondStart;
                Vector2d dir1 = GetLineDirectionAwayFromIntersection(first, intersection, out trimFirstStart);
                Vector2d dir2 = GetLineDirectionAwayFromIntersection(second, intersection, out trimSecondStart);

                Point2d cut1 = intersection + (dir1 * firstDistance);
                Point2d cut2 = intersection + (dir2 * secondDistance);

                if (trimLines)
                {
                    SetLineEndpoint(first, trimFirstStart, cut1);
                    SetLineEndpoint(second, trimSecondStart, cut2);
                }

                Line chamfer = new Line(
                    new Point3d(cut1.X, cut1.Y, first.StartPoint.Z),
                    new Point3d(cut2.X, cut2.Y, first.StartPoint.Z));

                BlockTableRecord owner = tr.GetObject(first.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                ObjectId chamferId = owner.AppendEntity(chamfer);
                tr.AddNewlyCreatedDBObject(chamfer, true);

                Hashtable info = new Hashtable();
                info["line_id"] = chamferId.ToString();
                info["line_handle"] = chamfer.Handle.ToString();
                info["cut1_x"] = cut1.X;
                info["cut1_y"] = cut1.Y;
                info["cut2_x"] = cut2.X;
                info["cut2_y"] = cut2.Y;
                info["trimmed"] = trimLines;

                tr.Commit();
                return info;
            }
        }

        public Hashtable StretchEntitiesCrossingWindow(IList entityIds, double x1, double y1, double z1, double x2, double y2, double z2, double dx, double dy, double dz, bool moveContainedEntities)
        {
            Point3d min = new Point3d(Math.Min(x1, x2), Math.Min(y1, y2), Math.Min(z1, z2));
            Point3d max = new Point3d(Math.Max(x1, x2), Math.Max(y1, y2), Math.Max(z1, z2));
            Vector3d disp = new Vector3d(dx, dy, dz);

            int touchedEntities = 0;
            int movedVertices = 0;
            int movedWholeEntities = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)raw, OpenMode.ForWrite) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    int localVertices = StretchEntityInternal(entity, min, max, disp, moveContainedEntities);
                    if (localVertices > 0)
                    {
                        touchedEntities++;
                        movedVertices += localVertices;
                    }
                    else if (localVertices < 0)
                    {
                        touchedEntities++;
                        movedWholeEntities++;
                    }
                }

                tr.Commit();
            }

            Hashtable info = new Hashtable();
            info["touched_entities"] = touchedEntities;
            info["moved_vertices"] = movedVertices;
            info["moved_whole_entities"] = movedWholeEntities;
            return info;
        }

        public ObjectId TrimCurveStartToEntity(ObjectId entityId, ObjectId boundaryId, bool eraseSource)
        {
            Point3d target = GetBestIntersectionPoint(entityId, boundaryId, true, false);
            return TrimCurveStartAtPoint(entityId, target.X, target.Y, target.Z, eraseSource);
        }

        public ObjectId TrimCurveEndToEntity(ObjectId entityId, ObjectId boundaryId, bool eraseSource)
        {
            Point3d target = GetBestIntersectionPoint(entityId, boundaryId, false, false);
            return TrimCurveEndAtPoint(entityId, target.X, target.Y, target.Z, eraseSource);
        }

        public Point3d ExtendCurveStartToEntity(ObjectId entityId, ObjectId boundaryId)
        {
            Point3d target = GetBestIntersectionPoint(entityId, boundaryId, true, true);
            return ExtendCurveToPoint(entityId, target, true);
        }

        public Point3d ExtendCurveEndToEntity(ObjectId entityId, ObjectId boundaryId)
        {
            Point3d target = GetBestIntersectionPoint(entityId, boundaryId, false, true);
            return ExtendCurveToPoint(entityId, target, false);
        }

        public ObjectId[] OffsetEntityBothSides(ObjectId entityId, double offsetDistance)
        {
            List<ObjectId> ids = new List<ObjectId>();
            ids.AddRange(OffsetEntity(entityId, Math.Abs(offsetDistance)));
            ids.AddRange(OffsetEntity(entityId, -Math.Abs(offsetDistance)));
            return ids.ToArray();
        }

        public int OffsetEntitiesBothSides(IList entityIds, double offsetDistance)
        {
            int total = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    total += OffsetEntityBothSides((ObjectId)raw, offsetDistance).Length;
                }
            }
            return total;
        }

        public ObjectId[] CopyEntities(IList entityIds, double dx, double dy, double dz)
        {
            List<ObjectId> created = new List<ObjectId>();
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    created.Add(CopyEntity((ObjectId)raw, dx, dy, dz));
                }
            }
            return created.ToArray();
        }

        public ObjectId[] CopyEntityMultiple(ObjectId entityId, IList coordinates)
        {
            if (coordinates == null || coordinates.Count < 3 || coordinates.Count % 3 != 0)
            {
                throw new ArgumentException("coordinates deve contenere triple dx,dy,dz");
            }

            List<ObjectId> created = new List<ObjectId>();
            for (int i = 0; i < coordinates.Count; i += 3)
            {
                created.Add(CopyEntity(
                    entityId,
                    Convert.ToDouble(coordinates[i]),
                    Convert.ToDouble(coordinates[i + 1]),
                    Convert.ToDouble(coordinates[i + 2])));
            }
            return created.ToArray();
        }

        public ObjectId[] CopyEntitiesMultiple(IList entityIds, IList coordinates)
        {
            List<ObjectId> created = new List<ObjectId>();
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    created.AddRange(CopyEntityMultiple((ObjectId)raw, coordinates));
                }
            }
            return created.ToArray();
        }

        public ObjectId[] ArrayRectangularEntityEx(ObjectId entityId, int rows, int columns, int levels, double rowSpacing, double columnSpacing, double levelSpacing, bool eraseSource)
        {
            ObjectId[] created = ArrayRectangularEntity(entityId, rows, columns, levels, rowSpacing, columnSpacing, levelSpacing);
            if (eraseSource)
            {
                EraseEntity(entityId);
            }
            return created;
        }

        public ObjectId[] ArrayPolarEntityEx(ObjectId entityId, int itemCount, double centerX, double centerY, double centerZ, double fillAngleDegrees, bool rotateItems, bool eraseSource)
        {
            ObjectId[] created = ArrayPolarEntity(entityId, itemCount, centerX, centerY, centerZ, fillAngleDegrees, rotateItems);
            if (eraseSource)
            {
                EraseEntity(entityId);
            }
            return created;
        }

        public Hashtable TrimCurvesToBoundaries(IList curveIds, IList boundaryIds, string trimMode, bool eraseSource)
        {
            int changed = 0;
            ArrayList results = new ArrayList();

            foreach (object raw in curveIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }

                ObjectId curveId = (ObjectId)raw;
                Point3d? startHit = TryGetBestIntersectionPointFromList(curveId, boundaryIds, true, false);
                Point3d? endHit = TryGetBestIntersectionPointFromList(curveId, boundaryIds, false, false);
                string mode = (trimMode ?? "nearest").Trim().ToLowerInvariant();
                ObjectId newId = ObjectId.Null;

                if ((mode == "start" || mode == "nearest") && startHit.HasValue && !endHit.HasValue)
                {
                    newId = TrimCurveStartAtPoint(curveId, startHit.Value.X, startHit.Value.Y, startHit.Value.Z, eraseSource);
                }
                else if ((mode == "end" || mode == "nearest") && endHit.HasValue && !startHit.HasValue)
                {
                    newId = TrimCurveEndAtPoint(curveId, endHit.Value.X, endHit.Value.Y, endHit.Value.Z, eraseSource);
                }
                else if (startHit.HasValue && endHit.HasValue)
                {
                    if (mode == "start")
                    {
                        newId = TrimCurveStartAtPoint(curveId, startHit.Value.X, startHit.Value.Y, startHit.Value.Z, eraseSource);
                    }
                    else if (mode == "end")
                    {
                        newId = TrimCurveEndAtPoint(curveId, endHit.Value.X, endHit.Value.Y, endHit.Value.Z, eraseSource);
                    }
                    else
                    {
                        double startDist = DistanceToCurveEndpoint(curveId, startHit.Value, true);
                        double endDist = DistanceToCurveEndpoint(curveId, endHit.Value, false);
                        newId = startDist <= endDist
                            ? TrimCurveStartAtPoint(curveId, startHit.Value.X, startHit.Value.Y, startHit.Value.Z, eraseSource)
                            : TrimCurveEndAtPoint(curveId, endHit.Value.X, endHit.Value.Y, endHit.Value.Z, eraseSource);
                    }
                }

                if (!newId.IsNull)
                {
                    changed++;
                    results.Add(newId);
                }
            }

            Hashtable info = new Hashtable();
            info["changed"] = changed;
            info["result_ids"] = results;
            return info;
        }

        public Hashtable ExtendCurvesToBoundaries(IList curveIds, IList boundaryIds, string extendMode)
        {
            int changed = 0;
            ArrayList points = new ArrayList();

            foreach (object raw in curveIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }

                ObjectId curveId = (ObjectId)raw;
                Point3d? startHit = TryGetBestIntersectionPointFromList(curveId, boundaryIds, true, true);
                Point3d? endHit = TryGetBestIntersectionPointFromList(curveId, boundaryIds, false, true);
                string mode = (extendMode ?? "nearest").Trim().ToLowerInvariant();
                Point3d movedPoint = Point3d.Origin;
                bool done = false;

                if ((mode == "start" || mode == "nearest") && startHit.HasValue && !endHit.HasValue)
                {
                    movedPoint = ExtendCurveToPoint(curveId, startHit.Value, true);
                    done = true;
                }
                else if ((mode == "end" || mode == "nearest") && endHit.HasValue && !startHit.HasValue)
                {
                    movedPoint = ExtendCurveToPoint(curveId, endHit.Value, false);
                    done = true;
                }
                else if (startHit.HasValue && endHit.HasValue)
                {
                    if (mode == "start")
                    {
                        movedPoint = ExtendCurveToPoint(curveId, startHit.Value, true);
                    }
                    else if (mode == "end")
                    {
                        movedPoint = ExtendCurveToPoint(curveId, endHit.Value, false);
                    }
                    else
                    {
                        double startDist = DistanceToCurveEndpoint(curveId, startHit.Value, true);
                        double endDist = DistanceToCurveEndpoint(curveId, endHit.Value, false);
                        movedPoint = startDist <= endDist
                            ? ExtendCurveToPoint(curveId, startHit.Value, true)
                            : ExtendCurveToPoint(curveId, endHit.Value, false);
                    }
                    done = true;
                }

                if (done)
                {
                    changed++;
                    Hashtable pt = new Hashtable();
                    pt["x"] = movedPoint.X;
                    pt["y"] = movedPoint.Y;
                    pt["z"] = movedPoint.Z;
                    points.Add(pt);
                }
            }

            Hashtable info = new Hashtable();
            info["changed"] = changed;
            info["points"] = points;
            return info;
        }

        public ObjectId[] BreakCurveAtAllIntersections(ObjectId curveId, IList boundaryIds, bool eraseSource)
        {
            List<double> parameters = new List<double>();

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(curveId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("curveId non identifica una curva");
                }

                foreach (object raw in boundaryIds)
                {
                    if (!(raw is ObjectId))
                    {
                        continue;
                    }

                    Entity boundary = tr.GetObject((ObjectId)raw, OpenMode.ForRead) as Entity;
                    if (boundary == null)
                    {
                        continue;
                    }

                    Point3dCollection pts = new Point3dCollection();
                    curve.IntersectWith(boundary, Intersect.OnBothOperands, pts, 0, 0);
                    foreach (Point3d pt in pts)
                    {
                        Point3d onCurve = curve.GetClosestPointTo(pt, false);
                        parameters.Add(curve.GetParameterAtPoint(onCurve));
                    }
                }
            }

            DoubleCollection splitParams = BuildSortedUniqueParameters(parameters);
            return SplitCurveKeepParts(curveId, splitParams, "all", eraseSource);
        }

        public Hashtable BreakEntitiesAtIntersections(IList entityIds, bool eraseSource)
        {
            ArrayList createdGroups = new ArrayList();
            int changed = 0;

            List<ObjectId> ids = new List<ObjectId>();
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    ids.Add((ObjectId)raw);
                }
            }

            for (int i = 0; i < ids.Count; i++)
            {
                ArrayList boundaries = new ArrayList();
                for (int j = 0; j < ids.Count; j++)
                {
                    if (i != j)
                    {
                        boundaries.Add(ids[j]);
                    }
                }

                try
                {
                    ObjectId[] parts = BreakCurveAtAllIntersections(ids[i], boundaries, eraseSource);
                    if (parts.Length > 0)
                    {
                        changed++;
                        createdGroups.Add(parts);
                    }
                }
                catch
                {
                }
            }

            Hashtable info = new Hashtable();
            info["changed"] = changed;
            info["groups"] = createdGroups;
            return info;
        }

        public ObjectId OffsetEntityTowardPoint(ObjectId entityId, double offsetDistance, double x, double y, double z)
        {
            ObjectId[] candidates = OffsetEntityBothSides(entityId, offsetDistance);
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException("Offset non ha prodotto entita");
            }

            Point3d seed = new Point3d(x, y, z);
            ObjectId best = candidates[0];
            double bestDist = DistancePointToEntity(seed, best);
            for (int i = 1; i < candidates.Length; i++)
            {
                double dist = DistancePointToEntity(seed, candidates[i]);
                if (dist < bestDist)
                {
                    best = candidates[i];
                    bestDist = dist;
                }
            }

            foreach (ObjectId id in candidates)
            {
                if (id != best)
                {
                    EraseEntity(id);
                }
            }

            return best;
        }

        public ObjectId[] OffsetEntitiesTowardPoint(IList entityIds, double offsetDistance, double x, double y, double z)
        {
            List<ObjectId> ids = new List<ObjectId>();
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    ids.Add(OffsetEntityTowardPoint((ObjectId)raw, offsetDistance, x, y, z));
                }
            }
            return ids.ToArray();
        }

        public Hashtable MatchEntityProperties(ObjectId sourceEntityId, IList targetEntityIds, Hashtable options)
        {
            bool copyLayer = ReadBoolOption(options, "layer", true);
            bool copyColor = ReadBoolOption(options, "color", true);
            bool copyLinetype = ReadBoolOption(options, "linetype", true);
            bool copyLineweight = ReadBoolOption(options, "lineweight", true);
            bool copyLtscale = ReadBoolOption(options, "linetype_scale", true);
            bool copyVisibility = ReadBoolOption(options, "visible", false);

            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity source = tr.GetObject(sourceEntityId, OpenMode.ForRead) as Entity;
                if (source == null)
                {
                    throw new ArgumentException("sourceEntityId non identifica una Entity");
                }

                foreach (object raw in targetEntityIds)
                {
                    if (!(raw is ObjectId))
                    {
                        continue;
                    }

                    ObjectId targetId = (ObjectId)raw;
                    if (targetId == sourceEntityId)
                    {
                        continue;
                    }

                    Entity target = tr.GetObject(targetId, OpenMode.ForWrite) as Entity;
                    if (target == null)
                    {
                        continue;
                    }

                    if (copyLayer) target.Layer = source.Layer;
                    if (copyColor) target.Color = source.Color;
                    if (copyLinetype) target.Linetype = source.Linetype;
                    if (copyLineweight) target.LineWeight = source.LineWeight;
                    if (copyLtscale) target.LinetypeScale = source.LinetypeScale;
                    if (copyVisibility) target.Visible = source.Visible;
                    changed++;
                }

                tr.Commit();
            }

            Hashtable info = new Hashtable();
            info["changed"] = changed;
            info["layer"] = copyLayer;
            info["color"] = copyColor;
            info["linetype"] = copyLinetype;
            info["lineweight"] = copyLineweight;
            info["linetype_scale"] = copyLtscale;
            info["visible"] = copyVisibility;
            return info;
        }

        private Point3d? TryGetBestIntersectionPointFromList(ObjectId curveId, IList boundaryIds, bool nearStart, bool extendEntity)
        {
            Point3d? best = null;
            double bestDistance = double.MaxValue;

            foreach (object raw in boundaryIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }

                try
                {
                    Point3d point = GetBestIntersectionPoint(curveId, (ObjectId)raw, nearStart, extendEntity);
                    double dist = DistanceToCurveEndpoint(curveId, point, nearStart);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        best = point;
                    }
                }
                catch
                {
                }
            }

            return best;
        }

        private double DistanceToCurveEndpoint(ObjectId curveId, Point3d point, bool nearStart)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(curveId, OpenMode.ForRead) as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("curveId non identifica una curva");
                }

                return (nearStart ? curve.StartPoint : curve.EndPoint).DistanceTo(point);
            }
        }

        private double DistancePointToEntity(Point3d point, ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                Curve curve = entity as Curve;
                if (curve != null)
                {
                    return point.DistanceTo(curve.GetClosestPointTo(point, false));
                }

                Extents3d? ext = TryGetExtents(entity);
                if (ext.HasValue)
                {
                    Point3d center = new Point3d(
                        (ext.Value.MinPoint.X + ext.Value.MaxPoint.X) * 0.5,
                        (ext.Value.MinPoint.Y + ext.Value.MaxPoint.Y) * 0.5,
                        (ext.Value.MinPoint.Z + ext.Value.MaxPoint.Z) * 0.5);
                    return point.DistanceTo(center);
                }

                return double.MaxValue;
            }
        }

        private static bool ReadBoolOption(Hashtable options, string key, bool defaultValue)
        {
            if (options == null || !options.ContainsKey(key))
            {
                return defaultValue;
            }

            return Convert.ToBoolean(options[key]);
        }

        private static Point2d ToPoint2d(Point3d point)
        {
            return new Point2d(point.X, point.Y);
        }

        private static Point2d IntersectInfiniteLines(Point2d a1, Point2d a2, Point2d b1, Point2d b2)
        {
            Vector2d r = a2 - a1;
            Vector2d s = b2 - b1;
            double denom = Cross2d(r, s);
            if (Math.Abs(denom) <= 1e-9)
            {
                throw new InvalidOperationException("Le linee sono parallele");
            }

            Vector2d qp = b1 - a1;
            double t = Cross2d(qp, s) / denom;
            return a1 + (r * t);
        }

        private static double Cross2d(Vector2d a, Vector2d b)
        {
            return (a.X * b.Y) - (a.Y * b.X);
        }

        private static Vector2d GetLineDirectionAwayFromIntersection(Line line, Point2d intersection, out bool trimStart)
        {
            Point2d start = ToPoint2d(line.StartPoint);
            Point2d end = ToPoint2d(line.EndPoint);
            double ds = start.GetDistanceTo(intersection);
            double de = end.GetDistanceTo(intersection);
            trimStart = ds <= de;
            Vector2d direction = trimStart ? (end - start) : (start - end);
            if (direction.Length <= 1e-9)
            {
                throw new InvalidOperationException("Linea degenerata");
            }
            return direction.GetNormal();
        }

        private static void SetLineEndpoint(Line line, bool setStart, Point2d point)
        {
            Point3d p3 = new Point3d(point.X, point.Y, setStart ? line.StartPoint.Z : line.EndPoint.Z);
            if (setStart)
            {
                line.StartPoint = p3;
            }
            else
            {
                line.EndPoint = p3;
            }
        }

        private static void EnsureMinorArc(Arc arc)
        {
            double start = arc.StartAngle;
            double end = arc.EndAngle;
            double sweep = end - start;
            while (sweep <= -Math.PI)
            {
                sweep += Math.PI * 2.0;
            }
            while (sweep > Math.PI)
            {
                sweep -= Math.PI * 2.0;
            }
            arc.StartAngle = start;
            arc.EndAngle = start + sweep;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private int StretchEntityInternal(Entity entity, Point3d min, Point3d max, Vector3d displacement, bool moveContainedEntities)
        {
            Line line = entity as Line;
            if (line != null)
            {
                int moved = 0;
                if (PointInsideWindow(line.StartPoint, min, max))
                {
                    line.StartPoint = line.StartPoint + displacement;
                    moved++;
                }
                if (PointInsideWindow(line.EndPoint, min, max))
                {
                    line.EndPoint = line.EndPoint + displacement;
                    moved++;
                }
                return moved;
            }

            Polyline pl = entity as Polyline;
            if (pl != null)
            {
                int moved = 0;
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    Point2d pt = pl.GetPoint2dAt(i);
                    Point3d p3 = new Point3d(pt.X, pt.Y, pl.Elevation);
                    if (PointInsideWindow(p3, min, max))
                    {
                        pl.SetPointAt(i, new Point2d(pt.X + displacement.X, pt.Y + displacement.Y));
                        moved++;
                    }
                }
                return moved;
            }

            if (entity is DBText)
            {
                DBText text = (DBText)entity;
                if (PointInsideWindow(text.Position, min, max))
                {
                    text.Position = text.Position + displacement;
                    return -1;
                }
                return 0;
            }

            if (entity is MText)
            {
                MText text = (MText)entity;
                if (PointInsideWindow(text.Location, min, max))
                {
                    text.Location = text.Location + displacement;
                    return -1;
                }
                return 0;
            }

            if (entity is DBPoint)
            {
                DBPoint point = (DBPoint)entity;
                if (PointInsideWindow(point.Position, min, max))
                {
                    point.Position = point.Position + displacement;
                    return -1;
                }
                return 0;
            }

            if (entity is Circle)
            {
                Circle circle = (Circle)entity;
                if (PointInsideWindow(circle.Center, min, max))
                {
                    circle.Center = circle.Center + displacement;
                    return -1;
                }
                return 0;
            }

            if (entity is Arc)
            {
                Arc arc = (Arc)entity;
                if (PointInsideWindow(arc.Center, min, max))
                {
                    arc.Center = arc.Center + displacement;
                    return -1;
                }
                return 0;
            }

            if (entity is BlockReference)
            {
                BlockReference br = (BlockReference)entity;
                if (PointInsideWindow(br.Position, min, max))
                {
                    br.Position = br.Position + displacement;
                    return -1;
                }
                return 0;
            }

            if (moveContainedEntities)
            {
                Extents3d? ext = TryGetExtents(entity);
                if (ext.HasValue && PointInsideWindow(ext.Value.MinPoint, min, max) && PointInsideWindow(ext.Value.MaxPoint, min, max))
                {
                    entity.TransformBy(Matrix3d.Displacement(displacement));
                    return -1;
                }
            }

            return 0;
        }

        private static bool PointInsideWindow(Point3d point, Point3d min, Point3d max)
        {
            return point.X >= min.X - 1e-9 && point.X <= max.X + 1e-9 &&
                   point.Y >= min.Y - 1e-9 && point.Y <= max.Y + 1e-9 &&
                   point.Z >= min.Z - 1e-9 && point.Z <= max.Z + 1e-9;
        }

        private Point3d GetBestIntersectionPoint(ObjectId entityId, ObjectId boundaryId, bool nearStart, bool extendEntity)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                Entity boundary = tr.GetObject(boundaryId, OpenMode.ForRead) as Entity;
                if (curve == null || boundary == null)
                {
                    throw new ArgumentException("I due ObjectId devono identificare una curva e una entita valida");
                }

                Point3dCollection pts = new Point3dCollection();
                curve.IntersectWith(boundary, extendEntity ? Intersect.ExtendThis : Intersect.OnBothOperands, pts, 0, 0);
                if (pts.Count == 0)
                {
                    Point3d fallbackPoint;
                    if (extendEntity && TryGetTerminalSegmentBoundaryIntersection(curve, boundary, nearStart, out fallbackPoint))
                    {
                        return fallbackPoint;
                    }

                    throw new InvalidOperationException("Nessuna intersezione utile trovata");
                }

                Point3d reference = nearStart ? curve.StartPoint : curve.EndPoint;
                Point3d best = pts[0];
                double bestDist = reference.DistanceTo(best);
                for (int i = 1; i < pts.Count; i++)
                {
                    double dist = reference.DistanceTo(pts[i]);
                    if (dist < bestDist)
                    {
                        best = pts[i];
                        bestDist = dist;
                    }
                }

                return best;
            }
        }

        private static bool TryGetTerminalSegmentBoundaryIntersection(Curve curve, Entity boundary, bool nearStart, out Point3d point)
        {
            point = Point3d.Origin;

            Point2d segStart;
            Point2d segEnd;
            if (!TryGetCurveTerminalSegment(curve, nearStart, out segStart, out segEnd))
            {
                return false;
            }

            List<Tuple<Point2d, Point2d>> boundarySegments = GetBoundarySegments(boundary);
            if (boundarySegments.Count == 0)
            {
                return false;
            }

            Point2d reference = nearStart ? segStart : segEnd;
            double bestDist = double.MaxValue;
            bool found = false;

            foreach (Tuple<Point2d, Point2d> segment in boundarySegments)
            {
                Point2d candidate;
                if (!TryIntersectInfiniteWithSegment(segStart, segEnd, segment.Item1, segment.Item2, out candidate))
                {
                    continue;
                }

                double dist = candidate.GetDistanceTo(reference);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    point = new Point3d(candidate.X, candidate.Y, nearStart ? curve.StartPoint.Z : curve.EndPoint.Z);
                    found = true;
                }
            }

            return found;
        }

        private static bool TryGetCurveTerminalSegment(Curve curve, bool nearStart, out Point2d start, out Point2d end)
        {
            start = Point2d.Origin;
            end = Point2d.Origin;

            Line line = curve as Line;
            if (line != null)
            {
                start = ToPoint2d(line.StartPoint);
                end = ToPoint2d(line.EndPoint);
                return true;
            }

            Polyline pl = curve as Polyline;
            if (pl != null)
            {
                int segmentIndex = nearStart ? 0 : GetPolylineSegmentCountInternal(pl) - 1;
                if (segmentIndex < 0 || pl.GetSegmentType(segmentIndex) != SegmentType.Line)
                {
                    return false;
                }

                LineSegment2d seg = pl.GetLineSegment2dAt(segmentIndex);
                start = seg.StartPoint;
                end = seg.EndPoint;
                return true;
            }

            return false;
        }

        private static List<Tuple<Point2d, Point2d>> GetBoundarySegments(Entity boundary)
        {
            List<Tuple<Point2d, Point2d>> segments = new List<Tuple<Point2d, Point2d>>();

            Line line = boundary as Line;
            if (line != null)
            {
                segments.Add(Tuple.Create(ToPoint2d(line.StartPoint), ToPoint2d(line.EndPoint)));
                return segments;
            }

            Polyline pl = boundary as Polyline;
            if (pl != null)
            {
                int count = GetPolylineSegmentCountInternal(pl);
                for (int i = 0; i < count; i++)
                {
                    if (pl.GetSegmentType(i) != SegmentType.Line)
                    {
                        continue;
                    }

                    LineSegment2d seg = pl.GetLineSegment2dAt(i);
                    segments.Add(Tuple.Create(seg.StartPoint, seg.EndPoint));
                }
            }

            return segments;
        }

        private static bool TryIntersectInfiniteWithSegment(Point2d infA, Point2d infB, Point2d segA, Point2d segB, out Point2d point)
        {
            point = Point2d.Origin;

            Vector2d r = infB - infA;
            Vector2d s = segB - segA;
            double denom = Cross2d(r, s);
            if (Math.Abs(denom) <= 1e-9)
            {
                return false;
            }

            Vector2d qp = segA - infA;
            double t = Cross2d(qp, s) / denom;
            double u = Cross2d(qp, r) / denom;
            if (u < -1e-9 || u > 1.0 + 1e-9)
            {
                return false;
            }

            point = infA + (r * t);
            return true;
        }

        private Point3d ExtendCurveToPoint(ObjectId entityId, Point3d point, bool atStart)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'entita non e valida");
                }

                Line line = entity as Line;
                if (line != null)
                {
                    if (atStart)
                    {
                        line.StartPoint = point;
                    }
                    else
                    {
                        line.EndPoint = point;
                    }
                    tr.Commit();
                    return point;
                }

                Polyline pl = entity as Polyline;
                if (pl != null)
                {
                    int segmentIndex = atStart ? 0 : GetPolylineSegmentCountInternal(pl) - 1;
                    if (segmentIndex < 0 || pl.GetSegmentType(segmentIndex) != SegmentType.Line)
                    {
                        throw new NotSupportedException("L'estensione automatica della polyline richiede un segmento terminale lineare");
                    }

                    int vertexIndex = atStart ? 0 : pl.NumberOfVertices - 1;
                    pl.SetPointAt(vertexIndex, new Point2d(point.X, point.Y));
                    tr.Commit();
                    return point;
                }

                throw new NotSupportedException("ExtendCurveToEntity supporta Line e Polyline con estremi lineari");
            }
        }
    }
}
