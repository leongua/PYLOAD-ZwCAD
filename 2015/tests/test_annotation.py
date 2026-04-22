import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[ANNOTATION TEST] " + msg)


log("Avvio test annotazione e curve")

res = cad.GetPoint("Punto base annotation test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_ANNOTATION", 2)
    cad.SetCurrentLayer("PYLOAD_ANNOTATION")
    log("Layer corrente impostato a PYLOAD_ANNOTATION")

    line_id = cad.AddLine(p.X, p.Y, p.Z, p.X + 80.0, p.Y, p.Z)
    log("Linea base creata")

    spline_pts = [
        p.X, p.Y + 20.0, p.Z,
        p.X + 20.0, p.Y + 45.0, p.Z,
        p.X + 45.0, p.Y + 10.0, p.Z,
        p.X + 70.0, p.Y + 40.0, p.Z,
        p.X + 95.0, p.Y + 15.0, p.Z,
    ]
    spline_id = cad.AddSpline(spline_pts)
    log("Spline creata")

    mtext_id = cad.AddMText("PYLOAD\\PAnnotazione multilinea", p.X + 90.0, p.Y + 30.0, p.Z, 4.0, 60.0)
    log("MText creato")

    dim_id = cad.AddAlignedDimension(
        p.X, p.Y, p.Z,
        p.X + 80.0, p.Y, p.Z,
        p.X + 40.0, p.Y + 18.0, p.Z,
        ""
    )
    log("Quota allineata creata")

    cad.SetEntityColor(line_id, 1)
    cad.SetEntityColor(spline_id, 4)
    cad.SetEntityColor(mtext_id, 3)
    cad.SetEntityColor(dim_id, 6)
    log("Colori assegnati")

    cad.Regen()
    cad.ZoomExtents()
    log("Test annotation completato")
