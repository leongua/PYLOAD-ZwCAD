import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")


def log(msg):
    cad.Msg("[LAYER ADV TEST] " + msg)


layer_name = "PYLOAD_LAYER_ADV"

log("Avvio test layer avanzati")

if cad.LayerExists(layer_name):
    if cad.GetCurrentLayer() == layer_name:
        cad.SetCurrentLayer("0")
    try:
        cad.ThawLayer(layer_name)
    except:
        pass
    try:
        cad.UnlockLayer(layer_name)
    except:
        pass
    try:
        cad.DeleteLayer(layer_name)
    except Exception as ex:
        log("Pulizia iniziale non riuscita: " + str(ex))

cad.EnsureLayer(layer_name, 6)
log("Layer creato: " + layer_name)

cad.SetLayerLineWeight(layer_name, 35)
log("Lineweight impostato a 35")

info = cad.GetLayerInfo(layer_name)
log("Info iniziali -> color=" + str(info["color_index"]) + " lw=" + str(info["lineweight"]))

cad.LockLayer(layer_name)
log("Layer locked? " + str(cad.IsLayerLocked(layer_name)))

cad.UnlockLayer(layer_name)
log("Layer locked dopo unlock? " + str(cad.IsLayerLocked(layer_name)))

cad.FreezeLayer(layer_name)
log("Layer frozen? " + str(cad.IsLayerFrozen(layer_name)))

cad.ThawLayer(layer_name)
log("Layer frozen dopo thaw? " + str(cad.IsLayerFrozen(layer_name)))

cad.SetCurrentLayer(layer_name)
log("Layer corrente impostato a " + cad.GetCurrentLayer())

cad.SetCurrentLayer("0")
log("Ritorno a layer 0")

final_info = cad.GetLayerInfo(layer_name)
log("Info finali -> off=" + str(final_info["is_off"]) + " locked=" + str(final_info["is_locked"]) + " frozen=" + str(final_info["is_frozen"]))

cad.DeleteLayer(layer_name)
log("Layer temporaneo eliminato")

log("Test layer avanzati completato")
