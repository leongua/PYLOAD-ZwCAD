import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[VERIFY FIX16] " + msg)


log("Avvio verifica build/metodi")
res = cad.GetPoint("Punto base verifica FIX16:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    try:
        log("Build marker = " + str(cad.GetBuildMarker()))
    except Exception as ex:
        log("GetBuildMarker ERRORE: " + str(ex))

    names = [
        "GetLayoutNamesFromBlockTable",
        "BuildIntersectionsMatrix",
        "CopyTransformBatch",
        "GetEntityPropertySnapshot",
        "ExportEntityAuditCsv",
    ]

    for n in names:
        ok = hasattr(cad, n)
        log(n + " = " + str(ok))

    log("Verifica completata")
