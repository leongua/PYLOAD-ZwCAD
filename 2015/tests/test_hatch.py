import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[HATCH TEST] " + msg)


log("Avvio test hatch")

res = cad.GetPoint("Centro hatch:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_HATCH", 2)
    cad.SetCurrentLayer("PYLOAD_HATCH")
    log("Layer corrente impostato a PYLOAD_HATCH")

    pts = [
        p.X - 40.0, p.Y - 20.0,
        p.X + 40.0, p.Y - 20.0,
        p.X + 25.0, p.Y + 20.0,
        p.X - 25.0, p.Y + 20.0,
    ]

    log("Creo hatch SOLID su contorno chiuso...")
    hatch_id = cad.DrawHatch(pts, "SOLID", 1.0, 0.0)
    info = cad.GetEntityInfo(hatch_id)
    log("Hatch creato. Handle=" + str(info["handle"]) + " type=" + str(info["type"]))

    cad.Regen()
    cad.ZoomExtents()
    log("Test hatch completato.")
