import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[MASTER 2026 FIX4] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


def tv(code, value):
    h = Hashtable()
    h["type_code"] = code
    h["value"] = value
    return h


log("Avvio smoke test completo ZWCAD 2026")
try:
    log("Build marker = " + str(cad.GetBuildMarker()))
except Exception as ex:
    log("Build marker non disponibile: " + str(ex))

res = cad.GetPoint("Punto base smoke test 2026 FIX4:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    state = {}

    def step_commands():
        cad.ClearShellTranscript()
        cad.RunLisp('(princ "\\n[PYLOAD 2026 FIX4] ok")')
        cad.Princ("[PYLOAD 2026 FIX4] editor ok", True)
        log("Commands/LISP ok, transcript=" + str(len(cad.GetShellTranscript())))

    def step_geometry():
        state["line"] = cad.AddLine(p.X, p.Y, p.Z, p.X + 60.0, p.Y + 15.0, p.Z)
        log("Geometry checkpoint: line ok")
        state["circle"] = cad.AddCircle(p.X + 95.0, p.Y + 10.0, p.Z, 14.0)
        log("Geometry checkpoint: circle ok")
        state["arc"] = cad.AddArc(p.X + 135.0, p.Y + 10.0, p.Z, 16.0, 25.0, 160.0)
        log("Geometry checkpoint: arc ok")
        state["point"] = cad.AddPoint(p.X + 165.0, p.Y + 25.0, p.Z)
        log("Geometry checkpoint: point ok")
        state["text"] = cad.AddText("PYLOAD 2026 FIX4", p.X + 10.0, p.Y + 45.0, p.Z, 4.0)
        log("Geometry checkpoint: text ok")
        state["mtext"] = cad.AddMText("MASTER 2026 FIX4", p.X + 65.0, p.Y + 50.0, p.Z, 4.0, 38.0)
        log("Geometry checkpoint: mtext ok")
        state["leader_note"] = cad.AddMText("NOTE", p.X + 210.0, p.Y + 45.0, p.Z, 3.5, 25.0)
        log("Geometry checkpoint: leader note ok")
        state["leader"] = cad.AddLeader([
            p.X + 180.0, p.Y + 10.0, p.Z,
            p.X + 200.0, p.Y + 30.0, p.Z,
            p.X + 210.0, p.Y + 45.0, p.Z
        ], state["leader_note"])
        log("Geometry checkpoint: leader ok")
        state["poly"] = cad.AddPolyline([
            p.X, p.Y + 70.0,
            p.X + 35.0, p.Y + 70.0,
            p.X + 50.0, p.Y + 90.0,
            p.X + 15.0, p.Y + 105.0,
            p.X - 5.0, p.Y + 85.0,
        ], True)
        log("Geometry checkpoint: poly ok")
        state["hatch"] = cad.DrawHatch([
            p.X + 230.0, p.Y,
            p.X + 260.0, p.Y,
            p.X + 260.0, p.Y + 20.0,
            p.X + 230.0, p.Y + 20.0,
        ], "SOLID", 1.0, 0.0)
        log("Geometry checkpoint: hatch ok")

        ellipse = ArrayList()
        ellipse.Add(pair(0, "ELLIPSE"))
        ellipse.Add(pair(8, "PYLOAD2026"))
        ellipse.Add(pair(10, p.X + 290.0))
        ellipse.Add(pair(20, p.Y + 10.0))
        ellipse.Add(pair(30, p.Z))
        ellipse.Add(pair(11, 30.0))
        ellipse.Add(pair(21, 0.0))
        ellipse.Add(pair(31, 0.0))
        ellipse.Add(pair(40, 0.6))
        ellipse.Add(pair(41, 0.0))
        ellipse.Add(pair(42, 6.283185307179586))
        state["ellipse"] = cad.EntMake(ellipse)
        log("Geometry checkpoint: ellipse entmake ok")

        cad.SetCircleDiameter(state["circle"], 40.0)
        cad.SetMTextContents(state["mtext"], "MASTER 2026 FIX4 OK")
        cad.SetLeaderHasArrowHead(state["leader"], True)
        cad.SetLeaderHasHookLine(state["leader"], False)
        cad.SetBulgeAt(state["poly"], 0, 0.35)
        cad.SetStartWidthAt(state["poly"], 0, 1.5)
        cad.SetEndWidthAt(state["poly"], 0, 2.5)
        cad.SetPolylineElevation(state["poly"], 3.0)
        cad.SetPolylineThickness(state["poly"], 1.0)

        log("Geometry checkpoint: entities created")
        poly_info = cad.GetPolylineInfo(state["poly"])
        log("Geometry checkpoint: poly info ok")
        ellipse_info = cad.GetEllipseInfo(state["ellipse"])
        log("Geometry checkpoint: ellipse info ok")
        hatch_info = cad.GetHatchInfo(state["hatch"])
        log("Geometry ok, poly segments=" + str(poly_info["segment_count"]) + ", ellipse ratio=" + str(ellipse_info["radius_ratio"]) + ", hatch pattern=" + str(hatch_info["pattern_name"]))

    def step_dxf():
        circle = ArrayList()
        circle.Add(pair(0, "CIRCLE"))
        circle.Add(pair(8, "PYLOAD2026_DXF"))
        circle.Add(pair(10, p.X + 340.0))
        circle.Add(pair(20, p.Y + 10.0))
        circle.Add(pair(30, p.Z))
        circle.Add(pair(40, 12.0))
        state["dxf_circle"] = cad.EntMake(circle)
        log("DXF checkpoint: circle entmake ok")
        cad.SetEntityDxfValue(state["dxf_circle"], 40, 18.0)
        log("DXF checkpoint: circle set radius ok")

        txt = ArrayList()
        txt.Add(pair(0, "TEXT"))
        txt.Add(pair(8, "PYLOAD2026_DXF"))
        txt.Add(pair(10, p.X + 370.0))
        txt.Add(pair(20, p.Y + 10.0))
        txt.Add(pair(30, p.Z))
        txt.Add(pair(40, 4.0))
        txt.Add(pair(1, "DXF 2026 FIX4"))
        txt.Add(pair(41, 0.9))
        txt.Add(pair(51, 12.0))
        state["dxf_text"] = cad.EntMake(txt)
        log("DXF checkpoint: text entmake ok")

        filters = ArrayList()
        filters.Add(pair(0, "CIRCLE"))
        filters.Add(pair(8, "PYLOAD2026_DXF"))
        filters.Add(pair(10, str(p.X + 340.0)))
        filters.Add(pair(20, str(p.Y + 10.0)))
        filters.Add(pair(40, ">=18"))
        hits = cad.GetSelectionByDxf(filters)
        log("DXF checkpoint: selection ok")
        owner = cad.GetEntityDxfValue(state["dxf_text"], 330)
        log("DXF ok, circle hits=" + str(len(hits)) + ", text owner=" + str(owner))

    def step_blocks_attrs():
        names = cad.GetBlockNames()
        rich = None
        alt = None
        for name in names:
            info = cad.GetBlockDefinitionInfo(str(name))
            if rich is None and info["has_attribute_definitions"]:
                rich = str(name)
            if alt is None and str(name) != rich:
                alt = str(name)
            if rich is not None and alt is not None:
                break

        if rich is None:
            log("Blocks/ATTRIB saltato: nessun blocco con attributi nel DWG")
            return

        state["br1"] = cad.InsertBlock(rich, p.X + 420.0, p.Y + 5.0, p.Z)
        state["br2"] = cad.InsertBlock(rich, p.X + 450.0, p.Y + 5.0, p.Z)
        cad.SyncBlockReferenceAttributesBatch(ArrayList([state["br1"], state["br2"]]), False)

        defs = cad.GetBlockAttributeDefinitions(rich)
        first_tag = None
        for key in defs.Keys:
            first_tag = str(key)
            break

        if first_tag is not None:
            cad.UpdateBlockAttributeByTagBatch(ArrayList([state["br1"], state["br2"]]), first_tag, "MASTER2026FIX4")
            vals = Hashtable()
            vals[first_tag] = "MAP2026FIX4"
            cad.UpdateBlockAttributesByMapBatch(ArrayList([state["br1"], state["br2"]]), vals)

        if alt is not None:
            repl = cad.ReplaceBlockReference(state["br1"], alt, True, False)
            repl_info = cad.GetBlockReferenceInfo(repl)
            log("Blocks ok, replace -> " + str(repl_info["name"]))
        else:
            log("Blocks ok, refs=" + cad.GetEntityHandle(state["br1"]) + "/" + cad.GetEntityHandle(state["br2"]))

    def step_modify():
        base = cad.AddLine(p.X, p.Y + 140.0, p.Z, p.X + 80.0, p.Y + 140.0, p.Z)
        parts = cad.BreakCurveAtPoint(base, p.X + 25.0, p.Y + 140.0, p.Z, True)
        log("Modify break parts=" + str(len(parts)))

        line = cad.AddLine(p.X + 95.0, p.Y + 140.0, p.Z, p.X + 135.0, p.Y + 140.0, p.Z)
        cad.ReverseCurve(line)
        new_end = cad.ExtendLineEndToPoint(line, p.X + 155.0, p.Y + 140.0, p.Z)

        poly = cad.AddLightWeightPolyline([p.X + 165.0, p.Y + 140.0, p.X + 190.0, p.Y + 140.0, p.X + 205.0, p.Y + 155.0], False)
        cad.ReverseCurve(poly)
        new_poly = cad.ExtendPolylineEndToPoint(poly, p.X + 215.0, p.Y + 165.0, p.Z)

        j1 = cad.AddLine(p.X + 230.0, p.Y + 140.0, p.Z, p.X + 250.0, p.Y + 140.0, p.Z)
        j2 = cad.AddLine(p.X + 250.0, p.Y + 140.0, p.Z, p.X + 270.0, p.Y + 140.0, p.Z)
        join = cad.JoinEntities(ArrayList([j1, j2]), True)

        f1 = cad.AddLine(p.X + 290.0, p.Y + 140.0, p.Z, p.X + 340.0, p.Y + 140.0, p.Z)
        f2 = cad.AddLine(p.X + 290.0, p.Y + 140.0, p.Z, p.X + 290.0, p.Y + 190.0, p.Z)
        fillet = cad.FilletLines(f1, f2, 8.0, True)

        c1 = cad.AddLine(p.X + 360.0, p.Y + 140.0, p.Z, p.X + 410.0, p.Y + 140.0, p.Z)
        c2 = cad.AddLine(p.X + 360.0, p.Y + 140.0, p.Z, p.X + 360.0, p.Y + 190.0, p.Z)
        chamfer = cad.ChamferLines(c1, c2, 10.0, 6.0, True)

        arr_src = cad.AddCircle(p.X + 440.0, p.Y + 155.0, p.Z, 5.0)
        arr = cad.ArrayRectangularEntityEx(arr_src, 2, 3, 1, 15.0, 15.0, 0.0, False)
        log("Modify ok, extend line x=" + str(new_end.X) + ", extend poly x=" + str(new_poly.X) + ", join=" + str(join["joined_count"]) + ", fillet=" + str(fillet["arc_handle"]) + ", chamfer=" + str(chamfer["line_handle"]) + ", array=" + str(len(arr)))

    def step_database():
        dict_id = cad.CreateNamedDictionary("PYLOAD2026/META/SESSION")
        typed = ArrayList()
        typed.Add(tv(1000, "hello2026fix4"))
        typed.Add(tv(1070, 264))
        cad.SetNamedXRecord("PYLOAD2026/META/SESSION", "REC1", typed)

        smap = Hashtable()
        smap["target"] = "2026fix4"
        smap["kind"] = "master"
        cad.SetNamedStringMap("PYLOAD2026/META/SESSION", "MAP1", smap)

        line = cad.AddLine(p.X + 520.0, p.Y + 140.0, p.Z, p.X + 560.0, p.Y + 140.0, p.Z)
        cad.SetEntityStringMap(line, "PYLOAD_META/LOCAL", "INFO", smap)
        ms_id = cad.GetModelSpaceRecordId()
        clones = cad.CloneObjectsToOwner(ArrayList([line]), ms_id)

        target_dict = cad.CreateNamedDictionary("PYLOAD2026/META/SESSION2")
        cad.CopyXRecordBetweenDictionaries(dict_id, "REC1", target_dict, "REC1_COPY", True)
        copied = cad.GetXRecordData(target_dict, "REC1_COPY")
        tree = cad.ListNamedDictionaryTree("PYLOAD2026", 3)
        log("Database ok, clones=" + str(len(clones)) + ", xrecord count=" + str(copied["count"]) + ", tree=" + str(len(tree)))

    safe("commands", step_commands)
    safe("geometry", step_geometry)
    safe("dxf", step_dxf)
    safe("blocks_attrs", step_blocks_attrs)
    safe("modify", step_modify)
    safe("database", step_database)

    cad.RegenNative()
    cad.ZoomExtents()
    log("Smoke test completo ZWCAD 2026 completato")
