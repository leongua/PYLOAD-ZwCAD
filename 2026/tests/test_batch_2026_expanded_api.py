import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[EXPANDED 2026] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio test API 2026 estese")
res = cad.GetPoint("Punto base expanded 2026:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    ids = {}

    def step_commands():
        cad.ClearShellTranscript()
        cad.RunCommand("_.REGEN")
        cad.RunCommands(ArrayList(["_.REGEN", "_.REGEN"]))
        cad.SendEnter()
        cad.Command(ArrayList(["_.REGEN"]))
        cad.Princ("[EXPANDED 2026] command pipeline ok", True)

        try:
            if hasattr(cad, "SupportsSystemVariables") and not cad.SupportsSystemVariables():
                log("Commands ok, sysvar API non disponibile in questa build")
                return
            vars_map = cad.GetVars(ArrayList(["CVPORT", "CMDECHO"]))
            log("Commands ok, vars=" + str(vars_map.Count) + " transcript=" + str(len(cad.GetShellTranscript())))
        except Exception as ex:
            log("Commands ok, GetVars non disponibile: " + str(ex))

    def step_geometry_curves():
        ids["line"] = cad.AddLine(p.X, p.Y, p.Z, p.X + 80.0, p.Y + 20.0, p.Z)
        ids["circle"] = cad.AddCircle(p.X + 100.0, p.Y + 10.0, p.Z, 18.0)
        ids["poly"] = cad.AddPolyline([
            p.X, p.Y + 60.0,
            p.X + 40.0, p.Y + 60.0,
            p.X + 55.0, p.Y + 80.0,
            p.X + 15.0, p.Y + 95.0
        ], False)

        cad.SetCircleRadius(ids["circle"], 22.0)
        cad.SetCircleCenter(ids["circle"], p.X + 105.0, p.Y + 12.0, p.Z)
        cad.SetCircleThickness(ids["circle"], 1.2)
        cad.SetCircleNormal(ids["circle"], 0.0, 0.0, 1.0)

        length_line = cad.GetCurveLength(ids["line"])
        atdist = cad.GetPointAtDist(ids["line"], 20.0)
        par = cad.GetParameterAtDistance(ids["line"], 20.0)
        d1 = cad.GetCurveFirstDerivativeAtParameter(ids["line"], par)
        d2 = cad.GetCurveSecondDerivativeAtParameter(ids["line"], par)

        seg_count = cad.GetPolylineSegmentCount(ids["poly"])
        v_count = cad.GetPolylineVertexCount(ids["poly"])
        cad.SetBulgeAt(ids["poly"], 0, 0.25)
        cad.SetStartWidthAt(ids["poly"], 0, 1.0)
        cad.SetEndWidthAt(ids["poly"], 0, 1.8)
        bulge0 = cad.GetBulgeAt(ids["poly"], 0)
        width0 = cad.GetStartWidthAt(ids["poly"], 0)
        p25 = cad.GetPolylinePointAtPercent(ids["poly"], 0.25)

        log(
            "Curves ok, line len={0}, atdist_x={1}, d1={2}, d2={3}, poly seg/vert={4}/{5}, bulge0={6}, w0={7}, p25_x={8}".format(
                length_line,
                atdist.X,
                d1["length"],
                d2["length"],
                seg_count,
                v_count,
                bulge0,
                width0,
                p25.X,
            )
        )

    def step_selection_transform():
        ids["sel_line"] = cad.AddLine(p.X + 160.0, p.Y, p.Z, p.X + 210.0, p.Y, p.Z)
        copied = cad.CopyEntity(ids["sel_line"], 0.0, 18.0, 0.0)
        cad.MoveEntity(copied, 8.0, 0.0, 0.0)
        cad.RotateEntity(copied, p.X + 160.0, p.Y + 18.0, p.Z, 20.0)
        cad.ScaleEntity(copied, p.X + 160.0, p.Y + 18.0, p.Z, 1.1)
        mirrored = cad.MirrorEntity(copied, p.X + 160.0, p.Y - 10.0, p.Z, p.X + 160.0, p.Y + 40.0, p.Z, False)
        try:
            offs = cad.OffsetEntity(ids["sel_line"], 6.0)
        except Exception:
            offs = []
        try:
            ex = cad.ExplodeEntity(mirrored, False)
        except Exception:
            ex = []
        all_sel = cad.SelectAll()
        by_layer = cad.GetSelectionByLayer("0")
        by_type = cad.GetSelectionByType("Line")
        log("Selection/Transform ok, copy={0}, mirror={1}, offset={2}, explode={3}, all={4}, layer0={5}, line={6}".format(
            copied, mirrored, len(offs), len(ex), len(all_sel), len(by_layer), len(by_type)
        ))

    def step_blocks_batch():
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
            log("Blocks batch saltato: nessun blocco con attributi nel DWG")
            return

        br1 = cad.InsertBlock(rich, p.X + 250.0, p.Y + 10.0, p.Z)
        br2 = cad.InsertBlock(rich, p.X + 280.0, p.Y + 10.0, p.Z)
        changed_sync = cad.SyncBlockReferenceAttributes(br1, False)
        changed_batch = cad.SyncBlockReferenceAttributesBatch(ArrayList([br1, br2]), False)

        defs = cad.GetBlockAttributeDefinitions(rich)
        tag = None
        for k in defs.Keys:
            tag = str(k)
            break

        upd_tag = 0
        upd_map = 0
        if tag is not None:
            upd_tag = cad.UpdateBlockAttributeByTagBatch(ArrayList([br1, br2]), tag, "EXP2026")
            m = Hashtable()
            m[tag] = "MAP2026"
            upd_map = cad.UpdateBlockAttributesByMapBatch(ArrayList([br1, br2]), m)
            renamed = cad.RenameBlockAttributeTag(rich, tag, tag + "_N", True)
            log("Blocks rename defs/refs={0}/{1}".format(renamed["changed_definitions"], renamed["changed_references"]))

        repl = br1
        if alt is not None:
            repl = cad.ReplaceBlockReference(br1, alt, True, False)
            cad.ReplaceBlockReferencesBatch(ArrayList([br2]), alt, True, False)

        exploded = cad.ExplodeBlockReferenceEx(repl, False, True)
        log("Blocks batch ok, sync={0}/{1}, upd={2}/{3}, exploded={4}".format(changed_sync, changed_batch, upd_tag, upd_map, len(exploded)))

    safe("commands", step_commands)
    safe("geometry_curves", step_geometry_curves)
    safe("selection_transform", step_selection_transform)
    safe("blocks_batch", step_blocks_batch)

    cad.RegenNative()
    log("Zoom APIs non eseguite in questo smoke per evitare prompt interattivi residui")
    log("Test API 2026 estese completato")
