import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")


def log(msg):
    cad.Msg("[DOC TEST] " + msg)


log("Avvio test documenti")
log("Drawing corrente: " + cad.DrawingName)
log("Path corrente: " + cad.DrawingPath)
log("Database filename: " + str(cad.DatabaseFilename))

docs = cad.GetOpenDrawings()
log("Documenti aperti: " + str(len(docs)))
for name in docs:
    log(" - " + str(name))

if cad.HasFullDrawingPath:
    sep = "\\" if not script_dir.endswith("\\") else ""
    out_path = script_dir + sep + "pyload_doc_test_copy.dwg"
    cad.SaveDrawingAs(out_path)
    log("Copia salvata in: " + out_path)
else:
    log("Disegno non salvato: salto SaveDrawingAs")

switched = cad.SwitchDrawing(cad.DrawingName)
log("Switch sul disegno corrente: " + str(switched))
log("Test documenti completato")
