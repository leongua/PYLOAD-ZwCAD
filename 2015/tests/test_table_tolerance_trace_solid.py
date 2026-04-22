import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[TTTS TEST] " + msg)


log("Avvio test table/tolerance/trace/solid")

res = cad.GetPoint("Punto base TTTS test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    trace_id = cad.AddTrace(
        p.X, p.Y, p.Z,
        p.X + 20.0, p.Y, p.Z,
        p.X + 25.0, p.Y + 10.0, p.Z,
        p.X + 5.0, p.Y + 15.0, p.Z)
    log("AddTrace ok, handle=" + cad.GetEntityHandle(trace_id))

    solid_id = cad.AddSolid(
        p.X + 40.0, p.Y, p.Z,
        p.X + 60.0, p.Y, p.Z,
        p.X + 62.0, p.Y + 12.0, p.Z,
        p.X + 42.0, p.Y + 14.0, p.Z)
    log("AddSolid ok, handle=" + cad.GetEntityHandle(solid_id))

    tol_id = cad.AddTolerance("{\\Fgdt;j6}", p.X + 80.0, p.Y + 10.0, p.Z, 1.0, 0.0, 0.0)
    log("AddTolerance ok, handle=" + cad.GetEntityHandle(tol_id))

    table_id = cad.AddTable(p.X, p.Y + 40.0, p.Z, 3, 3, 6.0, 20.0)
    log("AddTable ok, handle=" + cad.GetEntityHandle(table_id))

    cad.Regen()
    cad.ZoomExtents()
    log("Test table/tolerance/trace/solid completato")
