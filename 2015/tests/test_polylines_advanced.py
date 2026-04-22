import clr
clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[POLY ADV TEST] " + msg)


log("Avvio test polilinee avanzate")

res = cad.GetPoint("Punto base poly advanced test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    pts = [
        p.X, p.Y,
        p.X + 40.0, p.Y,
        p.X + 55.0, p.Y + 20.0,
        p.X + 20.0, p.Y + 35.0,
        p.X - 5.0, p.Y + 18.0,
    ]
    pl_id = cad.AddPolyline(pts, True)
    log("Polyline creata")

    info = cad.GetPolylineInfo(pl_id)
    log("Info -> vertex_count=" + str(info["vertex_count"]) + " segment_count=" + str(info["segment_count"]))
    log("Info -> length=" + str(info["length"]) + " area=" + str(info["area"]))

    param = cad.GetParameterAtDistance(pl_id, 20.0)
    pt_param = cad.GetPointAtParameter(pl_id, param)
    log("GetParameterAtDistance(20) = " + str(param))
    log("GetPointAtParameter(param) = (" + str(pt_param.X) + ", " + str(pt_param.Y) + ", " + str(pt_param.Z) + ")")

    v1 = cad.GetPolylineVertexAt(pl_id, 1)
    param_at_v1 = cad.GetParameterAtPoint(pl_id, v1.X, v1.Y, v1.Z)
    len_to_v1 = cad.GetPolylineLengthToVertex(pl_id, 1)
    log("GetParameterAtPoint(v1) = " + str(param_at_v1))
    log("GetPolylineLengthToVertex(1) = " + str(len_to_v1))

    log("GetPolylinePointAtPercent(0.25) ...")
    pt_25 = cad.GetPolylinePointAtPercent(pl_id, 0.25)
    log("PointAtPercent = (" + str(pt_25.X) + ", " + str(pt_25.Y) + ", " + str(pt_25.Z) + ")")

    seg_count = cad.GetPolylineSegmentCount(pl_id)
    log("Segment count = " + str(seg_count))
    if seg_count > 0:
        seg_type = cad.GetPolylineSegmentType(pl_id, 0)
        log("Segment[0] type = " + str(seg_type))
        line_seg = cad.GetLineSegment2dAt(pl_id, 0)
        log("LineSegment[0] length = " + str(line_seg["length"]))

    log("Bulge iniziale v0 = " + str(cad.GetBulgeAt(pl_id, 0)))
    cad.SetBulgeAt(pl_id, 0, 0.35)
    log("Bulge dopo set v0 = " + str(cad.GetBulgeAt(pl_id, 0)))
    arc_type = cad.GetPolylineSegmentType(pl_id, 0)
    log("Segment[0] type dopo bulge = " + str(arc_type))
    if "Arc" in arc_type:
        arc_seg = cad.GetArcSegment2dAt(pl_id, 0)
        log("ArcSegment[0] radius = " + str(arc_seg["radius"]))

    log("StartWidth v0 iniziale = " + str(cad.GetStartWidthAt(pl_id, 0)))
    log("EndWidth v0 iniziale = " + str(cad.GetEndWidthAt(pl_id, 0)))
    cad.SetStartWidthAt(pl_id, 0, 2.5)
    cad.SetEndWidthAt(pl_id, 0, 4.0)
    log("StartWidth v0 dopo set = " + str(cad.GetStartWidthAt(pl_id, 0)))
    log("EndWidth v0 dopo set = " + str(cad.GetEndWidthAt(pl_id, 0)))

    cad.SetPolylineElevation(pl_id, 5.0)
    cad.SetPolylineThickness(pl_id, 1.5)
    cad.SetPolylineNormal(pl_id, 0.0, 0.0, 1.0)
    info = cad.GetPolylineInfo(pl_id)
    log("Info finale -> elevation=" + str(info["elevation"]) + " thickness=" + str(info["thickness"]))

    cad.Regen()
    log("Test polilinee avanzate completato")
