using System;
using System.Collections;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PYLOAD
{
    public partial class PyCad
    {
        public ObjectId Add3DFace(
            double x1, double y1, double z1,
            double x2, double y2, double z2,
            double x3, double y3, double z3,
            double x4, double y4, double z4)
        {
            Face face = new Face(
                new Point3d(x1, y1, z1),
                new Point3d(x2, y2, z2),
                new Point3d(x3, y3, z3),
                new Point3d(x4, y4, z4),
                true,
                true,
                true,
                true);
            return AddEntity(face);
        }

        public ObjectId Add3DPoly(IList coordinates, bool closed)
        {
            if (coordinates == null || coordinates.Count < 6 || coordinates.Count % 3 != 0)
            {
                throw new ArgumentException("coordinates deve contenere triple x,y,z");
            }

            Point3dCollection pts = new Point3dCollection();
            for (int i = 0; i < coordinates.Count; i += 3)
            {
                double x = Convert.ToDouble(coordinates[i]);
                double y = Convert.ToDouble(coordinates[i + 1]);
                double z = Convert.ToDouble(coordinates[i + 2]);
                pts.Add(new Point3d(x, y, z));
            }

            return AddEntity(new Polyline3d(Poly3dType.SimplePoly, pts, closed));
        }

        public ObjectId AddPolyfaceMesh(IList vertexCoordinates, IList faceIndices)
        {
            if (vertexCoordinates == null || vertexCoordinates.Count < 9 || vertexCoordinates.Count % 3 != 0)
            {
                throw new ArgumentException("vertexCoordinates deve contenere triple x,y,z");
            }
            if (faceIndices == null || faceIndices.Count < 3 || faceIndices.Count % 4 != 0)
            {
                throw new ArgumentException("faceIndices deve contenere gruppi di 4 indici");
            }

            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                PolyFaceMesh mesh = new PolyFaceMesh();
                ObjectId meshId = btr.AppendEntity(mesh);
                tr.AddNewlyCreatedDBObject(mesh, true);

                ObjectIdCollection vertexIds = new ObjectIdCollection();
                for (int i = 0; i < vertexCoordinates.Count; i += 3)
                {
                    double x = Convert.ToDouble(vertexCoordinates[i]);
                    double y = Convert.ToDouble(vertexCoordinates[i + 1]);
                    double z = Convert.ToDouble(vertexCoordinates[i + 2]);
                    PolyFaceMeshVertex v = new PolyFaceMeshVertex(new Point3d(x, y, z));
                    mesh.AppendVertex(v);
                    tr.AddNewlyCreatedDBObject(v, true);
                    vertexIds.Add(v.ObjectId);
                }

                for (int i = 0; i < faceIndices.Count; i += 4)
                {
                    short i1 = Convert.ToInt16(faceIndices[i]);
                    short i2 = Convert.ToInt16(faceIndices[i + 1]);
                    short i3 = Convert.ToInt16(faceIndices[i + 2]);
                    short i4 = Convert.ToInt16(faceIndices[i + 3]);
                    FaceRecord face = new FaceRecord(i1, i2, i3, i4);
                    mesh.AppendFaceRecord(face);
                    tr.AddNewlyCreatedDBObject(face, true);
                }

                tr.Commit();
                return meshId;
            }
        }

        public Hashtable Get3DFaceInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                Face face = tr.GetObject(entityId, OpenMode.ForRead) as Face;
                if (face == null)
                {
                    throw new ArgumentException("L'entita non e una Face");
                }

                Hashtable info = new Hashtable();
                info["type"] = face.GetType().Name;
                info["handle"] = face.Handle.ToString();
                info["layer"] = face.Layer;
                Point3d p0 = face.GetVertexAt(0);
                Point3d p1 = face.GetVertexAt(1);
                Point3d p2 = face.GetVertexAt(2);
                Point3d p3 = face.GetVertexAt(3);
                info["p0_x"] = p0.X; info["p0_y"] = p0.Y; info["p0_z"] = p0.Z;
                info["p1_x"] = p1.X; info["p1_y"] = p1.Y; info["p1_z"] = p1.Z;
                info["p2_x"] = p2.X; info["p2_y"] = p2.Y; info["p2_z"] = p2.Z;
                info["p3_x"] = p3.X; info["p3_y"] = p3.Y; info["p3_z"] = p3.Z;
                return info;
            }
        }

        public Hashtable GetPolyfaceMeshInfo(ObjectId entityId)
        {
            using (Transaction tr = _db.TransactionManager.StartTransaction())
            {
                PolyFaceMesh mesh = tr.GetObject(entityId, OpenMode.ForRead) as PolyFaceMesh;
                if (mesh == null)
                {
                    throw new ArgumentException("L'entita non e una PolyFaceMesh");
                }

                int vertexCount = 0;
                int faceCount = 0;
                foreach (ObjectId childId in mesh)
                {
                    DBObject child = tr.GetObject(childId, OpenMode.ForRead);
                    if (child is PolyFaceMeshVertex)
                    {
                        vertexCount++;
                    }
                    else if (child is FaceRecord)
                    {
                        faceCount++;
                    }
                }

                Hashtable info = new Hashtable();
                info["type"] = mesh.GetType().Name;
                info["handle"] = mesh.Handle.ToString();
                info["layer"] = mesh.Layer;
                info["vertex_count"] = vertexCount;
                info["face_count"] = faceCount;
                return info;
            }
        }
    }
}
