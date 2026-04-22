using System;
using System.Collections;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ObjectId EntMake(IList dxfPairs)
        {
            Hashtable map = NormalizeDxfPairs(dxfPairs);
            string dxfName = GetRequiredDxfName(map);

            Entity entity;
            switch (dxfName.ToUpperInvariant())
            {
                case "LINE":
                    entity = BuildLineFromDxf(map);
                    break;
                case "RAY":
                    entity = BuildRayFromDxf(map);
                    break;
                case "CIRCLE":
                    entity = BuildCircleFromDxf(map);
                    break;
                case "ARC":
                    entity = BuildArcFromDxf(map);
                    break;
                case "POINT":
                    entity = BuildPointFromDxf(map);
                    break;
                case "TRACE":
                    entity = BuildTraceFromDxf(map);
                    break;
                case "SOLID":
                    entity = BuildSolidFromDxf(map);
                    break;
                case "TEXT":
                    entity = BuildTextFromDxf(map);
                    break;
                case "3DFACE":
                case "FACE":
                    entity = Build3DFaceFromDxf(map);
                    break;
                case "ATTDEF":
                    entity = BuildAttributeDefinitionFromDxf(map);
                    break;
                case "ATTRIB":
                    return BuildAttributeReferenceFromDxf(map);
                case "MTEXT":
                    entity = BuildMTextFromDxf(map);
                    break;
                case "ELLIPSE":
                    entity = BuildEllipseFromDxf(map);
                    break;
                case "HATCH":
                    return BuildHatchFromDxf(map);
                case "LEADER":
                    return BuildLeaderFromDxf(map);
                case "INSERT":
                    return BuildInsertFromDxf(map);
                case "LWPOLYLINE":
                case "POLYLINE":
                    entity = BuildPolylineFromDxf(map);
                    break;
                default:
                    throw new NotSupportedException("DXF name non supportato da EntMake: " + dxfName);
            }

            ApplyCommonDxfToNewEntity(entity, map);
            return AddEntity(entity);
        }

        public ObjectId[] EntMakeMany(IList entitiesDxfPairs)
        {
            List<ObjectId> ids = new List<ObjectId>();
            foreach (object raw in entitiesDxfPairs)
            {
                IList pairs = raw as IList;
                if (pairs == null)
                {
                    continue;
                }
                ids.Add(EntMake(pairs));
            }
            return ids.ToArray();
        }

        public ObjectId EntLast()
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                ObjectId last = ObjectId.Null;
                foreach (ObjectId id in ms)
                {
                    last = id;
                }
                return last;
            }
        }

        public ObjectId GetEntityByHandle(string handle)
        {
            if (string.IsNullOrWhiteSpace(handle))
            {
                return ObjectId.Null;
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Handle h = new Handle(Convert.ToInt64(handle, 16));
                if (_db.TryGetObjectId(h, out ObjectId id))
                {
                    return id;
                }
                return ObjectId.Null;
            }
        }

        public ObjectId EntNext(ObjectId entityId)
        {
            ObjectId[] siblings = GetOwnerEntityIds(entityId);
            for (int i = 0; i < siblings.Length - 1; i++)
            {
                if (siblings[i] == entityId)
                {
                    return siblings[i + 1];
                }
            }
            return ObjectId.Null;
        }

        public ObjectId EntPrevious(ObjectId entityId)
        {
            ObjectId[] siblings = GetOwnerEntityIds(entityId);
            for (int i = 1; i < siblings.Length; i++)
            {
                if (siblings[i] == entityId)
                {
                    return siblings[i - 1];
                }
            }
            return ObjectId.Null;
        }

        public ObjectId[] GetOwnerEntityIds(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                if (dbo == null)
                {
                    throw new ArgumentException("DBObject non trovato");
                }

                List<ObjectId> ids = new List<ObjectId>();

                if (!dbo.OwnerId.IsNull)
                {
                    DBObject owner = tr.GetObject(dbo.OwnerId, OpenMode.ForRead);
                    BlockTableRecord btr = owner as BlockTableRecord;
                    if (btr != null)
                    {
                        foreach (ObjectId id in btr)
                        {
                            ids.Add(id);
                        }
                        return ids.ToArray();
                    }

                    BlockReference br = owner as BlockReference;
                    if (br != null)
                    {
                        foreach (ObjectId id in br.AttributeCollection)
                        {
                            ids.Add(id);
                        }
                        return ids.ToArray();
                    }
                }

                ids.Add(entityId);
                return ids.ToArray();
            }
        }

        public ObjectId EntFirstAttribute(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                foreach (ObjectId id in br.AttributeCollection)
                {
                    return id;
                }

                return ObjectId.Null;
            }
        }

        public ObjectId GetAttributeOwnerBlockReference(ObjectId attributeId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(attributeId, OpenMode.ForRead);
                AttributeReference ar = dbo as AttributeReference;
                if (ar == null)
                {
                    throw new ArgumentException("L'entita non e un AttributeReference");
                }

                DBObject owner = tr.GetObject(ar.OwnerId, OpenMode.ForRead);
                BlockReference br = owner as BlockReference;
                if (br != null)
                {
                    return br.ObjectId;
                }

                return ObjectId.Null;
            }
        }

        public void EntDel(ObjectId entityId)
        {
            EraseEntity(entityId);
        }

        public ObjectId EntCopy(ObjectId entityId, double dx, double dy, double dz)
        {
            return CopyEntity(entityId, dx, dy, dz);
        }

        public ArrayList EntGet(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                return BuildDxfPairs(entity);
            }
        }

        public Hashtable EntGetMap(ObjectId entityId)
        {
            return NormalizeDxfPairs(EntGet(entityId));
        }

        public object GetEntityDxfValue(ObjectId entityId, int code)
        {
            Hashtable map = EntGetMap(entityId);
            return map.ContainsKey(code) ? map[code] : null;
        }

        public bool HasEntityDxfCode(ObjectId entityId, int code)
        {
            return EntGetMap(entityId).ContainsKey(code);
        }

        public int[] GetEntityDxfCodes(ObjectId entityId)
        {
            Hashtable map = EntGetMap(entityId);
            List<int> codes = new List<int>();
            foreach (DictionaryEntry entry in map)
            {
                codes.Add(Convert.ToInt32(entry.Key));
            }
            codes.Sort();
            return codes.ToArray();
        }

        public void SetEntityDxfValue(ObjectId entityId, int code, object value)
        {
            ArrayList pairs = new ArrayList();
            AddDxfPair(pairs, code, value);
            EntMod(entityId, pairs);
        }

        public ObjectId EntParent(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                if (dbo == null || dbo.OwnerId.IsNull)
                {
                    return ObjectId.Null;
                }

                DBObject owner = tr.GetObject(dbo.OwnerId, OpenMode.ForRead);
                BlockReference br = owner as BlockReference;
                if (br != null)
                {
                    return br.ObjectId;
                }

                return dbo.OwnerId;
            }
        }

        public ObjectId[] EntChildren(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                List<ObjectId> ids = new List<ObjectId>();

                BlockReference br = dbo as BlockReference;
                if (br != null)
                {
                    foreach (ObjectId id in br.AttributeCollection)
                    {
                        ids.Add(id);
                    }
                    return ids.ToArray();
                }

                BlockTableRecord btr = dbo as BlockTableRecord;
                if (btr != null)
                {
                    foreach (ObjectId id in btr)
                    {
                        ids.Add(id);
                    }
                    return ids.ToArray();
                }

                return ids.ToArray();
            }
        }

        public ObjectId[] EntAncestors(ObjectId entityId)
        {
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                while (dbo != null && !dbo.OwnerId.IsNull)
                {
                    ids.Add(dbo.OwnerId);
                    dbo = tr.GetObject(dbo.OwnerId, OpenMode.ForRead);
                }
            }
            return ids.ToArray();
        }

        public ObjectId[] EntDescendants(ObjectId entityId, int maxDepth)
        {
            List<ObjectId> ids = new List<ObjectId>();
            if (maxDepth < 1)
            {
                return ids.ToArray();
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                CollectDescendants(tr, entityId, 1, maxDepth, ids);
            }
            return ids.ToArray();
        }

        public Hashtable EntOwnerInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                Hashtable info = new Hashtable();
                info["entity_id"] = entityId.ToString();
                info["owner_id"] = dbo.OwnerId.ToString();
                info["owner_handle"] = string.Empty;
                info["owner_type"] = string.Empty;

                if (!dbo.OwnerId.IsNull)
                {
                    DBObject owner = tr.GetObject(dbo.OwnerId, OpenMode.ForRead);
                    info["owner_type"] = owner.GetType().Name;
                    if (owner is DBObject)
                    {
                        info["owner_handle"] = owner.Handle.ToString();
                    }
                }

                return info;
            }
        }

        public ObjectId EntRootOwner(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                ObjectId last = ObjectId.Null;
                while (dbo != null && !dbo.OwnerId.IsNull)
                {
                    last = dbo.OwnerId;
                    dbo = tr.GetObject(dbo.OwnerId, OpenMode.ForRead);
                }
                return last;
            }
        }

        public ArrayList EntOwnerChainInfo(ObjectId entityId)
        {
            ArrayList items = new ArrayList();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                while (dbo != null && !dbo.OwnerId.IsNull)
                {
                    DBObject owner = tr.GetObject(dbo.OwnerId, OpenMode.ForRead);
                    Hashtable info = new Hashtable();
                    info["id"] = owner.ObjectId.ToString();
                    info["handle"] = owner.Handle.ToString();
                    info["type"] = owner.GetType().Name;
                    items.Add(info);
                    dbo = owner;
                }
            }
            return items;
        }

        public ObjectId EntOwningBlockReference(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
                while (dbo != null && !dbo.OwnerId.IsNull)
                {
                    DBObject owner = tr.GetObject(dbo.OwnerId, OpenMode.ForRead);
                    BlockReference br = owner as BlockReference;
                    if (br != null)
                    {
                        return br.ObjectId;
                    }
                    dbo = owner;
                }
                return ObjectId.Null;
            }
        }

        public ObjectId[] EntAttributeSiblings(ObjectId attributeId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                AttributeReference ar = tr.GetObject(attributeId, OpenMode.ForRead) as AttributeReference;
                if (ar == null)
                {
                    throw new ArgumentException("L'entita non e un AttributeReference");
                }

                BlockReference br = tr.GetObject(ar.OwnerId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    return new ObjectId[0];
                }

                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in br.AttributeCollection)
                {
                    ids.Add(id);
                }
                return ids.ToArray();
            }
        }

        public ObjectId EntNextAttribute(ObjectId attributeId)
        {
            ObjectId[] ids = EntAttributeSiblings(attributeId);
            for (int i = 0; i < ids.Length - 1; i++)
            {
                if (ids[i] == attributeId)
                {
                    return ids[i + 1];
                }
            }
            return ObjectId.Null;
        }

        public ObjectId EntPreviousAttribute(ObjectId attributeId)
        {
            ObjectId[] ids = EntAttributeSiblings(attributeId);
            for (int i = 1; i < ids.Length; i++)
            {
                if (ids[i] == attributeId)
                {
                    return ids[i - 1];
                }
            }
            return ObjectId.Null;
        }

        public Hashtable GetInsertAttributeTraversalInfo(ObjectId blockReferenceId)
        {
            Hashtable info = new Hashtable();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                BlockTableRecord def = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                int refCount = 0;
                foreach (ObjectId _ in br.AttributeCollection)
                {
                    refCount++;
                }

                int defCount = 0;
                int constCount = 0;
                if (def != null)
                {
                    foreach (ObjectId entId in def)
                    {
                        AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                        if (ad == null)
                        {
                            continue;
                        }
                        defCount++;
                        if (ad.Constant)
                        {
                            constCount++;
                        }
                    }
                }

                info["block_name"] = br.Name;
                info["block_reference_id"] = blockReferenceId.ToString();
                info["definition_id"] = br.BlockTableRecord.ToString();
                info["attribute_reference_count"] = refCount;
                info["attribute_definition_count"] = defCount;
                info["constant_attribute_definition_count"] = constCount;
                info["has_attribute_references"] = refCount > 0;
                info["has_attribute_definitions"] = defCount > 0;
                return info;
            }
        }

        public ObjectId[] GetBlockDefinitionEntityIds(ObjectId blockReferenceId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(blockReferenceId, OpenMode.ForRead) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("L'entita non e un BlockReference");
                }

                BlockTableRecord def = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                List<ObjectId> ids = new List<ObjectId>();
                foreach (ObjectId id in def)
                {
                    ids.Add(id);
                }
                return ids.ToArray();
            }
        }

        public void EntMod(ObjectId entityId, IList dxfPairs)
        {
            Hashtable map = NormalizeDxfPairs(dxfPairs);
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                ApplyCommonDxfToExistingEntity(entity, map);

                Line line = entity as Line;
                if (line != null)
                {
                    if (HasPoint(map, 10))
                    {
                        line.StartPoint = GetPoint(map, 10);
                    }
                    if (HasPoint(map, 11))
                    {
                        line.EndPoint = GetPoint(map, 11);
                    }
                    tr.Commit();
                    return;
                }

                Ray ray = entity as Ray;
                if (ray != null)
                {
                    if (HasPoint(map, 10))
                    {
                        ray.BasePoint = GetPoint(map, 10);
                    }
                    if (HasVector(map, 11))
                    {
                        Vector3d dir = GetVector(map, 11);
                        if (dir.Length > 0.0)
                        {
                            ray.UnitDir = dir.GetNormal();
                        }
                    }
                    tr.Commit();
                    return;
                }

                Circle circle = entity as Circle;
                if (circle != null)
                {
                    if (HasPoint(map, 10))
                    {
                        circle.Center = GetPoint(map, 10);
                    }
                    if (map.ContainsKey(40))
                    {
                        circle.Radius = Convert.ToDouble(map[40]);
                    }
                    tr.Commit();
                    return;
                }

                Arc arc = entity as Arc;
                if (arc != null)
                {
                    if (HasPoint(map, 10))
                    {
                        arc.Center = GetPoint(map, 10);
                    }
                    if (map.ContainsKey(40))
                    {
                        arc.Radius = Convert.ToDouble(map[40]);
                    }
                    if (map.ContainsKey(50))
                    {
                        arc.StartAngle = DegreesToRadians(Convert.ToDouble(map[50]));
                    }
                    if (map.ContainsKey(51))
                    {
                        arc.EndAngle = DegreesToRadians(Convert.ToDouble(map[51]));
                    }
                    tr.Commit();
                    return;
                }

                DBPoint point = entity as DBPoint;
                if (point != null)
                {
                    if (HasPoint(map, 10))
                    {
                        point.Position = GetPoint(map, 10);
                    }
                    tr.Commit();
                    return;
                }

                Face face = entity as Face;
                if (face != null)
                {
                    if (HasPoint(map, 10)) face.SetVertexAt(0, GetPoint(map, 10));
                    if (HasPoint(map, 11)) face.SetVertexAt(1, GetPoint(map, 11));
                    if (HasPoint(map, 12)) face.SetVertexAt(2, GetPoint(map, 12));
                    if (HasPoint(map, 13)) face.SetVertexAt(3, GetPoint(map, 13));
                    tr.Commit();
                    return;
                }

                Trace trace = entity as Trace;
                if (trace != null)
                {
                    if (HasPoint(map, 10)) trace.SetPointAt(0, GetPoint(map, 10));
                    if (HasPoint(map, 11)) trace.SetPointAt(1, GetPoint(map, 11));
                    if (HasPoint(map, 12)) trace.SetPointAt(2, GetPoint(map, 12));
                    if (HasPoint(map, 13)) trace.SetPointAt(3, GetPoint(map, 13));
                    tr.Commit();
                    return;
                }

                Solid solid = entity as Solid;
                if (solid != null)
                {
                    if (HasPoint(map, 10)) solid.SetPointAt(0, GetPoint(map, 10));
                    if (HasPoint(map, 11)) solid.SetPointAt(1, GetPoint(map, 11));
                    if (HasPoint(map, 12)) solid.SetPointAt(2, GetPoint(map, 12));
                    if (HasPoint(map, 13)) solid.SetPointAt(3, GetPoint(map, 13));
                    tr.Commit();
                    return;
                }

                AttributeDefinition def = entity as AttributeDefinition;
                if (def != null)
                {
                    if (HasPoint(map, 10))
                    {
                        def.Position = GetPoint(map, 10);
                    }
                    if (map.ContainsKey(1))
                    {
                        def.TextString = Convert.ToString(map[1]);
                    }
                    if (map.ContainsKey(2))
                    {
                        def.Tag = Convert.ToString(map[2]);
                    }
                    if (map.ContainsKey(3))
                    {
                        def.Prompt = Convert.ToString(map[3]);
                    }
                    if (map.ContainsKey(40))
                    {
                        def.Height = Convert.ToDouble(map[40]);
                    }
                    if (map.ContainsKey(50))
                    {
                        def.Rotation = DegreesToRadians(Convert.ToDouble(map[50]));
                    }
                    ApplyTextLikeDxf(def, map);
                    if (map.ContainsKey(70))
                    {
                        int flags = Convert.ToInt32(map[70]);
                        def.Invisible = (flags & 1) == 1;
                        def.Constant = (flags & 2) == 2;
                        def.Verifiable = (flags & 4) == 4;
                    }
                    tr.Commit();
                    return;
                }

                AttributeReference attr = entity as AttributeReference;
                if (attr != null)
                {
                    if (HasPoint(map, 10))
                    {
                        attr.Position = GetPoint(map, 10);
                    }
                    if (map.ContainsKey(1))
                    {
                        attr.TextString = Convert.ToString(map[1]);
                    }
                    if (map.ContainsKey(2))
                    {
                        attr.Tag = Convert.ToString(map[2]);
                    }
                    if (map.ContainsKey(40))
                    {
                        attr.Height = Convert.ToDouble(map[40]);
                    }
                    if (map.ContainsKey(50))
                    {
                        attr.Rotation = DegreesToRadians(Convert.ToDouble(map[50]));
                    }
                    ApplyTextLikeDxf(attr, map);
                    if (map.ContainsKey(70))
                    {
                        int flags = Convert.ToInt32(map[70]);
                        attr.Invisible = (flags & 1) == 1;
                    }
                    ApplyAttributeReferenceStyleDxf(attr, map);
                    tr.Commit();
                    return;
                }

                DBText text = entity as DBText;
                if (text != null)
                {
                    if (HasPoint(map, 10))
                    {
                        text.Position = GetPoint(map, 10);
                    }
                    if (map.ContainsKey(1))
                    {
                        text.TextString = Convert.ToString(map[1]);
                    }
                    if (map.ContainsKey(40))
                    {
                        text.Height = Convert.ToDouble(map[40]);
                    }
                    if (map.ContainsKey(50))
                    {
                        text.Rotation = DegreesToRadians(Convert.ToDouble(map[50]));
                    }
                    ApplyTextLikeDxf(text, map);
                    tr.Commit();
                    return;
                }

                MText mtext = entity as MText;
                if (mtext != null)
                {
                    if (HasPoint(map, 10))
                    {
                        mtext.Location = GetPoint(map, 10);
                    }
                    if (map.ContainsKey(1))
                    {
                        mtext.Contents = Convert.ToString(map[1]);
                    }
                    if (map.ContainsKey(40))
                    {
                        mtext.TextHeight = Convert.ToDouble(map[40]);
                    }
                    if (map.ContainsKey(41))
                    {
                        mtext.Width = Convert.ToDouble(map[41]);
                    }
                    if (map.ContainsKey(50))
                    {
                        mtext.Rotation = DegreesToRadians(Convert.ToDouble(map[50]));
                    }
                    ApplyMTextDxf(mtext, map);
                    tr.Commit();
                    return;
                }

                Ellipse ellipse = entity as Ellipse;
                if (ellipse != null)
                {
                    if (HasPoint(map, 10))
                    {
                        ellipse.Center = GetPoint(map, 10);
                    }
                    if (map.ContainsKey(40))
                    {
                        ellipse.RadiusRatio = Convert.ToDouble(map[40]);
                    }
                    if (map.ContainsKey(41))
                    {
                        ellipse.StartAngle = Convert.ToDouble(map[41]);
                    }
                    if (map.ContainsKey(42))
                    {
                        ellipse.EndAngle = Convert.ToDouble(map[42]);
                    }
                    tr.Commit();
                    return;
                }

                BlockReference br = entity as BlockReference;
                if (br != null)
                {
                    if (HasPoint(map, 10))
                    {
                        br.Position = GetPoint(map, 10);
                    }
                    if (map.ContainsKey(41) || map.ContainsKey(42) || map.ContainsKey(43))
                    {
                        br.ScaleFactors = new Scale3d(
                            map.ContainsKey(41) ? Convert.ToDouble(map[41]) : br.ScaleFactors.X,
                            map.ContainsKey(42) ? Convert.ToDouble(map[42]) : br.ScaleFactors.Y,
                            map.ContainsKey(43) ? Convert.ToDouble(map[43]) : br.ScaleFactors.Z);
                    }
                    if (map.ContainsKey(50))
                    {
                        br.Rotation = DegreesToRadians(Convert.ToDouble(map[50]));
                    }
                    tr.Commit();
                    return;
                }

                Hatch hatch = entity as Hatch;
                if (hatch != null)
                {
                    HatchPatternType patternType = GetHatchPatternTypeFromDxf(map, hatch.PatternType);
                    string patternName = map.ContainsKey(2) ? Convert.ToString(map[2]) : hatch.PatternName;
                    if (map.ContainsKey(2) || map.ContainsKey(76))
                    {
                        hatch.SetHatchPattern(patternType, string.IsNullOrWhiteSpace(patternName) ? "SOLID" : patternName);
                    }
                    if (map.ContainsKey(41))
                    {
                        hatch.PatternScale = Convert.ToDouble(map[41]);
                    }
                    if (map.ContainsKey(52))
                    {
                        hatch.PatternAngle = DegreesToRadians(Convert.ToDouble(map[52]));
                    }
                    if (map.ContainsKey(70))
                    {
                        hatch.Associative = Convert.ToInt32(map[70]) != 0;
                    }
                    ApplyHatchOptionalDxf(hatch, map);
                    hatch.EvaluateHatch(true);
                    tr.Commit();
                    return;
                }

                Leader leader = entity as Leader;
                if (leader != null)
                {
                    if (HasArrayValues(map, 10) && HasArrayValues(map, 20))
                    {
                        ReplaceLeaderVertices(leader, map);
                    }
                    if (map.ContainsKey(71))
                    {
                        TrySetRuntimePropertyValue(leader, "HasArrowHead", Convert.ToInt32(map[71]) != 0);
                    }
                    if (map.ContainsKey(340))
                    {
                        ObjectId annotationId = ResolveObjectId(map[340]);
                        if (!annotationId.IsNull)
                        {
                            TrySetRuntimePropertyValue(leader, "Annotation", annotationId);
                        }
                    }
                    leader.EvaluateLeader();
                    tr.Commit();
                    return;
                }

                Polyline pl = entity as Polyline;
                if (pl != null)
                {
                    bool hasVertexWidths = HasArrayValues(map, 40) || HasArrayValues(map, 41);
                    if (map.ContainsKey(70))
                    {
                        pl.Closed = Convert.ToInt32(map[70]) != 0;
                    }
                    if (hasVertexWidths)
                    {
                        pl.ConstantWidth = 0.0;
                    }
                    else if (map.ContainsKey(43))
                    {
                        pl.ConstantWidth = Convert.ToDouble(map[43]);
                    }

                    ArrayList xs = map.ContainsKey(10) ? (ArrayList)map[10] : null;
                    ArrayList ys = map.ContainsKey(20) ? (ArrayList)map[20] : null;
                    if (xs != null && ys != null && xs.Count == ys.Count && xs.Count >= 2)
                    {
                        while (pl.NumberOfVertices > 0)
                        {
                            pl.RemoveVertexAt(pl.NumberOfVertices - 1);
                        }
                        for (int i = 0; i < xs.Count; i++)
                        {
                            double x = Convert.ToDouble(xs[i]);
                            double y = Convert.ToDouble(ys[i]);
                            pl.AddVertexAt(i, new Point2d(x, y), 0.0, 0.0, 0.0);
                        }
                    }

                    ApplyPolylineVertexData(pl, map);

                    tr.Commit();
                    return;
                }

                throw new NotSupportedException("EntMod non supportato per il tipo: " + entity.GetType().Name);
            }
        }

        private static Hashtable NormalizeDxfPairs(IList dxfPairs)
        {
            Hashtable map = new Hashtable();
            foreach (object raw in dxfPairs)
            {
                Hashtable item = raw as Hashtable;
                if (item == null || !item.ContainsKey("code"))
                {
                    continue;
                }

                int code = Convert.ToInt32(item["code"]);
                object value = item.ContainsKey("value") ? item["value"] : null;

                if (map.ContainsKey(code))
                {
                    ArrayList list = map[code] as ArrayList;
                    if (list == null)
                    {
                        list = new ArrayList { map[code] };
                        map[code] = list;
                    }
                    list.Add(value);
                }
                else
                {
                    map[code] = value;
                }
            }
            return map;
        }

        private static string GetRequiredDxfName(Hashtable map)
        {
            if (!map.ContainsKey(0))
            {
                throw new ArgumentException("EntMake richiede il codice DXF 0");
            }
            return Convert.ToString(map[0]) ?? string.Empty;
        }

        private static Entity BuildLineFromDxf(Hashtable map)
        {
            return new Line(GetPoint(map, 10), GetPoint(map, 11));
        }

        private static Entity BuildRayFromDxf(Hashtable map)
        {
            if (!HasVector(map, 11))
            {
                throw new ArgumentException("RAY richiede una direzione DXF 11/21/31");
            }

            Vector3d dir = GetVector(map, 11);
            if (dir.Length == 0.0)
            {
                throw new ArgumentException("RAY richiede una direzione non nulla");
            }

            Ray ray = new Ray();
            ray.BasePoint = GetPoint(map, 10);
            ray.UnitDir = dir.GetNormal();
            return ray;
        }

        private static Entity BuildCircleFromDxf(Hashtable map)
        {
            return new Circle(GetPoint(map, 10), Vector3d.ZAxis, Convert.ToDouble(map[40]));
        }

        private static Entity BuildArcFromDxf(Hashtable map)
        {
            return new Arc(
                GetPoint(map, 10),
                Convert.ToDouble(map[40]),
                DegreesToRadians(Convert.ToDouble(map[50])),
                DegreesToRadians(Convert.ToDouble(map[51])));
        }

        private static Entity BuildPointFromDxf(Hashtable map)
        {
            return new DBPoint(GetPoint(map, 10));
        }

        private static Entity Build3DFaceFromDxf(Hashtable map)
        {
            return new Face(
                GetPoint(map, 10),
                GetPoint(map, 11),
                GetPoint(map, 12),
                GetPoint(map, 13),
                true,
                true,
                true,
                true);
        }

        private static Entity BuildTraceFromDxf(Hashtable map)
        {
            Trace trace = new Trace();
            trace.SetPointAt(0, GetPoint(map, 10));
            trace.SetPointAt(1, GetPoint(map, 11));
            trace.SetPointAt(2, GetPoint(map, 12));
            trace.SetPointAt(3, GetPoint(map, 13));
            return trace;
        }

        private static Entity BuildSolidFromDxf(Hashtable map)
        {
            return new Solid(
                GetPoint(map, 10),
                GetPoint(map, 11),
                GetPoint(map, 12),
                GetPoint(map, 13));
        }

        private static Entity BuildTextFromDxf(Hashtable map)
        {
            DBText text = new DBText();
            text.Position = GetPoint(map, 10);
            text.TextString = map.ContainsKey(1) ? Convert.ToString(map[1]) : string.Empty;
            text.Height = map.ContainsKey(40) ? Convert.ToDouble(map[40]) : 1.0;
            text.Rotation = map.ContainsKey(50) ? DegreesToRadians(Convert.ToDouble(map[50])) : 0.0;
            ApplyTextLikeDxf(text, map);
            return text;
        }

        private static Entity BuildAttributeDefinitionFromDxf(Hashtable map)
        {
            AttributeDefinition def = new AttributeDefinition();
            def.Position = GetPoint(map, 10);
            def.TextString = map.ContainsKey(1) ? Convert.ToString(map[1]) : string.Empty;
            def.Tag = map.ContainsKey(2) ? Convert.ToString(map[2]) : string.Empty;
            def.Prompt = map.ContainsKey(3) ? Convert.ToString(map[3]) : string.Empty;
            def.Height = map.ContainsKey(40) ? Convert.ToDouble(map[40]) : 1.0;
            def.Rotation = map.ContainsKey(50) ? DegreesToRadians(Convert.ToDouble(map[50])) : 0.0;
            ApplyTextLikeDxf(def, map);
            if (map.ContainsKey(70))
            {
                int flags = Convert.ToInt32(map[70]);
                def.Invisible = (flags & 1) == 1;
                def.Constant = (flags & 2) == 2;
                def.Verifiable = (flags & 4) == 4;
            }
            return def;
        }

        private ObjectId BuildAttributeReferenceFromDxf(Hashtable map)
        {
            if (!map.ContainsKey(330))
            {
                throw new ArgumentException("ATTRIB richiede il codice DXF 330 con owner BlockReference");
            }

            ObjectId ownerId = ResolveObjectId(map[330]);
            if (ownerId.IsNull)
            {
                throw new ArgumentException("Owner BlockReference non risolto da DXF 330");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockReference br = tr.GetObject(ownerId, OpenMode.ForWrite) as BlockReference;
                if (br == null)
                {
                    throw new ArgumentException("DXF 330 non punta a un BlockReference");
                }

                AttributeReference attr = new AttributeReference();

                string tag = map.ContainsKey(2) ? Convert.ToString(map[2]) : string.Empty;
                AttributeDefinition matchedDef = FindAttributeDefinitionByTag(tr, br, tag);
                if (matchedDef != null)
                {
                    attr.SetAttributeFromBlock(matchedDef, br.BlockTransform);
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        attr.Tag = tag;
                    }
                }
                else
                {
                    attr.Position = HasPoint(map, 10) ? GetPoint(map, 10) : br.Position;
                    attr.Tag = tag;
                    attr.Height = map.ContainsKey(40) ? Convert.ToDouble(map[40]) : 1.0;
                    attr.Rotation = map.ContainsKey(50) ? DegreesToRadians(Convert.ToDouble(map[50])) : 0.0;
                }

                if (map.ContainsKey(1))
                {
                    attr.TextString = Convert.ToString(map[1]);
                }
                if (HasPoint(map, 10))
                {
                    attr.Position = GetPoint(map, 10);
                }
                if (map.ContainsKey(40))
                {
                    attr.Height = Convert.ToDouble(map[40]);
                }
                if (map.ContainsKey(50))
                {
                    attr.Rotation = DegreesToRadians(Convert.ToDouble(map[50]));
                }
                ApplyTextLikeDxf(attr, map);
                if (map.ContainsKey(70))
                {
                    int flags = Convert.ToInt32(map[70]);
                    attr.Invisible = (flags & 1) == 1;
                }

                ApplyCommonDxfToNewEntity(attr, map);
                br.AttributeCollection.AppendAttribute(attr);
                tr.AddNewlyCreatedDBObject(attr, true);
                tr.Commit();
                return attr.ObjectId;
            }
        }

        private static Entity BuildMTextFromDxf(Hashtable map)
        {
            MText text = new MText();
            text.Location = GetPoint(map, 10);
            text.Contents = map.ContainsKey(1) ? Convert.ToString(map[1]) : string.Empty;
            text.TextHeight = map.ContainsKey(40) ? Convert.ToDouble(map[40]) : 1.0;
            text.Width = map.ContainsKey(41) ? Convert.ToDouble(map[41]) : 0.0;
            if (map.ContainsKey(50))
            {
                text.Rotation = DegreesToRadians(Convert.ToDouble(map[50]));
            }
            ApplyMTextDxf(text, map);
            return text;
        }

        private static Entity BuildEllipseFromDxf(Hashtable map)
        {
            Point3d center = GetPoint(map, 10);
            Vector3d majorAxis = HasVector(map, 11) ? GetVector(map, 11) : new Vector3d(10.0, 0.0, 0.0);
            double ratio = map.ContainsKey(40) ? Convert.ToDouble(map[40]) : 0.5;
            double startParam = map.ContainsKey(41) ? Convert.ToDouble(map[41]) : 0.0;
            double endParam = map.ContainsKey(42) ? Convert.ToDouble(map[42]) : Math.PI * 2.0;
            return new Ellipse(center, Vector3d.ZAxis, majorAxis, ratio, startParam, endParam);
        }

        private ObjectId BuildInsertFromDxf(Hashtable map)
        {
            if (!map.ContainsKey(2))
            {
                throw new ArgumentException("INSERT richiede il codice DXF 2 (nome blocco)");
            }

            string blockName = Convert.ToString(map[2]);
            Point3d pt = GetPoint(map, 10);
            double sx = map.ContainsKey(41) ? Convert.ToDouble(map[41]) : 1.0;
            double sy = map.ContainsKey(42) ? Convert.ToDouble(map[42]) : 1.0;
            double sz = map.ContainsKey(43) ? Convert.ToDouble(map[43]) : 1.0;
            double rot = map.ContainsKey(50) ? Convert.ToDouble(map[50]) : 0.0;
            ObjectId id = InsertBlockScaled(blockName, pt.X, pt.Y, pt.Z, sx, sy, sz, rot);

            ArrayList common = new ArrayList();
            CopyIfPresent(map, common, 8);
            CopyIfPresent(map, common, 6);
            CopyIfPresent(map, common, 48);
            CopyIfPresent(map, common, 60);
            CopyIfPresent(map, common, 62);
            CopyIfPresent(map, common, 370);
            if (common.Count > 0)
            {
                EntMod(id, common);
            }
            return id;
        }

        private ObjectId BuildHatchFromDxf(Hashtable map)
        {
            ArrayList xs = map[10] as ArrayList;
            ArrayList ys = map[20] as ArrayList;
            if (xs == null || ys == null || xs.Count != ys.Count || xs.Count < 3)
            {
                throw new ArgumentException("HATCH richiede almeno 3 vertici DXF 10/20");
            }

            string pattern = map.ContainsKey(2) ? Convert.ToString(map[2]) : "SOLID";
            double scale = map.ContainsKey(41) ? Convert.ToDouble(map[41]) : 1.0;
            double angle = map.ContainsKey(52) ? Convert.ToDouble(map[52]) : 0.0;
            HatchPatternType patternType = GetHatchPatternTypeFromDxf(map, HatchPatternType.PreDefined);

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline boundary = new Polyline();
                for (int i = 0; i < xs.Count; i++)
                {
                    boundary.AddVertexAt(i, new Point2d(Convert.ToDouble(xs[i]), Convert.ToDouble(ys[i])), 0.0, 0.0, 0.0);
                }
                boundary.Closed = true;
                ApplyCommonDxfToNewEntity(boundary, map);
                ObjectId boundaryId = btr.AppendEntity(boundary);
                tr.AddNewlyCreatedDBObject(boundary, true);

                Hatch hatch = new Hatch();
                hatch.SetDatabaseDefaults();
                hatch.SetHatchPattern(patternType, string.IsNullOrWhiteSpace(pattern) ? "SOLID" : pattern);
                hatch.PatternScale = scale <= 0.0 ? 1.0 : scale;
                hatch.PatternAngle = DegreesToRadians(angle);
                if (map.ContainsKey(70))
                {
                    hatch.Associative = Convert.ToInt32(map[70]) != 0;
                }
                ApplyHatchOptionalDxf(hatch, map);
                ApplyCommonDxfToNewEntity(hatch, map);

                ObjectId hatchId = btr.AppendEntity(hatch);
                tr.AddNewlyCreatedDBObject(hatch, true);

                ObjectIdCollection loopIds = new ObjectIdCollection();
                loopIds.Add(boundaryId);
                hatch.Associative = true;
                hatch.AppendLoop(HatchLoopTypes.Default, loopIds);
                hatch.EvaluateHatch(true);

                tr.Commit();
                return hatchId;
            }
        }

        private ObjectId BuildLeaderFromDxf(Hashtable map)
        {
            ArrayList xs = map.ContainsKey(10) ? map[10] as ArrayList : null;
            ArrayList ys = map.ContainsKey(20) ? map[20] as ArrayList : null;
            if (xs == null || ys == null || xs.Count != ys.Count || xs.Count < 2)
            {
                throw new ArgumentException("LEADER richiede almeno 2 vertici DXF 10/20");
            }

            ArrayList coords = new ArrayList();
            for (int i = 0; i < xs.Count; i++)
            {
                coords.Add(Convert.ToDouble(xs[i]));
                coords.Add(Convert.ToDouble(ys[i]));
                coords.Add(GetOptionalZAt(map, 30, i));
            }

            ObjectId annotationId = map.ContainsKey(340) ? ResolveObjectId(map[340]) : ObjectId.Null;
            ObjectId id = AddLeader(coords, annotationId);

            ArrayList modPairs = new ArrayList();
            CopyIfPresent(map, modPairs, 8);
            CopyIfPresent(map, modPairs, 6);
            CopyIfPresent(map, modPairs, 48);
            CopyIfPresent(map, modPairs, 60);
            CopyIfPresent(map, modPairs, 62);
            CopyIfPresent(map, modPairs, 370);
            CopyIfPresent(map, modPairs, 71);
            if (modPairs.Count > 0)
            {
                EntMod(id, modPairs);
            }
            return id;
        }

        private static Entity BuildPolylineFromDxf(Hashtable map)
        {
            ArrayList xs = map[10] as ArrayList;
            ArrayList ys = map[20] as ArrayList;
            if (xs == null || ys == null || xs.Count != ys.Count || xs.Count < 2)
            {
                throw new ArgumentException("LWPOLYLINE richiede liste DXF 10/20 coerenti");
            }

            Polyline pl = new Polyline();
            for (int i = 0; i < xs.Count; i++)
            {
                pl.AddVertexAt(i, new Point2d(Convert.ToDouble(xs[i]), Convert.ToDouble(ys[i])), 0.0, 0.0, 0.0);
            }
            pl.Closed = map.ContainsKey(70) && Convert.ToInt32(map[70]) != 0;
            bool hasVertexWidths = HasArrayValues(map, 40) || HasArrayValues(map, 41);
            if (hasVertexWidths)
            {
                pl.ConstantWidth = 0.0;
            }
            else if (map.ContainsKey(43))
            {
                pl.ConstantWidth = Convert.ToDouble(map[43]);
            }
            ApplyPolylineVertexData(pl, map);
            return pl;
        }

        private void ApplyCommonDxfToNewEntity(Entity entity, Hashtable map)
        {
            if (map.ContainsKey(8))
            {
                string layerName = Convert.ToString(map[8]);
                EnsureLayer(layerName, 7);
                entity.Layer = layerName;
            }
            if (map.ContainsKey(6))
            {
                string linetypeName = Convert.ToString(map[6]);
                if (!string.IsNullOrWhiteSpace(linetypeName))
                {
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        LinetypeTable ltt = (LinetypeTable)tr.GetObject(_db.LinetypeTableId, OpenMode.ForRead);
                        if (!ltt.Has(linetypeName))
                        {
                            throw new ArgumentException("Linetype non trovato: " + linetypeName);
                        }
                    }
                    entity.Linetype = linetypeName;
                }
            }
            if (map.ContainsKey(62))
            {
                entity.ColorIndex = Convert.ToInt16(map[62]);
            }
            if (map.ContainsKey(48))
            {
                entity.LinetypeScale = Convert.ToDouble(map[48]);
            }
            if (map.ContainsKey(60))
            {
                entity.Visible = Convert.ToInt32(map[60]) == 0;
            }
            if (map.ContainsKey(370))
            {
                entity.LineWeight = (LineWeight)Convert.ToInt32(map[370]);
            }

            DBText text = entity as DBText;
            if (text != null && map.ContainsKey(7))
            {
                string styleName = Convert.ToString(map[7]);
                if (!string.IsNullOrWhiteSpace(styleName))
                {
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                        if (!table.Has(styleName))
                        {
                            throw new ArgumentException("TextStyle non trovato: " + styleName);
                        }
                        text.TextStyleId = table[styleName];
                    }
                }
            }

            AttributeDefinition attrDef = entity as AttributeDefinition;
            if (attrDef != null && map.ContainsKey(7))
            {
                string styleName = Convert.ToString(map[7]);
                if (!string.IsNullOrWhiteSpace(styleName))
                {
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                        if (!table.Has(styleName))
                        {
                            throw new ArgumentException("TextStyle non trovato: " + styleName);
                        }
                        attrDef.TextStyleId = table[styleName];
                    }
                }
            }

            AttributeReference attrRef = entity as AttributeReference;
            if (attrRef != null)
            {
                ApplyAttributeReferenceStyleDxf(attrRef, map);
            }

            MText mtext = entity as MText;
            if (mtext != null && map.ContainsKey(7))
            {
                string styleName = Convert.ToString(map[7]);
                if (!string.IsNullOrWhiteSpace(styleName))
                {
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                        if (!table.Has(styleName))
                        {
                            throw new ArgumentException("TextStyle non trovato: " + styleName);
                        }
                        mtext.TextStyleId = table[styleName];
                    }
                }
            }

            ApplyThicknessAndNormalDxf(entity, map);
        }

        private void ApplyCommonDxfToExistingEntity(Entity entity, Hashtable map)
        {
            if (map.ContainsKey(8))
            {
                string layerName = Convert.ToString(map[8]);
                EnsureLayer(layerName, 7);
                entity.Layer = layerName;
            }

            if (map.ContainsKey(6))
            {
                string linetypeName = Convert.ToString(map[6]);
                if (!string.IsNullOrWhiteSpace(linetypeName))
                {
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        LinetypeTable ltt = (LinetypeTable)tr.GetObject(_db.LinetypeTableId, OpenMode.ForRead);
                        if (!ltt.Has(linetypeName))
                        {
                            throw new ArgumentException("Linetype non trovato: " + linetypeName);
                        }
                    }
                    entity.Linetype = linetypeName;
                }
            }

            if (map.ContainsKey(62))
            {
                entity.ColorIndex = Convert.ToInt16(map[62]);
            }
            if (map.ContainsKey(48))
            {
                entity.LinetypeScale = Convert.ToDouble(map[48]);
            }
            if (map.ContainsKey(60))
            {
                entity.Visible = Convert.ToInt32(map[60]) == 0;
            }
            if (map.ContainsKey(370))
            {
                entity.LineWeight = (LineWeight)Convert.ToInt32(map[370]);
            }

            DBText text = entity as DBText;
            if (text != null && map.ContainsKey(7))
            {
                string styleName = Convert.ToString(map[7]);
                if (!string.IsNullOrWhiteSpace(styleName))
                {
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                        if (!table.Has(styleName))
                        {
                            throw new ArgumentException("TextStyle non trovato: " + styleName);
                        }
                        text.TextStyleId = table[styleName];
                    }
                }
            }

            AttributeDefinition attrDef = entity as AttributeDefinition;
            if (attrDef != null && map.ContainsKey(7))
            {
                string styleName = Convert.ToString(map[7]);
                if (!string.IsNullOrWhiteSpace(styleName))
                {
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                        if (!table.Has(styleName))
                        {
                            throw new ArgumentException("TextStyle non trovato: " + styleName);
                        }
                        attrDef.TextStyleId = table[styleName];
                    }
                }
            }

            AttributeReference attrRef = entity as AttributeReference;
            if (attrRef != null)
            {
                ApplyAttributeReferenceStyleDxf(attrRef, map);
            }

            MText mtext = entity as MText;
            if (mtext != null && map.ContainsKey(7))
            {
                string styleName = Convert.ToString(map[7]);
                if (!string.IsNullOrWhiteSpace(styleName))
                {
                    using (Transaction tr = _db.TransactionManager.StartTransaction())
                    {
                        TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                        if (!table.Has(styleName))
                        {
                            throw new ArgumentException("TextStyle non trovato: " + styleName);
                        }
                        mtext.TextStyleId = table[styleName];
                    }
                }
            }

            ApplyThicknessAndNormalDxf(entity, map);
        }

        private static bool HasPoint(Hashtable map, int baseCode)
        {
            return map.ContainsKey(baseCode) && map.ContainsKey(baseCode + 10);
        }

        private static Point3d GetPoint(Hashtable map, int baseCode)
        {
            double x = Convert.ToDouble(map[baseCode]);
            double y = Convert.ToDouble(map[baseCode + 10]);
            double z = map.ContainsKey(baseCode + 20) ? Convert.ToDouble(map[baseCode + 20]) : 0.0;
            return new Point3d(x, y, z);
        }

        private static void AddDxfPair(ArrayList pairs, int code, object value)
        {
            Hashtable item = new Hashtable();
            item["code"] = code;
            item["value"] = value;
            pairs.Add(item);
        }

        private static void AddPointDxf(ArrayList pairs, int baseCode, Point3d pt)
        {
            AddDxfPair(pairs, baseCode, pt.X);
            AddDxfPair(pairs, baseCode + 10, pt.Y);
            AddDxfPair(pairs, baseCode + 20, pt.Z);
        }

        private static ArrayList BuildDxfPairs(Entity entity)
        {
            ArrayList pairs = new ArrayList();
            AddDxfPair(pairs, 0, entity.GetRXClass().DxfName);
            AddDxfPair(pairs, 5, entity.Handle.ToString());
            string ownerHandle = GetOwnerHandleString(entity);
            if (!string.IsNullOrWhiteSpace(ownerHandle))
            {
                AddDxfPair(pairs, 330, ownerHandle);
            }
            AddDxfPair(pairs, 67, GetSpaceFlag(entity));
            string layoutName = GetEntityLayoutName(entity);
            if (!string.IsNullOrWhiteSpace(layoutName))
            {
                AddDxfPair(pairs, 410, layoutName);
            }
            AddDxfPair(pairs, 8, entity.Layer);
            AddDxfPair(pairs, 6, entity.Linetype);
            AddDxfPair(pairs, 48, entity.LinetypeScale);
            AddDxfPair(pairs, 60, entity.Visible ? 0 : 1);
            AddDxfPair(pairs, 62, entity.ColorIndex);
            AddDxfPair(pairs, 370, (int)entity.LineWeight);

            Line line = entity as Line;
            if (line != null)
            {
                AddPointDxf(pairs, 10, line.StartPoint);
                AddPointDxf(pairs, 11, line.EndPoint);
                AddDxfPair(pairs, 39, line.Thickness);
                AddVectorDxf(pairs, 210, line.Normal);
                return pairs;
            }

            Ray ray = entity as Ray;
            if (ray != null)
            {
                AddPointDxf(pairs, 10, ray.BasePoint);
                AddVectorDxf(pairs, 11, ray.UnitDir);
                return pairs;
            }

            Circle circle = entity as Circle;
            if (circle != null)
            {
                AddPointDxf(pairs, 10, circle.Center);
                AddDxfPair(pairs, 40, circle.Radius);
                AddDxfPair(pairs, 39, circle.Thickness);
                AddVectorDxf(pairs, 210, circle.Normal);
                return pairs;
            }

            Arc arc = entity as Arc;
            if (arc != null)
            {
                AddPointDxf(pairs, 10, arc.Center);
                AddDxfPair(pairs, 40, arc.Radius);
                AddDxfPair(pairs, 50, RadiansToDegrees(arc.StartAngle));
                AddDxfPair(pairs, 51, RadiansToDegrees(arc.EndAngle));
                AddDxfPair(pairs, 39, arc.Thickness);
                AddVectorDxf(pairs, 210, arc.Normal);
                return pairs;
            }

            DBPoint point = entity as DBPoint;
            if (point != null)
            {
                AddPointDxf(pairs, 10, point.Position);
                AddVectorDxf(pairs, 210, point.Normal);
                AddDxfPair(pairs, 39, point.Thickness);
                return pairs;
            }

            Face face = entity as Face;
            if (face != null)
            {
                AddPointDxf(pairs, 10, face.GetVertexAt(0));
                AddPointDxf(pairs, 11, face.GetVertexAt(1));
                AddPointDxf(pairs, 12, face.GetVertexAt(2));
                AddPointDxf(pairs, 13, face.GetVertexAt(3));
                return pairs;
            }

            Trace trace = entity as Trace;
            if (trace != null)
            {
                AddPointDxf(pairs, 10, trace.GetPointAt(0));
                AddPointDxf(pairs, 11, trace.GetPointAt(1));
                AddPointDxf(pairs, 12, trace.GetPointAt(2));
                AddPointDxf(pairs, 13, trace.GetPointAt(3));
                return pairs;
            }

            Solid solid = entity as Solid;
            if (solid != null)
            {
                AddPointDxf(pairs, 10, solid.GetPointAt(0));
                AddPointDxf(pairs, 11, solid.GetPointAt(1));
                AddPointDxf(pairs, 12, solid.GetPointAt(2));
                AddPointDxf(pairs, 13, solid.GetPointAt(3));
                return pairs;
            }

            AttributeDefinition def = entity as AttributeDefinition;
            if (def != null)
            {
                AddPointDxf(pairs, 10, def.Position);
                AddDxfPair(pairs, 1, def.TextString);
                AddDxfPair(pairs, 2, def.Tag);
                AddDxfPair(pairs, 3, def.Prompt);
                AddDxfPair(pairs, 7, GetAttributeDefinitionStyleName(def));
                AddDxfPair(pairs, 40, def.Height);
                AddTextLikeDxf(pairs, def);
                AddDxfPair(pairs, 50, RadiansToDegrees(def.Rotation));
                AddDxfPair(pairs, 70, BuildAttributeFlags(def.Invisible, def.Constant, def.Verifiable));
                AddDxfPair(pairs, 39, def.Thickness);
                AddVectorDxf(pairs, 210, def.Normal);
                return pairs;
            }

            AttributeReference attr = entity as AttributeReference;
            if (attr != null)
            {
                AddPointDxf(pairs, 10, attr.Position);
                AddDxfPair(pairs, 1, attr.TextString);
                AddDxfPair(pairs, 2, attr.Tag);
                AddDxfPair(pairs, 7, GetAttributeReferenceStyleName(attr));
                AddDxfPair(pairs, 40, attr.Height);
                AddTextLikeDxf(pairs, attr);
                AddDxfPair(pairs, 50, RadiansToDegrees(attr.Rotation));
                AddDxfPair(pairs, 70, attr.Invisible ? 1 : 0);
                AddDxfPair(pairs, 39, attr.Thickness);
                AddVectorDxf(pairs, 210, attr.Normal);
                return pairs;
            }

            DBText text = entity as DBText;
            if (text != null)
            {
                AddPointDxf(pairs, 10, text.Position);
                AddDxfPair(pairs, 1, text.TextString);
                AddDxfPair(pairs, 7, GetTextStyleName(text));
                AddDxfPair(pairs, 40, text.Height);
                AddTextLikeDxf(pairs, text);
                AddDxfPair(pairs, 50, RadiansToDegrees(text.Rotation));
                AddDxfPair(pairs, 39, text.Thickness);
                AddVectorDxf(pairs, 210, text.Normal);
                return pairs;
            }

            MText mtext = entity as MText;
            if (mtext != null)
            {
                AddPointDxf(pairs, 10, mtext.Location);
                AddDxfPair(pairs, 1, mtext.Contents);
                AddDxfPair(pairs, 7, GetMTextStyleName(mtext));
                AddDxfPair(pairs, 40, mtext.TextHeight);
                AddDxfPair(pairs, 41, mtext.Width);
                AddDxfPair(pairs, 50, RadiansToDegrees(mtext.Rotation));
                AddMTextExtraDxf(pairs, mtext);
                AddVectorDxf(pairs, 210, mtext.Normal);
                return pairs;
            }

            Ellipse ellipse = entity as Ellipse;
            if (ellipse != null)
            {
                AddPointDxf(pairs, 10, ellipse.Center);
                AddVectorDxf(pairs, 11, ellipse.MajorAxis);
                AddDxfPair(pairs, 40, ellipse.RadiusRatio);
                AddDxfPair(pairs, 41, ellipse.StartAngle);
                AddDxfPair(pairs, 42, ellipse.EndAngle);
                AddVectorDxf(pairs, 210, ellipse.Normal);
                return pairs;
            }

            BlockReference br = entity as BlockReference;
            if (br != null)
            {
                AddDxfPair(pairs, 2, br.Name);
                AddDxfPair(pairs, 66, br.AttributeCollection.Count > 0 ? 1 : 0);
                AddPointDxf(pairs, 10, br.Position);
                AddDxfPair(pairs, 41, br.ScaleFactors.X);
                AddDxfPair(pairs, 42, br.ScaleFactors.Y);
                AddDxfPair(pairs, 43, br.ScaleFactors.Z);
                AddDxfPair(pairs, 50, RadiansToDegrees(br.Rotation));
                AddVectorDxf(pairs, 210, br.Normal);
                return pairs;
            }

            Hatch hatch = entity as Hatch;
            if (hatch != null)
            {
                AddDxfPair(pairs, 2, hatch.PatternName);
                AddDxfPair(pairs, 70, hatch.Associative ? 1 : 0);
                AddDxfPair(pairs, 41, hatch.PatternScale);
                AddDxfPair(pairs, 52, RadiansToDegrees(hatch.PatternAngle));
                AddEnumIntDxf(pairs, hatch, "HatchStyle", 75);
                AddDxfPair(pairs, 76, (int)hatch.PatternType);
                AddOptionalBoolAsIntDxf(pairs, hatch, "PatternDouble", 77);
                return pairs;
            }

            Leader leader = entity as Leader;
            if (leader != null)
            {
                AddDxfPair(pairs, 71, GetLeaderBoolProperty(leader, "HasArrowHead") ? 1 : 0);
                ObjectId annotationId = GetLeaderObjectIdProperty(leader, "Annotation");
                if (!annotationId.IsNull)
                {
                    AddDxfPair(pairs, 340, annotationId.Handle.ToString());
                }
                int count = GetLeaderVertexCount(leader);
                AddDxfPair(pairs, 76, count);
                for (int i = 0; i < count; i++)
                {
                    Point3d pt = leader.VertexAt(i);
                    AddPointDxf(pairs, 10, pt);
                }
                return pairs;
            }

            Polyline pl = entity as Polyline;
            if (pl != null)
            {
                AddDxfPair(pairs, 90, pl.NumberOfVertices);
                AddDxfPair(pairs, 70, pl.Closed ? 1 : 0);
                AddDxfPair(pairs, 38, pl.Elevation);
                AddDxfPair(pairs, 39, pl.Thickness);
                if (PolylineHasConstantWidth(pl, out double constantWidth))
                {
                    AddDxfPair(pairs, 43, constantWidth);
                }
                AddVectorDxf(pairs, 210, pl.Normal);
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    Point2d pt = pl.GetPoint2dAt(i);
                    AddDxfPair(pairs, 10, pt.X);
                    AddDxfPair(pairs, 20, pt.Y);
                    AddDxfPair(pairs, 40, pl.GetStartWidthAt(i));
                    AddDxfPair(pairs, 41, pl.GetEndWidthAt(i));
                    AddDxfPair(pairs, 42, pl.GetBulgeAt(i));
                }
                return pairs;
            }

            return pairs;
        }

        private static void AddVectorDxf(ArrayList pairs, int baseCode, Vector3d vec)
        {
            AddDxfPair(pairs, baseCode, vec.X);
            AddDxfPair(pairs, baseCode + 10, vec.Y);
            AddDxfPair(pairs, baseCode + 20, vec.Z);
        }

        private static string GetOwnerHandleString(DBObject dbo)
        {
            try
            {
                if (dbo == null || dbo.OwnerId.IsNull)
                {
                    return string.Empty;
                }

                DBObject owner = dbo.OwnerId.GetObject(OpenMode.ForRead);
                return owner == null ? string.Empty : owner.Handle.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int GetSpaceFlag(DBObject dbo)
        {
            BlockTableRecord space = GetContainingSpaceRecord(dbo);
            if (space == null)
            {
                return 0;
            }

            try
            {
                return space.Name == BlockTableRecord.PaperSpace ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetEntityLayoutName(DBObject dbo)
        {
            BlockTableRecord space = GetContainingSpaceRecord(dbo);
            if (space == null || !space.IsLayout || space.LayoutId.IsNull)
            {
                return string.Empty;
            }

            try
            {
                Layout layout = space.LayoutId.GetObject(OpenMode.ForRead) as Layout;
                return layout == null ? string.Empty : layout.LayoutName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static BlockTableRecord GetContainingSpaceRecord(DBObject dbo)
        {
            try
            {
                DBObject current = dbo;
                while (current != null && !current.OwnerId.IsNull)
                {
                    DBObject owner = current.OwnerId.GetObject(OpenMode.ForRead);
                    BlockTableRecord btr = owner as BlockTableRecord;
                    if (btr != null)
                    {
                        if (btr.IsLayout)
                        {
                            return btr;
                        }

                        current = btr;
                        continue;
                    }

                    current = owner;
                }
            }
            catch
            {
            }

            return null;
        }

        private static void ApplyThicknessAndNormalDxf(Entity entity, Hashtable map)
        {
            if (entity is Line)
            {
                Line line = (Line)entity;
                if (map.ContainsKey(39))
                {
                    line.Thickness = Convert.ToDouble(map[39]);
                }
                if (HasVector(map, 210))
                {
                    line.Normal = GetVector(map, 210);
                }
                return;
            }

            if (entity is Circle)
            {
                Circle circle = (Circle)entity;
                if (map.ContainsKey(39))
                {
                    circle.Thickness = Convert.ToDouble(map[39]);
                }
                if (HasVector(map, 210))
                {
                    circle.Normal = GetVector(map, 210);
                }
                return;
            }

            if (entity is Arc)
            {
                Arc arc = (Arc)entity;
                if (map.ContainsKey(39))
                {
                    arc.Thickness = Convert.ToDouble(map[39]);
                }
                if (HasVector(map, 210))
                {
                    arc.Normal = GetVector(map, 210);
                }
                return;
            }

            if (entity is DBPoint)
            {
                DBPoint point = (DBPoint)entity;
                if (map.ContainsKey(39))
                {
                    point.Thickness = Convert.ToDouble(map[39]);
                }
                if (HasVector(map, 210))
                {
                    point.Normal = GetVector(map, 210);
                }
                return;
            }

            if (entity is DBText)
            {
                DBText text = (DBText)entity;
                if (map.ContainsKey(39))
                {
                    text.Thickness = Convert.ToDouble(map[39]);
                }
                if (HasVector(map, 210))
                {
                    text.Normal = GetVector(map, 210);
                }
                return;
            }

            if (entity is Polyline)
            {
                Polyline pl = (Polyline)entity;
                if (map.ContainsKey(39))
                {
                    pl.Thickness = Convert.ToDouble(map[39]);
                }
                if (map.ContainsKey(38))
                {
                    pl.Elevation = Convert.ToDouble(map[38]);
                }
                if (HasVector(map, 210))
                {
                    pl.Normal = GetVector(map, 210);
                }
                return;
            }

            if (entity is Ellipse)
            {
                return;
            }
        }

        private static bool HasVector(Hashtable map, int baseCode)
        {
            return map.ContainsKey(baseCode) && map.ContainsKey(baseCode + 10) && map.ContainsKey(baseCode + 20);
        }

        private ObjectId ResolveObjectId(object raw)
        {
            if (raw is ObjectId)
            {
                return (ObjectId)raw;
            }

            string text = Convert.ToString(raw);
            if (string.IsNullOrWhiteSpace(text))
            {
                return ObjectId.Null;
            }

            return GetEntityByHandle(text);
        }

        private static AttributeDefinition FindAttributeDefinitionByTag(Transaction tr, BlockReference br, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }

            BlockTableRecord def = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
            if (def == null || !def.HasAttributeDefinitions)
            {
                return null;
            }

            foreach (ObjectId entId in def)
            {
                AttributeDefinition ad = tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
                if (ad != null && string.Equals(ad.Tag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    return ad;
                }
            }

            return null;
        }

        private static void CopyIfPresent(Hashtable map, ArrayList pairs, int code)
        {
            if (!map.ContainsKey(code))
            {
                return;
            }

            ArrayList values = map[code] as ArrayList;
            if (values != null)
            {
                foreach (object value in values)
                {
                    AddDxfPair(pairs, code, value);
                }
                return;
            }

            AddDxfPair(pairs, code, map[code]);
        }

        private static Vector3d GetVector(Hashtable map, int baseCode)
        {
            return new Vector3d(
                Convert.ToDouble(map[baseCode]),
                Convert.ToDouble(map[baseCode + 10]),
                Convert.ToDouble(map[baseCode + 20]));
        }

        private static double RadiansToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        private static string GetTextStyleName(DBText text)
        {
            try
            {
                DBObject dbo = text.TextStyleId.GetObject(OpenMode.ForRead);
                TextStyleTableRecord rec = dbo as TextStyleTableRecord;
                if (rec != null)
                {
                    return rec.Name;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string GetMTextStyleName(MText text)
        {
            try
            {
                DBObject dbo = text.TextStyleId.GetObject(OpenMode.ForRead);
                TextStyleTableRecord rec = dbo as TextStyleTableRecord;
                if (rec != null)
                {
                    return rec.Name;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string GetAttributeDefinitionStyleName(AttributeDefinition def)
        {
            try
            {
                DBObject dbo = def.TextStyleId.GetObject(OpenMode.ForRead);
                TextStyleTableRecord rec = dbo as TextStyleTableRecord;
                if (rec != null)
                {
                    return rec.Name;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string GetAttributeReferenceStyleName(AttributeReference attr)
        {
            try
            {
                DBObject dbo = attr.TextStyleId.GetObject(OpenMode.ForRead);
                TextStyleTableRecord rec = dbo as TextStyleTableRecord;
                if (rec != null)
                {
                    return rec.Name;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private void ApplyAttributeReferenceStyleDxf(AttributeReference attr, Hashtable map)
        {
            if (!map.ContainsKey(7))
            {
                return;
            }

            string styleName = Convert.ToString(map[7]);
            if (string.IsNullOrWhiteSpace(styleName))
            {
                return;
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                TextStyleTable table = (TextStyleTable)tr.GetObject(_db.TextStyleTableId, OpenMode.ForRead);
                if (!table.Has(styleName))
                {
                    throw new ArgumentException("TextStyle non trovato: " + styleName);
                }
                attr.TextStyleId = table[styleName];
            }
        }

        private static void ApplyPolylineVertexData(Polyline pl, Hashtable map)
        {
            ArrayList bulges = map.ContainsKey(42) ? map[42] as ArrayList : null;
            ArrayList startWidths = map.ContainsKey(40) ? map[40] as ArrayList : null;
            ArrayList endWidths = map.ContainsKey(41) ? map[41] as ArrayList : null;

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                if (bulges != null && i < bulges.Count)
                {
                    pl.SetBulgeAt(i, Convert.ToDouble(bulges[i]));
                }
                if (startWidths != null && i < startWidths.Count)
                {
                    pl.SetStartWidthAt(i, Convert.ToDouble(startWidths[i]));
                }
                if (endWidths != null && i < endWidths.Count)
                {
                    pl.SetEndWidthAt(i, Convert.ToDouble(endWidths[i]));
                }
            }
        }

        private static bool PolylineHasConstantWidth(Polyline pl, out double constantWidth)
        {
            constantWidth = 0.0;
            if (pl.NumberOfVertices == 0)
            {
                return false;
            }

            double firstStart = pl.GetStartWidthAt(0);
            double firstEnd = pl.GetEndWidthAt(0);
            if (Math.Abs(firstStart - firstEnd) > 1e-9)
            {
                return false;
            }

            for (int i = 1; i < pl.NumberOfVertices; i++)
            {
                double sw = pl.GetStartWidthAt(i);
                double ew = pl.GetEndWidthAt(i);
                if (Math.Abs(sw - ew) > 1e-9)
                {
                    return false;
                }
                if (Math.Abs(sw - firstStart) > 1e-9)
                {
                    return false;
                }
            }

            constantWidth = firstStart;
            return Math.Abs(constantWidth) > 1e-9;
        }

        private static bool HasArrayValues(Hashtable map, int code)
        {
            ArrayList list = map.ContainsKey(code) ? map[code] as ArrayList : null;
            return list != null && list.Count > 0;
        }

        private static double GetOptionalZAt(Hashtable map, int code, int index)
        {
            ArrayList list = map.ContainsKey(code) ? map[code] as ArrayList : null;
            if (list != null && index < list.Count)
            {
                return Convert.ToDouble(list[index]);
            }

            if (map.ContainsKey(code) && !(map[code] is ArrayList) && index == 0)
            {
                return Convert.ToDouble(map[code]);
            }

            return 0.0;
        }

        private static void ReplaceLeaderVertices(Leader leader, Hashtable map)
        {
            ArrayList xs = map[10] as ArrayList;
            ArrayList ys = map[20] as ArrayList;
            if (xs == null || ys == null || xs.Count != ys.Count || xs.Count < 2)
            {
                return;
            }

            int currentCount = GetLeaderVertexCount(leader);
            for (int i = currentCount - 1; i >= 0; i--)
            {
                leader.RemoveLastVertex();
            }

            for (int i = 0; i < xs.Count; i++)
            {
                leader.AppendVertex(new Point3d(
                    Convert.ToDouble(xs[i]),
                    Convert.ToDouble(ys[i]),
                    GetOptionalZAt(map, 30, i)));
            }
        }

        private static void CollectDescendants(Transaction tr, ObjectId entityId, int depth, int maxDepth, List<ObjectId> ids)
        {
            if (depth > maxDepth)
            {
                return;
            }

            DBObject dbo = tr.GetObject(entityId, OpenMode.ForRead);
            BlockReference br = dbo as BlockReference;
            if (br != null)
            {
                foreach (ObjectId id in br.AttributeCollection)
                {
                    ids.Add(id);
                    CollectDescendants(tr, id, depth + 1, maxDepth, ids);
                }
            }

            BlockTableRecord btr = dbo as BlockTableRecord;
            if (btr != null)
            {
                foreach (ObjectId id in btr)
                {
                    ids.Add(id);
                    CollectDescendants(tr, id, depth + 1, maxDepth, ids);
                }
            }
        }

        private static void ApplyTextLikeDxf(DBText text, Hashtable map)
        {
            if (map.ContainsKey(71))
            {
                int flags = Convert.ToInt32(map[71]);
                TrySetRuntimePropertyValue(text, "IsMirroredInX", (flags & 2) == 2);
                TrySetRuntimePropertyValue(text, "IsMirroredInY", (flags & 4) == 4);
            }
            if (map.ContainsKey(41))
            {
                TrySetRuntimePropertyValue(text, "WidthFactor", Convert.ToDouble(map[41]));
            }
            if (map.ContainsKey(51))
            {
                TrySetRuntimePropertyValue(text, "Oblique", DegreesToRadians(Convert.ToDouble(map[51])));
            }
            if (HasPoint(map, 11))
            {
                TrySetRuntimePropertyValue(text, "AlignmentPoint", GetPoint(map, 11));
            }
            ApplyEnumIntProperty(text, "HorizontalMode", map, 72);
            ApplyEnumIntProperty(text, "VerticalMode", map, 73);
        }

        private static void ApplyMTextDxf(MText text, Hashtable map)
        {
            ApplyEnumIntProperty(text, "Attachment", map, 71);
            ApplyEnumIntProperty(text, "FlowDirection", map, 72);
            ApplyEnumIntProperty(text, "LineSpacingStyle", map, 73);
            if (map.ContainsKey(44))
            {
                TrySetRuntimePropertyValue(text, "LineSpacingFactor", Convert.ToDouble(map[44]));
            }
        }

        private static void ApplyEnumIntProperty(object instance, string propertyName, Hashtable map, int code)
        {
            if (!map.ContainsKey(code))
            {
                return;
            }

            var prop = instance.GetType().GetProperty(propertyName);
            if (prop == null || !prop.CanWrite)
            {
                return;
            }

            int raw = Convert.ToInt32(map[code]);
            object enumValue = Enum.ToObject(prop.PropertyType, raw);
            prop.SetValue(instance, enumValue, null);
        }

        private static void AddTextLikeDxf(ArrayList pairs, DBText text)
        {
            int generation = 0;
            object mirroredX = GetRuntimePropertyValue(text, "IsMirroredInX");
            object mirroredY = GetRuntimePropertyValue(text, "IsMirroredInY");
            if (mirroredX is bool && (bool)mirroredX)
            {
                generation |= 2;
            }
            if (mirroredY is bool && (bool)mirroredY)
            {
                generation |= 4;
            }
            if (generation != 0)
            {
                AddDxfPair(pairs, 71, generation);
            }

            object widthFactor = GetRuntimePropertyValue(text, "WidthFactor");
            if (widthFactor != null)
            {
                AddDxfPair(pairs, 41, widthFactor);
            }

            object oblique = GetRuntimePropertyValue(text, "Oblique");
            if (oblique != null)
            {
                AddDxfPair(pairs, 51, RadiansToDegrees(Convert.ToDouble(oblique)));
            }

            object alignment = GetRuntimePropertyValue(text, "AlignmentPoint");
            if (alignment is Point3d)
            {
                AddPointDxf(pairs, 11, (Point3d)alignment);
            }

            AddEnumIntDxf(pairs, text, "HorizontalMode", 72);
            AddEnumIntDxf(pairs, text, "VerticalMode", 73);
        }

        private static void AddMTextExtraDxf(ArrayList pairs, MText text)
        {
            AddEnumIntDxf(pairs, text, "Attachment", 71);
            AddEnumIntDxf(pairs, text, "FlowDirection", 72);
            AddEnumIntDxf(pairs, text, "LineSpacingStyle", 73);
            object spacing = GetRuntimePropertyValue(text, "LineSpacingFactor");
            if (spacing != null)
            {
                AddDxfPair(pairs, 44, spacing);
            }
        }

        private static void AddEnumIntDxf(ArrayList pairs, object instance, string propertyName, int code)
        {
            object value = GetRuntimePropertyValue(instance, propertyName);
            if (value == null)
            {
                return;
            }
            AddDxfPair(pairs, code, Convert.ToInt32(value));
        }

        private static void AddOptionalBoolAsIntDxf(ArrayList pairs, object instance, string propertyName, int code)
        {
            object value = GetRuntimePropertyValue(instance, propertyName);
            if (value is bool)
            {
                AddDxfPair(pairs, code, (bool)value ? 1 : 0);
            }
        }

        private static HatchPatternType GetHatchPatternTypeFromDxf(Hashtable map, HatchPatternType fallback)
        {
            if (!map.ContainsKey(76))
            {
                return fallback;
            }

            try
            {
                return (HatchPatternType)Convert.ToInt32(map[76]);
            }
            catch
            {
                return fallback;
            }
        }

        private static void ApplyHatchOptionalDxf(Hatch hatch, Hashtable map)
        {
            ApplyEnumIntProperty(hatch, "HatchStyle", map, 75);
            if (map.ContainsKey(77))
            {
                TrySetRuntimePropertyValue(hatch, "PatternDouble", Convert.ToInt32(map[77]) != 0);
            }
        }

        private static int BuildAttributeFlags(bool invisible, bool constant, bool verifiable)
        {
            int flags = 0;
            if (invisible)
            {
                flags |= 1;
            }
            if (constant)
            {
                flags |= 2;
            }
            if (verifiable)
            {
                flags |= 4;
            }
            return flags;
        }
    }
}
