import clr
clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def msg(text):
    cad.Msg("[2D LOW TEST] " + text)


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


msg("Avvio test 2D low-level")

res = cad.GetPoint("Punto base 2D low-level test:")
if res.Status != PromptStatus.OK:
    msg("Annullato.")
else:
    p = res.Value

    mtext_id = cad.AddMText("MTEXT BASE", p.X + 5.0, p.Y + 35.0, p.Z, 4.0, 40.0)
    msg("MText creato")
    mtext_info = cad.GetMTextInfo(mtext_id)
    msg("MText width iniziale = " + str(mtext_info["width"]))
    cad.SetMTextContents(mtext_id, "MTEXT MOD")
    cad.SetMTextHeight(mtext_id, 5.5)
    cad.SetMTextWidth(mtext_id, 55.0)
    cad.SetMTextRotation(mtext_id, 12.5)
    cad.SetMTextLocation(mtext_id, p.X + 8.0, p.Y + 38.0, p.Z)
    mtext_info = cad.GetMTextInfo(mtext_id)
    msg("MText modificato -> contents=" + str(mtext_info["contents"]))

    ellipse_pairs = ArrayList()
    ellipse_pairs.Add(pair(0, "ELLIPSE"))
    ellipse_pairs.Add(pair(8, "PYLOAD_2DLOW"))
    ellipse_pairs.Add(pair(10, p.X))
    ellipse_pairs.Add(pair(20, p.Y))
    ellipse_pairs.Add(pair(30, p.Z))
    ellipse_pairs.Add(pair(11, 30.0))
    ellipse_pairs.Add(pair(21, 0.0))
    ellipse_pairs.Add(pair(31, 0.0))
    ellipse_pairs.Add(pair(40, 0.5))
    ellipse_pairs.Add(pair(41, 0.0))
    ellipse_pairs.Add(pair(42, 6.283185307179586))
    ellipse_id = cad.EntMake(ellipse_pairs)
    msg("Ellipse creata")
    ellipse_info = cad.GetEllipseInfo(ellipse_id)
    msg("Ellipse ratio iniziale = " + str(ellipse_info["radius_ratio"]))
    cad.SetEllipseCenter(ellipse_id, p.X + 10.0, p.Y, p.Z)
    cad.SetEllipseRadiusRatio(ellipse_id, 0.65)
    cad.SetEllipseAngles(ellipse_id, 0.0, 360.0)
    ellipse_info = cad.GetEllipseInfo(ellipse_id)
    msg("Ellipse modificata -> area=" + str(ellipse_info["area"]))

    hatch_id = cad.DrawHatch([
        p.X - 20.0, p.Y - 15.0,
        p.X + 20.0, p.Y - 15.0,
        p.X + 20.0, p.Y + 15.0,
        p.X - 20.0, p.Y + 15.0,
    ], "SOLID", 1.0, 0.0)
    msg("Hatch creato")
    hatch_info = cad.GetHatchInfo(hatch_id)
    msg("Hatch pattern iniziale = " + str(hatch_info["pattern_name"]))
    cad.SetHatchScale(hatch_id, 1.75)
    cad.SetHatchAngle(hatch_id, 22.5)
    hatch_info = cad.GetHatchInfo(hatch_id)
    msg("Hatch modificato -> angle=" + str(hatch_info["pattern_angle"]))

    ann_id = cad.AddMText("LEADER NOTE", p.X + 65.0, p.Y + 50.0, p.Z, 3.5, 35.0)
    leader_id = cad.AddLeader([
        p.X + 30.0, p.Y + 15.0, p.Z,
        p.X + 55.0, p.Y + 35.0, p.Z,
        p.X + 65.0, p.Y + 50.0, p.Z,
    ], ann_id)
    msg("Leader creato")
    cad.SetLeaderHasArrowHead(leader_id, True)
    cad.SetLeaderHasHookLine(leader_id, False)
    cad.SetLeaderAnnotation(leader_id, ann_id)
    leader_info = cad.GetLeaderInfo(leader_id)
    msg("Leader vertex_count = " + str(leader_info["vertex_count"]))
    msg("Leader hookline = " + str(leader_info["has_hook_line"]))

    sel_area = cad.GetSelectionByArea(500.0, 10000.0, "", "", True)
    msg("GetSelectionByArea -> " + str(len(sel_area)))

    sel_len = cad.GetSelectionByLength(50.0, 1000.0, "", "", False)
    msg("GetSelectionByLength -> " + str(len(sel_len)))

    sel_closed = cad.GetSelectionByClosed(True, "", "ELLIPSE")
    msg("GetSelectionByClosed ELLIPSE -> " + str(len(sel_closed)))

    hatch_filter = ArrayList()
    hatch_filter.Add(pair(0, "HATCH"))
    hatch_filter.Add(pair(2, "SOLID"))
    hatch_filter.Add(pair(52, ">=20"))
    hatch_filter = cad.GetSelectionByDxf(hatch_filter)
    msg("GetSelectionByDxf HATCH pattern+angle -> " + str(len(hatch_filter)))

    ellipse_filter = ArrayList()
    ellipse_filter.Add(pair(0, "ELLIPSE"))
    ellipse_filter.Add(pair(40, ">=0.6"))
    ellipse_filter.Add(pair(8, "PYLOAD_2DLOW"))
    ellipse_filter = cad.GetSelectionByDxf(ellipse_filter)
    msg("GetSelectionByDxf ELLIPSE ratio+layer -> " + str(len(ellipse_filter)))

    cad.Regen()
    msg("Test 2D low-level completato")
