import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[ENTITY TEST] " + msg)


log("Avvio test entities")

res = cad.GetPoint("Punto base entity test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    line_id = cad.AddLine(p.X, p.Y, p.Z, p.X + 60.0, p.Y + 20.0, p.Z)
    circle_id = cad.AddCircle(p.X + 90.0, p.Y, p.Z, 15.0)
    log("Entita create")

    log("Line handle = " + cad.GetEntityHandle(line_id))
    log("Line type = " + cad.GetEntityTypeName(line_id))
    log("Line owner = " + cad.GetEntityOwnerId(line_id))
    log("Circle visible iniziale = " + str(cad.IsEntityVisible(circle_id)))

    common = cad.GetEntityCommonInfo(circle_id)
    log("Circle common type = " + str(common["type"]))
    log("Circle common layer = " + str(common["layer"]))
    log("Circle common linetype = " + str(common["linetype"]))

    cad.SetEntityVisible(circle_id, False)
    log("Circle visible dopo hide = " + str(cad.IsEntityVisible(circle_id)))
    cad.SetEntityVisible(circle_id, True)
    log("Circle visible dopo show = " + str(cad.IsEntityVisible(circle_id)))

    batch = cad.GetEntitiesCommonInfo([line_id, circle_id])
    log("GetEntitiesCommonInfo -> " + str(batch.Count))
    for key in batch.Keys:
        item = batch[key]
        log(" item[" + str(key) + "] type=" + str(item["type"]) + " handle=" + str(item["handle"]))

    cad.Regen()
    log("Test entities completato")
