using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public string[] GetViewNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ViewTable vt = tr.GetObject(_db.ViewTableId, OpenMode.ForRead) as ViewTable;
                ArrayList names = new ArrayList();
                foreach (ObjectId id in vt)
                {
                    ViewTableRecord vtr = tr.GetObject(id, OpenMode.ForRead) as ViewTableRecord;
                    if (vtr != null) names.Add(vtr.Name);
                }
                string[] result = new string[names.Count];
                names.CopyTo(result);
                return result;
            }
        }

        public string[] GetUcsNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                UcsTable ut = tr.GetObject(_db.UcsTableId, OpenMode.ForRead) as UcsTable;
                ArrayList names = new ArrayList();
                foreach (ObjectId id in ut)
                {
                    UcsTableRecord utr = tr.GetObject(id, OpenMode.ForRead) as UcsTableRecord;
                    if (utr != null) names.Add(utr.Name);
                }
                string[] result = new string[names.Count];
                names.CopyTo(result);
                return result;
            }
        }

        public Hashtable GetViewUcsViewportStats()
        {
            Hashtable info = NewInfo();
            int views = 0;
            int ucs = 0;
            int vps = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ViewTable vt = tr.GetObject(_db.ViewTableId, OpenMode.ForRead) as ViewTable;
                UcsTable ut = tr.GetObject(_db.UcsTableId, OpenMode.ForRead) as UcsTable;
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord ps = tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId _ in vt) views++;
                foreach (ObjectId _ in ut) ucs++;
                foreach (ObjectId id in ps)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Viewport) vps++;
                }
            }

            info["views"] = views;
            info["ucs"] = ucs;
            info["paper_viewports"] = vps;
            return info;
        }

        public bool EnsureNamedView(string name, double centerX, double centerY, double width, double height)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name non valido");
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ViewTable vt = tr.GetObject(_db.ViewTableId, OpenMode.ForWrite) as ViewTable;
                if (vt.Has(name))
                {
                    ViewTableRecord existing = tr.GetObject(vt[name], OpenMode.ForWrite) as ViewTableRecord;
                    existing.CenterPoint = new Point2d(centerX, centerY);
                    existing.Width = width;
                    existing.Height = height;
                    tr.Commit();
                    return false;
                }

                ViewTableRecord vtr = new ViewTableRecord();
                vtr.Name = name;
                vtr.CenterPoint = new Point2d(centerX, centerY);
                vtr.Width = width;
                vtr.Height = height;
                vt.Add(vtr);
                tr.AddNewlyCreatedDBObject(vtr, true);
                tr.Commit();
                return true;
            }
        }

        public bool EnsureNamedUcs(string name, double ox, double oy, double oz, double xax, double xay, double xaz, double yax, double yay, double yaz)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name non valido");
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                UcsTable ut = tr.GetObject(_db.UcsTableId, OpenMode.ForWrite) as UcsTable;
                if (ut.Has(name))
                {
                    UcsTableRecord existing = tr.GetObject(ut[name], OpenMode.ForWrite) as UcsTableRecord;
                    existing.Origin = new Point3d(ox, oy, oz);
                    existing.XAxis = new Vector3d(xax, xay, xaz);
                    existing.YAxis = new Vector3d(yax, yay, yaz);
                    tr.Commit();
                    return false;
                }

                UcsTableRecord utr = new UcsTableRecord();
                utr.Name = name;
                utr.Origin = new Point3d(ox, oy, oz);
                utr.XAxis = new Vector3d(xax, xay, xaz);
                utr.YAxis = new Vector3d(yax, yay, yaz);
                ut.Add(utr);
                tr.AddNewlyCreatedDBObject(utr, true);
                tr.Commit();
                return true;
            }
        }

        public ObjectId AddBox(double x, double y, double z, double length, double width, double height)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(_db), OpenMode.ForWrite) as BlockTableRecord;
                Solid3d s = new Solid3d();
                s.SetDatabaseDefaults();
                s.CreateBox(length, width, height);
                s.TransformBy(Matrix3d.Displacement(new Vector3d(x, y, z)));
                ObjectId id = ms.AppendEntity(s);
                tr.AddNewlyCreatedDBObject(s, true);
                tr.Commit();
                return id;
            }
        }

        public Hashtable GetSolid3dInfo(ObjectId solidId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Solid3d s = tr.GetObject(solidId, OpenMode.ForRead) as Solid3d;
                if (s == null) throw new ArgumentException("solidId non identifica un Solid3d");
                Hashtable info = NewInfo();
                info["id"] = solidId.ToString();
                info["handle"] = s.Handle.ToString();
                try
                {
                    info["volume"] = s.MassProperties.Volume;
                }
                catch
                {
                    info["volume"] = 0.0;
                }
                return info;
            }
        }

        public ObjectId[] CreateRegionsFromEntities(IList entityIds)
        {
            if (entityIds == null) return new ObjectId[0];
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(_db), OpenMode.ForWrite) as BlockTableRecord;
                DBObjectCollection curves = new DBObjectCollection();
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId)) continue;
                    Entity ent = tr.GetObject((ObjectId)raw, OpenMode.ForRead, false) as Entity;
                    if (ent != null) curves.Add(ent);
                }

                DBObjectCollection regs = Region.CreateFromCurves(curves);
                List<ObjectId> ids = new List<ObjectId>();
                foreach (DBObject obj in regs)
                {
                    Region reg = obj as Region;
                    if (reg == null) continue;
                    ObjectId id = ms.AppendEntity(reg);
                    tr.AddNewlyCreatedDBObject(reg, true);
                    ids.Add(id);
                }
                tr.Commit();
                return ids.ToArray();
            }
        }

        public double GetRegionArea(ObjectId regionId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Region reg = tr.GetObject(regionId, OpenMode.ForRead) as Region;
                if (reg == null) throw new ArgumentException("regionId non identifica una Region");
                return reg.Area;
            }
        }

        public void BooleanRegions(ObjectId primaryRegionId, ObjectId otherRegionId, string operation)
        {
            string op = (operation ?? "union").Trim().ToLowerInvariant();
            BooleanOperationType type = BooleanOperationType.BoolUnite;
            if (op == "subtract") type = BooleanOperationType.BoolSubtract;
            else if (op == "intersect") type = BooleanOperationType.BoolIntersect;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Region a = tr.GetObject(primaryRegionId, OpenMode.ForWrite) as Region;
                Region b = tr.GetObject(otherRegionId, OpenMode.ForWrite) as Region;
                if (a == null || b == null) throw new ArgumentException("BooleanRegions richiede due Region valide");
                a.BooleanOperation(type, b);
                tr.Commit();
            }
        }

        public ObjectId[] ExplodeRegion(ObjectId regionId, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Region reg = tr.GetObject(regionId, OpenMode.ForRead) as Region;
                if (reg == null) throw new ArgumentException("regionId non identifica una Region");
                DBObjectCollection parts = new DBObjectCollection();
                reg.Explode(parts);
                BlockTableRecord owner = tr.GetObject(reg.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                List<ObjectId> ids = new List<ObjectId>();
                foreach (DBObject obj in parts)
                {
                    Entity e = obj as Entity;
                    if (e == null) continue;
                    ObjectId id = owner.AppendEntity(e);
                    tr.AddNewlyCreatedDBObject(e, true);
                    ids.Add(id);
                }
                if (eraseSource)
                {
                    Region w = tr.GetObject(regionId, OpenMode.ForWrite) as Region;
                    if (w != null && !w.IsErased) w.Erase(true);
                }
                tr.Commit();
                return ids.ToArray();
            }
        }

        public string EnsureTestAttributedBlock(string blockName, string tag, string prompt, string defaultText, double textHeight)
        {
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentException("blockName non valido");
            if (string.IsNullOrWhiteSpace(tag)) tag = "TAG1";
            if (string.IsNullOrWhiteSpace(prompt)) prompt = "PROMPT";
            if (textHeight <= 0.0) textHeight = 2.5;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                if (bt.Has(blockName))
                {
                    tr.Commit();
                    return blockName;
                }

                BlockTableRecord btr = new BlockTableRecord();
                btr.Name = blockName;
                bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);

                Line l = new Line(new Point3d(0, 0, 0), new Point3d(16, 0, 0));
                btr.AppendEntity(l);
                tr.AddNewlyCreatedDBObject(l, true);

                AttributeDefinition ad = new AttributeDefinition();
                ad.Position = new Point3d(1.0, 1.0, 0.0);
                ad.Tag = tag;
                ad.Prompt = prompt;
                ad.TextString = defaultText ?? string.Empty;
                ad.Height = textHeight;
                ad.Verifiable = false;
                ad.Invisible = false;
                btr.AppendEntity(ad);
                tr.AddNewlyCreatedDBObject(ad, true);

                tr.Commit();
                return blockName;
            }
        }

        public ObjectId InsertBlockWithAttributes(string blockName, double x, double y, double z, Hashtable values)
        {
            ObjectId brId = InsertBlock(blockName, x, y, z);
            if (values == null || values.Count == 0) return brId;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(brId, OpenMode.ForRead) as BlockReference;
                if (br == null) return brId;
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                    if (ar == null) continue;
                    if (values.ContainsKey(ar.Tag))
                    {
                        ar.TextString = Convert.ToString(values[ar.Tag]);
                    }
                }
                tr.Commit();
            }
            return brId;
        }

        public ObjectId[] GetModelSpaceEntityIds()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(_db), OpenMode.ForRead) as BlockTableRecord;
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in ms) ids.Add(id);
                return ids.ToArray();
            }
        }

        public ObjectId[] GetPaperSpaceEntityIds()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord ps = tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForRead) as BlockTableRecord;
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in ps) ids.Add(id);
                return ids.ToArray();
            }
        }

        public Hashtable CountEntitiesInModelSpaceByDxf()
        {
            Hashtable counts = NewInfo();
            int total = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(_db), OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId id in ms)
                {
                    total++;
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (e == null) continue;
                    string dxf = e.GetRXClass().DxfName;
                    int current = counts.ContainsKey(dxf) ? Convert.ToInt32(counts[dxf]) : 0;
                    counts[dxf] = current + 1;
                }
            }
            counts["total"] = total;
            return counts;
        }

        public ObjectId[] AddBoxesBatch(IList boxItems)
        {
            if (boxItems == null) return new ObjectId[0];
            List<ObjectId> ids = new List<ObjectId>();
            foreach (object raw in boxItems)
            {
                Hashtable item = raw as Hashtable;
                if (item == null) continue;
                double x = item.ContainsKey("x") ? Convert.ToDouble(item["x"]) : 0.0;
                double y = item.ContainsKey("y") ? Convert.ToDouble(item["y"]) : 0.0;
                double z = item.ContainsKey("z") ? Convert.ToDouble(item["z"]) : 0.0;
                double l = item.ContainsKey("length") ? Convert.ToDouble(item["length"]) : 10.0;
                double w = item.ContainsKey("width") ? Convert.ToDouble(item["width"]) : 10.0;
                double h = item.ContainsKey("height") ? Convert.ToDouble(item["height"]) : 10.0;
                ids.Add(AddBox(x, y, z, l, w, h));
            }
            return ids.ToArray();
        }

        public Hashtable GetRegionInfo(ObjectId regionId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Region reg = tr.GetObject(regionId, OpenMode.ForRead) as Region;
                if (reg == null) throw new ArgumentException("regionId non identifica una Region");
                DBObjectCollection parts = new DBObjectCollection();
                reg.Explode(parts);

                Hashtable info = NewInfo();
                info["id"] = regionId.ToString();
                info["handle"] = reg.Handle.ToString();
                info["area"] = reg.Area;
                info["exploded_count"] = parts.Count;
                return info;
            }
        }

        public Hashtable BooleanRegionsBatch(IList operations)
        {
            int changed = 0;
            if (operations != null)
            {
                foreach (object raw in operations)
                {
                    Hashtable op = raw as Hashtable;
                    if (op == null) continue;
                    if (!op.ContainsKey("primary") || !op.ContainsKey("other")) continue;
                    if (!(op["primary"] is ObjectId) || !(op["other"] is ObjectId)) continue;
                    string kind = op.ContainsKey("operation") ? Convert.ToString(op["operation"]) : "union";
                    try
                    {
                        BooleanRegions((ObjectId)op["primary"], (ObjectId)op["other"], kind);
                        changed++;
                    }
                    catch
                    {
                    }
                }
            }
            Hashtable info = NewInfo();
            info["changed"] = changed;
            info["total"] = operations == null ? 0 : operations.Count;
            return info;
        }

        public bool DeleteNamedView(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ViewTable vt = tr.GetObject(_db.ViewTableId, OpenMode.ForWrite) as ViewTable;
                if (!vt.Has(name))
                {
                    tr.Commit();
                    return false;
                }
                ViewTableRecord rec = tr.GetObject(vt[name], OpenMode.ForWrite) as ViewTableRecord;
                if (rec != null && !rec.IsErased) rec.Erase(true);
                tr.Commit();
                return true;
            }
        }

        public bool DeleteNamedUcs(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                UcsTable ut = tr.GetObject(_db.UcsTableId, OpenMode.ForWrite) as UcsTable;
                if (!ut.Has(name))
                {
                    tr.Commit();
                    return false;
                }
                UcsTableRecord rec = tr.GetObject(ut[name], OpenMode.ForWrite) as UcsTableRecord;
                if (rec != null && !rec.IsErased) rec.Erase(true);
                tr.Commit();
                return true;
            }
        }

        public Hashtable GetBlockReferenceAttributes(ObjectId blockReferenceId)
        {
            Hashtable map = NewInfo();
            int count = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null) return map;
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (ar == null) continue;
                    map[ar.Tag] = ar.TextString;
                    count++;
                }
            }
            map["count"] = count;
            return map;
        }

        public int SetBlockReferenceAttributes(ObjectId blockReferenceId, Hashtable values)
        {
            if (values == null || values.Count == 0) return 0;
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null) return 0;
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                    if (ar == null) continue;
                    if (values.ContainsKey(ar.Tag))
                    {
                        ar.TextString = Convert.ToString(values[ar.Tag]);
                        changed++;
                    }
                }
                tr.Commit();
            }
            return changed;
        }

        public int GetNamedDictionaryEntriesCount(string dictionaryPath)
        {
            return GetNamedDictionaryEntries(dictionaryPath).Count;
        }
    }
}
