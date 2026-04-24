import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable
from ZwSoft.ZwCAD.DatabaseServices import ObjectId


def log(msg):
    cad.Msg("[FIX23 LAYER/HANDLE/CMD] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio batch FIX23 (command clean + layer/layout + handle)")
res = cad.GetPoint("Punto base FIX23:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def step_command_clean():
        cad.ClearShellTranscript()
        cad.RunCommandNoiseFree("_.ZOOM _E")
        cad.CommandSilent(ArrayList(["_.ZOOM", "_P"]))
        cad.RunLispQuiet('(princ "\\n[FIX23 QUIET] ok")')
        cad.FlushCommandChannel(1, 1)
        tr = cad.GetShellTranscript()
        log("Command clean ok, transcript={0}".format(len(tr)))

    def step_layer_layout():
        line1 = cad.AddLine(p.X + 0.0, p.Y + 0.0, p.Z, p.X + 20.0, p.Y + 0.0, p.Z)
        line2 = cad.AddLine(p.X + 0.0, p.Y + 10.0, p.Z, p.X + 20.0, p.Y + 10.0, p.Z)
        ids = ArrayList([line1, line2])

        layer_name = "PYLOAD_FIX23"
        moved = cad.MoveEntitiesToLayer(ids, layer_name)
        state0 = cad.GetLayerState(layer_name)

        st = Hashtable()
        st["is_locked"] = True
        set_one = cad.SetLayerState(layer_name, st)
        state1 = cad.GetLayerState(layer_name)

        batch = Hashtable()
        x = Hashtable()
        x["is_locked"] = False
        x["is_off"] = False
        batch[layer_name] = x
        batch_res = cad.SetLayerStatesBatch(batch)
        state2 = cad.GetLayerState(layer_name)

        lnames = cad.GetLayerNames()
        tnames = cad.GetLayoutTabNames()
        lcounts = cad.GetLayerEntityCounts()
        dcounts = cad.GetDxfEntityCountsBySpace("*Model_Space")

        moved_by_type = cad.MoveEntitiesByDxfToLayer("LINE", "0", True)
        vis_changed = cad.BatchSetEntityVisibility(ids, True)

        log(
            "Layer/Layout ok, layers={0}, layouts={1}, moved={2}, moved_by_type={3}, vis={4}, "
            "locked(before/set/after)={5}/{6}/{7}, layer_count={8}, line_in_model={9}".format(
                len(lnames),
                len(tnames),
                moved,
                moved_by_type,
                vis_changed,
                state0["is_locked"],
                set_one,
                state2["is_locked"],
                lcounts[layer_name] if lcounts.ContainsKey(layer_name) else 0,
                dcounts["LINE"] if dcounts.ContainsKey("LINE") else 0,
            )
        )

    def step_handle_block_counts():
        lid = cad.AddLine(p.X + 40.0, p.Y + 0.0, p.Z, p.X + 60.0, p.Y + 0.0, p.Z)
        hid = cad.GetEntityHandle(lid)
        found = cad.GetObjectIdsByHandleStrings(ArrayList([hid, "NOPE"]))
        hmap = cad.GetHandleMap(ArrayList([lid]))

        blk = cad.EnsureTestAttributedBlock("PYLOAD_FIX23_BLK", "TAG", "P", "D", 2.5)
        for i in range(2):
            vals = Hashtable()
            vals["TAG"] = "V" + str(i + 1)
            cad.InsertBlockWithAttributes(blk, p.X + 80.0 + i * 12.0, p.Y, p.Z, vals)
        bcounts = cad.GetBlockReferenceCountsByName()
        bcount = bcounts[blk] if bcounts.ContainsKey(blk) else 0

        log("Handle/Block ok, handle={0}, found={1}, map={2}, block_refs={3}".format(
            hid, len(found), hmap.Count, bcount))

    safe("command_clean", step_command_clean)
    safe("layer_layout", step_layer_layout)
    safe("handle_block_counts", step_handle_block_counts)

    marker = cad.GetBuildMarker()
    log("Marker=" + marker)
    cad.RegenNative()
    log("Batch FIX23 completato")
