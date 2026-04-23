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
                EnsureReferenceAttributes(br, tr, false);
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
            if (blockReferenceIds == null)
            {
                return 0;
            }

            foreach (object raw in blockReferenceIds)
            {
                if (!(raw is ObjectId)) continue;
                using (Transaction tr = _db.TransactionManager.StartTransaction())
                {
                    BlockReference br = tr.GetObject((ObjectId)raw, OpenMode.ForWrite) as BlockReference;
                    if (br != null)
                    {
                        changed += EnsureReferenceAttributes(br, tr, overwriteExistingText);
                        tr.Commit();
                    }
                }
            }
            return changed;
        }

        public int SyncBlockReferenceAttributes(ObjectId blockReferenceId, bool overwriteExistingText)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForWrite) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                int changed = EnsureReferenceAttributes(br, tr, overwriteExistingText);
                tr.Commit();
                return changed;
            }
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
            if (values == null)
            {
                return 0;
            }

            int changed = 0;
            foreach (DictionaryEntry de in values) changed += UpdateBlockAttributeByTagBatch(blockReferenceIds, Convert.ToString(de.Key), Convert.ToString(de.Value));
            return changed;
        }

        public ObjectId ReplaceBlockReference(ObjectId blockReferenceId, string newBlockName, bool preserveAttributeValues, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference source = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (source == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(newBlockName))
                {
                    throw new ArgumentException("Blocco non trovato: " + newBlockName);
                }

                Hashtable sourceAttributes = preserveAttributeValues ? GetBlockAttributesInternal(source, tr) : new Hashtable();
                BlockTableRecord owner = (BlockTableRecord)tr.GetObject(source.OwnerId, OpenMode.ForWrite);
                BlockReference replacement = new BlockReference(source.Position, bt[newBlockName]);
                replacement.ScaleFactors = source.ScaleFactors;
                replacement.Rotation = source.Rotation;
                replacement.Layer = source.Layer;
                replacement.Color = source.Color;
                replacement.Linetype = source.Linetype;
                replacement.LineWeight = source.LineWeight;
                replacement.LinetypeScale = source.LinetypeScale;
                replacement.Normal = source.Normal;
                ObjectId id = owner.AppendEntity(replacement);
                tr.AddNewlyCreatedDBObject(replacement, true);
                EnsureReferenceAttributes(replacement, tr, false, sourceAttributes);
                if (eraseSource) { source.UpgradeOpen(); source.Erase(); }
                tr.Commit();
                return id;
            }
        }

        public ObjectId[] ReplaceBlockReferencesBatch(IList blockReferenceIds, string newBlockName, bool preserveAttributeValues, bool eraseSource)
        {
            if (blockReferenceIds == null)
            {
                return new ObjectId[0];
            }

            ArrayList ids = new ArrayList();
            foreach (object raw in blockReferenceIds)
            {
                if (raw is ObjectId)
                {
                    ids.Add(ReplaceBlockReference((ObjectId)raw, newBlockName, preserveAttributeValues, eraseSource));
                }
            }

            ObjectId[] result = new ObjectId[ids.Count];
            ids.CopyTo(result);
            return result;
        }

        public Hashtable RenameBlockAttributeTag(string blockName, string oldTag, string newTag, bool updateReferences)
        {
            if (string.IsNullOrWhiteSpace(blockName))
            {
                throw new ArgumentException("blockName non valido");
            }

            int changedDefinitions = 0;
            int changedReferences = 0;

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null || !bt.Has(blockName))
                {
                    throw new ArgumentException("Blocco non trovato: " + blockName);
                }

                BlockTableRecord def = tr.GetObject(bt[blockName], OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId entId in def)
                {
                    AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForWrite) as AttributeDefinition;
                    if (ad != null && string.Equals(ad.Tag, oldTag, StringComparison.OrdinalIgnoreCase))
                    {
                        ad.Tag = newTag;
                        changedDefinitions++;
                    }
                }

                if (updateReferences)
                {
                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord owner = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                        if (owner == null || owner.IsAnonymous)
                        {
                            continue;
                        }

                        foreach (ObjectId entId in owner)
                        {
                            BlockReference br = tr.GetObject(entId, OpenMode.ForRead) as BlockReference;
                            if (br == null || br.BlockTableRecord != def.ObjectId)
                            {
                                continue;
                            }

                            foreach (ObjectId attId in br.AttributeCollection)
                            {
                                AttributeReference ar = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                                if (ar != null && string.Equals(ar.Tag, oldTag, StringComparison.OrdinalIgnoreCase))
                                {
                                    ar.Tag = newTag;
                                    changedReferences++;
                                }
                            }
                        }
                    }
                }

                tr.Commit();
            }

            Hashtable info = NewInfo();
            info["changed_definitions"] = changedDefinitions;
            info["changed_references"] = changedReferences;
            return info;
        }

        public ObjectId[] ExplodeBlockReferenceEx(ObjectId blockReferenceId, bool eraseSource, bool copySourceProperties)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                DBObjectCollection exploded = new DBObjectCollection();
                br.Explode(exploded);
                BlockTableRecord owner = tr.GetObject(br.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                ArrayList ids = new ArrayList();

                foreach (DBObject dbo in exploded)
                {
                    Entity child = dbo as Entity;
                    if (child == null)
                    {
                        continue;
                    }

                    if (copySourceProperties)
                    {
                        child.Layer = br.Layer;
                        child.Color = br.Color;
                        child.Linetype = br.Linetype;
                        child.LineWeight = br.LineWeight;
                        child.LinetypeScale = br.LinetypeScale;
                    }

                    ObjectId id = owner.AppendEntity(child);
                    tr.AddNewlyCreatedDBObject(child, true);
                    ids.Add(id);
                }

                if (eraseSource)
                {
                    BlockReference writable = tr.GetObject(blockReferenceId, OpenMode.ForWrite) as BlockReference;
                    if (writable != null && !writable.IsErased)
                    {
                        writable.Erase(true);
                    }
                }

                tr.Commit();
                ObjectId[] result = new ObjectId[ids.Count];
                ids.CopyTo(result);
                return result;
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

        private static int EnsureReferenceAttributes(BlockReference br, Transaction tr, bool overwriteExistingText)
        {
            return EnsureReferenceAttributes(br, tr, overwriteExistingText, null);
        }

        private static int EnsureReferenceAttributes(BlockReference br, Transaction tr, bool overwriteExistingText, Hashtable sourceValues)
        {
            int created = 0;
            BlockTableRecord def = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
            foreach (ObjectId id in def)
            {
                AttributeDefinition ad = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
                if (ad == null || ad.Constant) continue;
                AttributeReference existing = null;
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar0 = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (ar0 != null && string.Equals(ar0.Tag, ad.Tag, StringComparison.OrdinalIgnoreCase))
                    {
                        existing = ar0;
                        break;
                    }
                }

                if (existing != null)
                {
                    if (overwriteExistingText)
                    {
                        AttributeReference writable = tr.GetObject(existing.ObjectId, OpenMode.ForWrite) as AttributeReference;
                        if (writable != null)
                        {
                            writable.SetAttributeFromBlock(ad, br.BlockTransform);
                            writable.TextString = ad.TextString;
                            created++;
                        }
                    }
                    continue;
                }

                AttributeReference ar = new AttributeReference();
                ar.SetAttributeFromBlock(ad, br.BlockTransform);
                ar.Position = ad.Position.TransformBy(br.BlockTransform);
                string sourceText = sourceValues != null && sourceValues.ContainsKey(ad.Tag) ? Convert.ToString(sourceValues[ad.Tag]) : null;
                ar.TextString = sourceText ?? ad.TextString;
                br.AttributeCollection.AppendAttribute(ar);
                tr.AddNewlyCreatedDBObject(ar, true);
                created++;
            }
            return created;
        }

        private static Hashtable GetBlockAttributesInternal(BlockReference br, Transaction tr)
        {
            Hashtable attrs = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (ObjectId attId in br.AttributeCollection)
            {
                AttributeReference ar = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                if (ar != null)
                {
                    attrs[ar.Tag] = ar.TextString;
                }
            }

            return attrs;
        }
    }
}
