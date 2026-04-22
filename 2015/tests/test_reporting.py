import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[REPORT TEST] " + msg)


def path_in_script_dir(name):
    sep = "\\" if not script_dir.endswith("\\") else ""
    return script_dir + sep + name


log("Avvio test reporting/export")

res = cad.GetPoint("Punto base report test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_REPORT", 3)
    cad.SetCurrentLayer("PYLOAD_REPORT")

    line_id = cad.AddLine(p.X, p.Y, p.Z, p.X + 50.0, p.Y, p.Z)
    poly_id = cad.DrawRectangle(p.X, p.Y + 10.0, p.X + 40.0, p.Y + 30.0)
    text_id = cad.AddText("REPORT", p.X + 5.0, p.Y + 40.0, p.Z, 4.0)
    ids = [line_id, poly_id, text_id]
    log("Entita create: " + str(len(ids)))

    single_info_path = cad.ExportEntityInfo(poly_id, path_in_script_dir("report_single.txt"))
    log("ExportEntityInfo -> " + single_info_path)

    multi_info_path = cad.ExportEntitiesInfo(ids, path_in_script_dir("report_entities.txt"))
    log("ExportEntitiesInfo -> " + multi_info_path)

    poly_csv_path = cad.ExportPolylineVertices(poly_id, path_in_script_dir("report_poly_vertices.csv"))
    log("ExportPolylineVertices -> " + poly_csv_path)

    selection_csv_path = cad.ExportSelectionToCsv(ids, path_in_script_dir("report_selection.csv"))
    log("ExportSelectionToCsv -> " + selection_csv_path)

    summary = cad.BuildSelectionSummary(ids)
    log("Summary total = " + str(summary["total"]))

    by_type = summary["by_type"]
    for key in by_type.Keys:
        log("Summary by_type -> " + str(key) + ": " + str(by_type[key]))

    by_layer = summary["by_layer"]
    for key in by_layer.Keys:
        log("Summary by_layer -> " + str(key) + ": " + str(by_layer[key]))

    cad.Regen()
    log("Test reporting completato")
