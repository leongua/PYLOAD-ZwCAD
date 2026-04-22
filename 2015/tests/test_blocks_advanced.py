import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import Hashtable, ArrayList


def log(msg):
    cad.Msg("[BLOCK ADV TEST] " + msg)


log("Avvio test blocchi avanzati")

names = cad.GetBlockNames()
log("Blocchi disponibili: " + str(len(names)))

if len(names) == 0:
    log("Nessun blocco disponibile. Test terminato.")
else:
    first_name = str(names[0])
    log("Primo blocco: " + first_name)

    key = first_name[:3] if len(first_name) >= 3 else first_name
    found = cad.FindBlockNames(key)
    log("FindBlockNames('" + key + "') -> " + str(len(found)))

    def_info = cad.GetBlockDefinitionInfo(first_name)
    log("Definition name = " + str(def_info["name"]))
    log("Definition entity_count = " + str(def_info["entity_count"]))
    log("Definition has attrs = " + str(def_info["has_attribute_definitions"]))

    res = cad.GetPoint("Punto base block advanced test:")
    if res.Status != PromptStatus.OK:
        log("Operazione annullata.")
    else:
        p = res.Value

        block_id = cad.InsertBlock(first_name, p.X, p.Y, p.Z)
        ref_info = cad.GetBlockReferenceInfo(block_id)
        log("BlockReference singolo handle = " + str(ref_info["handle"]))
        log("BlockReference singolo attr_count = " + str(ref_info["attribute_count"]))

        specs = ArrayList()

        spec1 = Hashtable()
        spec1["block"] = first_name
        spec1["x"] = p.X + 80.0
        spec1["y"] = p.Y
        spec1["z"] = p.Z
        specs.Add(spec1)

        spec2 = Hashtable()
        spec2["block"] = first_name
        spec2["x"] = p.X + 160.0
        spec2["y"] = p.Y
        spec2["z"] = p.Z
        spec2["sx"] = 1.2
        spec2["sy"] = 1.2
        spec2["sz"] = 1.0
        spec2["rotation"] = 15.0
        specs.Add(spec2)

        inserted = cad.InsertBlocks(specs)
        log("InsertBlocks -> " + str(len(inserted)) + " riferimenti")

        batch_ids = ArrayList()
        for item in inserted:
            batch_ids.Add(item)

        attrs = cad.GetBlockAttributes(block_id)
        log("Attributi sul riferimento singolo = " + str(len(attrs)))

        if len(attrs) > 0:
            first_tag = None
            for key in attrs.Keys:
                first_tag = str(key)
                break

            if first_tag is not None:
                cad.SetBlockAttribute(block_id, first_tag, "PYLOAD_ONE")
                updated = cad.GetBlockAttributes(block_id)
                log("SetBlockAttribute -> " + first_tag + " = " + str(updated[first_tag]))

                batch_values = Hashtable()
                batch_values[first_tag] = "PYLOAD_BATCH"
                changed = cad.SetBlockAttributesBatch(batch_ids, batch_values)
                log("SetBlockAttributesBatch -> changed = " + str(changed))
        else:
            log("Il blocco scelto non ha attributi modificabili.")

        cad.Regen()
        cad.ZoomExtents()
        log("Test blocchi avanzati completato")
