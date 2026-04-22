using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public string[] GetBlockNames()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                ArrayList names = new ArrayList();
                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!btr.IsAnonymous && !btr.IsLayout) names.Add(btr.Name);
                }
                string[] result = new string[names.Count];
                names.CopyTo(result);
                return result;
            }
        }

        public Hashtable GetBlockDefinitionInfo(string blockName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                Hashtable info = NewInfo();
                info["name"] = btr.Name;
                info["has_attribute_definitions"] = HasAttributeDefinitions(btr, tr);
                return info;
            }
        }

        public ObjectId InsertBlock(string blockName, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(_db);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);
                BlockReference br = new BlockReference(new Point3d(x, y, z), bt[blockName]);
                ObjectId id = ms.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);
                EnsureReferenceAttributes(br, tr);
                tr.Commit();
                return id;
            }
        }

        public Hashtable GetBlockAttributeDefinitions(string blockName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForRead);
                Hashtable attrs = NewInfo();
                foreach (ObjectId entId in btr)
                {
                    AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                    if (ad == null) continue;
                    Hashtable info = NewInfo();
                    info["tag"] = ad.Tag;
                    info["prompt"] = ad.Prompt;
                    info["default"] = ad.TextString;
                    attrs[ad.Tag] = info;
                }
                return attrs;
            }
        }

        public int SyncBlockReferenceAttributesBatch(IList blockReferenceIds, bool overwriteExistingText)
        {
            int changed = 0;
            foreach (object raw in blockReferenceIds)
            {
                if (!(raw is ObjectId)) continue;
                using (Transaction tr = _db.TransactionManager.StartTransaction())
                {
                    BlockReference br = tr.GetObject((ObjectId)raw, OpenMode.ForWrite) as BlockReference;
                    if (br != null)
                    {
                        changed += EnsureReferenceAttributes(br, tr);
                        tr.Commit();
                    }
                }
            }
            return changed;
        }

        public int UpdateBlockAttributeByTagBatch(IList blockReferenceIds, string tag, string value)
        {
            int changed = 0;
            foreach (object raw in blockReferenceIds)
            {
                if (!(raw is ObjectId)) continue;
                using (Transaction tr = _db.TransactionManager.StartTransaction())
                {
                    BlockReference br = tr.GetObject((ObjectId)raw, OpenMode.ForRead) as BlockReference;
                    if (br == null) continue;
                    foreach (ObjectId attId in br.AttributeCollection)
                    {
                        AttributeReference ar = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                        if (ar != null && string.Equals(ar.Tag, tag, StringComparison.OrdinalIgnoreCase))
                        {
                            ar.TextString = value ?? string.Empty;
                            changed++;
                        }
                    }
                    tr.Commit();
                }
            }
            return changed;
        }

        public int UpdateBlockAttributesByMapBatch(IList blockReferenceIds, Hashtable values)
        {
            int changed = 0;
            foreach (DictionaryEntry de in values) changed += UpdateBlockAttributeByTagBatch(blockReferenceIds, Convert.ToString(de.Key), Convert.ToString(de.Value));
            return changed;
        }

        public ObjectId ReplaceBlockReference(ObjectId blockReferenceId, string newBlockName, bool preserveAttributeValues, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference source = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord owner = (BlockTableRecord)tr.GetObject(source.OwnerId, OpenMode.ForWrite);
                BlockReference replacement = new BlockReference(source.Position, bt[newBlockName]);
                replacement.ScaleFactors = source.ScaleFactors;
                replacement.Rotation = source.Rotation;
                replacement.Layer = source.Layer;
                replacement.Color = source.Color;
                ObjectId id = owner.AppendEntity(replacement);
                tr.AddNewlyCreatedDBObject(replacement, true);
                EnsureReferenceAttributes(replacement, tr);
                if (eraseSource) { source.UpgradeOpen(); source.Erase(); }
                tr.Commit();
                return id;
            }
        }

        public Hashtable GetBlockReferenceInfo(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                BlockTableRecord def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                Hashtable info = NewInfo();
                info["name"] = def.Name;
                info["handle"] = br.Handle.ToString();
                return info;
            }
        }

        private static bool HasAttributeDefinitions(BlockTableRecord btr, Transaction tr)
        {
            foreach (ObjectId id in btr)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is AttributeDefinition) return true;
            }
            return false;
        }

        private static int EnsureReferenceAttributes(BlockReference br, Transaction tr)
        {
            int created = 0;
            BlockTableRecord def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
            foreach (ObjectId id in def)
            {
                AttributeDefinition ad = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
                if (ad == null || ad.Constant) continue;
                bool exists = false;
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar0 = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (ar0 != null && string.Equals(ar0.Tag, ad.Tag, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
                }
                if (exists) continue;
                AttributeReference ar = new AttributeReference();
                ar.SetAttributeFromBlock(ad, br.BlockTransform);
                ar.Position = ad.Position.TransformBy(br.BlockTransform);
                ar.TextString = ad.TextString;
                br.AttributeCollection.AppendAttribute(ar);
                tr.AddNewlyCreatedDBObject(ar, true);
                created++;
            }
            return created;
        }
    }
}
