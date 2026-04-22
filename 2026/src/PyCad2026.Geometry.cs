using System;
using System.Collections;
using System.Globalization;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD2026R
{
    public partial class PyCad2026
    {
        public ObjectId AddLine(double x1, double y1, double z1, double x2, double y2, double z2) { return AddEntity(new Line(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2))); }
        public ObjectId AddCircle(double x, double y, double z, double radius) { return AddEntity(new Circle(new Point3d(x, y, z), Vector3d.ZAxis, radius)); }
        public ObjectId AddArc(double x, double y, double z, double radius, double startAngleDegrees, double endAngleDegrees) { return AddEntity(new Arc(new Point3d(x, y, z), radius, DegToRad(startAngleDegrees), DegToRad(endAngleDegrees))); }
        public ObjectId AddPoint(double x, double y, double z) { return AddEntity(new DBPoint(new Point3d(x, y, z))); }

        public ObjectId AddText(string text, double x, double y, double z, double height)
        {
            DBText dbText = new DBText();
            dbText.TextString = text ?? string.Empty;
            dbText.Position = new Point3d(x, y, z);
            dbText.Height = height;
            return AddEntity(dbText);
        }

        public ObjectId AddMText(string text, double x, double y, double z, double textHeight, double width)
        {
            MText mt = new MText();
            mt.Contents = text ?? string.Empty;
            mt.Location = new Point3d(x, y, z);
            mt.TextHeight = textHeight;
            mt.Width = width;
            return AddEntity(mt);
        }

        public ObjectId AddPolyline(IList coordinates, bool closed)
        {
            if (coordinates == null || coordinates.Count < 4 || coordinates.Count % 2 != 0) throw new ArgumentException("coordinates deve contenere coppie x,y");
            Polyline pl = new Polyline();
            int idx = 0;
            for (int i = 0; i < coordinates.Count; i += 2)
            {
                double x = Convert.ToDouble(coordinates[i], CultureInfo.InvariantCulture);
                double y = Convert.ToDouble(coordinates[i + 1], CultureInfo.InvariantCulture);
                pl.AddVertexAt(idx++, new Point2d(x, y), 0.0, 0.0, 0.0);
            }
            pl.Closed = closed;
            return AddEntity(pl);
        }

        public ObjectId AddLightWeightPolyline(IList coordinates, bool closed) { return AddPolyline(coordinates, closed); }

        public ObjectId AddLeader(IList coordinates, ObjectId annotationId)
        {
            if (coordinates == null || coordinates.Count < 6 || coordinates.Count % 3 != 0) throw new ArgumentException("coordinates deve contenere almeno 2 punti x,y,z");
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(_db);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);
                Leader leader = new Leader();
                for (int i = 0; i < coordinates.Count; i += 3)
                {
                    double x = Convert.ToDouble(coordinates[i], CultureInfo.InvariantCulture);
                    double y = Convert.ToDouble(coordinates[i + 1], CultureInfo.InvariantCulture);
                    double z = Convert.ToDouble(coordinates[i + 2], CultureInfo.InvariantCulture);
                    leader.AppendVertex(new Point3d(x, y, z));
                }
                leader.HasArrowHead = true;
                ObjectId id = ms.AppendEntity(leader);
                tr.AddNewlyCreatedDBObject(leader, true);
                if (!annotationId.IsNull && tr.GetObject(annotationId, OpenMode.ForRead) is MText)
                {
                    leader.Annotation = annotationId;
                    leader.EvaluateLeader();
                }
                tr.Commit();
                return id;
            }
        }

        public ObjectId DrawHatch(IList coordinates, string pattern, double scale, double angleDegrees)
        {
            if (coordinates == null || coordinates.Count < 6 || coordinates.Count % 2 != 0) throw new ArgumentException("coordinates deve contenere almeno 3 punti x,y");
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(_db);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);
                Polyline boundary = new Polyline();
                int idx = 0;
                for (int i = 0; i < coordinates.Count; i += 2)
                {
                    double x = Convert.ToDouble(coordinates[i], CultureInfo.InvariantCulture);
                    double y = Convert.ToDouble(coordinates[i + 1], CultureInfo.InvariantCulture);
                    boundary.AddVertexAt(idx++, new Point2d(x, y), 0.0, 0.0, 0.0);
                }
                boundary.Closed = true;
                ObjectId boundaryId = ms.AppendEntity(boundary);
                tr.AddNewlyCreatedDBObject(boundary, true);

                Hatch hatch = new Hatch();
                hatch.SetDatabaseDefaults();
                hatch.SetHatchPattern(HatchPatternType.PreDefined, string.IsNullOrWhiteSpace(pattern) ? "SOLID" : pattern);
                hatch.PatternScale = scale <= 0.0 ? 1.0 : scale;
                hatch.PatternAngle = DegToRad(angleDegrees);
                hatch.Associative = true;
                ObjectId hatchId = ms.AppendEntity(hatch);
                tr.AddNewlyCreatedDBObject(hatch, true);
                ObjectIdCollection loops = new ObjectIdCollection();
                loops.Add(boundaryId);
                hatch.AppendLoop(HatchLoopTypes.Default, loops);
                hatch.EvaluateHatch(true);
                tr.Commit();
                return hatchId;
            }
        }

        public void SetCircleDiameter(ObjectId entityId, double diameter) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { Circle c = tr.GetObject(entityId, OpenMode.ForWrite) as Circle; if (c == null) throw new ArgumentException("L'entita non e un Circle"); c.Radius = diameter / 2.0; tr.Commit(); } }
        public void SetMTextContents(ObjectId entityId, string contents) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { MText t = tr.GetObject(entityId, OpenMode.ForWrite) as MText; if (t == null) throw new ArgumentException("L'entita non e un MText"); t.Contents = contents ?? string.Empty; tr.Commit(); } }
        public void SetLeaderHasArrowHead(ObjectId entityId, bool hasArrowHead) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { Leader l = tr.GetObject(entityId, OpenMode.ForWrite) as Leader; if (l == null) throw new ArgumentException("L'entita non e un Leader"); l.HasArrowHead = hasArrowHead; l.EvaluateLeader(); tr.Commit(); } }
        public void SetLeaderHasHookLine(ObjectId entityId, bool hasHookLine) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { Leader l = tr.GetObject(entityId, OpenMode.ForWrite) as Leader; if (l == null) throw new ArgumentException("L'entita non e un Leader"); var p = typeof(Leader).GetProperty("HasHookLine"); if (p != null && p.CanWrite) p.SetValue(l, hasHookLine, null); l.EvaluateLeader(); tr.Commit(); } }
        public void SetBulgeAt(ObjectId entityId, int index, double bulge) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { Polyline p = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline; if (p == null) throw new ArgumentException("L'entita non e una Polyline"); p.SetBulgeAt(index, bulge); tr.Commit(); } }
        public void SetStartWidthAt(ObjectId entityId, int index, double width) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { Polyline p = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline; if (p == null) throw new ArgumentException("L'entita non e una Polyline"); p.SetStartWidthAt(index, width); tr.Commit(); } }
        public void SetEndWidthAt(ObjectId entityId, int index, double width) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { Polyline p = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline; if (p == null) throw new ArgumentException("L'entita non e una Polyline"); p.SetEndWidthAt(index, width); tr.Commit(); } }
        public void SetPolylineElevation(ObjectId entityId, double elevation) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { Polyline p = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline; if (p == null) throw new ArgumentException("L'entita non e una Polyline"); p.Elevation = elevation; tr.Commit(); } }
        public void SetPolylineThickness(ObjectId entityId, double thickness) { using (Transaction tr = _db.TransactionManager.StartTransaction()) { Polyline p = tr.GetObject(entityId, OpenMode.ForWrite) as Polyline; if (p == null) throw new ArgumentException("L'entita non e una Polyline"); p.Thickness = thickness; tr.Commit(); } }

        public Hashtable GetPolylineInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Polyline pl = tr.GetObject(entityId, OpenMode.ForRead) as Polyline;
                if (pl == null) throw new ArgumentException("L'entita non e una Polyline");
                Hashtable info = NewInfo();
                info["vertex_count"] = pl.NumberOfVertices;
                info["segment_count"] = pl.Closed ? pl.NumberOfVertices : Math.Max(0, pl.NumberOfVertices - 1);
                info["length"] = pl.Length;
                info["area"] = pl.Area;
                info["elevation"] = pl.Elevation;
                info["thickness"] = pl.Thickness;
                return info;
            }
        }

        public Hashtable GetEllipseInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Ellipse e = tr.GetObject(entityId, OpenMode.ForRead) as Ellipse;
                if (e == null) throw new ArgumentException("L'entita non e una Ellipse");
                Hashtable info = NewInfo();
                info["radius_ratio"] = e.RadiusRatio;
                info["area"] = Math.PI * e.MajorAxis.Length * e.MajorAxis.Length * e.RadiusRatio;
                return info;
            }
        }

        public Hashtable GetHatchInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Hatch h = tr.GetObject(entityId, OpenMode.ForRead) as Hatch;
                if (h == null) throw new ArgumentException("L'entita non e un Hatch");
                Hashtable info = NewInfo();
                info["pattern_name"] = h.PatternName;
                info["pattern_angle"] = RadToDeg(h.PatternAngle);
                return info;
            }
        }
    }
}
