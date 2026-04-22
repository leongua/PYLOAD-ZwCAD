import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[GRAPHICS TEST] " + msg)


log("Avvio test proprieta grafiche")

linetypes = cad.ListLinetypes()
log("Linetype disponibili: " + str(len(linetypes)))
for name in linetypes[:10]:
    log(" - " + str(name))

res = cad.GetPoint("Punto base graphics test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_GRAPHICS", 4)
    cad.SetCurrentLayer("PYLOAD_GRAPHICS")

    line_id = cad.AddLine(p.X, p.Y, p.Z, p.X + 80.0, p.Y, p.Z)
    rect_id = cad.DrawRectangle(p.X, p.Y + 15.0, p.X + 70.0, p.Y + 45.0)
    log("Entita create")

    cad.SetEntityLineWeight(line_id, 35)
    cad.SetEntityLineWeight(rect_id, 70)
    log("Lineweight applicati")

    if len(linetypes) > 0:
        preferred = None
        for candidate in ["Continuous", "CONTINUOUS", "ByLayer"]:
            if candidate in linetypes:
                preferred = candidate
                break
        if preferred is None:
            preferred = linetypes[0]

        cad.SetEntityLinetype(line_id, preferred)
        changed = cad.SetEntitiesLinetype([line_id, rect_id], preferred)
        log("Linetype applicato: " + str(preferred) + " su " + str(changed) + " entita")
    else:
        log("Nessun linetype disponibile trovato")

    changed_lw = cad.SetEntitiesLineWeight([line_id, rect_id], 50)
    log("SetEntitiesLineWeight -> " + str(changed_lw))

    cad.Regen()
    cad.ZoomExtents()
    log("Test proprieta grafiche completato")
