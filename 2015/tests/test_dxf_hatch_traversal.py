import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[DXF HATCH TEST] " + msg)


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


log("Avvio test HATCH DXF-like + traversal attributi")

res = cad.GetPoint("Punto base dxf hatch test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    hatch_pairs = ArrayList()
    hatch_pairs.Add(pair(0, "HATCH"))
    hatch_pairs.Add(pair(8, "PYLOAD_DXF_HATCH"))
    hatch_pairs.Add(pair(62, 2))
    hatch_pairs.Add(pair(2, "SOLID"))
    hatch_pairs.Add(pair(10, p.X))
    hatch_pairs.Add(pair(20, p.Y))
    hatch_pairs.Add(pair(10, p.X + 30.0))
    hatch_pairs.Add(pair(20, p.Y))
    hatch_pairs.Add(pair(10, p.X + 30.0))
    hatch_pairs.Add(pair(20, p.Y + 20.0))
    hatch_pairs.Add(pair(10, p.X))
    hatch_pairs.Add(pair(20, p.Y + 20.0))
    hatch_id = cad.EntMake(hatch_pairs)
    log("EntMake HATCH ok, handle=" + cad.GetEntityHandle(hatch_id))

    hatch_map = cad.EntGetMap(hatch_id)
    log("EntGetMap hatch pattern = " + str(hatch_map[2]))

    hatch_mod = ArrayList()
    hatch_mod.Add(pair(41, 1.5))
    hatch_mod.Add(pair(52, 30.0))
    cad.EntMod(hatch_id, hatch_mod)
    hatch_map2 = cad.EntGetMap(hatch_id)
    log("EntMod HATCH scale = " + str(hatch_map2[41]))

    names = cad.GetBlockNames()
    if len(names) > 0:
        block_name = str(names[0])
        log("Uso blocco per traversal: " + block_name)

        ins_pairs = ArrayList()
        ins_pairs.Add(pair(0, "INSERT"))
        ins_pairs.Add(pair(2, block_name))
        ins_pairs.Add(pair(10, p.X + 60.0))
        ins_pairs.Add(pair(20, p.Y))
        ins_pairs.Add(pair(30, p.Z))
        ref_id = cad.EntMake(ins_pairs)
        log("EntMake INSERT ok, handle=" + cad.GetEntityHandle(ref_id))

        first_attr = cad.EntFirstAttribute(ref_id)
        log("EntFirstAttribute is null? " + str(first_attr.IsNull))
        if not first_attr.IsNull:
            owner_ref = cad.GetAttributeOwnerBlockReference(first_attr)
            log("GetAttributeOwnerBlockReference = " + cad.GetEntityHandle(owner_ref))
            next_attr = cad.EntNext(first_attr)
            log("EntNext(attribute) is null? " + str(next_attr.IsNull))

    cad.Regen()
    cad.ZoomExtents()
    log("Test HATCH DXF-like + traversal attributi completato")
