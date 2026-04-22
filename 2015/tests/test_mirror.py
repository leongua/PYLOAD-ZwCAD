import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[MIRROR TEST] " + msg)


log("Avvio test mirror")

res = cad.GetPoint("Punto base mirror:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    cad.EnsureLayer("PYLOAD_MIRROR", 1)
    cad.SetCurrentLayer("PYLOAD_MIRROR")

    src_id = cad.DrawRectangle(p.X - 20.0, p.Y - 10.0, p.X + 20.0, p.Y + 10.0)
    cad.SetEntityColor(src_id, 1)
    log("Rettangolo sorgente creato")

    mir_id = cad.MirrorEntity(src_id, p.X, p.Y - 40.0, p.Z, p.X, p.Y + 40.0, p.Z, False)
    cad.SetEntityColor(mir_id, 3)
    log("Mirror creato")

    src_info = cad.GetEntityInfo(src_id)
    mir_info = cad.GetEntityInfo(mir_id)
    log("Source handle=" + str(src_info["handle"]))
    log("Mirror handle=" + str(mir_info["handle"]))

    cad.Regen()
    cad.ZoomExtents()
    log("Test mirror completato")
