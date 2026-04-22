import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[3D TEST] " + msg)


log("Avvio test 3D base")

res = cad.GetPoint("Punto base 3D test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    face_id = cad.Add3DFace(
        p.X, p.Y, p.Z,
        p.X + 20.0, p.Y, p.Z + 5.0,
        p.X + 25.0, p.Y + 15.0, p.Z + 10.0,
        p.X + 5.0, p.Y + 18.0, p.Z + 2.0)
    log("Add3DFace ok, handle=" + cad.GetEntityHandle(face_id))

    poly3d_id = cad.Add3DPoly([
        p.X + 40.0, p.Y, p.Z,
        p.X + 55.0, p.Y + 5.0, p.Z + 10.0,
        p.X + 65.0, p.Y + 18.0, p.Z + 5.0,
        p.X + 80.0, p.Y + 10.0, p.Z + 15.0
    ], False)
    log("Add3DPoly ok, handle=" + cad.GetEntityHandle(poly3d_id))

    mesh_id = cad.AddPolyfaceMesh([
        p.X + 100.0, p.Y, p.Z,
        p.X + 120.0, p.Y, p.Z,
        p.X + 120.0, p.Y + 20.0, p.Z,
        p.X + 100.0, p.Y + 20.0, p.Z,
        p.X + 110.0, p.Y + 10.0, p.Z + 15.0
    ], [
        1, 2, 5, 0,
        2, 3, 5, 0,
        3, 4, 5, 0,
        4, 1, 5, 0
    ])
    log("AddPolyfaceMesh ok, handle=" + cad.GetEntityHandle(mesh_id))

    face_info = cad.Get3DFaceInfo(face_id)
    log("3DFace type = " + str(face_info["type"]))

    mesh_info = cad.GetPolyfaceMeshInfo(mesh_id)
    log("PolyfaceMesh vertex_count = " + str(mesh_info["vertex_count"]))
    log("PolyfaceMesh face_count = " + str(mesh_info["face_count"]))

    cad.Regen()
    cad.ZoomExtents()
    log("Test 3D base completato")
