import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[POLY TEST] " + msg)


log("Avvio test polilinee")

res = cad.GetPoint("Punto base poly test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_POLY", 2)
    cad.SetCurrentLayer("PYLOAD_POLY")

    pl = [
        p.X, p.Y,
        p.X + 40.0, p.Y,
        p.X + 55.0, p.Y + 20.0,
        p.X + 15.0, p.Y + 35.0,
        p.X - 10.0, p.Y + 15.0,
    ]
    pl_id = cad.AddPolyline(pl, True)
    log("Polyline creata")

    log("Vertex count iniziale: " + str(cad.GetPolylineVertexCount(pl_id)))
    log("Closed iniziale: " + str(cad.IsPolylineClosed(pl_id)))
    log("Area iniziale: " + str(cad.GetPolylineArea(pl_id)))
    log("Length iniziale: " + str(cad.GetCurveLength(pl_id)))

    verts = cad.GetPolylineVertices(pl_id)
    log("Vertici letti: " + str(len(verts)))
    for v in verts:
        log(" v" + str(v["index"]) + " = (" + str(v["x"]) + ", " + str(v["y"]) + ")")

    pt_at_20 = cad.GetPointAtDist(pl_id, 20.0)
    log("PointAtDist(20) = (" + str(pt_at_20.X) + ", " + str(pt_at_20.Y) + ", " + str(pt_at_20.Z) + ")")

    closest = cad.GetClosestPointTo(pl_id, p.X + 12.0, p.Y + 11.0, p.Z, False)
    log("ClosestPoint = (" + str(closest.X) + ", " + str(closest.Y) + ", " + str(closest.Z) + ")")

    dist_at = cad.GetDistAtPoint(pl_id, closest.X, closest.Y, closest.Z)
    log("DistAtPoint(closest) = " + str(dist_at))

    cad.AddPolylineVertex(pl_id, 2, p.X + 48.0, p.Y + 8.0)
    log("Vertice aggiunto in indice 2")
    log("Vertex count dopo add: " + str(cad.GetPolylineVertexCount(pl_id)))

    cad.RemovePolylineVertex(pl_id, 2)
    log("Vertice rimosso in indice 2")
    log("Vertex count dopo remove: " + str(cad.GetPolylineVertexCount(pl_id)))

    cad.SetPolylineClosed(pl_id, False)
    log("Closed dopo open: " + str(cad.IsPolylineClosed(pl_id)))
    cad.SetPolylineClosed(pl_id, True)
    log("Closed dopo close: " + str(cad.IsPolylineClosed(pl_id)))

    cad.Regen()
    cad.ZoomExtents()
    log("Test polilinee completato")
