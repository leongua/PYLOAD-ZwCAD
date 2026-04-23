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
        public ObjectId EntMake(IList dxfPairs)
        {
            Hashtable map = NormalizeDxfPairs(dxfPairs);
            string dxf = Convert.ToString(map[0], CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(dxf)) throw new ArgumentException("EntMake richiede il codice DXF 0");
            dxf = dxf.ToUpperInvariant();

            Entity entity;
            if (dxf == "LINE")
            {
                entity = new Line(
                    new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30)),
                    new Point3d(GetDouble(map, 11), GetDouble(map, 21), GetDouble(map, 31)));
            }
            else if (dxf == "CIRCLE")
            {
                entity = new Circle(
                    new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30)),
                    Vector3d.ZAxis,
                    GetDouble(map, 40));
            }
            else if (dxf == "ARC")
            {
                entity = new Arc(
                    new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30)),
                    GetDouble(map, 40),
                    DegToRad(GetDouble(map, 50, 0.0)),
                    DegToRad(GetDouble(map, 51, 180.0)));
            }
            else if (dxf == "POINT")
            {
                entity = new DBPoint(new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30)));
            }
            else if (dxf == "TEXT")
            {
                DBText t = new DBText();
                t.Position = new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30));
                t.Height = GetDouble(map, 40, 2.5);
                t.TextString = GetString(map, 1, string.Empty);
                if (map.ContainsKey(41)) t.WidthFactor = GetDouble(map, 41, 1.0);
                if (map.ContainsKey(50)) t.Rotation = DegToRad(GetDouble(map, 50, 0.0));
                if (map.ContainsKey(51)) t.Oblique = DegToRad(GetDouble(map, 51, 0.0));
                entity = t;
            }
            else if (dxf == "ELLIPSE")
            {
                entity = new Ellipse(
                    new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30)),
                    Vector3d.ZAxis,
                    new Vector3d(GetDouble(map, 11), GetDouble(map, 21), GetDouble(map, 31)),
                    GetDouble(map, 40, 0.5),
                    GetDouble(map, 41, 0.0),
                    GetDouble(map, 42, Math.PI * 2.0));
            }
            else
            {
                throw new NotSupportedException("EntMake 2026R non supporta ancora " + dxf);
            }

            ApplyCommonDxfProperties(entity, map);
            return AddEntity(entity);
        }

        public void SetEntityDxfValue(ObjectId entityId, int code, object value)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null) throw new ArgumentException("entityId non valido");

                if (code == 8)
                {
                    string layer = Convert.ToString(value, CultureInfo.InvariantCulture);
                    EnsureLayerExists(layer);
                    entity.Layer = layer;
                }
                else if (code == 39)
                {
                    if (!TrySetThickness(entity, Convert.ToDouble(value, CultureInfo.InvariantCulture)))
                    {
                        throw new NotSupportedException("Thickness non supportato per " + entity.GetType().Name);
                    }
                }
                else if (code == 62)
                {
                    entity.ColorIndex = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                }
                else if (entity is Circle && code == 40)
                {
                    ((Circle)entity).Radius = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }
                else if (entity is DBText && code == 1)
                {
                    ((DBText)entity).TextString = Convert.ToString(value, CultureInfo.InvariantCulture);
                }
                else if (entity is DBText && code == 41)
                {
                    ((DBText)entity).WidthFactor = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }
                else if (entity is DBText && code == 50)
                {
                    ((DBText)entity).Rotation = DegToRad(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else if (entity is DBText && code == 51)
                {
                    ((DBText)entity).Oblique = DegToRad(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else if (entity is Arc && code == 50)
                {
                    ((Arc)entity).StartAngle = DegToRad(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else if (entity is Arc && code == 51)
                {
                    ((Arc)entity).EndAngle = DegToRad(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else if (entity is Line && (code == 10 || code == 20 || code == 30 || code == 11 || code == 21 || code == 31))
                {
                    SetLineCoordinate((Line)entity, code, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else if ((entity is Circle || entity is Arc || entity is DBPoint || entity is DBText || entity is Ellipse) &&
                         (code == 10 || code == 20 || code == 30))
                {
                    SetCenterLikeCoordinate(entity, code, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                }
                else
                {
                    throw new NotSupportedException("SetEntityDxfValue non supporta ancora il codice " + code);
                }

                tr.Commit();
            }
        }

        public object GetEntityDxfValue(ObjectId entityId, int code)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null) return null;
                return GetEntityDxfValueInternal(entity, code, tr);
            }
        }

        public ObjectId[] GetSelectionByDxf(IList dxfFilters)
        {
            Hashtable filters = NormalizeDxfPairs(dxfFilters);
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(_db);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    Entity entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity != null && MatchesFilters(entity, filters, tr)) ids.Add(id);
                }
            }
            return ids.ToArray();
        }

        public Hashtable DebugDxfFilterMatch(ObjectId entityId, IList dxfFilters)
        {
            Hashtable result = NewInfo();
            Hashtable details = NewInfo();
            result["details"] = details;
            result["matched"] = false;

            Hashtable filters = NormalizeDxfPairs(dxfFilters);
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null) return result;

                bool all = true;
                foreach (DictionaryEntry de in filters)
                {
                    int code = Convert.ToInt32(de.Key, CultureInfo.InvariantCulture);
                    object expected = de.Value;
                    object actual = GetEntityDxfValueInternal(entity, code, tr);
                    bool matched = CompareDxfValue(actual, expected);
                    if (!matched) all = false;

                    Hashtable item = NewInfo();
                    item["expected"] = expected;
                    item["actual"] = actual;
                    item["matched"] = matched;
                    details[code.ToString(CultureInfo.InvariantCulture)] = item;
                }

                result["matched"] = all;
                result["entity_type"] = entity.GetRXClass().DxfName;
                result["entity_id"] = entityId.ToString();
            }
            return result;
        }

        private static Hashtable NormalizeDxfPairs(IList pairs)
        {
            Hashtable ht = new Hashtable();
            if (pairs == null) return ht;
            foreach (object raw in pairs)
            {
                Hashtable item = raw as Hashtable;
                if (item == null || !item.ContainsKey("code")) continue;
                ht[Convert.ToInt32(item["code"], CultureInfo.InvariantCulture)] = item.ContainsKey("value") ? item["value"] : null;
            }
            return ht;
        }

        private void ApplyCommonDxfProperties(Entity entity, Hashtable map)
        {
            if (map.ContainsKey(8))
            {
                string layer = GetString(map, 8, entity.Layer);
                EnsureLayerExists(layer);
                entity.Layer = layer;
            }
            if (map.ContainsKey(39))
            {
                TrySetThickness(entity, GetDouble(map, 39, 0.0));
            }
            if (map.ContainsKey(62)) entity.ColorIndex = Convert.ToInt16(map[62], CultureInfo.InvariantCulture);
            if (map.ContainsKey(210) || map.ContainsKey(220) || map.ContainsKey(230))
            {
                Vector3d n = new Vector3d(GetDouble(map, 210, 0.0), GetDouble(map, 220, 0.0), GetDouble(map, 230, 1.0));
                TrySetNormal(entity, n);
            }
        }

        private bool MatchesFilters(Entity entity, Hashtable filters, Transaction tr)
        {
            foreach (DictionaryEntry de in filters)
            {
                int code = Convert.ToInt32(de.Key, CultureInfo.InvariantCulture);
                object expected = de.Value;
                object actual = GetEntityDxfValueInternal(entity, code, tr);
                if (!CompareDxfValue(actual, expected)) return false;
            }
            return true;
        }

        private static bool CompareDxfValue(object actual, object expected)
        {
            if (actual == null && expected == null) return true;
            if (actual == null || expected == null) return false;

            string expectedText = Convert.ToString(expected, CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(expectedText) && expectedText.StartsWith(">=", StringComparison.Ordinal))
            {
                return ToDouble(actual) >= ParseDoubleFlexible(expectedText.Substring(2));
            }
            if (!string.IsNullOrWhiteSpace(expectedText) && expectedText.StartsWith("<=", StringComparison.Ordinal))
            {
                return ToDouble(actual) <= ParseDoubleFlexible(expectedText.Substring(2));
            }
            if (!string.IsNullOrWhiteSpace(expectedText) && expectedText.StartsWith(">", StringComparison.Ordinal))
            {
                return ToDouble(actual) > ParseDoubleFlexible(expectedText.Substring(1));
            }
            if (!string.IsNullOrWhiteSpace(expectedText) && expectedText.StartsWith("<", StringComparison.Ordinal))
            {
                return ToDouble(actual) < ParseDoubleFlexible(expectedText.Substring(1));
            }

            if (IsNumeric(actual) || IsNumeric(expected))
            {
                double a = ToDouble(actual);
                double e = ToDouble(expected);
                double delta = Math.Abs(a - e);
                return delta <= 0.0001 + Math.Max(Math.Abs(a), Math.Abs(e)) * 0.000001;
            }

            return string.Equals(
                Convert.ToString(actual, CultureInfo.InvariantCulture),
                Convert.ToString(expected, CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }

        private object GetEntityDxfValueInternal(Entity entity, int code, Transaction tr)
        {
            switch (code)
            {
                case 0: return entity.GetRXClass().DxfName;
                case 1:
                    if (entity is DBText) return ((DBText)entity).TextString;
                    if (entity is MText) return ((MText)entity).Contents;
                    break;
                case 8: return entity.Layer;
                case 10:
                case 20:
                case 30:
                case 11:
                case 21:
                case 31:
                    return GetCoordinate(entity, code);
                case 39:
                    double thickness;
                    return TryGetThickness(entity, out thickness) ? (object)thickness : null;
                case 40:
                    if (entity is Circle) return ((Circle)entity).Radius;
                    if (entity is DBText) return ((DBText)entity).Height;
                    if (entity is Ellipse) return ((Ellipse)entity).RadiusRatio;
                    if (entity is Arc) return ((Arc)entity).Radius;
                    break;
                case 41:
                    if (entity is DBText) return ((DBText)entity).WidthFactor;
                    if (entity is Polyline) return ((Polyline)entity).ConstantWidth;
                    break;
                case 50:
                    if (entity is Arc) return RadToDeg(((Arc)entity).StartAngle);
                    if (entity is DBText) return RadToDeg(((DBText)entity).Rotation);
                    break;
                case 51:
                    if (entity is Arc) return RadToDeg(((Arc)entity).EndAngle);
                    if (entity is DBText) return RadToDeg(((DBText)entity).Oblique);
                    break;
                case 62:
                    return entity.ColorIndex;
                case 67:
                    return IsEntityInPaperSpace(entity, tr) ? 1 : 0;
                case 210:
                    Vector3d n;
                    return TryGetNormal(entity, out n) ? (object)n.X : null;
                case 220:
                    Vector3d n2;
                    return TryGetNormal(entity, out n2) ? (object)n2.Y : null;
                case 230:
                    Vector3d n3;
                    return TryGetNormal(entity, out n3) ? (object)n3.Z : null;
                case 330:
                    return GetOwnerHandle(entity, tr);
                case 410:
                    return GetEntityLayoutName(entity, tr);
            }
            return null;
        }

        private static object GetCoordinate(Entity entity, int code)
        {
            if (entity is Line)
            {
                Line line = (Line)entity;
                Point3d pt = (code == 11 || code == 21 || code == 31) ? line.EndPoint : line.StartPoint;
                if (code == 10 || code == 11) return pt.X;
                if (code == 20 || code == 21) return pt.Y;
                if (code == 30 || code == 31) return pt.Z;
            }

            Point3d center;
            if (entity is Circle) center = ((Circle)entity).Center;
            else if (entity is Arc) center = ((Arc)entity).Center;
            else if (entity is DBText) center = ((DBText)entity).Position;
            else if (entity is DBPoint) center = ((DBPoint)entity).Position;
            else if (entity is Ellipse) center = ((Ellipse)entity).Center;
            else if (entity is MText) center = ((MText)entity).Location;
            else return null;

            if (code == 10) return center.X;
            if (code == 20) return center.Y;
            if (code == 30) return center.Z;
            return null;
        }

        private static void SetCenterLikeCoordinate(Entity entity, int code, double value)
        {
            if (entity is Circle)
            {
                Circle c = (Circle)entity;
                c.Center = SetCoordinate(c.Center, code, value);
                return;
            }
            if (entity is Arc)
            {
                Arc a = (Arc)entity;
                a.Center = SetCoordinate(a.Center, code, value);
                return;
            }
            if (entity is DBPoint)
            {
                DBPoint p = (DBPoint)entity;
                p.Position = SetCoordinate(p.Position, code, value);
                return;
            }
            if (entity is DBText)
            {
                DBText t = (DBText)entity;
                t.Position = SetCoordinate(t.Position, code, value);
                return;
            }
            if (entity is Ellipse)
            {
                Ellipse e = (Ellipse)entity;
                e.Center = SetCoordinate(e.Center, code, value);
            }
        }

        private static void SetLineCoordinate(Line line, int code, double value)
        {
            if (code == 10 || code == 20 || code == 30)
            {
                line.StartPoint = SetCoordinate(line.StartPoint, code, value);
            }
            else
            {
                line.EndPoint = SetCoordinate(line.EndPoint, code - 1, value);
            }
        }

        private static Point3d SetCoordinate(Point3d p, int code, double value)
        {
            if (code == 10) return new Point3d(value, p.Y, p.Z);
            if (code == 20) return new Point3d(p.X, value, p.Z);
            if (code == 30) return new Point3d(p.X, p.Y, value);
            return p;
        }

        private static bool IsEntityInPaperSpace(Entity entity, Transaction tr)
        {
            BlockTableRecord owner = tr.GetObject(entity.OwnerId, OpenMode.ForRead) as BlockTableRecord;
            if (owner == null) return false;
            return owner.IsLayout && !string.Equals(owner.Name, BlockTableRecord.ModelSpace, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetEntityLayoutName(Entity entity, Transaction tr)
        {
            BlockTableRecord owner = tr.GetObject(entity.OwnerId, OpenMode.ForRead) as BlockTableRecord;
            if (owner == null) return string.Empty;
            return owner.Name;
        }

        private static string GetOwnerHandle(Entity entity, Transaction tr)
        {
            DBObject owner = tr.GetObject(entity.OwnerId, OpenMode.ForRead);
            return GetHandleString(owner);
        }

        private static bool IsNumeric(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is decimal;
        }

        private static bool TryGetThickness(Entity entity, out double thickness)
        {
            thickness = 0.0;
            if (entity is Line) { thickness = ((Line)entity).Thickness; return true; }
            if (entity is Circle) { thickness = ((Circle)entity).Thickness; return true; }
            if (entity is Arc) { thickness = ((Arc)entity).Thickness; return true; }
            if (entity is DBPoint) { thickness = ((DBPoint)entity).Thickness; return true; }
            if (entity is Polyline) { thickness = ((Polyline)entity).Thickness; return true; }
            if (entity is DBText) { thickness = ((DBText)entity).Thickness; return true; }
            return false;
        }

        private static bool TrySetThickness(Entity entity, double thickness)
        {
            if (entity is Line) { ((Line)entity).Thickness = thickness; return true; }
            if (entity is Circle) { ((Circle)entity).Thickness = thickness; return true; }
            if (entity is Arc) { ((Arc)entity).Thickness = thickness; return true; }
            if (entity is DBPoint) { ((DBPoint)entity).Thickness = thickness; return true; }
            if (entity is Polyline) { ((Polyline)entity).Thickness = thickness; return true; }
            if (entity is DBText) { ((DBText)entity).Thickness = thickness; return true; }
            return false;
        }

        private static bool TryGetNormal(Entity entity, out Vector3d normal)
        {
            normal = Vector3d.ZAxis;
            if (entity is Line) { normal = ((Line)entity).Normal; return true; }
            if (entity is Circle) { normal = ((Circle)entity).Normal; return true; }
            if (entity is Arc) { normal = ((Arc)entity).Normal; return true; }
            if (entity is DBPoint) { normal = ((DBPoint)entity).Normal; return true; }
            if (entity is Polyline) { normal = ((Polyline)entity).Normal; return true; }
            if (entity is DBText) { normal = ((DBText)entity).Normal; return true; }
            if (entity is Ellipse) { normal = ((Ellipse)entity).Normal; return true; }
            return false;
        }

        private static bool TrySetNormal(Entity entity, Vector3d normal)
        {
            if (entity is Line) { ((Line)entity).Normal = normal; return true; }
            if (entity is Circle) { ((Circle)entity).Normal = normal; return true; }
            if (entity is Arc) { ((Arc)entity).Normal = normal; return true; }
            if (entity is DBPoint) { ((DBPoint)entity).Normal = normal; return true; }
            if (entity is Polyline) { ((Polyline)entity).Normal = normal; return true; }
            if (entity is DBText) { ((DBText)entity).Normal = normal; return true; }
            return false;
        }

        private static double ToDouble(object value)
        {
            if (value == null) return 0.0;
            if (value is double) return (double)value;
            if (value is float) return (float)value;
            if (value is decimal) return Convert.ToDouble((decimal)value, CultureInfo.InvariantCulture);
            if (value is IConvertible) return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return ParseDoubleFlexible(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private static double ParseDoubleFlexible(string text)
        {
            double value;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) return value;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return value;
            return double.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        private static double GetDouble(Hashtable map, int code, double fallback = 0.0) { return map.ContainsKey(code) ? Convert.ToDouble(map[code], CultureInfo.InvariantCulture) : fallback; }
        private static string GetString(Hashtable map, int code, string fallback) { return map.ContainsKey(code) ? Convert.ToString(map[code], CultureInfo.InvariantCulture) : fallback; }
    }
}
