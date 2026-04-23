using System;
using System.Collections;
using System.Collections.Generic;
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

        public ObjectId TrimCurveStartAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
        {
            return TrimCurveAtPoint(entityId, new Point3d(x, y, z), false, eraseSource);
        }

        public ObjectId TrimCurveEndAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
        {
            return TrimCurveAtPoint(entityId, new Point3d(x, y, z), true, eraseSource);
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

        public Hashtable TrimCurvesToBoundaries(IList curveIds, IList boundaryIds, string trimMode, bool eraseSource)
        {
            int changed = 0;
            ArrayList results = new ArrayList();
            string mode = (trimMode ?? "nearest").Trim().ToLowerInvariant();

            if (curveIds != null)
            {
                foreach (object raw in curveIds)
                {
                    if (!(raw is ObjectId)) continue;
                    ObjectId curveId = (ObjectId)raw;
                    Point3d? startHit = TryGetBestIntersectionPointFromList(curveId, boundaryIds, true, false);
                    Point3d? endHit = TryGetBestIntersectionPointFromList(curveId, boundaryIds, false, false);
                    ObjectId trimmed = ObjectId.Null;

                    if ((mode == "start" || mode == "nearest") && startHit.HasValue && !endHit.HasValue)
                    {
                        trimmed = TrimCurveStartAtPoint(curveId, startHit.Value.X, startHit.Value.Y, startHit.Value.Z, eraseSource);
                    }
                    else if ((mode == "end" || mode == "nearest") && endHit.HasValue && !startHit.HasValue)
                    {
                        trimmed = TrimCurveEndAtPoint(curveId, endHit.Value.X, endHit.Value.Y, endHit.Value.Z, eraseSource);
                    }
                    else if (startHit.HasValue && endHit.HasValue)
                    {
                        if (mode == "start")
                        {
                            trimmed = TrimCurveStartAtPoint(curveId, startHit.Value.X, startHit.Value.Y, startHit.Value.Z, eraseSource);
                        }
                        else if (mode == "end")
                        {
                            trimmed = TrimCurveEndAtPoint(curveId, endHit.Value.X, endHit.Value.Y, endHit.Value.Z, eraseSource);
                        }
                        else
                        {
                            double startDist = DistanceToCurveEndpoint(curveId, startHit.Value, true);
                            double endDist = DistanceToCurveEndpoint(curveId, endHit.Value, false);
                            trimmed = startDist <= endDist
                                ? TrimCurveStartAtPoint(curveId, startHit.Value.X, startHit.Value.Y, startHit.Value.Z, eraseSource)
                                : TrimCurveEndAtPoint(curveId, endHit.Value.X, endHit.Value.Y, endHit.Value.Z, eraseSource);
                        }
                    }

                    if (!trimmed.IsNull)
                    {
                        changed++;
                        results.Add(trimmed);
                    }
                }
            }

            Hashtable info = NewInfo();
            info["changed"] = changed;
            info["result_ids"] = results;
            return info;
        }

        public Hashtable ExtendCurvesToBoundaries(IList curveIds, IList boundaryIds, string extendMode)
        {
            int changed = 0;
            ArrayList points = new ArrayList();
            string mode = (extendMode ?? "nearest").Trim().ToLowerInvariant();

            if (curveIds != null)
            {
                foreach (object raw in curveIds)
                {
                    if (!(raw is ObjectId)) continue;
                    ObjectId curveId = (ObjectId)raw;
                    Point3d? startHit = TryGetBestIntersectionPointFromList(curveId, boundaryIds, true, true);
                    Point3d? endHit = TryGetBestIntersectionPointFromList(curveId, boundaryIds, false, true);
                    Point3d moved = Point3d.Origin;
                    bool done = false;

                    if ((mode == "start" || mode == "nearest") && startHit.HasValue && !endHit.HasValue)
                    {
                        moved = ExtendCurveToPoint(curveId, startHit.Value, true);
                        done = true;
                    }
                    else if ((mode == "end" || mode == "nearest") && endHit.HasValue && !startHit.HasValue)
                    {
                        moved = ExtendCurveToPoint(curveId, endHit.Value, false);
                        done = true;
                    }
                    else if (startHit.HasValue && endHit.HasValue)
                    {
                        if (mode == "start")
                        {
                            moved = ExtendCurveToPoint(curveId, startHit.Value, true);
                        }
                        else if (mode == "end")
                        {
                            moved = ExtendCurveToPoint(curveId, endHit.Value, false);
                        }
                        else
                        {
                            double startDist = DistanceToCurveEndpoint(curveId, startHit.Value, true);
                            double endDist = DistanceToCurveEndpoint(curveId, endHit.Value, false);
                            moved = startDist <= endDist
                                ? ExtendCurveToPoint(curveId, startHit.Value, true)
                                : ExtendCurveToPoint(curveId, endHit.Value, false);
                        }
                        done = true;
                    }

                    if (done)
                    {
                        changed++;
                        Hashtable pt = NewInfo();
                        pt["x"] = moved.X;
                        pt["y"] = moved.Y;
                        pt["z"] = moved.Z;
                        points.Add(pt);
                    }
                }
            }

            Hashtable info = NewInfo();
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
                if (curve == null) throw new ArgumentException("curveId non identifica una curva");

                if (boundaryIds != null)
                {
                    foreach (object raw in boundaryIds)
                    {
                        if (!(raw is ObjectId)) continue;
                        ObjectId bid = (ObjectId)raw;
                        if (bid == curveId) continue;
                        Entity boundary;
                        try
                        {
                            boundary = tr.GetObject(bid, OpenMode.ForRead, false) as Entity;
                        }
                        catch
                        {
                            continue;
                        }
                        if (boundary == null) continue;
                        Point3dCollection pts = new Point3dCollection();
                        curve.IntersectWith(boundary, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                        foreach (Point3d pt in pts)
                        {
                            Point3d onCurve = curve.GetClosestPointTo(pt, false);
                            parameters.Add(curve.GetParameterAtPoint(onCurve));
                        }
                    }
                }
            }

            DoubleCollection splitParams = BuildSortedUniqueParameters(parameters);
            return SplitCurveKeepParts(curveId, splitParams, eraseSource);
        }

        public Hashtable BreakEntitiesAtIntersections(IList entityIds, bool eraseSource)
        {
            List<ObjectId> ids = new List<ObjectId>();
            if (entityIds != null)
            {
                foreach (object raw in entityIds) if (raw is ObjectId) ids.Add((ObjectId)raw);
            }

            int changed = 0;
            ArrayList created = new ArrayList();
            for (int i = 0; i < ids.Count; i++)
            {
                ArrayList boundaries = new ArrayList();
                for (int j = 0; j < ids.Count; j++) if (j != i) boundaries.Add(ids[j]);
                ObjectId[] parts;
                try
                {
                    parts = BreakCurveAtAllIntersections(ids[i], boundaries, eraseSource);
                }
                catch
                {
                    continue;
                }
                if (parts.Length > 0)
                {
                    changed++;
                    foreach (ObjectId part in parts) created.Add(part);
                }
            }

            Hashtable info = NewInfo();
            info["changed"] = changed;
            info["created_count"] = created.Count;
            info["created_ids"] = created;
            return info;
        }

        public ObjectId[] OffsetEntityBothSides(ObjectId entityId, double offsetDistance)
        {
            List<ObjectId> ids = new List<ObjectId>();
            double dist = Math.Abs(offsetDistance);
            if (dist <= 1e-9) return ids.ToArray();
            ids.AddRange(OffsetEntity(entityId, dist));
            ids.AddRange(OffsetEntity(entityId, -dist));
            return ids.ToArray();
        }

        public ObjectId OffsetEntityTowardPoint(ObjectId entityId, double offsetDistance, double x, double y, double z)
        {
            double dist = Math.Abs(offsetDistance);
            if (dist <= 1e-9) throw new ArgumentException("offsetDistance deve essere != 0");

            Point3d seed = new Point3d(x, y, z);
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity source = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                Curve curve = source as Curve;
                if (curve == null) throw new ArgumentException("L'entita non supporta offset: " + (source == null ? "null" : source.GetType().Name));

                DBObjectCollection plus = curve.GetOffsetCurves(dist);
                DBObjectCollection minus = curve.GetOffsetCurves(-dist);
                Entity best = null;
                double bestDistance = double.MaxValue;

                for (int i = 0; i < plus.Count; i++)
                {
                    Entity e = plus[i] as Entity;
                    if (e == null) continue;
                    double d = DistancePointToEntityCandidate(seed, e);
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = e;
                    }
                }

                for (int i = 0; i < minus.Count; i++)
                {
                    Entity e = minus[i] as Entity;
                    if (e == null) continue;
                    double d = DistancePointToEntityCandidate(seed, e);
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = e;
                    }
                }

                if (best == null) throw new InvalidOperationException("Offset non disponibile per l'entita");
                BlockTableRecord owner = tr.GetObject(source.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                ObjectId id = owner.AppendEntity(best);
                tr.AddNewlyCreatedDBObject(best, true);
                tr.Commit();
                return id;
            }
        }

        public ObjectId[] OffsetEntitiesTowardPoint(IList entityIds, double offsetDistance, double x, double y, double z)
        {
            List<ObjectId> ids = new List<ObjectId>();
            if (entityIds == null) return ids.ToArray();
            foreach (object raw in entityIds)
            {
                if (!(raw is ObjectId)) continue;
                try
                {
                    ids.Add(OffsetEntityTowardPoint((ObjectId)raw, offsetDistance, x, y, z));
                }
                catch
                {
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
                if (source == null) throw new ArgumentException("sourceEntityId non identifica una Entity");

                if (targetEntityIds != null)
                {
                    foreach (object raw in targetEntityIds)
                    {
                        if (!(raw is ObjectId)) continue;
                        ObjectId targetId = (ObjectId)raw;
                        if (targetId == sourceEntityId) continue;

                        Entity target = tr.GetObject(targetId, OpenMode.ForWrite, false) as Entity;
                        if (target == null || target.IsErased) continue;
                        if (copyLayer) target.Layer = source.Layer;
                        if (copyColor) target.Color = source.Color;
                        if (copyLinetype) target.Linetype = source.Linetype;
                        if (copyLineweight) target.LineWeight = source.LineWeight;
                        if (copyLtscale) target.LinetypeScale = source.LinetypeScale;
                        if (copyVisibility) target.Visible = source.Visible;
                        changed++;
                    }
                }
                tr.Commit();
            }

            Hashtable info = NewInfo();
            info["changed"] = changed;
            info["layer"] = copyLayer;
            info["color"] = copyColor;
            info["linetype"] = copyLinetype;
            info["lineweight"] = copyLineweight;
            info["linetype_scale"] = copyLtscale;
            info["visible"] = copyVisibility;
            return info;
        }

        private ObjectId TrimCurveAtPoint(ObjectId entityId, Point3d point, bool keepStartSegment, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("L'entita non e una curva");

                Point3d on = curve.GetClosestPointTo(point, false);
                double param = curve.GetParameterAtPoint(on);
                if (Math.Abs(param - curve.StartParam) <= 1e-9 || Math.Abs(param - curve.EndParam) <= 1e-9)
                {
                    return entityId;
                }

                DoubleCollection pars = new DoubleCollection();
                pars.Add(param);
                DBObjectCollection pieces = curve.GetSplitCurves(pars);
                if (pieces == null || pieces.Count == 0) return ObjectId.Null;

                Entity selected = null;
                double best = double.MaxValue;
                Point3d refPoint = keepStartSegment ? curve.StartPoint : curve.EndPoint;
                foreach (DBObject dbo in pieces)
                {
                    Curve part = dbo as Curve;
                    if (part == null) continue;
                    double d = Math.Min(part.StartPoint.DistanceTo(refPoint), part.EndPoint.DistanceTo(refPoint));
                    if (d < best)
                    {
                        best = d;
                        selected = part as Entity;
                    }
                }

                if (selected == null) return ObjectId.Null;

                BlockTableRecord owner = tr.GetObject(curve.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                ObjectId newId = owner.AppendEntity(selected);
                tr.AddNewlyCreatedDBObject(selected, true);
                if (eraseSource)
                {
                    curve.UpgradeOpen();
                    curve.Erase(true);
                }
                tr.Commit();
                return newId;
            }
        }

        private ObjectId[] SplitCurveKeepParts(ObjectId curveId, DoubleCollection splitParams, bool eraseSource)
        {
            if (splitParams == null || splitParams.Count == 0) return new ObjectId[0];
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(curveId, OpenMode.ForRead) as Curve;
                if (curve == null) throw new ArgumentException("curveId non identifica una curva");

                DBObjectCollection pieces = curve.GetSplitCurves(splitParams);
                BlockTableRecord owner = tr.GetObject(curve.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                ArrayList ids = new ArrayList();
                foreach (DBObject dbo in pieces)
                {
                    Entity e = dbo as Entity;
                    if (e == null) continue;
                    ObjectId id = owner.AppendEntity(e);
                    tr.AddNewlyCreatedDBObject(e, true);
                    ids.Add(id);
                }

                if (eraseSource)
                {
                    curve.UpgradeOpen();
                    curve.Erase(true);
                }

                tr.Commit();
                ObjectId[] result = new ObjectId[ids.Count];
                ids.CopyTo(result);
                return result;
            }
        }

        private static DoubleCollection BuildSortedUniqueParameters(List<double> values)
        {
            values.Sort();
            DoubleCollection pars = new DoubleCollection();
            double? last = null;
            foreach (double v in values)
            {
                if (last.HasValue && Math.Abs(v - last.Value) <= 1e-7) continue;
                pars.Add(v);
                last = v;
            }
            return pars;
        }

        private Point3d? TryGetBestIntersectionPointFromList(ObjectId curveId, IList boundaryIds, bool nearStart, bool extendEntity)
        {
            Point3d? best = null;
            double bestDistance = double.MaxValue;
            if (boundaryIds == null) return null;

            foreach (object raw in boundaryIds)
            {
                if (!(raw is ObjectId)) continue;
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
                if (curve == null) throw new ArgumentException("curveId non identifica una curva");
                return (nearStart ? curve.StartPoint : curve.EndPoint).DistanceTo(point);
            }
        }

        private Point3d GetBestIntersectionPoint(ObjectId entityId, ObjectId boundaryId, bool nearStart, bool extendEntity)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                Entity boundary = tr.GetObject(boundaryId, OpenMode.ForRead) as Entity;
                if (curve == null || boundary == null) throw new ArgumentException("I due ObjectId devono identificare una curva e una entita valida");

                Point3dCollection pts = new Point3dCollection();
                curve.IntersectWith(boundary, extendEntity ? Intersect.ExtendThis : Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                if (pts.Count == 0) throw new InvalidOperationException("Nessuna intersezione utile trovata");

                Point3d reference = nearStart ? curve.StartPoint : curve.EndPoint;
                Point3d best = pts[0];
                double bestDist = reference.DistanceTo(best);
                for (int i = 1; i < pts.Count; i++)
                {
                    double d = reference.DistanceTo(pts[i]);
                    if (d < bestDist)
                    {
                        best = pts[i];
                        bestDist = d;
                    }
                }
                return best;
            }
        }

        private Point3d ExtendCurveToPoint(ObjectId entityId, Point3d point, bool atStart)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null) throw new ArgumentException("L'entita non e valida");

                Line line = entity as Line;
                if (line != null)
                {
                    if (atStart) line.StartPoint = point;
                    else line.EndPoint = point;
                    tr.Commit();
                    return point;
                }

                Polyline pl = entity as Polyline;
                if (pl != null)
                {
                    int idx = atStart ? 0 : (pl.NumberOfVertices - 1);
                    pl.SetPointAt(idx, new Point2d(point.X, point.Y));
                    tr.Commit();
                    return point;
                }

                throw new NotSupportedException("ExtendCurveToEntity supporta Line e Polyline");
            }
        }

        private static bool ReadBoolOption(Hashtable options, string key, bool defaultValue)
        {
            if (options == null || !options.ContainsKey(key)) return defaultValue;
            return Convert.ToBoolean(options[key]);
        }

        private static double DistancePointToEntityCandidate(Point3d point, Entity entity)
        {
            Curve curve = entity as Curve;
            if (curve != null)
            {
                try
                {
                    Point3d on = curve.GetClosestPointTo(point, false);
                    return point.DistanceTo(on);
                }
                catch
                {
                }
            }

            try
            {
                Extents3d ext = entity.GeometricExtents;
                Point3d center = new Point3d(
                    (ext.MinPoint.X + ext.MaxPoint.X) * 0.5,
                    (ext.MinPoint.Y + ext.MaxPoint.Y) * 0.5,
                    (ext.MinPoint.Z + ext.MaxPoint.Z) * 0.5);
                return point.DistanceTo(center);
            }
            catch
            {
                return double.MaxValue;
            }
        }
    }
}
