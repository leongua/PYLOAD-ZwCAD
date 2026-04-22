using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public PromptEntityResult SelectEntity(string message)
        {
            PromptEntityOptions peo = new PromptEntityOptions("\n" + message);
            return _ed.GetEntity(peo);
        }

        public ObjectId[] SelectAll()
        {
            PromptSelectionResult psr = _ed.SelectAll();
            if (psr.Status != PromptStatus.OK || psr.Value == null)
            {
                return new ObjectId[0];
            }

            return psr.Value.GetObjectIds();
        }

        public ObjectId[] GetSelectionByLayer(string layerName)
        {
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity != null && string.Equals(entity.Layer, layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        ids.Add(id);
                    }
                }
            }
            return ids.ToArray();
        }

        public ObjectId[] GetSelectionByType(string typeName)
        {
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity != null && string.Equals(entity.GetType().Name, typeName, StringComparison.OrdinalIgnoreCase))
                    {
                        ids.Add(id);
                    }
                }
            }
            return ids.ToArray();
        }

        public ObjectId[] GetSelection(string layerName, string typeName)
        {
            List<ObjectId> ids = new List<ObjectId>();
            bool filterLayer = !string.IsNullOrWhiteSpace(layerName);
            bool filterType = !string.IsNullOrWhiteSpace(typeName);

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    if (filterLayer && !string.Equals(entity.Layer, layerName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (filterType && !string.Equals(entity.GetType().Name, typeName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ids.Add(id);
                }
            }
            return ids.ToArray();
        }

        public ObjectId[] GetSelectionByDxf(IList dxfFilters)
        {
            List<ObjectId> ids = new List<ObjectId>();
            Hashtable filters = NormalizeDxfPairs(dxfFilters);

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity != null && MatchesDxfFilters(entity, filters))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.ToArray();
        }

        public ObjectId[] SelectWindowByDxf(double x1, double y1, double z1, double x2, double y2, double z2, IList dxfFilters)
        {
            return FilterEntitiesByDxf(SelectWindow(x1, y1, z1, x2, y2, z2), dxfFilters);
        }

        public ObjectId[] SelectCrossingWindowByDxf(double x1, double y1, double z1, double x2, double y2, double z2, IList dxfFilters)
        {
            return FilterEntitiesByDxf(SelectCrossingWindow(x1, y1, z1, x2, y2, z2), dxfFilters);
        }

        public ObjectId[] FilterEntitiesByDxf(IList entityIds, IList dxfFilters)
        {
            List<ObjectId> ids = new List<ObjectId>();
            Hashtable filters = NormalizeDxfPairs(dxfFilters);

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                foreach (object raw in entityIds)
                {
                    if (!(raw is ObjectId))
                    {
                        continue;
                    }

                    ObjectId id = (ObjectId)raw;
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity != null && MatchesDxfFilters(entity, filters))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.ToArray();
        }

        public Hashtable DebugDxfFilterMatch(ObjectId entityId, IList dxfFilters)
        {
            Hashtable result = new Hashtable();
            Hashtable filters = NormalizeDxfPairs(dxfFilters);

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null)
                {
                    throw new ArgumentException("L'ObjectId non identifica una Entity");
                }

                result["entity_id"] = entityId.ToString();
                result["handle"] = entity.Handle.ToString();
                result["dxf_name"] = entity.GetRXClass().DxfName;
                result["matched"] = MatchesDxfFilters(entity, filters);

                Hashtable details = new Hashtable();
                foreach (DictionaryEntry entry in filters)
                {
                    int code = Convert.ToInt32(entry.Key);
                    Hashtable item = new Hashtable();
                    item["expected"] = entry.Value;
                    item["matched"] = MatchesSingleDxfFilter(entity, code, entry.Value);
                    item["actual"] = GetDebugActualDxfValue(entity, code);
                    details[code] = item;
                }

                result["details"] = details;
                return result;
            }
        }

        public ObjectId[] GetSelectionByArea(double minArea, double maxArea, string layerName, string typeName, bool onlyClosed)
        {
            List<ObjectId> ids = new List<ObjectId>();

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null || !MatchesBasicEntityFilters(entity, layerName, typeName))
                    {
                        continue;
                    }

                    if (!HasArea(entity))
                    {
                        continue;
                    }

                    if (onlyClosed && !IsClosedEntity(entity))
                    {
                        continue;
                    }

                    double area = GetAreaInternal(entity);
                    if (ValueInRange(area, minArea, maxArea))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.ToArray();
        }

        public ObjectId[] GetSelectionByLength(double minLength, double maxLength, string layerName, string typeName, bool closedOnly)
        {
            List<ObjectId> ids = new List<ObjectId>();

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null || !MatchesBasicEntityFilters(entity, layerName, typeName))
                    {
                        continue;
                    }

                    if (!HasPerimeter(entity))
                    {
                        continue;
                    }

                    if (closedOnly && !IsClosedEntity(entity))
                    {
                        continue;
                    }

                    double length = GetPerimeterInternal(entity);
                    if (ValueInRange(length, minLength, maxLength))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.ToArray();
        }

        public ObjectId[] GetSelectionByClosed(bool closed, string layerName, string typeName)
        {
            List<ObjectId> ids = new List<ObjectId>();

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null || !MatchesBasicEntityFilters(entity, layerName, typeName))
                    {
                        continue;
                    }

                    if (IsClosedEntity(entity) == closed)
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.ToArray();
        }

        public ObjectId[] SelectWindow(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            PromptSelectionResult psr = _ed.SelectWindow(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2));
            if (psr.Status != PromptStatus.OK || psr.Value == null)
            {
                return new ObjectId[0];
            }
            return psr.Value.GetObjectIds();
        }

        public ObjectId[] SelectCrossingWindow(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            PromptSelectionResult psr = _ed.SelectCrossingWindow(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2));
            if (psr.Status != PromptStatus.OK || psr.Value == null)
            {
                return new ObjectId[0];
            }
            return psr.Value.GetObjectIds();
        }

        public ObjectId CopyEntity(ObjectId entityId, double dx, double dy, double dz)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity source = (Entity)tr.GetObject(entityId, OpenMode.ForRead);
                Entity clone = (Entity)source.Clone();

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                clone.TransformBy(Matrix3d.Displacement(new Vector3d(dx, dy, dz)));
                ObjectId id = btr.AppendEntity(clone);
                tr.AddNewlyCreatedDBObject(clone, true);
                tr.Commit();
                return id;
            }
        }

        public void MoveEntity(ObjectId entityId, double dx, double dy, double dz)
        {
            TransformEntity(entityId, Matrix3d.Displacement(new Vector3d(dx, dy, dz)));
        }

        public void RotateEntity(ObjectId entityId, double baseX, double baseY, double baseZ, double angleDegrees)
        {
            Matrix3d matrix = Matrix3d.Rotation(
                DegreesToRadians(angleDegrees),
                Vector3d.ZAxis,
                new Point3d(baseX, baseY, baseZ));
            TransformEntity(entityId, matrix);
        }

        public void ScaleEntity(ObjectId entityId, double baseX, double baseY, double baseZ, double scaleFactor)
        {
            Matrix3d matrix = Matrix3d.Scaling(scaleFactor, new Point3d(baseX, baseY, baseZ));
            TransformEntity(entityId, matrix);
        }

        public ObjectId MirrorEntity(ObjectId entityId, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity source = (Entity)tr.GetObject(entityId, OpenMode.ForRead);
                Entity clone = (Entity)source.Clone();

                Line3d axis = new Line3d(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2));
                clone.TransformBy(Matrix3d.Mirroring(axis));

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ObjectId newId = btr.AppendEntity(clone);
                tr.AddNewlyCreatedDBObject(clone, true);

                if (eraseSource)
                {
                    Entity writable = (Entity)tr.GetObject(entityId, OpenMode.ForWrite);
                    writable.Erase(true);
                }

                tr.Commit();
                return newId;
            }
        }

        public void EraseEntity(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = (Entity)tr.GetObject(entityId, OpenMode.ForWrite);
                entity.Erase(true);
                tr.Commit();
            }
        }

        public ObjectId[] ExplodeEntity(ObjectId entityId, bool eraseSource)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = (Entity)tr.GetObject(entityId, OpenMode.ForRead);
                DBObjectCollection exploded = new DBObjectCollection();
                entity.Explode(exploded);

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                List<ObjectId> created = new List<ObjectId>();
                foreach (DBObject dbo in exploded)
                {
                    Entity child = dbo as Entity;
                    if (child == null)
                    {
                        continue;
                    }

                    ObjectId id = btr.AppendEntity(child);
                    tr.AddNewlyCreatedDBObject(child, true);
                    created.Add(id);
                }

                if (eraseSource)
                {
                    Entity writable = (Entity)tr.GetObject(entityId, OpenMode.ForWrite);
                    writable.Erase(true);
                }

                tr.Commit();
                return created.ToArray();
            }
        }

        public ObjectId[] OffsetEntity(ObjectId entityId, double offsetDistance)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = (Entity)tr.GetObject(entityId, OpenMode.ForRead);
                Curve curve = entity as Curve;
                if (curve == null)
                {
                    throw new ArgumentException("L'entita non supporta offset: " + entity.GetType().Name);
                }

                DBObjectCollection offsetObjects = curve.GetOffsetCurves(offsetDistance);

                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                List<ObjectId> created = new List<ObjectId>();
                foreach (DBObject dbo in offsetObjects)
                {
                    Entity child = dbo as Entity;
                    if (child == null)
                    {
                        continue;
                    }

                    ObjectId id = btr.AppendEntity(child);
                    tr.AddNewlyCreatedDBObject(child, true);
                    created.Add(id);
                }

                tr.Commit();
                return created.ToArray();
            }
        }

        public void ChangeText(ObjectId entityId, string text)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(entityId, OpenMode.ForWrite);

                DBText dbText = dbo as DBText;
                if (dbText != null)
                {
                    dbText.TextString = text;
                    tr.Commit();
                    return;
                }

                MText mText = dbo as MText;
                if (mText != null)
                {
                    mText.Contents = text;
                    tr.Commit();
                    return;
                }

                AttributeReference attr = dbo as AttributeReference;
                if (attr != null)
                {
                    attr.TextString = text;
                    tr.Commit();
                    return;
                }

                throw new ArgumentException("L'entita non supporta testo modificabile");
            }
        }

        public int MoveEntities(IList entityIds, double dx, double dy, double dz)
        {
            return TransformEntities(entityIds, Matrix3d.Displacement(new Vector3d(dx, dy, dz)));
        }

        public int RotateEntities(IList entityIds, double baseX, double baseY, double baseZ, double angleDegrees)
        {
            Matrix3d matrix = Matrix3d.Rotation(
                DegreesToRadians(angleDegrees),
                Vector3d.ZAxis,
                new Point3d(baseX, baseY, baseZ));
            return TransformEntities(entityIds, matrix);
        }

        public int ScaleEntities(IList entityIds, double baseX, double baseY, double baseZ, double scaleFactor)
        {
            Matrix3d matrix = Matrix3d.Scaling(scaleFactor, new Point3d(baseX, baseY, baseZ));
            return TransformEntities(entityIds, matrix);
        }

        public ObjectId[] MirrorEntities(IList entityIds, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
        {
            List<ObjectId> mirrored = new List<ObjectId>();
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    mirrored.Add(MirrorEntity((ObjectId)raw, x1, y1, z1, x2, y2, z2, eraseSource));
                }
            }
            return mirrored.ToArray();
        }

        public int ExplodeEntities(IList entityIds, bool eraseSource)
        {
            int totalCreated = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    totalCreated += ExplodeEntity((ObjectId)raw, eraseSource).Length;
                }
            }
            return totalCreated;
        }

        public int OffsetEntities(IList entityIds, double offsetDistance)
        {
            int totalCreated = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    totalCreated += OffsetEntity((ObjectId)raw, offsetDistance).Length;
                }
            }
            return totalCreated;
        }

        public int ChangeTexts(IList entityIds, string text)
        {
            int changed = 0;
            foreach (object raw in entityIds)
            {
                if (raw is ObjectId)
                {
                    ChangeText((ObjectId)raw, text);
                    changed++;
                }
            }
            return changed;
        }

        public int EraseEntities(IList entityIds)
        {
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < entityIds.Count; i++)
                {
                    if (!(entityIds[i] is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)entityIds[i], OpenMode.ForWrite) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    entity.Erase(true);
                    changed++;
                }
                tr.Commit();
            }
            return changed;
        }

        private void TransformEntity(ObjectId entityId, Matrix3d matrix)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = (Entity)tr.GetObject(entityId, OpenMode.ForWrite);
                entity.TransformBy(matrix);
                tr.Commit();
            }
        }

        private int TransformEntities(IList entityIds, Matrix3d matrix)
        {
            int changed = 0;
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < entityIds.Count; i++)
                {
                    if (!(entityIds[i] is ObjectId))
                    {
                        continue;
                    }

                    Entity entity = tr.GetObject((ObjectId)entityIds[i], OpenMode.ForWrite) as Entity;
                    if (entity == null)
                    {
                        continue;
                    }

                    entity.TransformBy(matrix);
                    changed++;
                }
                tr.Commit();
            }
            return changed;
        }

        private static bool MatchesDxfFilters(Entity entity, Hashtable filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return true;
            }

            foreach (DictionaryEntry entry in filters)
            {
                if (!MatchesSingleDxfFilter(entity, Convert.ToInt32(entry.Key), entry.Value))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool MatchesSingleDxfFilter(Entity entity, int code, object rawValue)
        {
            ArrayList values = rawValue as ArrayList;
            if (values != null)
            {
                foreach (object value in values)
                {
                    if (MatchesSingleDxfFilter(entity, code, value))
                    {
                        return true;
                    }
                }
                return false;
            }

            switch (code)
            {
                case 0:
                    return MatchStringFilter(entity.GetRXClass().DxfName, rawValue);
                case 8:
                    return MatchStringFilter(entity.Layer, rawValue);
                case 62:
                    return MatchNumericFilter(entity.ColorIndex, rawValue);
                case 6:
                    return MatchStringFilter(entity.Linetype, rawValue);
                case 60:
                    return MatchNumericFilter(entity.Visible ? 0 : 1, rawValue);
                case 370:
                    return MatchNumericFilter((int)entity.LineWeight, rawValue);
                case 2:
                    {
                        BlockReference br = entity as BlockReference;
                        if (br != null)
                        {
                            return MatchStringFilter(br.Name, rawValue);
                        }

                        Hatch hatch = entity as Hatch;
                        if (hatch != null)
                        {
                            return MatchStringFilter(hatch.PatternName, rawValue);
                        }

                        AttributeDefinition attDef = entity as AttributeDefinition;
                        if (attDef != null)
                        {
                            return MatchStringFilter(attDef.Tag, rawValue);
                        }

                        AttributeReference attRef = entity as AttributeReference;
                        if (attRef != null)
                        {
                            return MatchStringFilter(attRef.Tag, rawValue);
                        }

                        return false;
                    }
                case 5:
                    return MatchStringFilter(entity.Handle.ToString(), rawValue);
                case 330:
                    return MatchStringFilter(GetOwnerHandleSafe(entity), rawValue);
                case 410:
                    return MatchStringFilter(GetLayoutNameSafe(entity), rawValue);
                case 1:
                    {
                        DBText dbText = entity as DBText;
                        if (dbText != null)
                        {
                            return MatchStringFilter(dbText.TextString, rawValue);
                        }

                        MText mText = entity as MText;
                        if (mText != null)
                        {
                            return MatchStringFilter(mText.Contents, rawValue);
                        }

                        AttributeReference attr = entity as AttributeReference;
                        if (attr != null)
                        {
                            return MatchStringFilter(attr.TextString, rawValue);
                        }

                        return false;
                    }
                case 7:
                    {
                        DBText dbText = entity as DBText;
                        if (dbText != null)
                        {
                            return MatchStringFilter(GetTextStyleNameSafe(dbText.TextStyleId), rawValue);
                        }

                        MText mText = entity as MText;
                        if (mText != null)
                        {
                            return MatchStringFilter(GetTextStyleNameSafe(mText.TextStyleId), rawValue);
                        }

                        AttributeDefinition attDef = entity as AttributeDefinition;
                        if (attDef != null)
                        {
                            return MatchStringFilter(GetTextStyleNameSafe(attDef.TextStyleId), rawValue);
                        }

                        AttributeReference attRef = entity as AttributeReference;
                        if (attRef != null)
                        {
                            return MatchStringFilter(GetTextStyleNameSafe(attRef.TextStyleId), rawValue);
                        }

                        return false;
                    }
                case 66:
                    {
                        BlockReference br = entity as BlockReference;
                        if (br == null)
                        {
                            return false;
                        }
                        bool hasAttributes = br.AttributeCollection.Count > 0;
                        return MatchNumericFilter(hasAttributes ? 1 : 0, rawValue);
                    }
                case 67:
                    return MatchNumericFilter(GetSpaceFlagSafe(entity), rawValue);
                case 3:
                    {
                        AttributeDefinition attDef = entity as AttributeDefinition;
                        if (attDef != null)
                        {
                            return MatchStringFilter(attDef.Prompt, rawValue);
                        }
                        return false;
                    }
                case 10:
                case 20:
                case 30:
                case 11:
                case 21:
                case 31:
                case 210:
                case 220:
                case 230:
                case 52:
                    return MatchEntityNumericValue(entity, code, rawValue);
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 48:
                case 50:
                case 51:
                case 70:
                case 71:
                case 72:
                case 73:
                case 75:
                case 76:
                case 77:
                case 90:
                    return MatchEntityNumericValue(entity, code, rawValue);
                default:
                    return true;
            }
        }

        private static bool MatchEntityNumericValue(Entity entity, int code, object rawValue)
        {
            double actual;
            if (!TryGetEntityNumericValue(entity, code, out actual))
            {
                return false;
            }
            return MatchNumericFilter(actual, rawValue);
        }

        private static object GetDebugActualDxfValue(Entity entity, int code)
        {
            switch (code)
            {
                case 0:
                    return entity.GetRXClass().DxfName;
                case 8:
                    return entity.Layer;
                case 62:
                    return entity.ColorIndex;
                case 6:
                    return entity.Linetype;
                case 60:
                    return entity.Visible ? 0 : 1;
                case 370:
                    return (int)entity.LineWeight;
                case 5:
                    return entity.Handle.ToString();
                case 330:
                    return GetOwnerHandleSafe(entity);
                case 410:
                    return GetLayoutNameSafe(entity);
                case 67:
                    return GetSpaceFlagSafe(entity);
                case 2:
                case 1:
                case 3:
                case 7:
                    return GetDebugStringDxfValue(entity, code);
                default:
                    double numeric;
                    if (TryGetEntityNumericValue(entity, code, out numeric))
                    {
                        return numeric;
                    }
                    return null;
            }
        }

        private static object GetDebugStringDxfValue(Entity entity, int code)
        {
            switch (code)
            {
                case 2:
                    {
                        BlockReference br = entity as BlockReference;
                        if (br != null) return br.Name;
                        Hatch hatch = entity as Hatch;
                        if (hatch != null) return hatch.PatternName;
                        AttributeDefinition attDef = entity as AttributeDefinition;
                        if (attDef != null) return attDef.Tag;
                        AttributeReference attRef = entity as AttributeReference;
                        if (attRef != null) return attRef.Tag;
                        return null;
                    }
                case 1:
                    {
                        DBText dbText = entity as DBText;
                        if (dbText != null) return dbText.TextString;
                        MText mText = entity as MText;
                        if (mText != null) return mText.Contents;
                        AttributeReference attr = entity as AttributeReference;
                        if (attr != null) return attr.TextString;
                        return null;
                    }
                case 3:
                    {
                        AttributeDefinition attDef = entity as AttributeDefinition;
                        return attDef != null ? attDef.Prompt : null;
                    }
                case 7:
                    {
                        DBText dbText = entity as DBText;
                        if (dbText != null) return GetTextStyleNameSafe(dbText.TextStyleId);
                        MText mText = entity as MText;
                        if (mText != null) return GetTextStyleNameSafe(mText.TextStyleId);
                        AttributeDefinition attDef = entity as AttributeDefinition;
                        if (attDef != null) return GetTextStyleNameSafe(attDef.TextStyleId);
                        AttributeReference attRef = entity as AttributeReference;
                        if (attRef != null) return GetTextStyleNameSafe(attRef.TextStyleId);
                        return null;
                    }
                default:
                    return null;
            }
        }

        private static bool TryGetEntityNumericValue(Entity entity, int code, out double value)
        {
            value = 0.0;
            switch (code)
            {
                case 10:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 20:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 30:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 11:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 21:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 31:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 210:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 220:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 230:
                    return TryGetEntityCoordinateValue(entity, code, out value);
                case 38:
                    if (entity is Polyline) { value = ((Polyline)entity).Elevation; return true; }
                    return false;
                case 39:
                    if (entity is Line) { value = ((Line)entity).Thickness; return true; }
                    if (entity is Circle) { value = ((Circle)entity).Thickness; return true; }
                    if (entity is Arc) { value = ((Arc)entity).Thickness; return true; }
                    if (entity is DBPoint) { value = ((DBPoint)entity).Thickness; return true; }
                    if (entity is DBText) { value = ((DBText)entity).Thickness; return true; }
                    if (entity is AttributeDefinition) { value = ((AttributeDefinition)entity).Thickness; return true; }
                    if (entity is AttributeReference) { value = ((AttributeReference)entity).Thickness; return true; }
                    if (entity is Polyline) { value = ((Polyline)entity).Thickness; return true; }
                    return false;
                case 40:
                    if (entity is Circle) { value = ((Circle)entity).Radius; return true; }
                    if (entity is Arc) { value = ((Arc)entity).Radius; return true; }
                    if (entity is DBText) { value = ((DBText)entity).Height; return true; }
                    if (entity is AttributeDefinition) { value = ((AttributeDefinition)entity).Height; return true; }
                    if (entity is AttributeReference) { value = ((AttributeReference)entity).Height; return true; }
                    if (entity is MText) { value = ((MText)entity).TextHeight; return true; }
                    if (entity is Ellipse) { value = ((Ellipse)entity).RadiusRatio; return true; }
                    return false;
                case 41:
                    if (entity is DBText) { value = GetOptionalDoublePropertySel(entity, "WidthFactor"); return !double.IsNaN(value); }
                    if (entity is AttributeDefinition) { value = GetOptionalDoublePropertySel(entity, "WidthFactor"); return !double.IsNaN(value); }
                    if (entity is AttributeReference) { value = GetOptionalDoublePropertySel(entity, "WidthFactor"); return !double.IsNaN(value); }
                    if (entity is MText) { value = ((MText)entity).Width; return true; }
                    if (entity is Ellipse) { value = ((Ellipse)entity).StartAngle; return true; }
                    if (entity is BlockReference) { value = ((BlockReference)entity).ScaleFactors.X; return true; }
                    if (entity is Hatch) { value = ((Hatch)entity).PatternScale; return true; }
                    return false;
                case 42:
                    if (entity is Ellipse) { value = ((Ellipse)entity).EndAngle; return true; }
                    if (entity is BlockReference) { value = ((BlockReference)entity).ScaleFactors.Y; return true; }
                    return false;
                case 43:
                    if (entity is Polyline) { value = ((Polyline)entity).ConstantWidth; return true; }
                    if (entity is BlockReference) { value = ((BlockReference)entity).ScaleFactors.Z; return true; }
                    return false;
                case 44:
                    if (entity is MText) { value = GetOptionalDoublePropertySel(entity, "LineSpacingFactor"); return !double.IsNaN(value); }
                    return false;
                case 48:
                    value = entity.LinetypeScale;
                    return true;
                case 67:
                    value = GetSpaceFlagSafe(entity);
                    return true;
                case 50:
                    if (entity is DBText) { value = RadiansToDegrees(((DBText)entity).Rotation); return true; }
                    if (entity is AttributeDefinition) { value = RadiansToDegrees(((AttributeDefinition)entity).Rotation); return true; }
                    if (entity is AttributeReference) { value = RadiansToDegrees(((AttributeReference)entity).Rotation); return true; }
                    if (entity is MText) { value = RadiansToDegrees(((MText)entity).Rotation); return true; }
                    if (entity is Arc) { value = RadiansToDegrees(((Arc)entity).StartAngle); return true; }
                    if (entity is BlockReference) { value = RadiansToDegrees(((BlockReference)entity).Rotation); return true; }
                    if (entity is Hatch) { value = RadiansToDegrees(((Hatch)entity).PatternAngle); return true; }
                    return false;
                case 52:
                    if (entity is Hatch) { value = RadiansToDegrees(((Hatch)entity).PatternAngle); return true; }
                    return false;
                case 51:
                    if (entity is Arc) { value = RadiansToDegrees(((Arc)entity).EndAngle); return true; }
                    if (entity is DBText) { value = GetOptionalAnglePropertySel(entity, "Oblique"); return !double.IsNaN(value); }
                    if (entity is AttributeDefinition) { value = GetOptionalAnglePropertySel(entity, "Oblique"); return !double.IsNaN(value); }
                    if (entity is AttributeReference) { value = GetOptionalAnglePropertySel(entity, "Oblique"); return !double.IsNaN(value); }
                    return false;
                case 70:
                    if (entity is Polyline) { value = ((Polyline)entity).Closed ? 1 : 0; return true; }
                    if (entity is Hatch) { value = ((Hatch)entity).Associative ? 1 : 0; return true; }
                    if (entity is AttributeDefinition)
                    {
                        AttributeDefinition def = (AttributeDefinition)entity;
                        value = BuildAttributeFlags(def.Invisible, def.Constant, def.Verifiable);
                        return true;
                    }
                    if (entity is AttributeReference)
                    {
                        value = ((AttributeReference)entity).Invisible ? 1 : 0;
                        return true;
                    }
                    return false;
                case 71:
                    if (entity is DBText) { value = GetTextGenerationFlags(entity); return true; }
                    if (entity is AttributeDefinition) { value = GetTextGenerationFlags(entity); return true; }
                    if (entity is AttributeReference) { value = GetTextGenerationFlags(entity); return true; }
                    if (entity is MText) { value = GetOptionalEnumIntPropertySel(entity, "Attachment"); return !double.IsNaN(value); }
                    if (entity is Leader) { value = GetLeaderBoolProperty((Leader)entity, "HasArrowHead") ? 1 : 0; return true; }
                    return false;
                case 72:
                    if (entity is DBText) { value = GetOptionalEnumIntPropertySel(entity, "HorizontalMode"); return !double.IsNaN(value); }
                    if (entity is AttributeDefinition) { value = GetOptionalEnumIntPropertySel(entity, "HorizontalMode"); return !double.IsNaN(value); }
                    if (entity is AttributeReference) { value = GetOptionalEnumIntPropertySel(entity, "HorizontalMode"); return !double.IsNaN(value); }
                    return false;
                case 73:
                    if (entity is MText) { value = GetOptionalEnumIntPropertySel(entity, "LineSpacingStyle"); return !double.IsNaN(value); }
                    if (entity is DBText) { value = GetOptionalEnumIntPropertySel(entity, "VerticalMode"); return !double.IsNaN(value); }
                    if (entity is AttributeDefinition) { value = GetOptionalEnumIntPropertySel(entity, "VerticalMode"); return !double.IsNaN(value); }
                    if (entity is AttributeReference) { value = GetOptionalEnumIntPropertySel(entity, "VerticalMode"); return !double.IsNaN(value); }
                    return false;
                case 75:
                    if (entity is Hatch) { value = GetOptionalEnumIntPropertySel(entity, "HatchStyle"); return !double.IsNaN(value); }
                    return false;
                case 76:
                    if (entity is Hatch) { value = (int)((Hatch)entity).PatternType; return true; }
                    if (entity is Leader) { value = GetLeaderVertexCount((Leader)entity); return true; }
                    return false;
                case 77:
                    if (entity is Hatch) { value = GetOptionalBoolPropertySel(entity, "PatternDouble") ? 1 : 0; return true; }
                    return false;
                case 90:
                    if (entity is Polyline) { value = ((Polyline)entity).NumberOfVertices; return true; }
                    return false;
                default:
                    return false;
            }
        }

        private static bool TryGetEntityCoordinateValue(Entity entity, int code, out double value)
        {
            value = 0.0;
            try
            {
                Hashtable map = NormalizeDxfPairs(BuildDxfPairs(entity));
                if (!map.ContainsKey(code))
                {
                    return false;
                }

                object raw = map[code];
                ArrayList arr = raw as ArrayList;
                if (arr != null)
                {
                    if (arr.Count == 0)
                    {
                        return false;
                    }
                    raw = arr[0];
                }

                value = Convert.ToDouble(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetTextStyleNameSafe(ObjectId textStyleId)
        {
            try
            {
                DBObject dbo = textStyleId.GetObject(OpenMode.ForRead);
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

        private static string GetOwnerHandleSafe(Entity entity)
        {
            try
            {
                if (entity == null || entity.OwnerId.IsNull)
                {
                    return string.Empty;
                }

                DBObject owner = entity.OwnerId.GetObject(OpenMode.ForRead);
                return owner == null ? string.Empty : owner.Handle.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetLayoutNameSafe(Entity entity)
        {
            BlockTableRecord btr = GetContainingSpaceRecordSafe(entity);
            if (btr == null || !btr.IsLayout || btr.LayoutId.IsNull)
            {
                return string.Empty;
            }

            try
            {
                Layout layout = btr.LayoutId.GetObject(OpenMode.ForRead) as Layout;
                return layout == null ? string.Empty : layout.LayoutName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static double GetSpaceFlagSafe(Entity entity)
        {
            BlockTableRecord btr = GetContainingSpaceRecordSafe(entity);
            if (btr == null)
            {
                return 0.0;
            }

            try
            {
                return btr.Name == BlockTableRecord.PaperSpace ? 1.0 : 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        private static BlockTableRecord GetContainingSpaceRecordSafe(DBObject dbo)
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

        private static double GetOptionalDoublePropertySel(object instance, string propertyName)
        {
            try
            {
                var prop = instance.GetType().GetProperty(propertyName);
                if (prop == null)
                {
                    return double.NaN;
                }
                object value = prop.GetValue(instance, null);
                return value == null ? double.NaN : Convert.ToDouble(value);
            }
            catch
            {
                return double.NaN;
            }
        }

        private static double GetOptionalAnglePropertySel(object instance, string propertyName)
        {
            double radians = GetOptionalDoublePropertySel(instance, propertyName);
            return double.IsNaN(radians) ? double.NaN : RadiansToDegrees(radians);
        }

        private static double GetOptionalEnumIntPropertySel(object instance, string propertyName)
        {
            try
            {
                var prop = instance.GetType().GetProperty(propertyName);
                if (prop == null)
                {
                    return double.NaN;
                }
                object value = prop.GetValue(instance, null);
                return value == null ? double.NaN : Convert.ToInt32(value);
            }
            catch
            {
                return double.NaN;
            }
        }

        private static double GetTextGenerationFlags(object instance)
        {
            int flags = 0;
            try
            {
                object mirroredX = GetRuntimePropertyValueSel(instance, "IsMirroredInX");
                object mirroredY = GetRuntimePropertyValueSel(instance, "IsMirroredInY");
                if (mirroredX is bool && (bool)mirroredX)
                {
                    flags |= 2;
                }
                if (mirroredY is bool && (bool)mirroredY)
                {
                    flags |= 4;
                }
            }
            catch
            {
            }
            return flags;
        }

        private static object GetRuntimePropertyValueSel(object instance, string propertyName)
        {
            if (instance == null)
            {
                return null;
            }

            try
            {
                var prop = instance.GetType().GetProperty(propertyName);
                return prop == null ? null : prop.GetValue(instance, null);
            }
            catch
            {
                return null;
            }
        }

        private static bool GetOptionalBoolPropertySel(object instance, string propertyName)
        {
            object value = GetRuntimePropertyValueSel(instance, propertyName);
            return value is bool && (bool)value;
        }

        private static bool MatchStringFilter(string actual, object filter)
        {
            string expected = Convert.ToString(filter) ?? string.Empty;
            actual = actual ?? string.Empty;

            if (expected.IndexOf('*') >= 0 || expected.IndexOf('?') >= 0)
            {
                return WildcardMatch(actual, expected);
            }

            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchNumericFilter(double actual, object filter)
        {
            string text = Convert.ToString(filter);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();
            if (text.StartsWith(">="))
            {
                return actual >= ParseFilterDouble(text.Substring(2));
            }
            if (text.StartsWith("<="))
            {
                return actual <= ParseFilterDouble(text.Substring(2));
            }
            if (text.StartsWith(">"))
            {
                return actual > ParseFilterDouble(text.Substring(1));
            }
            if (text.StartsWith("<"))
            {
                return actual < ParseFilterDouble(text.Substring(1));
            }

            double expected = ParseFilterDouble(text);
            double delta = Math.Abs(actual - expected);
            double absTolerance = 0.0001;
            double relTolerance = Math.Max(Math.Abs(actual), Math.Abs(expected)) * 0.000001;
            double tolerance = Math.Max(absTolerance, relTolerance);
            return delta <= tolerance;
        }

        private static double ParseFilterDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0.0;
            }

            string value = text.Trim();
            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out double current))
            {
                return current;
            }

            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double invariant))
            {
                return invariant;
            }

            string swapped = value.IndexOf(',') >= 0 && value.IndexOf('.') < 0
                ? value.Replace(',', '.')
                : value.IndexOf('.') >= 0 && value.IndexOf(',') < 0
                    ? value.Replace('.', ',')
                    : value;

            if (double.TryParse(swapped, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double swappedInvariant))
            {
                return swappedInvariant;
            }

            return Convert.ToDouble(value, CultureInfo.CurrentCulture);
        }

        private static bool MatchesBasicEntityFilters(Entity entity, string layerName, string typeName)
        {
            if (!string.IsNullOrWhiteSpace(layerName) &&
                !string.Equals(entity.Layer, layerName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(typeName))
            {
                return true;
            }

            string runtimeType = entity.GetType().Name;
            string dxfType = entity.GetRXClass().DxfName;
            return string.Equals(runtimeType, typeName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dxfType, typeName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValueInRange(double value, double minValue, double maxValue)
        {
            if (value < minValue)
            {
                return false;
            }

            if (maxValue >= minValue && value > maxValue)
            {
                return false;
            }

            return true;
        }

        private static bool WildcardMatch(string input, string pattern)
        {
            string regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(
                input ?? string.Empty,
                regex,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
