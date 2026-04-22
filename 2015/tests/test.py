import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[TEST] " + msg)


def dump_entity(label, obj_id):
    info = cad.GetEntityInfo(obj_id)
    log(label + " -> id=" + str(info["id"]))
    log(label + " -> handle=" + str(info["handle"]))
    log(label + " -> type=" + str(info["type"]))
    log(label + " -> layer=" + str(info["layer"]))
    log(label + " -> color_index=" + str(info["color_index"]))
    if "min_x" in info:
        log(label + " -> extents min=(" + str(info["min_x"]) + ", " + str(info["min_y"]) + ", " + str(info["min_z"]) + ")")
        log(label + " -> extents max=(" + str(info["max_x"]) + ", " + str(info["max_y"]) + ", " + str(info["max_z"]) + ")")


log("Avvio test PYLOAD")
log("Script path: " + script_path)
log("Script dir: " + script_dir)
log("Drawing path: " + cad.DrawingPath)
log("Drawing name: " + cad.DrawingName)
log("Drawing directory: " + str(cad.DrawingDirectory))
log("Database filename: " + str(cad.DatabaseFilename))
log("Has full drawing path: " + str(cad.HasFullDrawingPath))
log("Is drawing saved: " + str(cad.IsDrawingSaved))
log("Layer corrente iniziale: " + cad.GetCurrentLayer())
layers = cad.ListLayers()
log("Numero layer nel disegno: " + str(len(layers)))

res = cad.GetPoint("Seleziona un punto per il test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata dall'utente.")
else:
    p = res.Value
    log("Punto selezionato: X=" + str(p.X) + " Y=" + str(p.Y) + " Z=" + str(p.Z))

    layer_name = "PYLOAD_TEST"
    log("Verifica layer: " + layer_name)
    if cad.LayerExists(layer_name):
        log("Il layer esiste gia.")
    else:
        log("Il layer non esiste, lo creo.")

    cad.EnsureLayer(layer_name, 3)
    log("Layer garantito: " + layer_name)

    log("Imposto colore layer PYLOAD_TEST = 3")
    cad.SetLayerColor(layer_name, 3)

    cad.SetCurrentLayer(layer_name)
    log("Layer corrente impostato a: " + cad.GetCurrentLayer())

    log("Creo cerchio di test...")
    circle_id = cad.AddCircle(p.X, p.Y, p.Z, 20.0)
    log("Cerchio creato.")

    log("Creo arco di test...")
    arc_id = cad.AddArc(p.X, p.Y, p.Z, 28.0, 20.0, 310.0)
    log("Arco creato.")

    log("Imposto colore cerchio = 1 (rosso)")
    cad.SetEntityColor(circle_id, 1)

    log("Creo linea orizzontale...")
    line1_id = cad.AddLine(p.X - 30.0, p.Y, p.Z, p.X + 30.0, p.Y, p.Z)
    log("Creo linea verticale...")
    line2_id = cad.AddLine(p.X, p.Y - 30.0, p.Z, p.X, p.Y + 30.0, p.Z)
    log("Creo punto centrale...")
    point_id = cad.AddPoint(p.X, p.Y, p.Z)

    log("Preparo coordinate polilinea chiusa 2D...")
    pl = [
        p.X - 15.0, p.Y - 15.0,
        p.X + 15.0, p.Y - 15.0,
        p.X + 15.0, p.Y + 15.0,
        p.X - 15.0, p.Y + 15.0,
    ]
    pl_id = cad.AddPolyline(pl, True)
    log("Polilinea 2D creata.")

    log("Preparo coordinate polilinea 3D aperta...")
    pl3 = [
        p.X - 40.0, p.Y - 40.0, p.Z,
        p.X - 20.0, p.Y - 20.0, p.Z + 10.0,
        p.X + 0.0, p.Y - 10.0, p.Z + 20.0,
        p.X + 25.0, p.Y + 10.0, p.Z + 10.0,
        p.X + 40.0, p.Y + 35.0, p.Z,
    ]
    pl3_id = cad.AddPolyline3d(pl3, False)
    log("Polilinea 3D creata.")

    log("Creo rettangolo con helper dedicato...")
    rect_id = cad.DrawRectangle(p.X - 55.0, p.Y - 18.0, p.X + 55.0, p.Y + 18.0)
    log("Rettangolo creato.")

    log("Imposto colori entita...")
    cad.SetEntityColor(line1_id, 2)
    cad.SetEntityColor(line2_id, 2)
    cad.SetEntityColor(point_id, 5)
    cad.SetEntityColor(pl_id, 4)
    cad.SetEntityColor(arc_id, 6)
    cad.SetEntityColor(pl3_id, 140)
    cad.SetEntityColor(rect_id, 1)
    log("Colori applicati.")

    log("Imposto alcune entita a colore ByLayer...")
    bylayer_res = cad.SetEntitiesColorByLayer([line1_id, line2_id, rect_id])
    log("ByLayer result -> changed=" + str(bylayer_res["changed"]) + " skipped=" + str(bylayer_res["skipped"]))

    log("Creo testo di test...")
    text_id = cad.AddText("PYLOAD TEST", p.X + 10.0, p.Y + 25.0, p.Z, 5.0)
    log("Testo creato.")

    log("Dump informazioni entita create...")
    dump_entity("CERCHIO", circle_id)
    dump_entity("ARCO", arc_id)
    dump_entity("LINEA_1", line1_id)
    dump_entity("LINEA_2", line2_id)
    dump_entity("PUNTO", point_id)
    dump_entity("POLILINEA_2D", pl_id)
    dump_entity("POLILINEA_3D", pl3_id)
    dump_entity("RETTANGOLO", rect_id)
    dump_entity("TESTO", text_id)

    all_ids = cad.SelectAll()
    log("Entita totali selezionate con SelectAll(): " + str(len(all_ids)))

    log("Eseguo Regen...")
    cad.Regen()
    log("Eseguo ZoomExtents...")
    cad.ZoomExtents()
    log("Invio comando nativo: _REGEN")
    cad.RunCommand("_REGEN")
    log("Geometria creata correttamente su layer: " + cad.GetCurrentLayer())
    log("Test completato.")
