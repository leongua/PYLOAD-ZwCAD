using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public bool BlockExists(string blockName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                return bt.Has(blockName);
            }
        }

        public string[] GetBlockNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                List<string> names = new List<string>();
                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!btr.IsAnonymous && !btr.IsLayout)
                    {
                        names.Add(btr.Name);
                    }
                }
                return names.ToArray();
            }
        }

        public string[] FindBlockNames(string containsText)
        {
            string[] all = GetBlockNames();
            List<string> matches = new List<string>();
            foreach (string name in all)
            {
                if (string.IsNullOrWhiteSpace(containsText) ||
                    name.IndexOf(containsText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(name);
                }
            }
            return matches.ToArray();
        }

        public Hashtable GetBlockDefinitionInfo(string blockName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                {
                    throw new ArgumentException("Blocco non trovato: " + blockName);
                }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                Hashtable info = new Hashtable();
                info["name"] = btr.Name;
                info["is_layout"] = btr.IsLayout;
                info["is_anonymous"] = btr.IsAnonymous;
                info["has_attribute_definitions"] = btr.HasAttributeDefinitions;
                int entityCount = 0;
                foreach (ObjectId _ in btr)
                {
                    entityCount++;
                }
                info["entity_count"] = entityCount;
                return info;
            }
        }

        public ObjectId InsertBlock(string blockName, double x, double y, double z)
        {
            return InsertBlockScaled(blockName, x, y, z, 1.0, 1.0, 1.0, 0.0);
        }

        public ObjectId InsertBlockScaled(string blockName, double x, double y, double z, double sx, double sy, double sz, double rotationDegrees)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                {
                    throw new ArgumentException("Blocco non trovato: " + blockName);
                }

                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                BlockReference br = new BlockReference(new Point3d(x, y, z), bt[blockName]);
                br.ScaleFactors = new Scale3d(sx, sy, sz);
                br.Rotation = DegreesToRadians(rotationDegrees);
                ObjectId id = ms.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                BlockTableRecord source = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                if (source.HasAttributeDefinitions)
                {
                    foreach (ObjectId entId in source)
                    {
                        DBObject obj = tr.GetObject(entId, OpenMode.ForRead);
                        AttributeDefinition ad = obj as AttributeDefinition;
                        if (ad != null && !ad.Constant)
                        {
                            AttributeReference ar = new AttributeReference();
                            ar.SetAttributeFromBlock(ad, br.BlockTransform);
                            ar.TextString = ad.TextString;
                            br.AttributeCollection.AppendAttribute(ar);
                            tr.AddNewlyCreatedDBObject(ar, true);
                        }
                    }
                }

                tr.Commit();
                return id;
            }
        }

        public ObjectId[] InsertBlocks(IList insertSpecs)
        {
            List<ObjectId> ids = new List<ObjectId>();
            foreach (object raw in insertSpecs)
            {
                Hashtable spec = raw as Hashtable;
                if (spec == null)
                {
                    continue;
                }

                string blockName = Convert.ToString(spec["block"]);
                double x = Convert.ToDouble(spec["x"]);
                double y = Convert.ToDouble(spec["y"]);
                double z = spec.ContainsKey("z") ? Convert.ToDouble(spec["z"]) : 0.0;
                double sx = spec.ContainsKey("sx") ? Convert.ToDouble(spec["sx"]) : 1.0;
                double sy = spec.ContainsKey("sy") ? Convert.ToDouble(spec["sy"]) : 1.0;
                double sz = spec.ContainsKey("sz") ? Convert.ToDouble(spec["sz"]) : 1.0;
                double rot = spec.ContainsKey("rotation") ? Convert.ToDouble(spec["rotation"]) : 0.0;

                ids.Add(InsertBlockScaled(blockName, x, y, z, sx, sy, sz, rot));
            }
            return ids.ToArray();
        }

        public Hashtable GetBlockAttributes(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(blockReferenceId, OpenMode.ForRead);
                Hashtable attrs = new Hashtable();
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                    attrs[ar.Tag] = ar.TextString;
                }
                return attrs;
            }
        }

        public ObjectId[] GetBlockAttributeReferenceIds(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    ids.Add(attId);
                }
                return ids.ToArray();
            }
        }

        public bool BlockReferenceHasAttributes(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                if (br.AttributeCollection.Count > 0)
                {
                    return true;
                }

                BlockTableRecord def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                if (!def.HasAttributeDefinitions)
                {
                    return false;
                }

                foreach (ObjectId entId in def)
                {
                    AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                    if (ad != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Hashtable GetConstantBlockAttributes(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                BlockTableRecord def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                Hashtable attrs = new Hashtable();
                if (!def.HasAttributeDefinitions)
                {
                    return attrs;
                }

                foreach (ObjectId entId in def)
                {
                    AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                    if (ad != null && ad.Constant)
                    {
                        attrs[ad.Tag] = ad.TextString;
                    }
                }

                return attrs;
            }
        }

        public ObjectId[] GetConstantBlockAttributeDefinitionIds(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                BlockTableRecord def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                List<ObjectId> ids = new List<ObjectId>();
                if (!def.HasAttributeDefinitions)
                {
                    return ids.ToArray();
                }

                foreach (ObjectId entId in def)
                {
                    AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                    if (ad != null && ad.Constant)
                    {
                        ids.Add(entId);
                    }
                }

                return ids.ToArray();
            }
        }

        public Hashtable GetBlockAttributeDefinitions(string blockName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                {
                    throw new ArgumentException("Blocco non trovato: " + blockName);
                }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                Hashtable attrs = new Hashtable();
                foreach (ObjectId entId in btr)
                {
                    AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                    if (ad == null)
                    {
                        continue;
                    }

                    Hashtable info = new Hashtable();
                    info["tag"] = ad.Tag;
                    info["text"] = ad.TextString;
                    info["prompt"] = ad.Prompt;
                    info["constant"] = ad.Constant;
                    info["invisible"] = ad.Invisible;
                    info["verifiable"] = ad.Verifiable;
                    info["height"] = ad.Height;
                    info["position_x"] = ad.Position.X;
                    info["position_y"] = ad.Position.Y;
                    info["position_z"] = ad.Position.Z;
                    attrs[ad.Tag] = info;
                }
                return attrs;
            }
        }

        public ObjectId[] GetBlockAttributeDefinitionIds(string blockName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                {
                    throw new ArgumentException("Blocco non trovato: " + blockName);
                }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId entId in btr)
                {
                    AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                    if (ad != null)
                    {
                        ids.Add(entId);
                    }
                }
                return ids.ToArray();
            }
        }

        public ObjectId FindBlockAttributeReferenceId(ObjectId blockReferenceId, string tag)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (ar != null && string.Equals(ar.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    {
                        return attId;
                    }
                }

                return ObjectId.Null;
            }
        }

        public void SetBlockAttribute(ObjectId blockReferenceId, string tag, string value)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(blockReferenceId, OpenMode.ForRead);
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar = (AttributeReference)tr.GetObject(attId, OpenMode.ForWrite);
                    if (string.Equals(ar.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    {
                        ar.TextString = value;
                        tr.Commit();
                        return;
                    }
                }

                throw new ArgumentException("Attributo non trovato: " + tag);
            }
        }

        public Hashtable GetBlockReferenceInfo(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                BlockTableRecord def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                Hashtable info = new Hashtable();
                info["id"] = blockReferenceId.ToString();
                info["handle"] = br.Handle.ToString();
                info["name"] = def.Name;
                info["layer"] = br.Layer;
                info["rotation"] = br.Rotation;
                info["position_x"] = br.Position.X;
                info["position_y"] = br.Position.Y;
                info["position_z"] = br.Position.Z;
                info["scale_x"] = br.ScaleFactors.X;
                info["scale_y"] = br.ScaleFactors.Y;
                info["scale_z"] = br.ScaleFactors.Z;
                info["normal_x"] = br.Normal.X;
                info["normal_y"] = br.Normal.Y;
                info["normal_z"] = br.Normal.Z;
                info["has_attributes"] = BlockReferenceHasAttributes(blockReferenceId);
                info["attribute_count"] = br.AttributeCollection.Count;
                info["insunits"] = (int)br.BlockUnit;
                info["insunits_factor"] = br.UnitFactor;
                return info;
            }
        }

        public int SetBlockAttributes(ObjectId blockReferenceId, Hashtable values)
        {
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(blockReferenceId, OpenMode.ForRead);
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar = (AttributeReference)tr.GetObject(attId, OpenMode.ForWrite);
                    foreach (DictionaryEntry item in values)
                    {
                        if (string.Equals(ar.Tag, Convert.ToString(item.Key), StringComparison.OrdinalIgnoreCase))
                        {
                            ar.TextString = Convert.ToString(item.Value);
                            changed++;
                            break;
                        }
                    }
                }
                tr.Commit();
            }
            return changed;
        }

        public int SetBlockAttributesBatch(IList blockReferenceIds, Hashtable values)
        {
            int changed = 0;
            foreach (object raw in blockReferenceIds)
            {
                if (raw is ObjectId)
                {
                    changed += SetBlockAttributes((ObjectId)raw, values);
                }
            }
            return changed;
        }

        public ObjectId AddAttributeDefinitionToBlock(
            string blockName,
            double height,
            int mode,
            string prompt,
            double x,
            double y,
            double z,
            string tag,
            string value)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                {
                    throw new ArgumentException("Blocco non trovato: " + blockName);
                }

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForWrite);
                AttributeDefinition ad = new AttributeDefinition();
                ad.Height = height;
                ad.Prompt = prompt ?? string.Empty;
                ad.Position = new Point3d(x, y, z);
                ad.Tag = tag ?? string.Empty;
                ad.TextString = value ?? string.Empty;

                int attrMode = mode;
                ad.Invisible = (attrMode & 1) == 1;
                ad.Constant = (attrMode & 2) == 2;
                ad.Verifiable = (attrMode & 4) == 4;

                ObjectId id = btr.AppendEntity(ad);
                tr.AddNewlyCreatedDBObject(ad, true);
                tr.Commit();
                return id;
            }
        }
    }
}
