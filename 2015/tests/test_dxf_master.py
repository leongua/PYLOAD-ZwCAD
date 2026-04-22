import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[DXF MASTER TEST] " + msg)


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


log("Avvio smoke test settore DXF/database-style")

res = cad.GetPoint("Punto base dxf master test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    circle = ArrayList()
    circle.Add(pair(0, "CIRCLE"))
    circle.Add(pair(8, "PYLOAD_DXF_MASTER"))
    circle.Add(pair(10, p.X))
    circle.Add(pair(20, p.Y))
    circle.Add(pair(30, p.Z))
    circle.Add(pair(40, 12.0))
    circle_id = cad.EntMake(circle)
    log("EntMake CIRCLE ok, handle=" + cad.GetEntityHandle(circle_id))

    cad.SetEntityDxfValue(circle_id, 40, 18.0)
    log("SetEntityDxfValue radius -> " + str(cad.GetEntityDxfValue(circle_id, 40)))

    hatch = ArrayList()
    hatch.Add(pair(0, "HATCH"))
    hatch.Add(pair(8, "PYLOAD_DXF_MASTER"))
    hatch.Add(pair(2, "SOLID"))
    hatch.Add(pair(10, p.X + 25.0))
    hatch.Add(pair(20, p.Y))
    hatch.Add(pair(10, p.X + 45.0))
    hatch.Add(pair(20, p.Y))
    hatch.Add(pair(10, p.X + 45.0))
    hatch.Add(pair(20, p.Y + 15.0))
    hatch.Add(pair(10, p.X + 25.0))
    hatch.Add(pair(20, p.Y + 15.0))
    hatch_id = cad.EntMake(hatch)
    log("EntMake HATCH ok, handle=" + cad.GetEntityHandle(hatch_id))

    names = cad.GetBlockNames()
    if len(names) > 0:
        block_name = None
        for name in names:
            defs = cad.GetBlockAttributeDefinitions(str(name))
            if len(defs) > 0:
                block_name = str(name)
                break

        if block_name is None:
            block_name = str(names[0])

        insert = ArrayList()
        insert.Add(pair(0, "INSERT"))
        insert.Add(pair(2, block_name))
        insert.Add(pair(8, "PYLOAD_DXF_MASTER"))
        insert.Add(pair(10, p.X + 70.0))
        insert.Add(pair(20, p.Y))
        insert.Add(pair(30, p.Z))
        ref_id = cad.EntMake(insert)
        log("EntMake INSERT ok, handle=" + cad.GetEntityHandle(ref_id))

        attr_ids = cad.GetBlockAttributeReferenceIds(ref_id)
        log("Attribute refs trovati = " + str(len(attr_ids)))
        if len(attr_ids) > 0:
            first_attr = attr_ids[0]
            log("First ATTRIB tag = " + str(cad.GetEntityDxfValue(first_attr, 2)))
            cad.SetEntityDxfValue(first_attr, 1, "MASTER_TEST")
            log("ATTRIB text -> " + str(cad.GetEntityDxfValue(first_attr, 1)))

    filters = ArrayList()
    filters.Add(pair(8, "PYLOAD_DXF_*"))
    filters.Add(pair(0, "C*"))
    filtered = cad.GetSelectionByDxf(filters)
    log("GetSelectionByDxf wildcard -> " + str(len(filtered)))

    radius_filter = ArrayList()
    radius_filter.Add(pair(0, "CIRCLE"))
    radius_filter.Add(pair(40, ">=18"))
    radius_hits = cad.GetSelectionByDxf(radius_filter)
    log("GetSelectionByDxf radius >= 18 -> " + str(len(radius_hits)))

    parent_id = cad.EntParent(circle_id)
    log("EntParent(circle) is null? " + str(parent_id.IsNull))
    log("Dxf code count circle = " + str(len(cad.GetEntityDxfCodes(circle_id))))

    cad.Regen()
    cad.ZoomExtents()
    log("Smoke test settore DXF/database-style completato")
