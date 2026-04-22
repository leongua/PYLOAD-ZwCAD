import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[ADV TRANSFORM TEST] " + msg)


log("Avvio test transforms advanced")

res = cad.GetPoint("Punto base advanced transforms:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    base_rect = cad.DrawRectangle(p.X, p.Y, p.X + 20.0, p.Y + 10.0)
    log("Rettangolo base creato")

    rect_count = cad.ArrayRectangularEntity(base_rect, 2, 3, 1, 18.0, 28.0, 0.0)
    log("ArrayRectangularEntity -> copie create: " + str(len(rect_count)))

    base_circle = cad.AddCircle(p.X + 120.0, p.Y + 10.0, p.Z, 8.0)
    log("Cerchio base creato")

    polar_count = cad.ArrayPolarEntity(base_circle, 6, p.X + 120.0, p.Y + 10.0, p.Z, 360.0, True)
    log("ArrayPolarEntity -> copie create: " + str(len(polar_count)))

    base_line = cad.AddLine(p.X, p.Y + 60.0, p.Z, p.X + 25.0, p.Y + 60.0, p.Z)
    batch_created = cad.ArrayRectangularEntities([base_line], 2, 2, 1, 12.0, 35.0, 0.0)
    log("ArrayRectangularEntities -> copie create: " + str(batch_created))

    cad.Regen()
    cad.ZoomExtents()
    log("Test transforms advanced completato")
