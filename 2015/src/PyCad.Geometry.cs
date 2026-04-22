using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ObjectId AddLine(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return AddEntity(new Line(new Point3d(x1, y1, z1), new Point3d(x2, y2, z2)));
        }

        public ObjectId AddCircle(double x, double y, double z, double radius)
        {
            return AddEntity(new Circle(new Point3d(x, y, z), Vector3d.ZAxis, radius));
        }

        public ObjectId AddArc(double x, double y, double z, double radius, double startAngleDegrees, double endAngleDegrees)
        {
            return AddEntity(new Arc(new Point3d(x, y, z), radius, DegreesToRadians(startAngleDegrees), DegreesToRadians(endAngleDegrees)));
        }

        public ObjectId AddPoint(double x, double y, double z)
        {
            return AddEntity(new DBPoint(new Point3d(x, y, z)));
        }

        public ObjectId AddTrace(
            double x1, double y1, double z1,
            double x2, double y2, double z2,
            double x3, double y3, double z3,
            double x4, double y4, double z4)
        {
            Trace trace = new Trace();
            trace.SetPointAt(0, new Point3d(x1, y1, z1));
            trace.SetPointAt(1, new Point3d(x2, y2, z2));
            trace.SetPointAt(2, new Point3d(x3, y3, z3));
            trace.SetPointAt(3, new Point3d(x4, y4, z4));
            return AddEntity(trace);
        }

        public ObjectId AddSolid(
            double x1, double y1, double z1,
            double x2, double y2, double z2,
            double x3, double y3, double z3,
            double x4, double y4, double z4)
        {
            Solid solid = new Solid(
                new Point3d(x1, y1, z1),
                new Point3d(x2, y2, z2),
                new Point3d(x3, y3, z3),
                new Point3d(x4, y4, z4));
            return AddEntity(solid);
        }

        public ObjectId AddRay(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            Vector3d dir = new Vector3d(x2 - x1, y2 - y1, z2 - z1);
            if (dir.Length == 0.0)
            {
                throw new ArgumentException("Point1 e Point2 non possono coincidere");
            }

            Ray ray = new Ray();
            ray.BasePoint = new Point3d(x1, y1, z1);
            ray.UnitDir = dir.GetNormal();
            return AddEntity(ray);
        }

        public ObjectId AddPolyline(IList coordinates, bool closed)
        {
            if (coordinates == null || coordinates.Count < 4 || coordinates.Count % 2 != 0)
            {
                throw new ArgumentException("coordinates deve contenere coppie x,y");
            }

            Polyline pl = new Polyline();
            int vertexIndex = 0;
            for (int i = 0; i < coordinates.Count; i += 2)
            {
                double x = Convert.ToDouble(coordinates[i], CultureInfo.InvariantCulture);
                double y = Convert.ToDouble(coordinates[i + 1], CultureInfo.InvariantCulture);
                pl.AddVertexAt(vertexIndex++, new Point2d(x, y), 0.0, 0.0, 0.0);
            }
            pl.Closed = closed;
            return AddEntity(pl);
        }

        public ObjectId AddLightWeightPolyline(IList coordinates, bool closed)
        {
            return AddPolyline(coordinates, closed);
        }

        public ObjectId DrawRectangle(double x1, double y1, double x2, double y2)
        {
            ArrayList pts = new ArrayList
            {
                x1, y1,
                x2, y1,
                x2, y2,
                x1, y2
            };
            return AddPolyline(pts, true);
        }

        public ObjectId AddPolyline3d(IList coordinates, bool closed)
        {
            if (coordinates == null || coordinates.Count < 6 || coordinates.Count % 3 != 0)
            {
                throw new ArgumentException("coordinates deve contenere triple x,y,z");
            }

            Point3dCollection pts = new Point3dCollection();
            for (int i = 0; i < coordinates.Count; i += 3)
            {
                double x = Convert.ToDouble(coordinates[i], CultureInfo.InvariantCulture);
                double y = Convert.ToDouble(coordinates[i + 1], CultureInfo.InvariantCulture);
                double z = Convert.ToDouble(coordinates[i + 2], CultureInfo.InvariantCulture);
                pts.Add(new Point3d(x, y, z));
            }

            return AddEntity(new Polyline3d(Poly3dType.SimplePoly, pts, closed));
        }

        public ObjectId AddText(string text, double x, double y, double z, double height)
        {
            DBText dbText = new DBText();
            dbText.TextString = text;
            dbText.Position = new Point3d(x, y, z);
            dbText.Height = height;
            return AddEntity(dbText);
        }

        public ObjectId AddMText(string text, double x, double y, double z, double textHeight, double width)
        {
            MText mt = new MText();
            mt.Contents = text;
            mt.Location = new Point3d(x, y, z);
            mt.TextHeight = textHeight;
            mt.Width = width;
            return AddEntity(mt);
        }

        public ObjectId AddSpline(IList coordinates)
        {
            if (coordinates == null || coordinates.Count < 6 || coordinates.Count % 3 != 0)
            {
                throw new ArgumentException("coordinates deve contenere almeno 2 punti x,y,z");
            }

            Point3dCollection pts = new Point3dCollection();
            for (int i = 0; i < coordinates.Count; i += 3)
            {
                double x = Convert.ToDouble(coordinates[i], CultureInfo.InvariantCulture);
                double y = Convert.ToDouble(coordinates[i + 1], CultureInfo.InvariantCulture);
                double z = Convert.ToDouble(coordinates[i + 2], CultureInfo.InvariantCulture);
                pts.Add(new Point3d(x, y, z));
            }

            Spline spline = new Spline(pts, 3, 0.0);
            return AddEntity(spline);
        }

        public ObjectId AddAlignedDimension(
            double x1, double y1, double z1,
            double x2, double y2, double z2,
            double dimLineX, double dimLineY, double dimLineZ,
            string dimensionText)
        {
            RotatedDimension dim = new RotatedDimension();
            dim.XLine1Point = new Point3d(x1, y1, z1);
            dim.XLine2Point = new Point3d(x2, y2, z2);
            dim.DimLinePoint = new Point3d(dimLineX, dimLineY, dimLineZ);
            dim.Rotation = Math.Atan2(y2 - y1, x2 - x1);
            if (!string.IsNullOrWhiteSpace(dimensionText))
            {
                dim.DimensionText = dimensionText;
            }
            return AddEntity(dim);
        }

        public ObjectId AddTolerance(string text, double x, double y, double z, double directionX, double directionY, double directionZ)
        {
            Point3d location = new Point3d(x, y, z);
            Vector3d direction = new Vector3d(directionX, directionY, directionZ);
            Type toleranceType = FindRuntimeType(
                "ZwSoft.ZwCAD.DatabaseServices.Fcf",
                "ZwSoft.ZwCAD.DatabaseServices.Tolerance",
                "ZwSoft.ZwCAD.DatabaseServices.FeatureControlFrame");

            if (toleranceType == null)
            {
                throw new NotSupportedException("Entita Tolerance non disponibile in questa API ZWCAD");
            }

            object instance = TryCreateToleranceEntity(toleranceType, text ?? string.Empty, location, direction);
            Entity entity = instance as Entity;
            if (entity == null)
            {
                throw new NotSupportedException("Il tipo Tolerance trovato non deriva da Entity: " + toleranceType.FullName);
            }

            return AddEntity(entity);
        }

        public ObjectId AddTable(double x, double y, double z, int rows, int columns, double rowHeight, double columnWidth)
        {
            if (rows < 1 || columns < 1)
            {
                throw new ArgumentException("rows e columns devono essere >= 1");
            }

            Table table = new Table();
            table.TableStyle = _db.Tablestyle;
            table.Position = new Point3d(x, y, z);
            table.NumRows = rows;
            table.NumColumns = columns;
            table.SetRowHeight(rowHeight);
            table.SetColumnWidth(columnWidth);
            return AddEntity(table);
        }

        private static Type FindRuntimeType(params string[] fullNames)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (string fullName in fullNames)
            {
                foreach (Assembly asm in assemblies)
                {
                    Type t = asm.GetType(fullName, false);
                    if (t != null)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        private static object TryCreateToleranceEntity(Type toleranceType, string text, Point3d location, Vector3d direction)
        {
            ConstructorInfo ctor = toleranceType.GetConstructor(new[] { typeof(string), typeof(Point3d), typeof(Vector3d) });
            if (ctor != null)
            {
                return ctor.Invoke(new object[] { text, location, direction });
            }

            ctor = toleranceType.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                throw new NotSupportedException("Nessun costruttore supportato per il tipo Tolerance: " + toleranceType.FullName);
            }

            object instance = ctor.Invoke(null);
            SetPropertyIfExists(toleranceType, instance, "TextString", text);
            SetPropertyIfExists(toleranceType, instance, "Contents", text);
            SetPropertyIfExists(toleranceType, instance, "Location", location);
            SetPropertyIfExists(toleranceType, instance, "Position", location);
            SetPropertyIfExists(toleranceType, instance, "DirectionVector", direction);
            SetPropertyIfExists(toleranceType, instance, "Direction", direction);
            return instance;
        }

        private static void SetPropertyIfExists(Type type, object instance, string propertyName, object value)
        {
            PropertyInfo prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(instance, value, null);
            }
        }

        public ObjectId AddLeader(IList coordinates, ObjectId annotationId)
        {
            if (coordinates == null || coordinates.Count < 6 || coordinates.Count % 3 != 0)
            {
                throw new ArgumentException("coordinates deve contenere almeno 2 punti x,y,z");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Leader leader = new Leader();
                for (int i = 0; i < coordinates.Count; i += 3)
                {
                    double x = Convert.ToDouble(coordinates[i], CultureInfo.InvariantCulture);
                    double y = Convert.ToDouble(coordinates[i + 1], CultureInfo.InvariantCulture);
                    double z = Convert.ToDouble(coordinates[i + 2], CultureInfo.InvariantCulture);
                    leader.AppendVertex(new Point3d(x, y, z));
                }

                leader.HasArrowHead = true;
                ObjectId id = btr.AppendEntity(leader);
                tr.AddNewlyCreatedDBObject(leader, true);

                if (!annotationId.IsNull)
                {
                    DBObject dbo = tr.GetObject(annotationId, OpenMode.ForRead);
                    if (dbo is MText)
                    {
                        leader.Annotation = annotationId;
                        leader.EvaluateLeader();
                    }
                }

                tr.Commit();
                return id;
            }
        }

        public ObjectId DrawHatch(IList coordinates, string pattern, double scale, double angleDegrees)
        {
            if (coordinates == null || coordinates.Count < 6 || coordinates.Count % 2 != 0)
            {
                throw new ArgumentException("coordinates deve contenere almeno 3 punti x,y");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline boundary = new Polyline();
                int vertexIndex = 0;
                for (int i = 0; i < coordinates.Count; i += 2)
                {
                    double x = Convert.ToDouble(coordinates[i], CultureInfo.InvariantCulture);
                    double y = Convert.ToDouble(coordinates[i + 1], CultureInfo.InvariantCulture);
                    boundary.AddVertexAt(vertexIndex++, new Point2d(x, y), 0.0, 0.0, 0.0);
                }
                boundary.Closed = true;

                ObjectId boundaryId = btr.AppendEntity(boundary);
                tr.AddNewlyCreatedDBObject(boundary, true);

                Hatch hatch = new Hatch();
                hatch.SetDatabaseDefaults();
                hatch.SetHatchPattern(HatchPatternType.PreDefined, string.IsNullOrWhiteSpace(pattern) ? "SOLID" : pattern);
                hatch.PatternScale = scale <= 0.0 ? 1.0 : scale;
                hatch.PatternAngle = DegreesToRadians(angleDegrees);

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
    }
}
