using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public Hashtable GetAttributeInfo(ObjectId attributeId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(attributeId, OpenMode.ForRead);

                AttributeDefinition def = dbo as AttributeDefinition;
                if (def != null)
                {
                    return BuildAttributeInfo(attributeId, def, true);
                }

                AttributeReference ar = dbo as AttributeReference;
                if (ar != null)
                {
                    return BuildAttributeInfo(attributeId, ar, false);
                }

                throw new ArgumentException("L'entita non e un AttributeDefinition o AttributeReference");
            }
        }

        public void SetAttributeText(ObjectId attributeId, string value)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(attributeId, OpenMode.ForWrite);

                AttributeDefinition def = dbo as AttributeDefinition;
                if (def != null)
                {
                    def.TextString = value ?? string.Empty;
                    tr.Commit();
                    return;
                }

                AttributeReference ar = dbo as AttributeReference;
                if (ar != null)
                {
                    ar.TextString = value ?? string.Empty;
                    tr.Commit();
                    return;
                }

                throw new ArgumentException("L'entita non e un AttributeDefinition o AttributeReference");
            }
        }

        public void SetAttributeTag(ObjectId attributeId, string tag)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                AttributeDefinition def = tr.GetObject(attributeId, OpenMode.ForWrite) as AttributeDefinition;
                if (def == null)
                {
                    throw new ArgumentException("L'entita non e un AttributeDefinition");
                }

                def.Tag = tag ?? string.Empty;
                tr.Commit();
            }
        }

        public void SetAttributePrompt(ObjectId attributeId, string prompt)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                AttributeDefinition def = tr.GetObject(attributeId, OpenMode.ForWrite) as AttributeDefinition;
                if (def == null)
                {
                    throw new ArgumentException("L'entita non e un AttributeDefinition");
                }

                def.Prompt = prompt ?? string.Empty;
                tr.Commit();
            }
        }

        public void SetAttributeHeight(ObjectId attributeId, double height)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(attributeId, OpenMode.ForWrite);

                AttributeDefinition def = dbo as AttributeDefinition;
                if (def != null)
                {
                    def.Height = height;
                    tr.Commit();
                    return;
                }

                AttributeReference ar = dbo as AttributeReference;
                if (ar != null)
                {
                    ar.Height = height;
                    tr.Commit();
                    return;
                }

                throw new ArgumentException("L'entita non e un AttributeDefinition o AttributeReference");
            }
        }

        public void SetAttributeRotation(ObjectId attributeId, double rotationDegrees)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(attributeId, OpenMode.ForWrite);

                AttributeDefinition def = dbo as AttributeDefinition;
                if (def != null)
                {
                    def.Rotation = DegreesToRadians(rotationDegrees);
                    tr.Commit();
                    return;
                }

                AttributeReference ar = dbo as AttributeReference;
                if (ar != null)
                {
                    ar.Rotation = DegreesToRadians(rotationDegrees);
                    tr.Commit();
                    return;
                }

                throw new ArgumentException("L'entita non e un AttributeDefinition o AttributeReference");
            }
        }

        public void SetAttributeInvisible(ObjectId attributeId, bool invisible)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(attributeId, OpenMode.ForWrite);

                AttributeDefinition def = dbo as AttributeDefinition;
                if (def != null)
                {
                    def.Invisible = invisible;
                    tr.Commit();
                    return;
                }

                AttributeReference ar = dbo as AttributeReference;
                if (ar != null)
                {
                    ar.Invisible = invisible;
                    tr.Commit();
                    return;
                }

                throw new ArgumentException("L'entita non e un AttributeDefinition o AttributeReference");
            }
        }

        public void SetAttributeConstant(ObjectId attributeId, bool constant)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                AttributeDefinition def = tr.GetObject(attributeId, OpenMode.ForWrite) as AttributeDefinition;
                if (def == null)
                {
                    throw new ArgumentException("L'entita non e un AttributeDefinition");
                }

                def.Constant = constant;
                tr.Commit();
            }
        }

        public void SetAttributeVerify(ObjectId attributeId, bool verify)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                AttributeDefinition def = tr.GetObject(attributeId, OpenMode.ForWrite) as AttributeDefinition;
                if (def == null)
                {
                    throw new ArgumentException("L'entita non e un AttributeDefinition");
                }

                def.Verifiable = verify;
                tr.Commit();
            }
        }

        public void SetAttributeInsertionPoint(ObjectId attributeId, double x, double y, double z)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                DBObject dbo = tr.GetObject(attributeId, OpenMode.ForWrite);
                Point3d pt = new Point3d(x, y, z);

                AttributeDefinition def = dbo as AttributeDefinition;
                if (def != null)
                {
                    def.Position = pt;
                    tr.Commit();
                    return;
                }

                AttributeReference ar = dbo as AttributeReference;
                if (ar != null)
                {
                    ar.Position = pt;
                    tr.Commit();
                    return;
                }

                throw new ArgumentException("L'entita non e un AttributeDefinition o AttributeReference");
            }
        }

        private Hashtable BuildAttributeInfo(ObjectId id, DBText attr, bool isDefinition)
        {
            Hashtable info = new Hashtable();
            info["id"] = id.ToString();
            info["handle"] = attr.Handle.ToString();
            info["type"] = attr.GetType().Name;
            info["is_definition"] = isDefinition;
            info["layer"] = attr.Layer;
            info["text"] = attr.TextString;
            info["height"] = attr.Height;
            info["rotation"] = attr.Rotation;
            info["position_x"] = attr.Position.X;
            info["position_y"] = attr.Position.Y;
            info["position_z"] = attr.Position.Z;

            AttributeDefinition def = attr as AttributeDefinition;
            if (def != null)
            {
                info["tag"] = def.Tag;
                info["prompt"] = def.Prompt;
                info["constant"] = def.Constant;
                info["invisible"] = def.Invisible;
                info["verifiable"] = def.Verifiable;
            }

            AttributeReference ar = attr as AttributeReference;
            if (ar != null)
            {
                info["tag"] = ar.Tag;
                info["invisible"] = ar.Invisible;
            }

            return info;
        }
    }
}
