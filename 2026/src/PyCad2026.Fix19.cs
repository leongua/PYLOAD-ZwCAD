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
        public Hashtable GetSpaceSummary(string spaceName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = ResolveSpaceRecordByName(tr, spaceName);
                if (space == null) throw new ArgumentException("Spazio non trovato: " + (spaceName ?? string.Empty));

                int total = 0;
                int viewports = 0;
                Hashtable byDxf = NewInfo();

                foreach (ObjectId id in space)
                {
                    Entity e;
                    try
                    {
                        e = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                    }
                    catch
                    {
                        continue;
                    }
                    if (e == null || e.IsErased) continue;
                    total++;
                    if (e is Viewport) viewports++;
                    string dxf = e.GetRXClass().DxfName;
                    int cur = byDxf.ContainsKey(dxf) ? Convert.ToInt32(byDxf[dxf], CultureInfo.InvariantCulture) : 0;
                    byDxf[dxf] = cur + 1;
                }

                Hashtable info = NewInfo();
                info["name"] = space.Name;
                info["total"] = total;
                info["viewports"] = viewports;
                info["by_dxf"] = byDxf;
                return info;
            }
        }

        public ObjectId AddPaperViewportToSpace(string spaceName, double centerX, double centerY, double width, double height, double viewCenterX, double viewCenterY, double viewHeight)
        {
            if (width <= 0.0 || height <= 0.0 || viewHeight <= 0.0) throw new ArgumentException("width/height/viewHeight devono essere > 0");
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = ResolveSpaceRecordByName(tr, spaceName);
                if (space == null) throw new ArgumentException("Spazio non trovato: " + (spaceName ?? string.Empty));

                // Viewports should be created in paper space. If caller passes model space, fallback.
                if (string.Equals(space.Name, BlockTableRecord.ModelSpace, StringComparison.OrdinalIgnoreCase))
                {
                    BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    space = tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForWrite) as BlockTableRecord;
                }
                else if (space.IsWriteEnabled == false)
                {
                    space = tr.GetObject(space.ObjectId, OpenMode.ForWrite) as BlockTableRecord;
                }

                Viewport vp = new Viewport();
                vp.SetDatabaseDefaults();
                vp.CenterPoint = new Point3d(centerX, centerY, 0.0);
                vp.Width = width;
                vp.Height = height;
                vp.ViewCenter = new Point2d(viewCenterX, viewCenterY);
                vp.ViewHeight = viewHeight;
                vp.ViewDirection = Vector3d.ZAxis;
                vp.On = true;
                vp.Locked = false;

                ObjectId id = space.AppendEntity(vp);
                tr.AddNewlyCreatedDBObject(vp, true);
                tr.Commit();
                return id;
            }
        }

        public ObjectId[] GetViewportIdsInSpace(string spaceName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = ResolveSpaceRecordByName(tr, spaceName);
                if (space == null) return new ObjectId[0];
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in space)
                {
                    Entity e;
                    try
                    {
                        e = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
                    }
                    catch
                    {
                        continue;
                    }
                    if (e is Viewport && !e.IsErased) ids.Add(id);
                }
                return ids.ToArray();
            }
        }

        public int EraseViewportsInSpace(string spaceName, bool keepFirst)
        {
            int erased = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = ResolveSpaceRecordByName(tr, spaceName);
                if (space == null) return 0;
                bool firstKept = false;
                foreach (ObjectId id in space)
                {
                    Viewport vp;
                    try
                    {
                        vp = tr.GetObject(id, OpenMode.ForWrite, false) as Viewport;
                    }
                    catch
                    {
                        continue;
                    }
                    if (vp == null || vp.IsErased) continue;
                    if (keepFirst && !firstKept)
                    {
                        firstKept = true;
                        continue;
                    }
                    try
                    {
                        vp.Erase(true);
                        erased++;
                    }
                    catch
                    {
                    }
                }
                tr.Commit();
            }
            return erased;
        }

        public ObjectId[] GetBlockReferenceIdsByName(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return new ObjectId[0];
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null || !bt.Has(blockName)) return new ObjectId[0];
                ObjectId defId = bt[blockName];
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord owner = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                    if (owner == null || !owner.IsLayout) continue;
                    foreach (ObjectId entId in owner)
                    {
                        BlockReference br = tr.GetObject(entId, OpenMode.ForRead, false) as BlockReference;
                        if (br == null || br.IsErased) continue;
                        if (br.BlockTableRecord == defId) ids.Add(entId);
                    }
                }
            }
            return ids.ToArray();
        }

        public Hashtable ReplaceBlockReferencesByName(string sourceBlockName, string newBlockName, bool preserveAttributeValues, bool eraseSource)
        {
            ObjectId[] refs = GetBlockReferenceIdsByName(sourceBlockName);
            ObjectId[] replaced = ReplaceBlockReferencesBatch(refs, newBlockName, preserveAttributeValues, eraseSource);
            Hashtable info = NewInfo();
            info["source_name"] = sourceBlockName;
            info["target_name"] = newBlockName;
            info["source_count"] = refs.Length;
            info["replaced_count"] = replaced.Length;
            return info;
        }

        public Hashtable ReplaceBlockReferencesByMap(Hashtable replacementMap, bool preserveAttributeValues, bool eraseSource)
        {
            int replaced = 0;
            int failed = 0;
            if (replacementMap == null || replacementMap.Count == 0)
            {
                Hashtable empty = NewInfo();
                empty["replaced"] = 0;
                empty["failed"] = 0;
                return empty;
            }

            ArrayList jobs = new ArrayList();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord owner = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                    if (owner == null || !owner.IsLayout) continue;
                    foreach (ObjectId entId in owner)
                    {
                        BlockReference br = tr.GetObject(entId, OpenMode.ForRead, false) as BlockReference;
                        if (br == null || br.IsErased) continue;
                        BlockTableRecord def = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        if (def == null) continue;
                        foreach (DictionaryEntry entry in replacementMap)
                        {
                            string src = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                            if (!string.Equals(def.Name, src, StringComparison.OrdinalIgnoreCase)) continue;
                            Hashtable item = NewInfo();
                            item["id"] = entId;
                            item["target"] = Convert.ToString(entry.Value, CultureInfo.InvariantCulture);
                            jobs.Add(item);
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                Hashtable item = jobs[i] as Hashtable;
                if (item == null || !(item["id"] is ObjectId)) continue;
                try
                {
                    ReplaceBlockReference((ObjectId)item["id"], Convert.ToString(item["target"], CultureInfo.InvariantCulture), preserveAttributeValues, eraseSource);
                    replaced++;
                }
                catch
                {
                    failed++;
                }
            }

            Hashtable info = NewInfo();
            info["jobs"] = jobs.Count;
            info["replaced"] = replaced;
            info["failed"] = failed;
            return info;
        }

        public int SyncBlockReferenceAttributesByName(string blockName, bool overwriteExistingText)
        {
            ObjectId[] refs = GetBlockReferenceIdsByName(blockName);
            return SyncBlockReferenceAttributesBatch(refs, overwriteExistingText);
        }

        public int UpdateBlockAttributesByNameMap(string blockName, Hashtable values)
        {
            ObjectId[] refs = GetBlockReferenceIdsByName(blockName);
            return UpdateBlockAttributesByMapBatch(refs, values);
        }

        public Hashtable ExportApiCompatibilityReport(string outputPath, IList requiredMethods)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("outputPath non valido");
            string full = Path.GetFullPath(outputPath);
            string dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            HashSet<string> publicMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            MethodInfo[] methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo mi = methods[i];
                if (mi.DeclaringType == typeof(object)) continue;
                publicMethods.Add(mi.Name);
            }

            int requested = 0;
            int present = 0;
            ArrayList missing = new ArrayList();
            using (StreamWriter sw = new StreamWriter(full, false))
            {
                sw.WriteLine("method,present");
                if (requiredMethods != null)
                {
                    for (int i = 0; i < requiredMethods.Count; i++)
                    {
                        string name = Convert.ToString(requiredMethods[i], CultureInfo.InvariantCulture);
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        requested++;
                        bool ok = publicMethods.Contains(name);
                        if (ok) present++;
                        else missing.Add(name);
                        sw.WriteLine(Csv(name) + "," + (ok ? "1" : "0"));
                    }
                }
            }

            Hashtable info = NewInfo();
            info["path"] = full;
            info["requested"] = requested;
            info["present"] = present;
            info["missing"] = requested - present;
            info["missing_names"] = missing;
            return info;
        }
    }
}
