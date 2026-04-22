import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[HEAVY TEST] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio test heavy modify + blocks")

res = cad.GetPoint("Punto base heavy test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def test_heavy_modify():
        c1 = cad.AddLine(p.X, p.Y, p.Z, p.X + 110.0, p.Y, p.Z)
        c2 = cad.AddLine(p.X + 40.0, p.Y - 20.0, p.Z, p.X + 40.0, p.Y + 20.0, p.Z)
        c3 = cad.AddLine(p.X + 80.0, p.Y - 20.0, p.Z, p.X + 80.0, p.Y + 20.0, p.Z)
        parts = cad.BreakCurveAtAllIntersections(c1, ArrayList([c2, c3]), True)
        log("BreakCurveAtAllIntersections -> parti=" + str(len(parts)))

        e1 = cad.AddLine(p.X, p.Y + 25.0, p.Z, p.X + 100.0, p.Y + 25.0, p.Z)
        e2 = cad.AddLine(p.X + 30.0, p.Y + 5.0, p.Z, p.X + 30.0, p.Y + 45.0, p.Z)
        e3 = cad.AddLine(p.X + 70.0, p.Y + 5.0, p.Z, p.X + 70.0, p.Y + 45.0, p.Z)
        broken = cad.BreakEntitiesAtIntersections(ArrayList([e1, e2, e3]), True)
        log("BreakEntitiesAtIntersections changed = " + str(broken["changed"]))

        tr1 = cad.AddLine(p.X + 130.0, p.Y, p.Z, p.X + 130.0, p.Y + 80.0, p.Z)
        tr2 = cad.AddLine(p.X + 105.0, p.Y + 35.0, p.Z, p.X + 165.0, p.Y + 35.0, p.Z)
        tr3 = cad.AddLine(p.X + 105.0, p.Y + 55.0, p.Z, p.X + 165.0, p.Y + 55.0, p.Z)
        trim_info = cad.TrimCurvesToBoundaries(ArrayList([tr1]), ArrayList([tr2, tr3]), "nearest", True)
        log("TrimCurvesToBoundaries changed = " + str(trim_info["changed"]))

        ex1 = cad.AddLine(p.X + 190.0, p.Y, p.Z, p.X + 190.0, p.Y + 20.0, p.Z)
        ex2 = cad.AddLine(p.X + 160.0, p.Y + 50.0, p.Z, p.X + 250.0, p.Y + 50.0, p.Z)
        ex3 = cad.AddLine(p.X + 160.0, p.Y + 70.0, p.Z, p.X + 250.0, p.Y + 70.0, p.Z)
        ext_info = cad.ExtendCurvesToBoundaries(ArrayList([ex1]), ArrayList([ex2, ex3]), "nearest")
        log("ExtendCurvesToBoundaries changed = " + str(ext_info["changed"]))

        off = cad.AddLine(p.X + 270.0, p.Y, p.Z, p.X + 330.0, p.Y, p.Z)
        toward = cad.OffsetEntityTowardPoint(off, 8.0, p.X + 300.0, p.Y + 20.0, p.Z)
        toward_box = cad.GetBoundingBox(toward)
        log("OffsetEntityTowardPoint center y = " + str(toward_box["center_y"]))

        off_a = cad.AddLine(p.X + 270.0, p.Y + 35.0, p.Z, p.X + 330.0, p.Y + 35.0, p.Z)
        off_b = cad.AddLine(p.X + 270.0, p.Y + 45.0, p.Z, p.X + 330.0, p.Y + 45.0, p.Z)
        toward_many = cad.OffsetEntitiesTowardPoint(ArrayList([off_a, off_b]), 5.0, p.X + 300.0, p.Y + 70.0, p.Z)
        log("OffsetEntitiesTowardPoint count = " + str(len(toward_many)))

        src = cad.AddCircle(p.X + 360.0, p.Y + 20.0, p.Z, 6.0)
        cad.SetEntityLayer(src, "PYLOAD_MATCH")
        cad.SetEntityColor(src, 2)
        targets = ArrayList([cad.AddLine(p.X + 350.0, p.Y + 5.0, p.Z, p.X + 390.0, p.Y + 5.0, p.Z),
                             cad.AddLine(p.X + 350.0, p.Y + 15.0, p.Z, p.X + 390.0, p.Y + 15.0, p.Z)])
        opts = Hashtable()
        opts["layer"] = True
        opts["color"] = True
        opts["linetype"] = True
        opts["lineweight"] = True
        opts["linetype_scale"] = True
        match = cad.MatchEntityProperties(src, targets, opts)
        log("MatchEntityProperties changed = " + str(match["changed"]))

    def test_blocks_batch():
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
            log("Nessun blocco con attributi trovato per test batch blocchi")
            return

        br1 = cad.InsertBlock(rich, p.X + 450.0, p.Y, p.Z)
        br2 = cad.InsertBlock(rich, p.X + 480.0, p.Y, p.Z)
        log("Block refs creati = " + cad.GetEntityHandle(br1) + "/" + cad.GetEntityHandle(br2))

        sync = cad.SyncBlockReferenceAttributesBatch(ArrayList([br1, br2]), False)
        log("SyncBlockReferenceAttributesBatch changed = " + str(sync))

        defs = cad.GetBlockAttributeDefinitions(rich)
        tags = defs.Keys
        first_tag = None
        for t in tags:
            first_tag = str(t)
            break

        if first_tag is not None:
            upd = cad.UpdateBlockAttributeByTagBatch(ArrayList([br1, br2]), first_tag, "BATCH_VALUE")
            log("UpdateBlockAttributeByTagBatch changed = " + str(upd))

            vals = Hashtable()
            vals[first_tag] = "MAP_VALUE"
            upd_map = cad.UpdateBlockAttributesByMapBatch(ArrayList([br1, br2]), vals)
            log("UpdateBlockAttributesByMapBatch changed = " + str(upd_map))

            rename = cad.RenameBlockAttributeTag(rich, first_tag, first_tag + "_REN", True)
            log("RenameBlockAttributeTag defs/refs = " + str(rename["changed_definitions"]) + "/" + str(rename["changed_references"]))

        if alt is not None:
            repl = cad.ReplaceBlockReference(br1, alt, True, False)
            repl_info = cad.GetBlockReferenceInfo(repl)
            log("ReplaceBlockReference -> name = " + str(repl_info["name"]))

        exploded = cad.ExplodeBlockReferenceEx(br2, True, True)
        log("ExplodeBlockReferenceEx count = " + str(len(exploded)))

    safe("heavy_modify", test_heavy_modify)
    safe("blocks_batch", test_blocks_batch)

    cad.RegenNative()
    log("Test heavy modify + blocks completato")
