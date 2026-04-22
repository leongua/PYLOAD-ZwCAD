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

            if (dxf == "CIRCLE")
            {
                Circle c = new Circle(new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30)), Vector3d.ZAxis, GetDouble(map, 40));
                if (map.ContainsKey(8))
                {
                    string layer = GetString(map, 8, c.Layer);
                    EnsureLayerExists(layer);
                    c.Layer = layer;
                }
                return AddEntity(c);
            }
            if (dxf == "TEXT")
            {
                DBText t = new DBText();
                t.Position = new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30));
                t.Height = GetDouble(map, 40, 2.5);
                t.TextString = GetString(map, 1, string.Empty);
                if (map.ContainsKey(41)) t.WidthFactor = GetDouble(map, 41, 1.0);
                if (map.ContainsKey(51)) t.Oblique = DegToRad(GetDouble(map, 51, 0.0));
                if (map.ContainsKey(8))
                {
                    string layer = GetString(map, 8, t.Layer);
                    EnsureLayerExists(layer);
                    t.Layer = layer;
                }
                return AddEntity(t);
            }
            if (dxf == "ELLIPSE")
            {
                Ellipse e = new Ellipse(
                    new Point3d(GetDouble(map, 10), GetDouble(map, 20), GetDouble(map, 30)),
                    Vector3d.ZAxis,
                    new Vector3d(GetDouble(map, 11), GetDouble(map, 21), GetDouble(map, 31)),
                    GetDouble(map, 40, 0.5),
                    GetDouble(map, 41, 0.0),
                    GetDouble(map, 42, Math.PI * 2.0));
                if (map.ContainsKey(8))
                {
                    string layer = GetString(map, 8, e.Layer);
                    EnsureLayerExists(layer);
                    e.Layer = layer;
                }
                return AddEntity(e);
            }

            throw new NotSupportedException("EntMake 2026R non supporta ancora " + dxf);
        }

        public void SetEntityDxfValue(ObjectId entityId, int code, object value)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (entity == null) throw new ArgumentException("entityId non valido");
                if (entity is Circle && code == 40) ((Circle)entity).Radius = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                else if (entity is DBText && code == 1) ((DBText)entity).TextString = Convert.ToString(value, CultureInfo.InvariantCulture);
                else throw new NotSupportedException("SetEntityDxfValue non supporta ancora il codice " + code);
                tr.Commit();
            }
        }

        public object GetEntityDxfValue(ObjectId entityId, int code)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (entity == null) return null;
                switch (code)
                {
                    case 0: return entity.GetRXClass().DxfName;
                    case 8: return entity.Layer;
                    case 10: return GetCoordinate(entity, 10);
                    case 20: return GetCoordinate(entity, 20);
                    case 30: return GetCoordinate(entity, 30);
                    case 40:
                        if (entity is Circle) return ((Circle)entity).Radius;
                        if (entity is DBText) return ((DBText)entity).Height;
                        if (entity is Ellipse) return ((Ellipse)entity).RadiusRatio;
                        break;
                    case 330:
                        return GetOwnerHandle(entity, tr);
                }
                return null;
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

        private bool MatchesFilters(Entity entity, Hashtable filters, Transaction tr)
        {
            foreach (DictionaryEntry de in filters)
            {
                int code = Convert.ToInt32(de.Key, CultureInfo.InvariantCulture);
                string expected = Convert.ToString(de.Value, CultureInfo.InvariantCulture);
                switch (code)
                {
                    case 0: if (!string.Equals(entity.GetRXClass().DxfName, expected, StringComparison.OrdinalIgnoreCase)) return false; break;
                    case 8: if (!string.Equals(entity.Layer, expected, StringComparison.OrdinalIgnoreCase)) return false; break;
                    case 10:
                    case 20:
                    case 40:
                        if (!MatchNumeric(Convert.ToDouble(GetEntityDxfValue(entity.ObjectId, code), CultureInfo.InvariantCulture), expected)) return false;
                        break;
                    case 330:
                        if (!string.Equals(GetOwnerHandle(entity, tr), expected, StringComparison.OrdinalIgnoreCase)) return false;
                        break;
                }
            }
            return true;
        }

        private static bool MatchNumeric(double actual, string filter)
        {
            if (filter.StartsWith(">=")) return actual >= ParseDoubleFlexible(filter.Substring(2));
            if (filter.StartsWith("<=")) return actual <= ParseDoubleFlexible(filter.Substring(2));
            if (filter.StartsWith(">")) return actual > ParseDoubleFlexible(filter.Substring(1));
            if (filter.StartsWith("<")) return actual < ParseDoubleFlexible(filter.Substring(1));
            double expected = ParseDoubleFlexible(filter);
            double delta = Math.Abs(actual - expected);
            return delta <= 0.0001 + Math.Max(Math.Abs(actual), Math.Abs(expected)) * 0.000001;
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

        private static object GetCoordinate(Entity entity, int code)
        {
            Point3d pt;
            if (entity is Circle) pt = ((Circle)entity).Center;
            else if (entity is Arc) pt = ((Arc)entity).Center;
            else if (entity is DBText) pt = ((DBText)entity).Position;
            else if (entity is DBPoint) pt = ((DBPoint)entity).Position;
            else if (entity is Ellipse) pt = ((Ellipse)entity).Center;
            else return null;
            if (code == 10) return pt.X;
            if (code == 20) return pt.Y;
            if (code == 30) return pt.Z;
            return null;
        }

        private static string GetOwnerHandle(Entity entity, Transaction tr)
        {
            DBObject owner = tr.GetObject(entity.OwnerId, OpenMode.ForRead);
            return GetHandleString(owner);
        }
    }
}
