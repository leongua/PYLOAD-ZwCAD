import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[DXF FILTER TEST] " + msg)


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


log("Avvio test filtri DXF avanzati")

res = cad.GetPoint("Punto base dxf filter test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    c1 = ArrayList()
    c1.Add(pair(0, "CIRCLE"))
    c1.Add(pair(8, "PYLOAD_DXF_FILTER"))
    c1.Add(pair(10, p.X))
    c1.Add(pair(20, p.Y))
    c1.Add(pair(30, p.Z))
    c1.Add(pair(40, 10.0))
    id1 = cad.EntMake(c1)

    c2 = ArrayList()
    c2.Add(pair(0, "CIRCLE"))
    c2.Add(pair(8, "PYLOAD_DXF_FILTER"))
    c2.Add(pair(10, p.X + 25.0))
    c2.Add(pair(20, p.Y))
    c2.Add(pair(30, p.Z))
    c2.Add(pair(40, 22.0))
    id2 = cad.EntMake(c2)

    t1 = ArrayList()
    t1.Add(pair(0, "TEXT"))
    t1.Add(pair(8, "PYLOAD_DXF_FILTER"))
    t1.Add(pair(10, p.X))
    t1.Add(pair(20, p.Y + 20.0))
    t1.Add(pair(30, p.Z))
    t1.Add(pair(1, "ABC_001"))
    t1.Add(pair(40, 3.0))
    tid = cad.EntMake(t1)

    log("Entita test create")
    log("Circle1 handle = " + cad.GetEntityHandle(id1))
    log("Circle2 handle = " + cad.GetEntityHandle(id2))

    radius_filter = ArrayList()
    radius_filter.Add(pair(0, "CIRCLE"))
    radius_filter.Add(pair(40, ">15"))
    big_circles = cad.GetSelectionByDxf(radius_filter)
    log("GetSelectionByDxf radius > 15 -> " + str(len(big_circles)))

    wildcard_filter = ArrayList()
    wildcard_filter.Add(pair(0, "TEXT"))
    wildcard_filter.Add(pair(1, "ABC_*"))
    texts = cad.GetSelectionByDxf(wildcard_filter)
    log("GetSelectionByDxf text wildcard -> " + str(len(texts)))

    layer_filter = ArrayList()
    layer_filter.Add(pair(8, "PYLOAD_DXF_*"))
    all_layer = cad.GetSelectionByDxf(layer_filter)
    log("GetSelectionByDxf layer wildcard -> " + str(len(all_layer)))

    handle = cad.GetEntityHandle(id2)
    by_handle = cad.GetEntityByHandle(handle)
    log("GetEntityByHandle == id2 ? " + str(by_handle == id2))

    radius_value = cad.GetEntityDxfValue(id2, 40)
    log("GetEntityDxfValue radius = " + str(radius_value))
    log("HasEntityDxfCode(40) = " + str(cad.HasEntityDxfCode(id2, 40)))
    log("Dxf codes count = " + str(len(cad.GetEntityDxfCodes(id2))))

    cad.SetEntityDxfValue(id2, 40, 30.0)
    log("SetEntityDxfValue radius -> " + str(cad.GetEntityDxfValue(id2, 40)))

    parent_id = cad.EntParent(id2)
    log("EntParent(id2) is null? " + str(parent_id.IsNull))
    children = cad.EntChildren(parent_id)
    log("EntChildren(parent) count = " + str(len(children)))

    cad.Regen()
    cad.ZoomExtents()
    log("Test filtri DXF avanzati completato")
