import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList


def log(msg):
    cad.Msg("[MODIFY TEST] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio test batch modifica")

res = cad.GetPoint("Punto base modify test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def test_break_trim_line():
        line_id = cad.AddLine(p.X, p.Y, p.Z, p.X + 80.0, p.Y, p.Z)
        log("Linea base break/trim handle=" + cad.GetEntityHandle(line_id))

        broken = cad.BreakCurveAtPoint(line_id, p.X + 25.0, p.Y, p.Z, True)
        log("BreakCurveAtPoint -> parti=" + str(len(broken)))
        if len(broken) == 2:
            log("Break part lengths = " + str(cad.GetCurveLength(broken[0])) + "/" + str(cad.GetCurveLength(broken[1])))

        line_two = cad.AddLine(p.X, p.Y + 12.0, p.Z, p.X + 80.0, p.Y + 12.0, p.Z)
        removed = cad.BreakCurveAtTwoPoints(line_two, p.X + 20.0, p.Y + 12.0, p.Z, p.X + 60.0, p.Y + 12.0, p.Z, True)
        log("BreakCurveAtTwoPoints -> parti=" + str(len(removed)))
        if len(removed) == 2:
            log("Break outer lengths = " + str(cad.GetCurveLength(removed[0])) + "/" + str(cad.GetCurveLength(removed[1])))

        line_three = cad.AddLine(p.X, p.Y + 24.0, p.Z, p.X + 90.0, p.Y + 24.0, p.Z)
        kept_mid = cad.KeepCurveSegmentBetweenPoints(line_three, p.X + 15.0, p.Y + 24.0, p.Z, p.X + 55.0, p.Y + 24.0, p.Z, True)
        log("KeepCurveSegmentBetweenPoints length = " + str(cad.GetCurveLength(kept_mid)))

        line_four = cad.AddLine(p.X, p.Y + 36.0, p.Z, p.X + 90.0, p.Y + 36.0, p.Z)
        trimmed_start = cad.TrimCurveStartAtDistance(line_four, 30.0, True)
        log("TrimCurveStartAtDistance length = " + str(cad.GetCurveLength(trimmed_start)))

        line_five = cad.AddLine(p.X, p.Y + 48.0, p.Z, p.X + 90.0, p.Y + 48.0, p.Z)
        trimmed_end = cad.TrimCurveEndAtPoint(line_five, p.X + 65.0, p.Y + 48.0, p.Z, True)
        log("TrimCurveEndAtPoint length = " + str(cad.GetCurveLength(trimmed_end)))

    def test_reverse_and_extend():
        line_id = cad.AddLine(p.X + 110.0, p.Y, p.Z, p.X + 150.0, p.Y + 10.0, p.Z)
        line_before = cad.GetLineInfo(line_id)
        cad.ReverseCurve(line_id)
        line_after = cad.GetLineInfo(line_id)
        log("ReverseCurve LINE start x before/after = " + str(line_before["start_x"]) + "/" + str(line_after["start_x"]))

        poly_id = cad.AddLightWeightPolyline([p.X + 110.0, p.Y + 25.0, p.X + 135.0, p.Y + 25.0, p.X + 150.0, p.Y + 40.0], False)
        poly_before = cad.GetPolylineVertexAt(poly_id, 0)
        cad.ReverseCurve(poly_id)
        poly_after = cad.GetPolylineVertexAt(poly_id, 0)
        log("ReverseCurve POLY first vertex x before/after = " + str(poly_before.X) + "/" + str(poly_after.X))

        line_ext = cad.AddLine(p.X + 110.0, p.Y + 55.0, p.Z, p.X + 140.0, p.Y + 55.0, p.Z)
        new_end = cad.ExtendLineEndToPoint(line_ext, p.X + 170.0, p.Y + 55.0, p.Z)
        line_ext_info = cad.GetLineInfo(line_ext)
        log("ExtendLineEndToPoint new end x = " + str(line_ext_info["end_x"]) + " projected=" + str(new_end.X))

        poly_ext = cad.AddLightWeightPolyline([p.X + 110.0, p.Y + 70.0, p.X + 140.0, p.Y + 70.0, p.X + 155.0, p.Y + 85.0], False)
        new_poly_end = cad.ExtendPolylineEndToPoint(poly_ext, p.X + 175.0, p.Y + 105.0)
        poly_last = cad.GetPolylineVertexAt(poly_ext, cad.GetPolylineVertexCount(poly_ext) - 1)
        log("ExtendPolylineEndToPoint last x = " + str(poly_last.X) + " projected=" + str(new_poly_end.X))

    def test_join_and_batch_reverse():
        poly_join = cad.AddLightWeightPolyline([p.X + 220.0, p.Y, p.X + 250.0, p.Y], False)
        line_join = cad.AddLine(p.X + 250.0, p.Y, p.Z, p.X + 280.0, p.Y, p.Z)
        join_info = cad.JoinEntities(poly_join, ArrayList([line_join]), True)
        log("JoinEntities success/joined = " + str(join_info["success"]) + "/" + str(join_info["joined_count"]))
        log("JoinEntities erased sources = " + str(join_info["erased_joined_sources"]))
        poly_info = cad.GetPolylineInfo(poly_join)
        log("JoinEntities segment_count/length = " + str(poly_info["segment_count"]) + "/" + str(poly_info["length"]))

        rev_a = cad.AddLine(p.X + 220.0, p.Y + 20.0, p.Z, p.X + 245.0, p.Y + 25.0, p.Z)
        rev_b = cad.AddLine(p.X + 220.0, p.Y + 30.0, p.Z, p.X + 245.0, p.Y + 35.0, p.Z)
        changed = cad.ReverseCurves(ArrayList([rev_a, rev_b]))
        log("ReverseCurves changed = " + str(changed))

    def test_distance_variants():
        line_id = cad.AddLine(p.X + 320.0, p.Y, p.Z, p.X + 420.0, p.Y, p.Z)
        parts = cad.BreakCurveAtTwoDistances(line_id, 15.0, 75.0, True)
        log("BreakCurveAtTwoDistances -> parti=" + str(len(parts)))
        if len(parts) == 2:
            log("BreakCurveAtTwoDistances lengths = " + str(cad.GetCurveLength(parts[0])) + "/" + str(cad.GetCurveLength(parts[1])))

        line_mid = cad.AddLine(p.X + 320.0, p.Y + 15.0, p.Z, p.X + 420.0, p.Y + 15.0, p.Z)
        mid = cad.KeepCurveSegmentBetweenDistances(line_mid, 20.0, 60.0, True)
        log("KeepCurveSegmentBetweenDistances length = " + str(cad.GetCurveLength(mid)))

        line_rm = cad.AddLine(p.X + 320.0, p.Y + 30.0, p.Z, p.X + 420.0, p.Y + 30.0, p.Z)
        remain = cad.RemoveCurveSegmentBetweenDistances(line_rm, 25.0, 65.0, True)
        log("RemoveCurveSegmentBetweenDistances -> parti=" + str(len(remain)))

    safe("break_trim_line", test_break_trim_line)
    safe("reverse_and_extend", test_reverse_and_extend)
    safe("join_and_batch_reverse", test_join_and_batch_reverse)
    safe("distance_variants", test_distance_variants)

    cad.RegenNative()
    log("Test batch modifica completato")
