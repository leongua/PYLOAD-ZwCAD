import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[BLOCKREF TEST] " + msg)


log("Avvio test BlockReference")

names = cad.GetBlockNames()
log("Blocchi disponibili: " + str(len(names)))

if len(names) == 0:
    log("Nessun blocco disponibile. Test terminato.")
else:
    name = str(names[0])
    log("Uso blocco: " + name)

    defs = cad.GetBlockAttributeDefinitions(name)
    log("Attribute definitions nel blocco: " + str(len(defs)))
    for key in defs.Keys:
        item = defs[key]
        log(" def " + str(key) + " constant=" + str(item["constant"]) + " text=" + str(item["text"]))

    res = cad.GetPoint("Punto base blockreference test:")
    if res.Status != PromptStatus.OK:
        log("Operazione annullata.")
    else:
        p = res.Value
        ref_id = cad.InsertBlock(name, p.X, p.Y, p.Z)
        log("BlockReference inserito. Handle=" + cad.GetEntityHandle(ref_id))

        info = cad.GetBlockReferenceInfo(ref_id)
        log("HasAttributes = " + str(info["has_attributes"]))
        log("AttributeCount = " + str(info["attribute_count"]))
        log("InsUnits = " + str(info["insunits"]))
        log("InsUnitsFactor = " + str(info["insunits_factor"]))

        attrs = cad.GetBlockAttributes(ref_id)
        log("GetBlockAttributes -> " + str(len(attrs)))
        const_attrs = cad.GetConstantBlockAttributes(ref_id)
        log("GetConstantBlockAttributes -> " + str(len(const_attrs)))
        log("BlockReferenceHasAttributes -> " + str(cad.BlockReferenceHasAttributes(ref_id)))

        cad.Regen()
        cad.ZoomExtents()
        log("Test BlockReference completato")
