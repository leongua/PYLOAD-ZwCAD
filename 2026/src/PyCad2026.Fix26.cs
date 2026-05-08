using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public ObjectId[] SelectByHandles(IList handleStrings)
        {
            return GetObjectIdsByHandleStrings(handleStrings);
        }

        public ObjectId[] SelectByDxfInSpace(IList dxfFilters, string spaceName)
        {
            Hashtable filters = NormalizeDxfPairs(dxfFilters);
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = ResolveSpaceRecordFix26(tr, spaceName);
                if (btr == null) return new ObjectId[0];
                foreach (ObjectId id in btr)
                {
                    Entity e;
                    try { e = tr.GetObject(id, OpenMode.ForRead, false) as Entity; }
                    catch { continue; }
                    if (e == null || e.IsErased) continue;
                    if (MatchesFilters(e, filters, tr)) ids.Add(id);
                }
            }
            return ids.ToArray();
        }

        public Hashtable GetSelectionStats(IList entityIds)
        {
            Hashtable info = NewInfo();
            Hashtable byLayer = NewInfo();
            Hashtable byDxf = NewInfo();
            info["by_layer"] = byLayer;
            info["by_dxf"] = byDxf;

            int count = 0;
            bool hasExt = false;
            Extents3d ext = new Extents3d();

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                if (entityIds != null)
                {
                    foreach (object raw in entityIds)
                    {
                        if (!(raw is ObjectId)) continue;
                        Entity e;
                        try { e = tr.GetObject((ObjectId)raw, OpenMode.ForRead, false) as Entity; }
                        catch { continue; }
                        if (e == null || e.IsErased) continue;
                        count++;

                        string layer = e.Layer ?? string.Empty;
                        byLayer[layer] = (byLayer.ContainsKey(layer) ? Convert.ToInt32(byLayer[layer], CultureInfo.InvariantCulture) : 0) + 1;
                        string dxf = e.GetRXClass().DxfName;
                        byDxf[dxf] = (byDxf.ContainsKey(dxf) ? Convert.ToInt32(byDxf[dxf], CultureInfo.InvariantCulture) : 0) + 1;

                        try
                        {
                            Extents3d eext = e.GeometricExtents;
                            if (!hasExt) { ext = eext; hasExt = true; }
                            else ext.AddExtents(eext);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            info["count"] = count;
            info["has_extents"] = hasExt;
            if (hasExt)
            {
                info["min_x"] = ext.MinPoint.X;
                info["min_y"] = ext.MinPoint.Y;
                info["min_z"] = ext.MinPoint.Z;
                info["max_x"] = ext.MaxPoint.X;
                info["max_y"] = ext.MaxPoint.Y;
                info["max_z"] = ext.MaxPoint.Z;
            }
            return info;
        }

        public ObjectId[] CopyEntitiesMultiple(IList entityIds, IList displacements)
        {
            List<ObjectId> outIds = new List<ObjectId>();
            if (entityIds == null || displacements == null) return outIds.ToArray();

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object rawId in entityIds)
                {
                    if (!(rawId is ObjectId)) continue;
                    Entity src;
                    try { src = tr.GetObject((ObjectId)rawId, OpenMode.ForRead, false) as Entity; }
                    catch { continue; }
                    if (src == null || src.IsErased) continue;
                    BlockTableRecord owner = tr.GetObject(src.OwnerId, OpenMode.ForWrite) as BlockTableRecord;

                    foreach (object rawDisp in displacements)
                    {
                        Hashtable d = rawDisp as Hashtable;
                        if (d == null) continue;
                        double dx = d.ContainsKey("dx") ? Convert.ToDouble(d["dx"], CultureInfo.InvariantCulture) : 0.0;
                        double dy = d.ContainsKey("dy") ? Convert.ToDouble(d["dy"], CultureInfo.InvariantCulture) : 0.0;
                        double dz = d.ContainsKey("dz") ? Convert.ToDouble(d["dz"], CultureInfo.InvariantCulture) : 0.0;

                        Entity clone = src.Clone() as Entity;
                        if (clone == null) continue;
                        clone.TransformBy(Matrix3d.Displacement(new Vector3d(dx, dy, dz)));
                        ObjectId nid = owner.AppendEntity(clone);
                        tr.AddNewlyCreatedDBObject(clone, true);
                        outIds.Add(nid);
                    }
                }
                tr.Commit();
            }
            return outIds.ToArray();
        }

        public Hashtable OffsetEntitiesBothSidesBatch(IList entityIds, double offsetDistance)
        {
            Hashtable info = NewInfo();
            int ok = 0;
            int fail = 0;
            int created = 0;
            ArrayList ids = new ArrayList();

            if (entityIds != null)
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId)) continue;
                    try
                    {
                        ObjectId[] pair = OffsetEntityBothSides((ObjectId)raw, offsetDistance);
                        ok++;
                        created += pair.Length;
                        foreach (ObjectId id in pair) ids.Add(id);
                    }
                    catch
                    {
                        fail++;
                    }
                }
            }

            info["ok"] = ok;
            info["fail"] = fail;
            info["created"] = created;
            info["ids"] = ids;
            return info;
        }

        public Hashtable BreakCurvesAtPointsBatch(IList jobs, bool eraseSource)
        {
            Hashtable info = NewInfo();
            int ok = 0;
            int fail = 0;
            int created = 0;
            ArrayList ids = new ArrayList();

            if (jobs != null)
            {
                foreach (object raw in jobs)
                {
                    Hashtable j = raw as Hashtable;
                    if (j == null || !j.ContainsKey("entity_id")) { fail++; continue; }
                    if (!(j["entity_id"] is ObjectId)) { fail++; continue; }
                    double x = j.ContainsKey("x") ? Convert.ToDouble(j["x"], CultureInfo.InvariantCulture) : 0.0;
                    double y = j.ContainsKey("y") ? Convert.ToDouble(j["y"], CultureInfo.InvariantCulture) : 0.0;
                    double z = j.ContainsKey("z") ? Convert.ToDouble(j["z"], CultureInfo.InvariantCulture) : 0.0;

                    try
                    {
                        ObjectId[] parts = BreakCurveAtPoint((ObjectId)j["entity_id"], x, y, z, eraseSource);
                        ok++;
                        created += parts.Length;
                        foreach (ObjectId id in parts) ids.Add(id);
                    }
                    catch
                    {
                        fail++;
                    }
                }
            }

            info["ok"] = ok;
            info["fail"] = fail;
            info["created"] = created;
            info["ids"] = ids;
            return info;
        }

        public Hashtable CopyRotateScaleBatch(IList jobs)
        {
            Hashtable info = NewInfo();
            int ok = 0;
            int fail = 0;
            ArrayList ids = new ArrayList();

            if (jobs != null)
            {
                foreach (object raw in jobs)
                {
                    Hashtable j = raw as Hashtable;
                    if (j == null || !j.ContainsKey("entity_id")) { fail++; continue; }
                    if (!(j["entity_id"] is ObjectId)) { fail++; continue; }
                    ObjectId srcId = (ObjectId)j["entity_id"];
                    try
                    {
                        double dx = j.ContainsKey("dx") ? Convert.ToDouble(j["dx"], CultureInfo.InvariantCulture) : 0.0;
                        double dy = j.ContainsKey("dy") ? Convert.ToDouble(j["dy"], CultureInfo.InvariantCulture) : 0.0;
                        double dz = j.ContainsKey("dz") ? Convert.ToDouble(j["dz"], CultureInfo.InvariantCulture) : 0.0;
                        double ang = j.ContainsKey("angle_degrees") ? Convert.ToDouble(j["angle_degrees"], CultureInfo.InvariantCulture) : 0.0;
                        double scale = j.ContainsKey("scale") ? Convert.ToDouble(j["scale"], CultureInfo.InvariantCulture) : 1.0;
                        double bx = j.ContainsKey("base_x") ? Convert.ToDouble(j["base_x"], CultureInfo.InvariantCulture) : 0.0;
                        double by = j.ContainsKey("base_y") ? Convert.ToDouble(j["base_y"], CultureInfo.InvariantCulture) : 0.0;
                        double bz = j.ContainsKey("base_z") ? Convert.ToDouble(j["base_z"], CultureInfo.InvariantCulture) : 0.0;

                        ObjectId nid = CopyEntity(srcId, dx, dy, dz);
                        if (Math.Abs(ang) > 1e-12) RotateEntity(nid, bx, by, bz, ang);
                        if (Math.Abs(scale - 1.0) > 1e-12) ScaleEntity(nid, bx, by, bz, scale);
                        ids.Add(nid);
                        ok++;
                    }
                    catch
                    {
                        fail++;
                    }
                }
            }

            info["ok"] = ok;
            info["fail"] = fail;
            info["ids"] = ids;
            return info;
        }

        private BlockTableRecord ResolveSpaceRecordFix26(Transaction tr, string spaceName)
        {
            BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
            string wanted = string.IsNullOrWhiteSpace(spaceName) ? BlockTableRecord.ModelSpace : spaceName.Trim();
            foreach (ObjectId id in bt)
            {
                BlockTableRecord btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null || !btr.IsLayout) continue;
                if (string.Equals(btr.Name, wanted, StringComparison.OrdinalIgnoreCase)) return btr;
            }

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
            return null;
        }
    }
}
