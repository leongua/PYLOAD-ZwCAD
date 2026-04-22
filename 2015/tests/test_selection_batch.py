import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[BATCH TEST] " + msg)


layer_name = "PYLOAD_BATCH"

log("Avvio test selezione filtrata e batch")

res = cad.GetPoint("Punto base batch test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer(layer_name, 4)
    cad.SetCurrentLayer(layer_name)
    log("Layer corrente impostato a " + layer_name)

    ids = []
    ids.append(cad.AddCircle(p.X, p.Y, p.Z, 10.0))
    ids.append(cad.AddCircle(p.X + 30.0, p.Y, p.Z, 10.0))
    ids.append(cad.AddLine(p.X - 20.0, p.Y - 20.0, p.Z, p.X + 20.0, p.Y - 20.0, p.Z))
    ids.append(cad.DrawRectangle(p.X - 15.0, p.Y + 15.0, p.X + 15.0, p.Y + 35.0))
    log("Entita iniziali create: " + str(len(ids)))

    by_layer = cad.GetSelectionByLayer(layer_name)
    log("Selezione per layer -> " + str(len(by_layer)) + " entita")

    circles = cad.GetSelectionByType("Circle")
    log("Selezione per tipo Circle -> " + str(len(circles)) + " entita")

    moved = cad.MoveEntities(by_layer, 80.0, 0.0, 0.0)
    log("MoveEntities eseguito su: " + str(moved) + " entita")

    rotated = cad.RotateEntities(by_layer, p.X + 80.0, p.Y, p.Z, 12.0)
    log("RotateEntities eseguito su: " + str(rotated) + " entita")

    scaled = cad.ScaleEntities(by_layer, p.X + 80.0, p.Y, p.Z, 1.1)
    log("ScaleEntities eseguito su: " + str(scaled) + " entita")

    cad.Regen()
    cad.ZoomExtents()
    log("Test batch completato")
