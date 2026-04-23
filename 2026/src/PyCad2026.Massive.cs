using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public string[] GetLayoutNamesFromBlockTable()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                ArrayList names = new ArrayList();
                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null || !btr.IsLayout) continue;
                    names.Add(btr.Name);
                }
                string[] result = new string[names.Count];
                names.CopyTo(result);
                return result;
            }
        }

        public Hashtable GetSpaceEntityStats()
        {
            Hashtable info = NewInfo();
            Hashtable bySpace = NewInfo();
            info["by_space"] = bySpace;

            int total = 0;
            int model = 0;
            int paper = 0;
            int layouts = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null || !btr.IsLayout) continue;

                    int count = 0;
                    foreach (ObjectId _ in btr) count++;
                    bySpace[btr.Name] = count;
                    total += count;
                    if (string.Equals(btr.Name, BlockTableRecord.ModelSpace, StringComparison.OrdinalIgnoreCase)) model += count;
                    else if (string.Equals(btr.Name, BlockTableRecord.PaperSpace, StringComparison.OrdinalIgnoreCase)) paper += count;
                    else layouts += count;
                }
            }

            info["total"] = total;
            info["model"] = model;
            info["paper"] = paper;
            info["layouts"] = layouts;
            return info;
        }

        public ObjectId[] GetEntitiesInSpace(string spaceName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = ResolveSpaceRecordByName(tr, spaceName);
                if (space == null) return new ObjectId[0];
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in space) ids.Add(id);
                return ids.ToArray();
            }
        }

        public Hashtable EraseByLayerBatch(string layerName, bool onlyModelSpace)
        {
            if (string.IsNullOrWhiteSpace(layerName)) throw new ArgumentException("layerName non valido");
            int checkedCount = 0;
            int erased = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                List<ObjectId> ids = CollectSpaceEntityIds(tr, onlyModelSpace);
                for (int i = 0; i < ids.Count; i++)
                {
                    Entity e = tr.GetObject(ids[i], OpenMode.ForWrite, false) as Entity;
                    if (e == null || e.IsErased) continue;
                    checkedCount++;
                    if (!string.Equals(e.Layer, layerName, StringComparison.OrdinalIgnoreCase)) continue;
                    e.Erase(true);
                    erased++;
                }
                tr.Commit();
            }

            Hashtable info = NewInfo();
            info["checked"] = checkedCount;
            info["erased"] = erased;
            info["layer"] = layerName;
            info["only_model_space"] = onlyModelSpace;
            return info;
        }

        public Hashtable EraseByTypeBatch(string dxfType, bool onlyModelSpace)
        {
            if (string.IsNullOrWhiteSpace(dxfType)) throw new ArgumentException("dxfType non valido");
            int checkedCount = 0;
            int erased = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                List<ObjectId> ids = CollectSpaceEntityIds(tr, onlyModelSpace);
                for (int i = 0; i < ids.Count; i++)
                {
                    Entity e = tr.GetObject(ids[i], OpenMode.ForWrite, false) as Entity;
                    if (e == null || e.IsErased) continue;
                    checkedCount++;
                    if (!string.Equals(e.GetRXClass().DxfName, dxfType, StringComparison.OrdinalIgnoreCase)) continue;
                    e.Erase(true);
                    erased++;
                }
                tr.Commit();
            }

            Hashtable info = NewInfo();
            info["checked"] = checkedCount;
            info["erased"] = erased;
            info["dxf_type"] = dxfType;
            info["only_model_space"] = onlyModelSpace;
            return info;
        }

        public Hashtable EraseByDxfFilterBatch(IList dxfFilters, bool onlyModelSpace)
        {
            Hashtable filters = NormalizeDxfPairs(dxfFilters);
            int checkedCount = 0;
            int erased = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                List<ObjectId> ids = CollectSpaceEntityIds(tr, onlyModelSpace);
                for (int i = 0; i < ids.Count; i++)
                {
                    Entity e = tr.GetObject(ids[i], OpenMode.ForWrite, false) as Entity;
                    if (e == null || e.IsErased) continue;
                    checkedCount++;
                    if (!MatchesFilters(e, filters, tr)) continue;
                    e.Erase(true);
                    erased++;
                }
                tr.Commit();
            }

            Hashtable info = NewInfo();
            info["checked"] = checkedCount;
            info["erased"] = erased;
            info["filter_count"] = filters.Count;
            info["only_model_space"] = onlyModelSpace;
            return info;
        }

        public Hashtable GetEntityPropertySnapshot(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity e = tr.GetObject(entityId, OpenMode.ForRead, false) as Entity;
                if (e == null) throw new ArgumentException("entityId non valido");
                Hashtable info = NewInfo();
                info["id"] = entityId.ToString();
                info["handle"] = e.Handle.ToString();
                info["dxf"] = e.GetRXClass().DxfName;
                info["layer"] = e.Layer;
                info["color_index"] = e.ColorIndex;
                info["linetype"] = e.Linetype;
                info["lineweight"] = (int)e.LineWeight;
                info["linetype_scale"] = e.LinetypeScale;
                info["visible"] = e.Visible;
                return info;
            }
        }

        public int ApplyEntityPropertySnapshot(IList entityIds, Hashtable snapshot)
        {
            if (entityIds == null || snapshot == null) return 0;
            int changed = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                string layer = snapshot.ContainsKey("layer") ? Convert.ToString(snapshot["layer"], CultureInfo.InvariantCulture) : null;
                bool hasColor = snapshot.ContainsKey("color_index");
                short color = hasColor ? Convert.ToInt16(snapshot["color_index"], CultureInfo.InvariantCulture) : (short)256;
                string ltype = snapshot.ContainsKey("linetype") ? Convert.ToString(snapshot["linetype"], CultureInfo.InvariantCulture) : null;
                bool hasLw = snapshot.ContainsKey("lineweight");
                LineWeight lw = hasLw ? (LineWeight)Convert.ToInt32(snapshot["lineweight"], CultureInfo.InvariantCulture) : LineWeight.ByLayer;
                bool hasLts = snapshot.ContainsKey("linetype_scale");
                double lts = hasLts ? Convert.ToDouble(snapshot["linetype_scale"], CultureInfo.InvariantCulture) : 1.0;
                bool hasVisible = snapshot.ContainsKey("visible");
                bool visible = hasVisible && Convert.ToBoolean(snapshot["visible"], CultureInfo.InvariantCulture);

                if (!string.IsNullOrWhiteSpace(layer))
                {
                    LayerTable lt = tr.GetObject(_db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (lt != null && !lt.Has(layer))
                    {
                        lt.UpgradeOpen();
                        LayerTableRecord ltr = new LayerTableRecord();
                        ltr.Name = layer;
                        lt.Add(ltr);
                        tr.AddNewlyCreatedDBObject(ltr, true);
                    }
                }

                for (int i = 0; i < entityIds.Count; i++)
                {
                    if (!(entityIds[i] is ObjectId)) continue;
                    Entity e = tr.GetObject((ObjectId)entityIds[i], OpenMode.ForWrite, false) as Entity;
                    if (e == null || e.IsErased) continue;
                    if (!string.IsNullOrWhiteSpace(layer)) e.Layer = layer;
                    if (hasColor) e.ColorIndex = color;
                    if (!string.IsNullOrWhiteSpace(ltype)) e.Linetype = ltype;
                    if (hasLw) e.LineWeight = lw;
                    if (hasLts) e.LinetypeScale = lts;
                    if (hasVisible) e.Visible = visible;
                    changed++;
                }

                tr.Commit();
            }
            return changed;
        }

        public Hashtable BuildIntersectionsMatrix(IList curveIds, bool extendThis, bool extendArgument)
        {
            ArrayList rows = new ArrayList();
            int totalIntersections = 0;

            Intersect mode = Intersect.OnBothOperands;
            if (extendThis && extendArgument) mode = Intersect.ExtendBoth;
            else if (extendThis) mode = Intersect.ExtendThis;
            else if (extendArgument) mode = Intersect.ExtendArgument;

            List<ObjectId> ids = new List<ObjectId>();
            if (curveIds != null)
            {
                foreach (object raw in curveIds) if (raw is ObjectId) ids.Add((ObjectId)raw);
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    Curve a = tr.GetObject(ids[i], OpenMode.ForRead, false) as Curve;
                    if (a == null || a.IsErased) continue;
                    for (int j = i + 1; j < ids.Count; j++)
                    {
                        Curve b = tr.GetObject(ids[j], OpenMode.ForRead, false) as Curve;
                        if (b == null || b.IsErased) continue;
                        Point3dCollection pts = new Point3dCollection();
                        a.IntersectWith(b, mode, pts, IntPtr.Zero, IntPtr.Zero);

                        Hashtable row = NewInfo();
                        row["a"] = ids[i];
                        row["b"] = ids[j];
                        row["count"] = pts.Count;

                        ArrayList points = new ArrayList();
                        for (int p = 0; p < pts.Count; p++)
                        {
                            Hashtable item = NewInfo();
                            item["x"] = pts[p].X;
                            item["y"] = pts[p].Y;
                            item["z"] = pts[p].Z;
                            points.Add(item);
                        }
                        row["points"] = points;
                        rows.Add(row);
                        totalIntersections += pts.Count;
                    }
                }
            }

            Hashtable info = NewInfo();
            info["pairs"] = rows.Count;
            info["intersections"] = totalIntersections;
            info["rows"] = rows;
            return info;
        }

        public Hashtable BreakCurvesAtAllIntersectionsBatch(IList curveIds, bool eraseSource)
        {
            int changed = 0;
            int parts = 0;
            ArrayList resultIds = new ArrayList();
            List<ObjectId> ids = new List<ObjectId>();
            if (curveIds != null)
            {
                foreach (object raw in curveIds) if (raw is ObjectId) ids.Add((ObjectId)raw);
            }

            for (int i = 0; i < ids.Count; i++)
            {
                ArrayList boundaries = new ArrayList();
                for (int j = 0; j < ids.Count; j++)
                {
                    if (j == i) continue;
                    boundaries.Add(ids[j]);
                }

                try
                {
                    ObjectId[] split = BreakCurveAtAllIntersections(ids[i], boundaries, eraseSource);
                    if (split.Length > 0)
                    {
                        changed++;
                        parts += split.Length;
                        for (int k = 0; k < split.Length; k++) resultIds.Add(split[k]);
                    }
                }
                catch
                {
                }
            }

            Hashtable info = NewInfo();
            info["changed"] = changed;
            info["parts"] = parts;
            info["ids"] = resultIds;
            return info;
        }

        public Hashtable AutoTrimExtendByBoundaries(IList curveIds, IList boundaryIds, string trimMode, string extendMode, bool eraseSource)
        {
            Hashtable trim = TrimCurvesToBoundaries(curveIds, boundaryIds, trimMode, eraseSource);
            Hashtable extend = ExtendCurvesToBoundaries(curveIds, boundaryIds, extendMode);
            Hashtable info = NewInfo();
            info["trim_changed"] = Convert.ToInt32(trim["changed"], CultureInfo.InvariantCulture);
            info["extend_changed"] = Convert.ToInt32(extend["changed"], CultureInfo.InvariantCulture);
            info["trim_result_ids"] = trim["result_ids"];
            info["extend_points"] = extend["points"];
            return info;
        }

        public Hashtable OffsetEntitiesTowardSeedsBatch(IList jobs)
        {
            int ok = 0;
            int fail = 0;
            ArrayList ids = new ArrayList();
            if (jobs != null)
            {
                foreach (object raw in jobs)
                {
                    Hashtable item = raw as Hashtable;
                    if (item == null || !item.ContainsKey("entity_id")) { fail++; continue; }
                    if (!(item["entity_id"] is ObjectId)) { fail++; continue; }
                    try
                    {
                        ObjectId id = OffsetEntityTowardPoint(
                            (ObjectId)item["entity_id"],
                            item.ContainsKey("distance") ? Convert.ToDouble(item["distance"], CultureInfo.InvariantCulture) : 2.0,
                            item.ContainsKey("x") ? Convert.ToDouble(item["x"], CultureInfo.InvariantCulture) : 0.0,
                            item.ContainsKey("y") ? Convert.ToDouble(item["y"], CultureInfo.InvariantCulture) : 0.0,
                            item.ContainsKey("z") ? Convert.ToDouble(item["z"], CultureInfo.InvariantCulture) : 0.0);
                        ids.Add(id);
                        ok++;
                    }
                    catch
                    {
                        fail++;
                    }
                }
            }

            Hashtable info = NewInfo();
            info["ok"] = ok;
            info["fail"] = fail;
            info["ids"] = ids;
            return info;
        }

        public Hashtable CopyTransformBatch(IList jobs)
        {
            int created = 0;
            ArrayList createdIds = new ArrayList();
            if (jobs == null)
            {
                Hashtable empty = NewInfo();
                empty["created"] = 0;
                empty["ids"] = createdIds;
                return empty;
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < jobs.Count; i++)
                {
                    Hashtable job = jobs[i] as Hashtable;
                    if (job == null || !job.ContainsKey("entity_id")) continue;
                    if (!(job["entity_id"] is ObjectId)) continue;

                    Entity src = tr.GetObject((ObjectId)job["entity_id"], OpenMode.ForRead, false) as Entity;
                    if (src == null || src.IsErased) continue;

                    int copies = job.ContainsKey("copies") ? Math.Max(1, Convert.ToInt32(job["copies"], CultureInfo.InvariantCulture)) : 1;
                    double dx = job.ContainsKey("dx") ? Convert.ToDouble(job["dx"], CultureInfo.InvariantCulture) : 0.0;
                    double dy = job.ContainsKey("dy") ? Convert.ToDouble(job["dy"], CultureInfo.InvariantCulture) : 0.0;
                    double dz = job.ContainsKey("dz") ? Convert.ToDouble(job["dz"], CultureInfo.InvariantCulture) : 0.0;
                    double rotDeg = job.ContainsKey("rot_deg") ? Convert.ToDouble(job["rot_deg"], CultureInfo.InvariantCulture) : 0.0;
                    double scale = job.ContainsKey("scale") ? Convert.ToDouble(job["scale"], CultureInfo.InvariantCulture) : 1.0;
                    Point3d basePt = ResolveTransformBasePoint(src);

                    BlockTableRecord owner = tr.GetObject(src.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                    for (int c = 1; c <= copies; c++)
                    {
                        Entity clone = src.Clone() as Entity;
                        if (clone == null) continue;
                        if (dx != 0.0 || dy != 0.0 || dz != 0.0)
                        {
                            clone.TransformBy(Matrix3d.Displacement(new Vector3d(dx * c, dy * c, dz * c)));
                        }
                        if (Math.Abs(rotDeg) > 1e-12)
                        {
                            clone.TransformBy(Matrix3d.Rotation(DegToRad(rotDeg * c), Vector3d.ZAxis, basePt));
                        }
                        if (Math.Abs(scale - 1.0) > 1e-12)
                        {
                            double s = 1.0 + ((scale - 1.0) * c);
                            clone.TransformBy(Matrix3d.Scaling(s, basePt));
                        }
                        ObjectId newId = owner.AppendEntity(clone);
                        tr.AddNewlyCreatedDBObject(clone, true);
                        createdIds.Add(newId);
                        created++;
                    }
                }
                tr.Commit();
            }

            Hashtable info = NewInfo();
            info["created"] = created;
            info["ids"] = createdIds;
            return info;
        }

        public string ExportEntityAuditCsv(string outputPath, IList entityIds)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("outputPath non valido");

            string full = Path.GetFullPath(outputPath);
            string dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (StreamWriter sw = new StreamWriter(full, false))
            {
                sw.WriteLine("id,handle,dxf,layer,color_index,linetype,lineweight,linetype_scale,visible,owner");
                using (Transaction tr = _db.TransactionManager.StartTransaction())
                {
                    if (entityIds != null)
                    {
                        for (int i = 0; i < entityIds.Count; i++)
                        {
                            if (!(entityIds[i] is ObjectId)) continue;
                            ObjectId id = (ObjectId)entityIds[i];
                            Entity e = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                            if (e == null || e.IsErased) continue;
                            string owner = string.Empty;
                            try
                            {
                                DBObject ownerObj = tr.GetObject(e.OwnerId, OpenMode.ForRead);
                                owner = ownerObj == null ? string.Empty : ownerObj.Handle.ToString();
                            }
                            catch
                            {
                            }

                            sw.WriteLine(
                                Csv(id.ToString()) + "," +
                                Csv(e.Handle.ToString()) + "," +
                                Csv(e.GetRXClass().DxfName) + "," +
                                Csv(e.Layer) + "," +
                                Csv(e.ColorIndex.ToString(CultureInfo.InvariantCulture)) + "," +
                                Csv(e.Linetype) + "," +
                                Csv(((int)e.LineWeight).ToString(CultureInfo.InvariantCulture)) + "," +
                                Csv(e.LinetypeScale.ToString(CultureInfo.InvariantCulture)) + "," +
                                Csv(e.Visible ? "1" : "0") + "," +
                                Csv(owner));
                        }
                    }
                }
            }

            return full;
        }

        public string ExportDatabaseSnapshot(string outputPath, string dictionaryPath, int maxDepth)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("outputPath non valido");
            string full = Path.GetFullPath(outputPath);
            string dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Hashtable counts = CountEntitiesInModelSpaceByDxf();
            Hashtable spaces = GetSpaceEntityStats();
            ArrayList tree;
            string dictError = string.Empty;
            try
            {
                tree = ListNamedDictionaryTree(dictionaryPath ?? string.Empty, Math.Max(0, maxDepth));
            }
            catch (Exception ex)
            {
                tree = new ArrayList();
                dictError = ex.Message;
            }

            using (StreamWriter sw = new StreamWriter(full, false))
            {
                sw.WriteLine("PYLOAD2026R DATABASE SNAPSHOT");
                sw.WriteLine("build=" + GetBuildMarker());
                sw.WriteLine("timestamp=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(dictError))
                {
                    sw.WriteLine("dictionary_error=" + dictError);
                }
                sw.WriteLine();
                sw.WriteLine("[ModelSpace counts]");
                foreach (DictionaryEntry de in counts)
                {
                    sw.WriteLine(Convert.ToString(de.Key, CultureInfo.InvariantCulture) + "=" + Convert.ToString(de.Value, CultureInfo.InvariantCulture));
                }
                sw.WriteLine();
                sw.WriteLine("[Space stats]");
                sw.WriteLine("total=" + Convert.ToString(spaces["total"], CultureInfo.InvariantCulture));
                sw.WriteLine("model=" + Convert.ToString(spaces["model"], CultureInfo.InvariantCulture));
                sw.WriteLine("paper=" + Convert.ToString(spaces["paper"], CultureInfo.InvariantCulture));
                sw.WriteLine("layouts=" + Convert.ToString(spaces["layouts"], CultureInfo.InvariantCulture));
                sw.WriteLine();
                sw.WriteLine("[Named dictionary tree]");
                for (int i = 0; i < tree.Count; i++)
                {
                    Hashtable item = tree[i] as Hashtable;
                    if (item == null) continue;
                    sw.WriteLine(
                        Convert.ToString(item["path"], CultureInfo.InvariantCulture) + " | " +
                        Convert.ToString(item["type"], CultureInfo.InvariantCulture) + " | " +
                        Convert.ToString(item["dxf_name"], CultureInfo.InvariantCulture));
                }
            }
            return full;
        }

        public string[] GetPublicApiMethodNames(string containsFilter)
        {
            string needle = containsFilter == null ? string.Empty : containsFilter.Trim();
            List<string> names = new List<string>();
            MethodInfo[] methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo mi = methods[i];
                if (mi.DeclaringType == typeof(object)) continue;
                if (!string.IsNullOrEmpty(needle) &&
                    mi.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                names.Add(mi.Name);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names.ToArray();
        }

        public string ExportApiMethodsReport(string outputPath, string containsFilter)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("outputPath non valido");
            string full = Path.GetFullPath(outputPath);
            string dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string[] names = GetPublicApiMethodNames(containsFilter);
            using (StreamWriter sw = new StreamWriter(full, false))
            {
                sw.WriteLine("PYLOAD2026R API METHODS");
                sw.WriteLine("build=" + GetBuildMarker());
                sw.WriteLine("timestamp=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                sw.WriteLine("filter=" + (containsFilter ?? string.Empty));
                sw.WriteLine("count=" + names.Length.ToString(CultureInfo.InvariantCulture));
                sw.WriteLine();
                for (int i = 0; i < names.Length; i++) sw.WriteLine(names[i]);
            }
            return full;
        }

        public Hashtable RunDeterministicModifyPack(double baseX, double baseY, double baseZ)
        {
            ObjectId main = AddLine(baseX + 0.0, baseY + 0.0, baseZ, baseX + 120.0, baseY + 0.0, baseZ);
            ObjectId cut1 = AddLine(baseX + 30.0, baseY - 20.0, baseZ, baseX + 30.0, baseY + 20.0, baseZ);
            ObjectId cut2 = AddLine(baseX + 90.0, baseY - 20.0, baseZ, baseX + 90.0, baseY + 20.0, baseZ);
            ObjectId[] parts = BreakCurveAtAllIntersections(main, new ArrayList { cut1, cut2 }, false);

            ObjectId trimLine = AddLine(baseX + 150.0, baseY + 0.0, baseZ, baseX + 210.0, baseY + 0.0, baseZ);
            ObjectId trimBoundary = AddLine(baseX + 180.0, baseY - 15.0, baseZ, baseX + 180.0, baseY + 15.0, baseZ);
            ObjectId trimmed = TrimCurveEndToEntity(trimLine, trimBoundary, true);
            double trimLength = GetCurveLength(trimmed);

            ObjectId extendLine = AddLine(baseX + 260.0, baseY + 0.0, baseZ, baseX + 300.0, baseY + 0.0, baseZ);
            ObjectId extendBoundary = AddLine(baseX + 340.0, baseY - 15.0, baseZ, baseX + 340.0, baseY + 15.0, baseZ);
            Point3d extended = ExtendCurveEndToEntity(extendLine, extendBoundary);

            ObjectId poly = AddPolyline(new[]
            {
                baseX + 360.0, baseY + 0.0,
                baseX + 395.0, baseY + 0.0,
                baseX + 395.0, baseY + 35.0
            }, false);
            ObjectId polyBoundary = AddLine(baseX + 370.0, baseY + 70.0, baseZ, baseX + 430.0, baseY + 70.0, baseZ);
            Point3d polyEnd = ExtendCurveEndToEntity(poly, polyBoundary);

            Hashtable matrix = BuildIntersectionsMatrix(new ArrayList { cut1, cut2, trimBoundary, extendBoundary }, false, false);

            Hashtable info = NewInfo();
            info["break_parts"] = parts.Length;
            info["trim_len"] = trimLength;
            info["extend_x"] = extended.X;
            info["extend_poly_x"] = polyEnd.X;
            info["matrix_pairs"] = matrix["pairs"];
            info["matrix_hits"] = matrix["intersections"];
            return info;
        }

        private static string Csv(string text)
        {
            string s = text ?? string.Empty;
            if (s.IndexOf('"') >= 0) s = s.Replace("\"", "\"\"");
            if (s.IndexOf(',') >= 0 || s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0) s = "\"" + s + "\"";
            return s;
        }

        private static Point3d ResolveTransformBasePoint(Entity entity)
        {
            try
            {
                Extents3d ext = entity.GeometricExtents;
                return new Point3d(
                    (ext.MinPoint.X + ext.MaxPoint.X) * 0.5,
                    (ext.MinPoint.Y + ext.MaxPoint.Y) * 0.5,
                    (ext.MinPoint.Z + ext.MaxPoint.Z) * 0.5);
            }
            catch
            {
                return Point3d.Origin;
            }
        }

        private BlockTableRecord ResolveSpaceRecordByName(Transaction tr, string spaceName)
        {
            BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
            string wanted = string.IsNullOrWhiteSpace(spaceName) ? BlockTableRecord.ModelSpace : spaceName.Trim();
            foreach (ObjectId id in bt)
            {
                BlockTableRecord btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null || !btr.IsLayout) continue;
                if (string.Equals(btr.Name, wanted, StringComparison.OrdinalIgnoreCase)) return btr;
            }

            // Fallback: allow passing layout tab names (Layout object name) instead of BTR names.
            try
            {
                DBDictionary layouts = tr.GetObject(_db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (layouts != null && layouts.Contains(wanted))
                {
                    Layout lo = tr.GetObject(layouts.GetAt(wanted), OpenMode.ForRead) as Layout;
                    if (lo != null)
                    {
                        BlockTableRecord byLayout = tr.GetObject(lo.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                        if (byLayout != null) return byLayout;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private List<ObjectId> CollectSpaceEntityIds(Transaction tr, bool onlyModelSpace)
        {
            List<ObjectId> ids = new List<ObjectId>();
            BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
            foreach (ObjectId id in bt)
            {
                BlockTableRecord btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null || !btr.IsLayout) continue;
                if (onlyModelSpace && !string.Equals(btr.Name, BlockTableRecord.ModelSpace, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (ObjectId entId in btr) ids.Add(entId);
            }
            return ids;
        }
    }
}
