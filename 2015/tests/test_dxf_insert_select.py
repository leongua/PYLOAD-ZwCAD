import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[DXF INSERT TEST] " + msg)


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


log("Avvio test INSERT/BLOCKREF + traversal + filtri DXF")

names = cad.GetBlockNames()
log("Blocchi disponibili: " + str(len(names)))

if len(names) == 0:
    log("Nessun blocco disponibile. Test terminato.")
else:
    name = str(names[0])
    log("Uso blocco: " + name)

    res = cad.GetPoint("Punto base dxf insert/select test:")
    if res.Status != PromptStatus.OK:
        log("Operazione annullata.")
    else:
        p = res.Value

        insert_pairs = ArrayList()
        insert_pairs.Add(pair(0, "INSERT"))
        insert_pairs.Add(pair(2, name))
        insert_pairs.Add(pair(8, "PYLOAD_DXF_INSERT"))
        insert_pairs.Add(pair(62, 4))
        insert_pairs.Add(pair(10, p.X))
        insert_pairs.Add(pair(20, p.Y))
        insert_pairs.Add(pair(30, p.Z))
        insert_pairs.Add(pair(41, 1.0))
        insert_pairs.Add(pair(42, 1.0))
        insert_pairs.Add(pair(43, 1.0))
        insert_pairs.Add(pair(50, 0.0))
        ref_id = cad.EntMake(insert_pairs)
        log("EntMake INSERT ok, handle=" + cad.GetEntityHandle(ref_id))

        ref_map = cad.EntGetMap(ref_id)
        log("EntGetMap insert name = " + str(ref_map[2]))
        log("EntGetMap insert layer = " + str(ref_map[8]))

        next_id = cad.EntNext(ref_id)
        prev_id = cad.EntPrevious(ref_id)
        log("EntNext is null? " + str(next_id.IsNull))
        log("EntPrevious is null? " + str(prev_id.IsNull))

        owner_ids = cad.GetOwnerEntityIds(ref_id)
        log("Owner entities count = " + str(len(owner_ids)))

        copy_id = cad.EntCopy(ref_id, 25.0, 0.0, 0.0)
        log("EntCopy INSERT ok, handle=" + cad.GetEntityHandle(copy_id))

        mod_pairs = ArrayList()
        mod_pairs.Add(pair(50, 25.0))
        mod_pairs.Add(pair(41, 1.2))
        mod_pairs.Add(pair(42, 1.2))
        mod_pairs.Add(pair(43, 1.0))
        cad.EntMod(copy_id, mod_pairs)
        copy_map = cad.EntGetMap(copy_id)
        log("EntMod INSERT rotation = " + str(copy_map[50]))

        filters = ArrayList()
        filters.Add(pair(0, "INSERT"))
        filters.Add(pair(8, "PYLOAD_DXF_INSERT"))
        insert_ids = cad.GetSelectionByDxf(filters)
        log("GetSelectionByDxf INSERT/layer -> " + str(len(insert_ids)))

        filters_block = ArrayList()
        filters_block.Add(pair(0, "INSERT"))
        filters_block.Add(pair(2, name))
        same_block_ids = cad.GetSelectionByDxf(filters_block)
        log("GetSelectionByDxf INSERT/block -> " + str(len(same_block_ids)))

        window_ids = cad.SelectWindowByDxf(p.X - 5.0, p.Y - 5.0, p.Z, p.X + 40.0, p.Y + 5.0, p.Z, filters)
        log("SelectWindowByDxf -> " + str(len(window_ids)))

        cad.Regen()
        cad.ZoomExtents()
        log("Test INSERT/BLOCKREF + traversal + filtri DXF completato")
