import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[CURVE TEST] " + msg)


log("Avvio test circle/line/spline")

res = cad.GetPoint("Punto base curve test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_CURVES", 5)
    cad.SetCurrentLayer("PYLOAD_CURVES")

    circle_id = cad.AddCircle(p.X, p.Y, p.Z, 18.0)
    line_id = cad.AddLine(p.X - 50.0, p.Y - 20.0, p.Z, p.X + 10.0, p.Y + 15.0, p.Z)
    spline_id = cad.AddSpline([
        p.X - 20.0, p.Y + 40.0, p.Z,
        p.X + 0.0, p.Y + 65.0, p.Z,
        p.X + 25.0, p.Y + 35.0, p.Z,
        p.X + 45.0, p.Y + 70.0, p.Z,
        p.X + 70.0, p.Y + 30.0, p.Z,
    ])
    log("Entita create")

    cinfo = cad.GetCircleInfo(circle_id)
    log("Circle radius = " + str(cinfo["radius"]))
    log("Circle area = " + str(cinfo["area"]))

    cad.SetCircleRadius(circle_id, 24.0)
    cad.SetCircleThickness(circle_id, 1.5)
    coff = cad.OffsetCircle(circle_id, 5.0)
    log("Circle offset -> " + str(len(coff)))

    linfo = cad.GetLineInfo(line_id)
    log("Line length = " + str(linfo["length"]))
    mid = cad.GetLineMidPoint(line_id)
    log("Line midpoint = (" + str(mid.X) + ", " + str(mid.Y) + ", " + str(mid.Z) + ")")

    cad.SetLineEndPoint(line_id, p.X + 35.0, p.Y + 25.0, p.Z)
    cad.SetLineThickness(line_id, 2.0)
    loff = cad.OffsetLine(line_id, 6.0)
    log("Line offset -> " + str(len(loff)))

    sinfo = cad.GetSplineInfo(spline_id)
    log("Spline degree = " + str(sinfo["degree"]))
    log("Spline length = " + str(sinfo["length"]))
    log("Spline closed = " + str(sinfo["is_closed"]))

    soff = cad.OffsetSpline(spline_id, 4.0)
    log("Spline offset -> " + str(len(soff)))

    cad.Regen()
    cad.ZoomExtents()
    log("Test curve completato")
