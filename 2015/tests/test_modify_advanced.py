import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList


def log(msg):
    cad.Msg("[MODIFY ADV TEST] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio test batch modifica avanzata")

res = cad.GetPoint("Punto base modify advanced test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def test_fillet_chamfer():
        l1 = cad.AddLine(p.X, p.Y, p.Z, p.X + 60.0, p.Y, p.Z)
        l2 = cad.AddLine(p.X, p.Y, p.Z, p.X, p.Y + 60.0, p.Z)
        fillet = cad.FilletLines(l1, l2, 10.0, True)
        log("FilletLines arc handle = " + str(fillet["arc_handle"]))
        log("Fillet tangency = " + str(fillet["tangent1_x"]) + "/" + str(fillet["tangent2_y"]))
        l1_info = cad.GetLineInfo(l1)
        l2_info = cad.GetLineInfo(l2)
        log("Fillet trim line end/start = " + str(l1_info["start_x"]) + "/" + str(l2_info["start_y"]))

        c1 = cad.AddLine(p.X + 90.0, p.Y, p.Z, p.X + 150.0, p.Y, p.Z)
        c2 = cad.AddLine(p.X + 90.0, p.Y, p.Z, p.X + 90.0, p.Y + 60.0, p.Z)
        chamfer = cad.ChamferLines(c1, c2, 12.0, 8.0, True)
        log("ChamferLines line handle = " + str(chamfer["line_handle"]))
        log("Chamfer cut points = " + str(chamfer["cut1_x"]) + "/" + str(chamfer["cut2_y"]))

    def test_auto_trim_extend():
        trim_target = cad.AddLine(p.X + 190.0, p.Y + 35.0, p.Z, p.X + 250.0, p.Y + 35.0, p.Z)
        trim_curve = cad.AddLine(p.X + 205.0, p.Y, p.Z, p.X + 205.0, p.Y + 80.0, p.Z)
        trimmed = cad.TrimCurveEndToEntity(trim_curve, trim_target, True)
        log("TrimCurveEndToEntity length = " + str(cad.GetCurveLength(trimmed)))

        ext_target = cad.AddLine(p.X + 280.0, p.Y + 45.0, p.Z, p.X + 340.0, p.Y + 45.0, p.Z)
        ext_curve = cad.AddLine(p.X + 295.0, p.Y, p.Z, p.X + 295.0, p.Y + 20.0, p.Z)
        new_end = cad.ExtendCurveEndToEntity(ext_curve, ext_target)
        log("ExtendCurveEndToEntity y = " + str(new_end.Y))

        poly_target = cad.AddLine(p.X + 360.0, p.Y + 55.0, p.Z, p.X + 455.0, p.Y + 55.0, p.Z)
        poly = cad.AddLightWeightPolyline([p.X + 390.0, p.Y, p.X + 390.0, p.Y + 20.0, p.X + 410.0, p.Y + 35.0], False)
        new_poly = cad.ExtendCurveEndToEntity(poly, poly_target)
        last = cad.GetPolylineVertexAt(poly, cad.GetPolylineVertexCount(poly) - 1)
        log("ExtendCurveEndToEntity POLY last y = " + str(last.Y) + " target=" + str(new_poly.Y))

    def test_stretch():
        line_id = cad.AddLine(p.X, p.Y + 110.0, p.Z, p.X + 50.0, p.Y + 110.0, p.Z)
        pl_id = cad.AddLightWeightPolyline([p.X, p.Y + 125.0, p.X + 20.0, p.Y + 125.0, p.X + 40.0, p.Y + 140.0], False)
        txt_id = cad.AddText("STRETCH", p.X + 10.0, p.Y + 150.0, p.Z, 4.0)
        ids = ArrayList([line_id, pl_id, txt_id])
        info = cad.StretchEntitiesCrossingWindow(ids, p.X - 1.0, p.Y + 108.0, p.Z - 1.0, p.X + 25.0, p.Y + 152.0, p.Z + 1.0, 15.0, 5.0, 0.0, True)
        log("StretchEntities touched = " + str(info["touched_entities"]))
        log("StretchEntities moved vertices/entities = " + str(info["moved_vertices"]) + "/" + str(info["moved_whole_entities"]))
        line_info = cad.GetLineInfo(line_id)
        log("Stretch line start x = " + str(line_info["start_x"]))

    def test_batch_transform_options():
        base = cad.AddCircle(p.X + 470.0, p.Y + 20.0, p.Z, 10.0)
        copies = cad.CopyEntityMultiple(base, [20.0, 0.0, 0.0, 40.0, 0.0, 0.0, 60.0, 10.0, 0.0])
        log("CopyEntityMultiple count = " + str(len(copies)))

        base2 = cad.AddLine(p.X + 470.0, p.Y + 55.0, p.Z, p.X + 500.0, p.Y + 55.0, p.Z)
        copied = cad.CopyEntities(ArrayList([base2]), 0.0, 18.0, 0.0)
        log("CopyEntities count = " + str(len(copied)))

        offs = cad.OffsetEntityBothSides(base2, 6.0)
        log("OffsetEntityBothSides count = " + str(len(offs)))

        arr_src = cad.AddCircle(p.X + 540.0, p.Y + 20.0, p.Z, 6.0)
        arr = cad.ArrayRectangularEntityEx(arr_src, 2, 3, 1, 18.0, 18.0, 0.0, False)
        log("ArrayRectangularEntityEx count = " + str(len(arr)))

        pol_src = cad.AddLine(p.X + 540.0, p.Y + 70.0, p.Z, p.X + 555.0, p.Y + 70.0, p.Z)
        pol = cad.ArrayPolarEntityEx(pol_src, 5, p.X + 540.0, p.Y + 70.0, p.Z, 360.0, True, False)
        log("ArrayPolarEntityEx count = " + str(len(pol)))

    safe("fillet_chamfer", test_fillet_chamfer)
    safe("auto_trim_extend", test_auto_trim_extend)
    safe("stretch", test_stretch)
    safe("batch_transform_options", test_batch_transform_options)

    cad.RegenNative()
    log("Test batch modifica avanzata completato")
