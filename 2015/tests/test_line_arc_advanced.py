import clr
clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[LINE/ARC ADV TEST] " + msg)


log("Avvio test linee/archi avanzato")

res = cad.GetPoint("Punto base line/arc advanced test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    line_id = cad.AddLine(p.X, p.Y, p.Z, p.X + 60.0, p.Y + 20.0, p.Z)
    arc_id = cad.AddArc(p.X + 90.0, p.Y, p.Z, 25.0, 20.0, 210.0)
    log("Entita create")

    line_info = cad.GetLineInfo(line_id)
    log("Line length = " + str(line_info["length"]))
    log("Line angle deg = " + str(cad.GetLineAngleDegrees(line_id)))
    line_start = cad.GetLineStartPoint(line_id)
    line_end = cad.GetLineEndPoint(line_id)
    log("Line start = (" + str(line_start.X) + ", " + str(line_start.Y) + ", " + str(line_start.Z) + ")")
    log("Line end = (" + str(line_end.X) + ", " + str(line_end.Y) + ", " + str(line_end.Z) + ")")
    cad.SetLineNormal(line_id, 0.0, 0.0, 1.0)

    line_param = cad.GetParameterAtDistance(line_id, 15.0)
    line_pt = cad.GetPointAtParameter(line_id, line_param)
    log("Line parameter at dist 15 = " + str(line_param))
    log("Line point at parameter = (" + str(line_pt.X) + ", " + str(line_pt.Y) + ", " + str(line_pt.Z) + ")")
    d1 = cad.GetCurveFirstDerivativeAtParameter(line_id, line_param)
    d2 = cad.GetCurveSecondDerivativeAtParameter(line_id, line_param)
    log("Line first derivative len = " + str(d1["length"]))
    log("Line second derivative len = " + str(d2["length"]))

    line_parts = cad.SplitCurveByParameters(line_id, [0.5])
    log("Split line by parameter -> " + str(len(line_parts)))

    arc_info = cad.GetArcInfo(arc_id)
    log("Arc radius = " + str(arc_info["radius"]))
    log("Arc total angle deg = " + str(cad.GetArcTotalAngleDegrees(arc_id)))
    arc_mid = cad.GetArcMidPoint(arc_id)
    log("Arc midpoint = (" + str(arc_mid.X) + ", " + str(arc_mid.Y) + ", " + str(arc_mid.Z) + ")")
    cad.SetArcAnglesDegrees(arc_id, 30.0, 240.0)
    log("Arc total angle deg dopo set = " + str(cad.GetArcTotalAngleDegrees(arc_id)))

    arc_param = cad.GetParameterAtDistance(arc_id, 10.0)
    arc_pt = cad.GetPointAtParameter(arc_id, arc_param)
    log("Arc parameter at dist 10 = " + str(arc_param))
    log("Arc point at parameter = (" + str(arc_pt.X) + ", " + str(arc_pt.Y) + ", " + str(arc_pt.Z) + ")")
    ad1 = cad.GetCurveFirstDerivativeAtParameter(arc_id, arc_param)
    ad2 = cad.GetCurveSecondDerivativeAtParameter(arc_id, arc_param)
    log("Arc first derivative len = " + str(ad1["length"]))
    log("Arc second derivative len = " + str(ad2["length"]))

    split_point = cad.GetPointAtDist(arc_id, 12.0)
    arc_parts = cad.SplitCurveByPoints(arc_id, [split_point.X, split_point.Y, split_point.Z])
    log("Split arc by point -> " + str(len(arc_parts)))

    cad.Regen()
    log("Test linee/archi avanzato completato")
