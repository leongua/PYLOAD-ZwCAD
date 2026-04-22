import math
import clr
clr.AddReference("ZwManaged")

from ZwSoft.ZwCAD.EditorInput import PromptStatus

ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Avvio test avanzato")
res = cad.GetPoint("Punto base circle/spline/hatch advanced test: ")
if res.Status != PromptStatus.OK:
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Annullato")
else:
    p = res.Value

    circle = cad.AddCircle(p.X, p.Y, p.Z, 18.0)
    spline = cad.AddSpline([
        p.X - 40.0, p.Y + 40.0, p.Z,
        p.X - 10.0, p.Y + 75.0, p.Z,
        p.X + 25.0, p.Y + 35.0, p.Z,
        p.X + 60.0, p.Y + 70.0, p.Z
    ])
    hatch = cad.DrawHatch([
        p.X - 20.0, p.Y - 20.0,
        p.X + 20.0, p.Y - 20.0,
        p.X + 20.0, p.Y + 20.0,
        p.X - 20.0, p.Y + 20.0
    ], "SOLID", 1.0, 0.0)

    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Entita create")

    circle_info = cad.GetCircleInfo(circle)
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Circle radius = %s" % circle_info["radius"])
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Circle diameter = %s" % cad.GetCircleDiameter(circle))
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Circle circumference = %s" % cad.GetCircleCircumference(circle))

    circle_start = cad.GetCircleStartPoint(circle)
    circle_end = cad.GetCircleEndPoint(circle)
    circle_at_90 = cad.GetCirclePointAtAngleDegrees(circle, 90.0)
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Circle start = (%s, %s, %s)" % (circle_start.X, circle_start.Y, circle_start.Z))
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Circle end = (%s, %s, %s)" % (circle_end.X, circle_end.Y, circle_end.Z))
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Circle point 90deg = (%s, %s, %s)" % (circle_at_90.X, circle_at_90.Y, circle_at_90.Z))

    cad.SetCircleDiameter(circle, 44.0)
    cad.SetCircleThickness(circle, 2.0)
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Circle diameter dopo set = %s" % cad.GetCircleDiameter(circle))

    spline_info = cad.GetSplineInfo(spline)
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline degree = %s" % spline_info["degree"])
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline control points = %s" % spline_info["control_point_count"])
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline fit points = %s" % spline_info["fit_point_count"])

    spline_start_param = cad.GetSplineStartParameter(spline)
    spline_end_param = cad.GetSplineEndParameter(spline)
    spline_mid_param = (spline_start_param + spline_end_param) * 0.5
    spline_mid = cad.GetSplinePointAtParameter(spline, spline_mid_param)
    spline_param_at_dist = cad.GetSplineParameterAtDistance(spline, spline_info["length"] * 0.25)
    spline_point_25 = cad.GetSplinePointAtPercent(spline, 0.25)
    spline_param_at_start = cad.GetSplineParameterAtPoint(spline, spline_info["start_x"], spline_info["start_y"], spline_info["start_z"])
    spline_start_tan = cad.GetSplineStartTangent(spline)
    spline_end_tan = cad.GetSplineEndTangent(spline)

    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline params = %s -> %s" % (spline_start_param, spline_end_param))
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline mid point = (%s, %s, %s)" % (spline_mid.X, spline_mid.Y, spline_mid.Z))
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline param at dist 25%% = %s" % spline_param_at_dist)
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline point 25%% = (%s, %s, %s)" % (spline_point_25.X, spline_point_25.Y, spline_point_25.Z))
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline parameter at start point = %s" % spline_param_at_start)
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline start tangent len = %s" % math.sqrt(spline_start_tan["x"] ** 2 + spline_start_tan["y"] ** 2 + spline_start_tan["z"] ** 2))
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Spline end tangent len = %s" % math.sqrt(spline_end_tan["x"] ** 2 + spline_end_tan["y"] ** 2 + spline_end_tan["z"] ** 2))

    hatch_info = cad.GetHatchInfo(hatch)
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Hatch pattern = %s" % hatch_info["pattern_name"])
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Hatch type = %s" % hatch_info["pattern_type"])
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Hatch loops = %s" % hatch_info["loop_count"])
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Hatch associative = %s" % hatch_info["associative"])

    cad.SetHatchScale(hatch, 1.75)
    cad.SetHatchAngle(hatch, 30.0)
    cad.SetHatchElevation(hatch, 3.0)
    cad.SetHatchNormal(hatch, 0.0, 0.0, 1.0)
    cad.SetHatchAssociative(hatch, True)
    hatch_info2 = cad.GetHatchInfo(hatch)
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Hatch scale dopo set = %s" % hatch_info2["pattern_scale"])
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Hatch angle dopo set = %s" % hatch_info2["pattern_angle"])
    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Hatch elevation dopo set = %s" % hatch_info2["elevation"])

    ed.WriteMessage("\n[CIRCLE/SPLINE/HATCH TEST] Test avanzato completato")
