import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[TRANSFORM TEST] " + msg)


log("Avvio test manipolazione oggetti")

res = cad.GetPoint("Punto base test trasformazioni:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_TRANSFORM", 6)
    cad.SetCurrentLayer("PYLOAD_TRANSFORM")
    log("Layer corrente impostato a PYLOAD_TRANSFORM")

    base_id = cad.DrawRectangle(p.X - 20.0, p.Y - 10.0, p.X + 20.0, p.Y + 10.0)
    cad.SetEntityColor(base_id, 1)
    log("Rettangolo base creato")

    copy_id = cad.CopyEntity(base_id, 60.0, 0.0, 0.0)
    cad.SetEntityColor(copy_id, 2)
    log("Copia creata")

    cad.MoveEntity(copy_id, 0.0, 40.0, 0.0)
    log("Copia spostata")

    cad.RotateEntity(copy_id, p.X + 60.0, p.Y + 40.0, p.Z, 25.0)
    log("Copia ruotata")

    cad.ScaleEntity(copy_id, p.X + 60.0, p.Y + 40.0, p.Z, 1.35)
    log("Copia scalata")

    base_info = cad.GetEntityInfo(base_id)
    copy_info = cad.GetEntityInfo(copy_id)
    log("Base handle=" + str(base_info["handle"]) + " type=" + str(base_info["type"]))
    log("Copy handle=" + str(copy_info["handle"]) + " type=" + str(copy_info["type"]))

    cad.Regen()
    cad.ZoomExtents()
    log("Test trasformazioni completato")
