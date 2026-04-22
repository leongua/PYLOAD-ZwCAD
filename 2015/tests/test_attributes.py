import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[ATTR TEST] " + msg)


log("Avvio test attributi")

names = cad.GetBlockNames()
if len(names) == 0:
    log("Nessun blocco disponibile. Test terminato.")
else:
    block_name = str(names[0])
    log("Uso blocco: " + block_name)

    defs = cad.GetBlockAttributeDefinitions(block_name)
    log("Definizioni attributo trovate: " + str(len(defs)))

    res = cad.GetPoint("Punto base attribute test:")
    if res.Status != PromptStatus.OK:
        log("Operazione annullata.")
    else:
        p = res.Value
        ref_id = cad.InsertBlock(block_name, p.X, p.Y, p.Z)
        log("BlockReference inserito. Handle=" + cad.GetEntityHandle(ref_id))

        attrs = cad.GetBlockAttributes(ref_id)
        log("Attributi riferimento: " + str(len(attrs)))
        attr_ids = cad.GetBlockAttributeReferenceIds(ref_id)
        log("AttributeReference ids: " + str(len(attr_ids)))
        if len(attrs) > 0:
            first_tag = None
            for key in attrs.Keys:
                first_tag = str(key)
                break

            if first_tag is not None:
                cad.SetBlockAttribute(ref_id, first_tag, "ATTR_TEST")
                updated = cad.GetBlockAttributes(ref_id)
                log("SetBlockAttribute -> " + first_tag + " = " + str(updated[first_tag]))
                attr_id = cad.FindBlockAttributeReferenceId(ref_id, first_tag)
                if not attr_id.IsNull:
                    info = cad.GetAttributeInfo(attr_id)
                    log("GetAttributeInfo -> tag=" + str(info["tag"]) + " text=" + str(info["text"]))

        if len(defs) > 0:
            def_ids = cad.GetBlockAttributeDefinitionIds(block_name)
            log("AttributeDefinition ids: " + str(len(def_ids)))
            first_def_tag = None
            for key in defs.Keys:
                first_def_tag = str(key)
                break

            if first_def_tag is not None:
                def_info = defs[first_def_tag]
                log("Prima definition -> tag=" + str(def_info["tag"]) + " text=" + str(def_info["text"]))

        const_attrs = cad.GetConstantBlockAttributes(ref_id)
        log("Attributi costanti: " + str(len(const_attrs)))
        const_ids = cad.GetConstantBlockAttributeDefinitionIds(ref_id)
        log("Constant AttributeDefinition ids: " + str(len(const_ids)))

        cad.Regen()
        log("Test attributi completato")
