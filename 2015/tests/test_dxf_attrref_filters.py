import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[DXF ATTRIB TEST] " + msg)


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


log("Avvio test ATTRIB DXF-like + filtri")

names = cad.GetBlockNames()
if len(names) == 0:
    log("Nessun blocco disponibile. Test terminato.")
else:
    block_name = None
    for name in names:
        defs = cad.GetBlockAttributeDefinitions(str(name))
        if len(defs) > 0:
            block_name = str(name)
            break

    if block_name is None:
        block_name = str(names[0])

    log("Uso blocco: " + block_name)

    res = cad.GetPoint("Punto base dxf attrib test:")
    if res.Status != PromptStatus.OK:
        log("Operazione annullata.")
    else:
        p = res.Value
        ref_id = cad.InsertBlock(block_name, p.X, p.Y, p.Z)
        log("InsertBlock ok, handle=" + cad.GetEntityHandle(ref_id))

        attr_ids = cad.GetBlockAttributeReferenceIds(ref_id)
        log("AttributeReference count = " + str(len(attr_ids)))

        if len(attr_ids) == 0:
            log("Nessun attributo reference nel blocco scelto. Verifica se il blocco genera ATTRIB non costanti.")
        else:
            first_attr = attr_ids[0]
            attr_map = cad.EntGetMap(first_attr)
            log("EntGetMap ATTRIB tag = " + str(attr_map[2]))
            log("EntGetMap ATTRIB text = " + str(attr_map[1]))

            old_tag = str(attr_map[2])
            cad.SetEntityDxfValue(first_attr, 1, "DXF_ATTRIB_TEST")
            log("SetEntityDxfValue text -> " + str(cad.GetEntityDxfValue(first_attr, 1)))

            tag_filters = ArrayList()
            tag_filters.Add(pair(2, old_tag))
            tag_matches = cad.FilterEntitiesByDxf(attr_ids, tag_filters)
            log("FilterEntitiesByDxf tag -> " + str(len(tag_matches)))

            text_filters = ArrayList()
            text_filters.Add(pair(1, "DXF_*"))
            text_matches = cad.FilterEntitiesByDxf(attr_ids, text_filters)
            log("FilterEntitiesByDxf text wildcard -> " + str(len(text_matches)))

            handle = cad.GetEntityHandle(first_attr)
            handle_filters = ArrayList()
            handle_filters.Add(pair(5, handle))
            handle_matches = cad.FilterEntitiesByDxf(attr_ids, handle_filters)
            log("FilterEntitiesByDxf handle -> " + str(len(handle_matches)))

            new_attr_pairs = ArrayList()
            new_attr_pairs.Add(pair(0, "ATTRIB"))
            new_attr_pairs.Add(pair(330, cad.GetEntityHandle(ref_id)))
            new_attr_pairs.Add(pair(2, old_tag))
            new_attr_pairs.Add(pair(1, "ENTMAKE_ATTRIB"))
            new_attr_pairs.Add(pair(10, p.X + 5.0))
            new_attr_pairs.Add(pair(20, p.Y + 5.0))
            new_attr_pairs.Add(pair(30, p.Z))
            new_attr_id = cad.EntMake(new_attr_pairs)
            log("EntMake ATTRIB ok, handle=" + cad.GetEntityHandle(new_attr_id))
            new_attr_map = cad.EntGetMap(new_attr_id)
            log("EntGetMap new ATTRIB tag = " + str(new_attr_map[2]))
            log("EntGetMap new ATTRIB text = " + str(new_attr_map[1]))

        cad.Regen()
        log("Test ATTRIB DXF-like + filtri completato")
