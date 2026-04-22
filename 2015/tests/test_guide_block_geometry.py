import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[GUIDE TEST] " + msg)


log("Avvio test metodi da guida")

res = cad.GetPoint("Punto base guide test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    lw = cad.AddLightWeightPolyline([
        p.X, p.Y,
        p.X + 30.0, p.Y,
        p.X + 35.0, p.Y + 15.0,
        p.X + 10.0, p.Y + 25.0
    ], True)
    log("AddLightWeightPolyline ok, handle=" + cad.GetEntityHandle(lw))

    ray = cad.AddRay(p.X, p.Y + 40.0, p.Z, p.X + 40.0, p.Y + 55.0, p.Z)
    log("AddRay ok, handle=" + cad.GetEntityHandle(ray))

    mt = cad.AddMText("LEADER TEST", p.X + 60.0, p.Y + 20.0, p.Z, 3.5, 40.0)
    leader = cad.AddLeader([
        p.X + 15.0, p.Y + 10.0, p.Z,
        p.X + 40.0, p.Y + 18.0, p.Z,
        p.X + 55.0, p.Y + 20.0, p.Z
    ], mt)
    log("AddLeader ok, handle=" + cad.GetEntityHandle(leader))

    names = cad.GetBlockNames()
    if len(names) > 0:
        first_name = str(names[0])
        log("Provo AddAttributeDefinitionToBlock su: " + first_name)
        attr_def = cad.AddAttributeDefinitionToBlock(
            first_name,
            2.5,
            0,
            "PROMPT_TEST",
            0.0,
            0.0,
            0.0,
            "PYLOAD_TAG",
            "PYLOAD_VALUE")
        log("AddAttributeDefinitionToBlock ok, handle=" + cad.GetEntityHandle(attr_def))
    else:
        log("Nessun blocco disponibile, salto test attribute definition")

    cad.Regen()
    cad.ZoomExtents()
    log("Test metodi da guida completato")
