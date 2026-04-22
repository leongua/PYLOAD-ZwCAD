import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[BLOCK TEST] " + msg)


log("Avvio test blocchi")

names = cad.GetBlockNames()
log("Blocchi disponibili: " + str(len(names)))

for name in names[:15]:
    log(" - " + str(name))

if len(names) == 0:
    log("Nessun blocco utente disponibile nel DWG. Test terminato.")
else:
    res = cad.GetPoint("Punto inserimento blocco:")
    if res.Status != PromptStatus.OK:
        log("Operazione annullata.")
    else:
        p = res.Value
        block_name = names[0]
        log("Inserisco blocco: " + str(block_name))
        block_id = cad.InsertBlock(block_name, p.X, p.Y, p.Z)
        info = cad.GetEntityInfo(block_id)
        log("BlockReference creato. Handle=" + str(info["handle"]))

        attrs = cad.GetBlockAttributes(block_id)
        log("Attributi trovati: " + str(len(attrs)))
        for key in attrs.Keys:
            log(" - " + str(key) + " = " + str(attrs[key]))

        cad.Regen()
        cad.ZoomExtents()
        log("Test blocchi completato.")
