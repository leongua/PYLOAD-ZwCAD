import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")


def log(msg):
    cad.Msg("[LAYER TEST] " + msg)


base_name = "PYLOAD_LAYER_TMP"
renamed_name = "PYLOAD_LAYER_TMP_RENAMED"

log("Avvio test layer")
log("Layer iniziali: " + str(len(cad.ListLayers())))
log("Layer corrente iniziale: " + cad.GetCurrentLayer())

for layer_name in [base_name, renamed_name]:
    if cad.LayerExists(layer_name):
        if cad.GetCurrentLayer() == layer_name:
            cad.SetCurrentLayer("0")
        try:
            cad.DeleteLayer(layer_name)
            log("Pulizia layer preesistente: " + layer_name)
        except Exception as ex:
            log("Pulizia non riuscita per " + layer_name + ": " + str(ex))

cad.EnsureLayer(base_name, 2)
log("Creato layer: " + base_name)

cad.SetLayerColor(base_name, 5)
log("Colore layer impostato a 5")

log("Layer acceso? " + str(cad.IsLayerOn(base_name)))
cad.TurnLayerOff(base_name)
log("Layer acceso dopo off? " + str(cad.IsLayerOn(base_name)))
cad.TurnLayerOn(base_name)
log("Layer acceso dopo on? " + str(cad.IsLayerOn(base_name)))

cad.RenameLayer(base_name, renamed_name)
log("Layer rinominato in: " + renamed_name)

cad.SetCurrentLayer(renamed_name)
log("Layer corrente ora: " + cad.GetCurrentLayer())

cad.SetCurrentLayer("0")
log("Torno a layer 0")

cad.DeleteLayer(renamed_name)
log("Layer temporaneo eliminato")

log("Layer finali: " + str(len(cad.ListLayers())))
log("Test layer completato")
