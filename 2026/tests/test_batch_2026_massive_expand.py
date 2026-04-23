import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[MASSIVE 2026] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


log("Avvio batch massivo aggiuntivo 2026")
res = cad.GetPoint("Punto base MASSIVE 2026:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def step_seed_geometry():
        l1 = cad.AddLine(p.X + 0.0, p.Y + 0.0, p.Z, p.X + 80.0, p.Y + 40.0, p.Z)
        l2 = cad.AddLine(p.X + 0.0, p.Y + 40.0, p.Z, p.X + 80.0, p.Y + 0.0, p.Z)
        c1 = cad.AddCircle(p.X + 40.0, p.Y + 20.0, p.Z, 12.0)
        a1 = cad.EntMake(ArrayList([pair(0, "ARC"), pair(8, "PYLOAD_MASSIVE"), pair(10, p.X + 95.0), pair(20, p.Y + 20.0), pair(40, 16.0), pair(50, 15.0), pair(51, 165.0)]))
        t1 = cad.AddText("MASSIVE", p.X + 0.0, p.Y - 12.0, p.Z, 2.5)
        return l1, l2, c1, a1, t1

    ids = step_seed_geometry()

    def step_spaces():
        layouts = cad.GetLayoutNamesFromBlockTable()
        stats = cad.GetSpaceEntityStats()
        default_space = layouts[0] if len(layouts) > 0 else ""
        in_space = cad.GetEntitiesInSpace(default_space)
        log("Spaces ok, layouts={0}, default={1}, in_space={2}, total={3}".format(len(layouts), default_space, len(in_space), stats["total"]))

    def step_intersections_and_break():
        curves = ArrayList([ids[0], ids[1], ids[2]])
        mat = cad.BuildIntersectionsMatrix(curves, False, False)
        brk = cad.BreakCurvesAtAllIntersectionsBatch(curves, False)
        auto = cad.AutoTrimExtendByBoundaries(ArrayList([ids[0], ids[1]]), ArrayList([ids[2]]), "nearest", "nearest", False)
        log("Intersections/Modify ok, pairs={0}, hits={1}, break_parts={2}, trim/extend={3}/{4}".format(mat["pairs"], mat["intersections"], brk["parts"], auto["trim_changed"], auto["extend_changed"]))

    def step_transform_offset():
        jobs = ArrayList()
        j1 = Hashtable()
        j1["entity_id"] = ids[0]
        j1["copies"] = 3
        j1["dx"] = 15.0
        j1["dy"] = 0.0
        j1["dz"] = 0.0
        j1["rot_deg"] = 8.0
        j1["scale"] = 1.03
        jobs.Add(j1)
        tx = cad.CopyTransformBatch(jobs)

        off_jobs = ArrayList()
        o1 = Hashtable()
        o1["entity_id"] = ids[2]
        o1["distance"] = 2.0
        o1["x"] = p.X + 40.0
        o1["y"] = p.Y + 35.0
        o1["z"] = p.Z
        off_jobs.Add(o1)
        off = cad.OffsetEntitiesTowardSeedsBatch(off_jobs)
        log("Transform/Offset ok, created={0}, offsets={1}/{2}".format(tx["created"], off["ok"], off["fail"]))

    def step_props_and_erase():
        snap = cad.GetEntityPropertySnapshot(ids[4])
        all_lines = cad.GetSelectionByType("LINE")
        changed = cad.ApplyEntityPropertySnapshot(ArrayList(all_lines), snap)

        filters = ArrayList([pair(0, "ARC"), pair(8, "PYLOAD_MASSIVE")])
        erased_dxf = cad.EraseByDxfFilterBatch(filters, True)
        erased_type = cad.EraseByTypeBatch("CIRCLE", True)
        log("Props/Erase ok, props_changed={0}, erased_filter={1}, erased_circle={2}".format(changed, erased_dxf["erased"], erased_type["erased"]))

    def step_exports():
        ms_ids = cad.GetModelSpaceEntityIds()
        base = r"C:\Users\user\Desktop\PYLOAD"
        csv_path = cad.ExportEntityAuditCsv(base + r"\massive_entity_audit.csv", ArrayList(ms_ids))
        snap_path = cad.ExportDatabaseSnapshot(base + r"\massive_db_snapshot.txt", "PYLOAD", 2)
        log("Export ok, csv={0}, snapshot={1}".format(csv_path, snap_path))

    safe("spaces", step_spaces)
    safe("intersections_break", step_intersections_and_break)
    safe("transform_offset", step_transform_offset)
    safe("props_erase", step_props_and_erase)
    safe("exports", step_exports)

    cad.RegenNative()
    log("Batch massivo aggiuntivo 2026 completato")
