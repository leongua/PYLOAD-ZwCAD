using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public int SyncBlockReferenceAttributes(ObjectId blockReferenceId, bool overwriteExistingText)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForWrite) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                BlockTableRecord def = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (def == null || !def.HasAttributeDefinitions)
                {
                    return 0;
                }

                Dictionary<string, AttributeReference> refs = new Dictionary<string, AttributeReference>(StringComparer.OrdinalIgnoreCase);
                foreach (ObjectId attId in br.AttributeCollection)
                {
                    AttributeReference ar = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                    if (ar != null)
                    {
                        refs[ar.Tag] = ar;
                    }
                }

                int changed = 0;
                foreach (ObjectId entId in def)
                {
                    AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                    if (ad == null || ad.Constant)
                    {
                        continue;
                    }

                    AttributeReference existing;
                    if (refs.TryGetValue(ad.Tag, out existing))
                    {
                        if (overwriteExistingText)
                        {
                            existing.SetAttributeFromBlock(ad, br.BlockTransform);
                            existing.TextString = ad.TextString;
                        }
                        else
                        {
                            string currentText = existing.TextString;
                            existing.SetAttributeFromBlock(ad, br.BlockTransform);
                            existing.TextString = currentText;
                        }
                        changed++;
                        continue;
                    }

                    AttributeReference created = new AttributeReference();
                    created.SetAttributeFromBlock(ad, br.BlockTransform);
                    created.TextString = ad.TextString;
                    br.AttributeCollection.AppendAttribute(created);
                    tr.AddNewlyCreatedDBObject(created, true);
                    changed++;
                }

                tr.Commit();
                return changed;
            }
        }

        public int SyncBlockReferenceAttributesBatch(IList blockReferenceIds, bool overwriteExistingText)
        {
            int changed = 0;
            foreach (object raw in blockReferenceIds)
            {
                if (raw is ObjectId)
                {
                    changed += SyncBlockReferenceAttributes((ObjectId)raw, overwriteExistingText);
                }
            }
            return changed;
        }

        public int UpdateBlockAttributeByTagBatch(IList blockReferenceIds, string tag, string value)
        {
            int changed = 0;
            foreach (object raw in blockReferenceIds)
            {
                if (!(raw is ObjectId))
                {
                    continue;
                }

                ObjectId attId = FindBlockAttributeReferenceId((ObjectId)raw, tag);
                if (!attId.IsNull)
                {
                    SetAttributeText(attId, value);
                    changed++;
                }
            }
            return changed;
        }

        public int UpdateBlockAttributesByMapBatch(IList blockReferenceIds, Hashtable values)
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

        public Hashtable RenameBlockAttributeTag(string blockName, string oldTag, string newTag, bool updateReferences)
        {
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

            Hashtable info = new Hashtable();
            info["changed_definitions"] = changedDefinitions;
            info["changed_references"] = changedReferences;
            return info;
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

                BlockTable bt = tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt == null || !bt.Has(newBlockName))
                {
                    throw new ArgumentException("Blocco non trovato: " + newBlockName);
                }

                Hashtable oldValues = preserveAttributeValues ? GetBlockAttributes(blockReferenceId) : new Hashtable();
                BlockTableRecord owner = tr.GetObject(source.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                BlockReference created = new BlockReference(source.Position, bt[newBlockName]);
                created.ScaleFactors = source.ScaleFactors;
                created.Rotation = source.Rotation;
                created.Layer = source.Layer;
                created.Color = source.Color;
                created.Linetype = source.Linetype;
                created.LineWeight = source.LineWeight;
                created.LinetypeScale = source.LinetypeScale;
                created.Normal = source.Normal;

                ObjectId newId = owner.AppendEntity(created);
                tr.AddNewlyCreatedDBObject(created, true);

                BlockTableRecord newDef = tr.GetObject(bt[newBlockName], OpenMode.ForRead) as BlockTableRecord;
                if (newDef != null && newDef.HasAttributeDefinitions)
                {
                    foreach (ObjectId entId in newDef)
                    {
                        AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                        if (ad == null || ad.Constant)
                        {
                            continue;
                        }

                        AttributeReference ar = new AttributeReference();
                        ar.SetAttributeFromBlock(ad, created.BlockTransform);
                        ar.TextString = oldValues.ContainsKey(ad.Tag) ? Convert.ToString(oldValues[ad.Tag]) : ad.TextString;
                        created.AttributeCollection.AppendAttribute(ar);
                        tr.AddNewlyCreatedDBObject(ar, true);
                    }
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
                return newId;
            }
        }

        public ObjectId[] ReplaceBlockReferencesBatch(IList blockReferenceIds, string newBlockName, bool preserveAttributeValues, bool eraseSource)
        {
            List<ObjectId> ids = new List<ObjectId>();
            foreach (object raw in blockReferenceIds)
            {
                if (raw is ObjectId)
                {
                    ids.Add(ReplaceBlockReference((ObjectId)raw, newBlockName, preserveAttributeValues, eraseSource));
                }
            }
            return ids.ToArray();
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
                List<ObjectId> ids = new List<ObjectId>();

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
                return ids.ToArray();
            }
        }
    }
}
