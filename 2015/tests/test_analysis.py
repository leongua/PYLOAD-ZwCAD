import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[ANALYSIS TEST] " + msg)


log("Avvio test analisi geometrica")

res = cad.GetPoint("Punto base analysis test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    line1 = cad.AddLine(p.X - 40.0, p.Y, p.Z, p.X + 40.0, p.Y, p.Z)
    line2 = cad.AddLine(p.X, p.Y - 30.0, p.Z, p.X, p.Y + 30.0, p.Z)
    circle = cad.AddCircle(p.X + 80.0, p.Y, p.Z, 18.0)
    spline = cad.AddSpline([
        p.X - 20.0, p.Y + 50.0, p.Z,
        p.X + 10.0, p.Y + 75.0, p.Z,
        p.X + 45.0, p.Y + 40.0, p.Z,
        p.X + 75.0, p.Y + 65.0, p.Z
    ])

    log("Entita create")

    hits = cad.GetIntersections(line1, line2)
    log("Intersections line1-line2 = " + str(len(hits)))
    for i, hit in enumerate(hits):
        log(" hit" + str(i) + " = (" + str(hit["x"]) + ", " + str(hit["y"]) + ", " + str(hit["z"]) + ")")

    ext_line = cad.AddLine(p.X - 60.0, p.Y - 20.0, p.Z, p.X - 10.0, p.Y - 20.0, p.Z)
    ext_circle = cad.AddCircle(p.X + 15.0, p.Y - 20.0, p.Z, 20.0)
    direct_count = cad.CountIntersections(ext_line, ext_circle, "both")
    extend_count = cad.CountIntersections(ext_line, ext_circle, "extend_both")
    log("CountIntersections both = " + str(direct_count))
    log("CountIntersections extend_both = " + str(extend_count))

    box = cad.GetBoundingBox(circle)
    log("Circle bbox size = (" + str(box["size_x"]) + ", " + str(box["size_y"]) + ", " + str(box["size_z"]) + ")")

    start = cad.GetCurveStartPoint(spline)
    end = cad.GetCurveEndPoint(spline)
    mid = cad.GetCurveMidPoint(spline)
    log("Spline start = (" + str(start.X) + ", " + str(start.Y) + ", " + str(start.Z) + ")")
    log("Spline end = (" + str(end.X) + ", " + str(end.Y) + ", " + str(end.Z) + ")")
    log("Spline mid = (" + str(mid.X) + ", " + str(mid.Y) + ", " + str(mid.Z) + ")")

    samples = cad.GetCurveSamplePoints(spline, 4)
    log("Spline sample points = " + str(len(samples)))
    for item in samples:
        log(" sample[" + str(item["index"]) + "] d=" + str(item["distance"]) + " -> (" + str(item["x"]) + ", " + str(item["y"]) + ", " + str(item["z"]) + ")")

    is_on_1 = cad.IsPointOnCurve(line1, p.X + 10.0, p.Y, p.Z, 1e-6)
    is_on_2 = cad.IsPointOnCurve(line1, p.X + 10.0, p.Y + 5.0, p.Z, 1e-6)
    log("IsPointOnCurve(line1, on) = " + str(is_on_1))
    log("IsPointOnCurve(line1, off) = " + str(is_on_2))

    poly = cad.AddPolyline([
        p.X + 110.0, p.Y - 10.0,
        p.X + 150.0, p.Y - 10.0,
        p.X + 155.0, p.Y + 20.0,
        p.X + 125.0, p.Y + 35.0,
        p.X + 105.0, p.Y + 15.0
    ], True)
    log("Polyline area = " + str(cad.GetEntityArea(poly)))
    log("Polyline perimeter = " + str(cad.GetEntityPerimeter(poly)))

    arc = cad.AddArc(p.X + 200.0, p.Y, p.Z, 22.0, 20.0, 160.0)
    arc_metrics = cad.GetEntityMetrics(arc)
    log("Arc metrics area = " + str(arc_metrics["area"]))
    log("Arc metrics perimeter = " + str(arc_metrics["perimeter"]))

    ids = [circle, poly, arc, line1]
    log("SumEntityAreas = " + str(cad.SumEntityAreas(ids)))
    log("SumEntityPerimeters = " + str(cad.SumEntityPerimeters(ids)))

    summary = cad.BuildMetricsSummary(ids)
    log("Metrics total_area = " + str(summary["total_area"]))
    log("Metrics total_perimeter = " + str(summary["total_perimeter"]))
    for key in summary["by_type_area"].Keys:
        log(" by_type_area " + str(key) + " = " + str(summary["by_type_area"][key]))
    for key in summary["by_type_perimeter"].Keys:
        log(" by_type_perimeter " + str(key) + " = " + str(summary["by_type_perimeter"][key]))

    cad.Regen()
    cad.ZoomExtents()
    log("Test analisi geometrica completato")
