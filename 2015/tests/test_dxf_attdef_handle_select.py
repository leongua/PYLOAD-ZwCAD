import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[DXF ATTDEF TEST] " + msg)


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


log("Avvio test ATTDEF + handle lookup + filtri DXF")

res = cad.GetPoint("Punto base dxf attdef test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    attdef_pairs = ArrayList()
    attdef_pairs.Add(pair(0, "ATTDEF"))
    attdef_pairs.Add(pair(8, "PYLOAD_DXF_ATTR"))
    attdef_pairs.Add(pair(10, p.X))
    attdef_pairs.Add(pair(20, p.Y))
    attdef_pairs.Add(pair(30, p.Z))
    attdef_pairs.Add(pair(1, "VALORE"))
    attdef_pairs.Add(pair(2, "TAG_TEST"))
    attdef_pairs.Add(pair(3, "PROMPT_TEST"))
    attdef_pairs.Add(pair(40, 3.0))
    attdef_pairs.Add(pair(70, 1))
    attdef_id = cad.EntMake(attdef_pairs)
    handle = cad.GetEntityHandle(attdef_id)
    log("EntMake ATTDEF ok, handle=" + handle)

    attdef_map = cad.EntGetMap(attdef_id)
    log("EntGetMap attdef tag = " + str(attdef_map[2]))
    log("EntGetMap attdef prompt = " + str(attdef_map[3]))
    log("EntGetMap attdef flags = " + str(attdef_map[70]))

    mod_pairs = ArrayList()
    mod_pairs.Add(pair(1, "VALORE_MOD"))
    mod_pairs.Add(pair(2, "TAG_MOD"))
    mod_pairs.Add(pair(3, "PROMPT_MOD"))
    mod_pairs.Add(pair(70, 2))
    cad.EntMod(attdef_id, mod_pairs)
    info = cad.GetAttributeInfo(attdef_id)
    log("EntMod ATTDEF -> tag=" + str(info["tag"]) + " constant=" + str(info["constant"]))

    by_handle = cad.GetEntityByHandle(handle)
    log("GetEntityByHandle trovato? " + str(not by_handle.IsNull))

    filters_handle = ArrayList()
    filters_handle.Add(pair(5, handle))
    handle_ids = cad.GetSelectionByDxf(filters_handle)
    log("GetSelectionByDxf handle -> " + str(len(handle_ids)))

    filters_layer = ArrayList()
    filters_layer.Add(pair(0, "ATTDEF"))
    filters_layer.Add(pair(8, "PYLOAD_DXF_ATTR"))
    layer_ids = cad.GetSelectionByDxf(filters_layer)
    log("GetSelectionByDxf ATTDEF/layer -> " + str(len(layer_ids)))

    names = cad.GetBlockNames()
    if len(names) > 0:
        block_name = str(names[0])
        ins_pairs = ArrayList()
        ins_pairs.Add(pair(0, "INSERT"))
        ins_pairs.Add(pair(2, block_name))
        ins_pairs.Add(pair(10, p.X + 40.0))
        ins_pairs.Add(pair(20, p.Y))
        ins_pairs.Add(pair(30, p.Z))
        ref_id = cad.EntMake(ins_pairs)
        log("EntMake INSERT ok, handle=" + cad.GetEntityHandle(ref_id))

        attr_filters = ArrayList()
        attr_filters.Add(pair(0, "INSERT"))
        attr_filters.Add(pair(66, 1))
        ref_ids = cad.GetSelectionByDxf(attr_filters)
        log("GetSelectionByDxf INSERT has_attrs -> " + str(len(ref_ids)))

    cad.Regen()
    cad.ZoomExtents()
    log("Test ATTDEF + handle lookup + filtri DXF completato")
