import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[OFFSET/TEXT TEST] " + msg)


log("Avvio test offset e cambio testo")

res = cad.GetPoint("Punto base offset/text:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_OFFSET", 5)
    cad.SetCurrentLayer("PYLOAD_OFFSET")

    pl_id = cad.DrawRectangle(p.X - 25.0, p.Y - 15.0, p.X + 25.0, p.Y + 15.0)
    log("Rettangolo base creato")

    offsets = cad.OffsetEntity(pl_id, 8.0)
    log("Offset creati: " + str(len(offsets)))

    txt_id = cad.AddText("TESTO BASE", p.X + 40.0, p.Y, p.Z, 4.0)
    mtxt_id = cad.AddMText("MTEXT BASE", p.X + 40.0, p.Y + 12.0, p.Z, 3.5, 40.0)
    log("Testi creati")

    cad.ChangeText(txt_id, "TESTO MODIFICATO")
    cad.ChangeText(mtxt_id, "MTEXT MODIFICATO")
    log("Testi modificati")

    changed = cad.ChangeTexts([txt_id, mtxt_id], "TESTO BATCH")
    log("ChangeTexts batch -> " + str(changed))

    cad.Regen()
    cad.ZoomExtents()
    log("Test offset/text completato")
