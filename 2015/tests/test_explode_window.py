import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[EXPLODE TEST] " + msg)


log("Avvio test explode/window")

res = cad.GetPoint("Punto base explode/window:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    cad.EnsureLayer("PYLOAD_EXPLODE", 2)
    cad.SetCurrentLayer("PYLOAD_EXPLODE")

    rect_id = cad.DrawRectangle(p.X - 30.0, p.Y - 15.0, p.X + 30.0, p.Y + 15.0)
    cir_id = cad.AddCircle(p.X, p.Y, p.Z, 8.0)
    log("Entita base create")

    by_both = cad.GetSelection("PYLOAD_EXPLODE", "Polyline")
    log("Filtro combinato layer+type -> " + str(len(by_both)))

    win_ids = cad.SelectWindow(p.X - 40.0, p.Y - 20.0, p.Z, p.X + 40.0, p.Y + 20.0, p.Z)
    log("SelectWindow -> " + str(len(win_ids)))

    cross_ids = cad.SelectCrossingWindow(p.X - 10.0, p.Y - 10.0, p.Z, p.X + 10.0, p.Y + 10.0, p.Z)
    log("SelectCrossingWindow -> " + str(len(cross_ids)))

    exploded = cad.ExplodeEntity(rect_id, True)
    log("ExplodeEntity ha creato " + str(len(exploded)) + " entita")

    circles = cad.GetSelectionByType("Circle")
    log("Cerchi trovati nel DWG -> " + str(len(circles)))

    cad.Regen()
    cad.ZoomExtents()
    log("Test explode/window completato")
