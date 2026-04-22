using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public Hashtable GetMTextInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                MText text = tr.GetObject(entityId, OpenMode.ForRead) as MText;
                if (text == null)
                {
                    throw new ArgumentException("L'entita non e un MText");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = text.Handle.ToString();
                info["type"] = text.GetType().Name;
                info["layer"] = text.Layer;
                info["color_index"] = text.ColorIndex;
                info["contents"] = text.Contents;
                info["text_height"] = text.TextHeight;
                info["width"] = text.Width;
                info["rotation"] = RadiansToDegrees(text.Rotation);
                info["location_x"] = text.Location.X;
                info["location_y"] = text.Location.Y;
                info["location_z"] = text.Location.Z;
                info["text_style"] = GetTextStyleNameSafe(text.TextStyleId);
                return info;
            }
        }

        public void SetMTextContents(ObjectId entityId, string contents)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                MText text = tr.GetObject(entityId, OpenMode.ForWrite) as MText;
                if (text == null)
                {
                    throw new ArgumentException("L'entita non e un MText");
                }
                text.Contents = contents ?? string.Empty;
                tr.Commit();
            }
        }

        public void SetMTextHeight(ObjectId entityId, double textHeight)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                MText text = tr.GetObject(entityId, OpenMode.ForWrite) as MText;
                if (text == null)
                {
                    throw new ArgumentException("L'entita non e un MText");
                }
                text.TextHeight = textHeight;
                tr.Commit();
            }
        }

        public void SetMTextWidth(ObjectId entityId, double width)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                MText text = tr.GetObject(entityId, OpenMode.ForWrite) as MText;
                if (text == null)
                {
                    throw new ArgumentException("L'entita non e un MText");
                }
                text.Width = width;
                tr.Commit();
            }
        }

        public void SetMTextLocation(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                MText text = tr.GetObject(entityId, OpenMode.ForWrite) as MText;
                if (text == null)
                {
                    throw new ArgumentException("L'entita non e un MText");
                }
                text.Location = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public void SetMTextRotation(ObjectId entityId, double rotationDegrees)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                MText text = tr.GetObject(entityId, OpenMode.ForWrite) as MText;
                if (text == null)
                {
                    throw new ArgumentException("L'entita non e un MText");
                }
                text.Rotation = DegreesToRadians(rotationDegrees);
                tr.Commit();
            }
        }

        public Hashtable GetEllipseInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Ellipse ellipse = tr.GetObject(entityId, OpenMode.ForRead) as Ellipse;
                if (ellipse == null)
                {
                    throw new ArgumentException("L'entita non e una Ellipse");
                }

                double majorRadius = ellipse.MajorAxis.Length;
                double minorRadius = majorRadius * ellipse.RadiusRatio;

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = ellipse.Handle.ToString();
                info["type"] = ellipse.GetType().Name;
                info["layer"] = ellipse.Layer;
                info["color_index"] = ellipse.ColorIndex;
                info["center_x"] = ellipse.Center.X;
                info["center_y"] = ellipse.Center.Y;
                info["center_z"] = ellipse.Center.Z;
                info["major_axis_x"] = ellipse.MajorAxis.X;
                info["major_axis_y"] = ellipse.MajorAxis.Y;
                info["major_axis_z"] = ellipse.MajorAxis.Z;
                info["major_radius"] = majorRadius;
                info["radius_ratio"] = ellipse.RadiusRatio;
                info["minor_radius"] = minorRadius;
                info["start_angle"] = RadiansToDegrees(ellipse.StartAngle);
                info["end_angle"] = RadiansToDegrees(ellipse.EndAngle);
                info["normal_x"] = ellipse.Normal.X;
                info["normal_y"] = ellipse.Normal.Y;
                info["normal_z"] = ellipse.Normal.Z;
                info["is_closed"] = IsClosedEntity(ellipse);
                info["length"] = GetPerimeterInternal(ellipse);
                info["area"] = HasArea(ellipse) ? GetAreaInternal(ellipse) : 0.0;
                return info;
            }
        }

        public void SetEllipseCenter(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Ellipse ellipse = tr.GetObject(entityId, OpenMode.ForWrite) as Ellipse;
                if (ellipse == null)
                {
                    throw new ArgumentException("L'entita non e una Ellipse");
                }
                ellipse.Center = new Point3d(x, y, z);
                tr.Commit();
            }
        }

        public void SetEllipseRadiusRatio(ObjectId entityId, double radiusRatio)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Ellipse ellipse = tr.GetObject(entityId, OpenMode.ForWrite) as Ellipse;
                if (ellipse == null)
                {
                    throw new ArgumentException("L'entita non e una Ellipse");
                }
                ellipse.RadiusRatio = radiusRatio;
                tr.Commit();
            }
        }

        public void SetEllipseAngles(ObjectId entityId, double startAngleDegrees, double endAngleDegrees)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Ellipse ellipse = tr.GetObject(entityId, OpenMode.ForWrite) as Ellipse;
                if (ellipse == null)
                {
                    throw new ArgumentException("L'entita non e una Ellipse");
                }
                ellipse.StartAngle = DegreesToRadians(startAngleDegrees);
                ellipse.EndAngle = DegreesToRadians(endAngleDegrees);
                tr.Commit();
            }
        }

        public Hashtable GetHatchInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Hatch hatch = tr.GetObject(entityId, OpenMode.ForRead) as Hatch;
                if (hatch == null)
                {
                    throw new ArgumentException("L'entita non e un Hatch");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = hatch.Handle.ToString();
                info["type"] = hatch.GetType().Name;
                info["layer"] = hatch.Layer;
                info["color_index"] = hatch.ColorIndex;
                info["pattern_name"] = hatch.PatternName;
                info["pattern_type"] = hatch.PatternType.ToString();
                info["pattern_scale"] = hatch.PatternScale;
                info["pattern_angle"] = RadiansToDegrees(hatch.PatternAngle);
                info["associative"] = hatch.Associative;
                info["elevation"] = hatch.Elevation;
                info["normal_x"] = hatch.Normal.X;
                info["normal_y"] = hatch.Normal.Y;
                info["normal_z"] = hatch.Normal.Z;
                info["loop_count"] = GetHatchLoopCount(hatch);
                info["pattern_double"] = GetOptionalBoolProperty(hatch, "PatternDouble");
                info["hatch_style"] = GetOptionalEnumProperty(hatch, "HatchStyle");
                return info;
            }
        }

        public void SetHatchPattern(ObjectId entityId, string patternName)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Hatch hatch = tr.GetObject(entityId, OpenMode.ForWrite) as Hatch;
                if (hatch == null)
                {
                    throw new ArgumentException("L'entita non e un Hatch");
                }
                hatch.SetHatchPattern(HatchPatternType.PreDefined, string.IsNullOrWhiteSpace(patternName) ? "SOLID" : patternName);
                hatch.EvaluateHatch(true);
                tr.Commit();
            }
        }

        public void SetHatchScale(ObjectId entityId, double patternScale)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Hatch hatch = tr.GetObject(entityId, OpenMode.ForWrite) as Hatch;
                if (hatch == null)
                {
                    throw new ArgumentException("L'entita non e un Hatch");
                }
                hatch.PatternScale = patternScale;
                hatch.EvaluateHatch(true);
                tr.Commit();
            }
        }

        public void SetHatchAngle(ObjectId entityId, double patternAngleDegrees)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Hatch hatch = tr.GetObject(entityId, OpenMode.ForWrite) as Hatch;
                if (hatch == null)
                {
                    throw new ArgumentException("L'entita non e un Hatch");
                }
                hatch.PatternAngle = DegreesToRadians(patternAngleDegrees);
                hatch.EvaluateHatch(true);
                tr.Commit();
            }
        }

        public void SetHatchAssociative(ObjectId entityId, bool associative)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Hatch hatch = tr.GetObject(entityId, OpenMode.ForWrite) as Hatch;
                if (hatch == null)
                {
                    throw new ArgumentException("L'entita non e un Hatch");
                }
                hatch.Associative = associative;
                hatch.EvaluateHatch(true);
                tr.Commit();
            }
        }

        public void SetHatchElevation(ObjectId entityId, double elevation)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Hatch hatch = tr.GetObject(entityId, OpenMode.ForWrite) as Hatch;
                if (hatch == null)
                {
                    throw new ArgumentException("L'entita non e un Hatch");
                }
                hatch.Elevation = elevation;
                hatch.EvaluateHatch(true);
                tr.Commit();
            }
        }

        public void SetHatchNormal(ObjectId entityId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Hatch hatch = tr.GetObject(entityId, OpenMode.ForWrite) as Hatch;
                if (hatch == null)
                {
                    throw new ArgumentException("L'entita non e un Hatch");
                }
                hatch.Normal = new Vector3d(x, y, z);
                hatch.EvaluateHatch(true);
                tr.Commit();
            }
        }

        public Hashtable GetLeaderInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Leader leader = tr.GetObject(entityId, OpenMode.ForRead) as Leader;
                if (leader == null)
                {
                    throw new ArgumentException("L'entita non e un Leader");
                }

                Hashtable info = new Hashtable();
                info["id"] = entityId.ToString();
                info["handle"] = leader.Handle.ToString();
                info["type"] = leader.GetType().Name;
                info["layer"] = leader.Layer;
                info["color_index"] = leader.ColorIndex;
                info["has_arrow_head"] = GetLeaderBoolProperty(leader, "HasArrowHead");
                info["has_hook_line"] = GetLeaderBoolProperty(leader, "HasHookLine");
                info["annotation_id"] = GetLeaderObjectIdProperty(leader, "Annotation").ToString();
                info["dimension_style"] = GetLeaderStringProperty(leader, "DimensionStyle");
                info["vertex_count"] = GetLeaderVertexCount(leader);
                info["vertices"] = GetLeaderVerticesInternal(leader);
                return info;
            }
        }

        public ArrayList GetLeaderVertices(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Leader leader = tr.GetObject(entityId, OpenMode.ForRead) as Leader;
                if (leader == null)
                {
                    throw new ArgumentException("L'entita non e un Leader");
                }
                return GetLeaderVerticesInternal(leader);
            }
        }

        public void SetLeaderHasArrowHead(ObjectId entityId, bool hasArrowHead)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Leader leader = tr.GetObject(entityId, OpenMode.ForWrite) as Leader;
                if (leader == null)
                {
                    throw new ArgumentException("L'entita non e un Leader");
                }

                TrySetRuntimePropertyValue(leader, "HasArrowHead", hasArrowHead);
                leader.EvaluateLeader();
                tr.Commit();
            }
        }

        public void SetLeaderHasHookLine(ObjectId entityId, bool hasHookLine)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Leader leader = tr.GetObject(entityId, OpenMode.ForWrite) as Leader;
                if (leader == null)
                {
                    throw new ArgumentException("L'entita non e un Leader");
                }

                TrySetRuntimePropertyValue(leader, "HasHookLine", hasHookLine);
                leader.EvaluateLeader();
                tr.Commit();
            }
        }

        public void SetLeaderAnnotation(ObjectId entityId, ObjectId annotationId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Leader leader = tr.GetObject(entityId, OpenMode.ForWrite) as Leader;
                if (leader == null)
                {
                    throw new ArgumentException("L'entita non e un Leader");
                }

                if (annotationId.IsNull)
                {
                    throw new ArgumentException("annotationId non valido");
                }

                DBObject annotation = tr.GetObject(annotationId, OpenMode.ForRead);
                if (!(annotation is Entity))
                {
                    throw new ArgumentException("annotationId non identifica una Entity");
                }

                if (!TrySetRuntimePropertyValue(leader, "Annotation", annotationId))
                {
                    throw new NotSupportedException("Proprieta non scrivibile: Annotation");
                }
                leader.EvaluateLeader();
                tr.Commit();
            }
        }

        private static bool GetLeaderBoolProperty(Leader leader, string propertyName)
        {
            object value = GetRuntimePropertyValue(leader, propertyName);
            return value is bool && (bool)value;
        }

        private static string GetLeaderStringProperty(Leader leader, string propertyName)
        {
            object value = GetRuntimePropertyValue(leader, propertyName);
            return value == null ? string.Empty : Convert.ToString(value);
        }

        private static bool GetOptionalBoolProperty(object instance, string propertyName)
        {
            object value = GetRuntimePropertyValue(instance, propertyName);
            return value is bool && (bool)value;
        }

        private static string GetOptionalEnumProperty(object instance, string propertyName)
        {
            object value = GetRuntimePropertyValue(instance, propertyName);
            return value == null ? string.Empty : value.ToString();
        }

        private static int GetHatchLoopCount(Hatch hatch)
        {
            System.Reflection.PropertyInfo prop = hatch.GetType().GetProperty("NumberOfLoops");
            if (prop == null)
            {
                return 0;
            }

            object value = prop.GetValue(hatch, null);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private static ObjectId GetLeaderObjectIdProperty(Leader leader, string propertyName)
        {
            object value = GetRuntimePropertyValue(leader, propertyName);
            return value is ObjectId ? (ObjectId)value : ObjectId.Null;
        }

        private static int GetLeaderVertexCount(Leader leader)
        {
            object value = GetRuntimePropertyValue(leader, "NumVertices");
            if (value == null)
            {
                value = GetRuntimePropertyValue(leader, "NumberOfVertices");
            }
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private static ArrayList GetLeaderVerticesInternal(Leader leader)
        {
            ArrayList result = new ArrayList();
            int count = GetLeaderVertexCount(leader);
            for (int i = 0; i < count; i++)
            {
                object point = InvokeRuntimeMethod(leader, "VertexAt", i);
                if (!(point is Point3d))
                {
                    point = InvokeRuntimeMethod(leader, "GetVertexAt", i);
                }

                if (point is Point3d)
                {
                    Point3d pt = (Point3d)point;
                    Hashtable item = new Hashtable();
                    item["index"] = i;
                    item["x"] = pt.X;
                    item["y"] = pt.Y;
                    item["z"] = pt.Z;
                    result.Add(item);
                }
            }
            return result;
        }

        private static object GetRuntimePropertyValue(object instance, string propertyName)
        {
            if (instance == null)
            {
                return null;
            }

            System.Reflection.PropertyInfo prop = instance.GetType().GetProperty(propertyName);
            return prop == null ? null : prop.GetValue(instance, null);
        }

        private static bool TrySetRuntimePropertyValue(object instance, string propertyName, object value)
        {
            if (instance == null)
            {
                return false;
            }

            System.Reflection.PropertyInfo prop = instance.GetType().GetProperty(propertyName);
            if (prop == null || !prop.CanWrite)
            {
                return false;
            }

            prop.SetValue(instance, value, null);
            return true;
        }

        private static object InvokeRuntimeMethod(object instance, string methodName, params object[] args)
        {
            if (instance == null)
            {
                return null;
            }

            System.Reflection.MethodInfo mi = instance.GetType().GetMethod(methodName);
            return mi == null ? null : mi.Invoke(instance, args);
        }
    }
}
