import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from ZwSoft.ZwCAD.DatabaseServices import (
    BlockTableRecord,
    BooleanOperationType,
    DBObjectCollection,
    Entity,
    OpenMode,
    Region,
    Solid3d,
    SymbolUtilityServices,
)
from ZwSoft.ZwCAD.Geometry import Matrix3d, Vector3d
from System.Collections import ArrayList, Hashtable
from System import DateTime


def log(msg):
    cad.Msg("[ULTRA STRESS 2026] " + msg)


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


def create_regions_from_entity_ids(entity_ids):
    created = []
    with db.TransactionManager.StartTransaction() as tr:
        ms_id = SymbolUtilityServices.GetBlockModelSpaceId(db)
        ms = tr.GetObject(ms_id, OpenMode.ForWrite)
        curves = DBObjectCollection()
        for eid in entity_ids:
            ent = tr.GetObject(eid, OpenMode.ForRead)
            if isinstance(ent, Entity):
                curves.Add(ent)
        regs = Region.CreateFromCurves(curves)
        for r in regs:
            rid = ms.AppendEntity(r)
            tr.AddNewlyCreatedDBObject(r, True)
            created.append(rid)
        tr.Commit()
    return created


def bool_region(a_id, b_id, op_name):
    op = BooleanOperationType.BoolUnite
    if op_name == "subtract":
        op = BooleanOperationType.BoolSubtract
    elif op_name == "intersect":
        op = BooleanOperationType.BoolIntersect
    with db.TransactionManager.StartTransaction() as tr:
        a = tr.GetObject(a_id, OpenMode.ForWrite)
        b = tr.GetObject(b_id, OpenMode.ForWrite)
        a.BooleanOperation(op, b)
        tr.Commit()


def region_area(region_id):
    with db.TransactionManager.StartTransaction() as tr:
        reg = tr.GetObject(region_id, OpenMode.ForRead)
        return reg.Area


def run_step(step_name, fn, stats):
    try:
        fn()
        stats[step_name + "_ok"] += 1
        return True
    except Exception as ex:
        stats[step_name + "_fail"] += 1
        log(step_name + " -> ERRORE: " + str(ex))
        return False


log("Avvio ULTRA STRESS LOOP (multi-ciclo senza riavvio)")
res = cad.GetPoint("Punto base ultra stress 2026:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    cycles = 10

    stats = {
        "commands_ok": 0, "commands_fail": 0,
        "dxf_ok": 0, "dxf_fail": 0,
        "transform_ok": 0, "transform_fail": 0,
        "modify_ok": 0, "modify_fail": 0,
        "database_ok": 0, "database_fail": 0,
        "region3d_ok": 0, "region3d_fail": 0,
    }

    rows = []
    cad.ClearShellTranscript()

    for i in range(cycles):
        ox = p.X + (i * 700.0)
        oy = p.Y + ((i % 2) * 260.0)
        layer_name = "PYLOAD_STRESS_" + str(i)
        cycle_info = {"cycle": i + 1, "ok": 0, "fail": 0}

        def step_commands():
            cad.Princ("[STRESS] cycle " + str(i + 1), True)
            cad.RegenNative()

        def step_dxf():
            dxf = ArrayList([
                pair(0, "CIRCLE"),
                pair(8, layer_name),
                pair(10, ox + 50.0),
                pair(20, oy + 10.0),
                pair(30, p.Z),
                pair(40, 12.0),
            ])
            c = cad.EntMake(dxf)
            ff = ArrayList([pair(0, "CIRCLE"), pair(8, layer_name), pair(10, ox + 50.0), pair(40, 12.0)])
            hits = cad.GetSelectionByDxf(ff)
            if len(hits) < 1:
                raise Exception("DXF hits attesi >= 1")
            dbg = cad.DebugDxfFilterMatch(c, ff)
            if not dbg["matched"]:
                raise Exception("DebugDxfFilterMatch == False")

        def step_transform():
            l = cad.AddLine(ox + 0.0, oy + 0.0, p.Z, ox + 70.0, oy + 0.0, p.Z)
            c1 = cad.CopyEntity(l, 0.0, 15.0, 0.0)
            c2 = cad.CopyEntity(l, 0.0, 30.0, 0.0)
            ids = ArrayList([l, c1, c2])
            moved = cad.MoveEntities(ids, 4.0, 0.0, 0.0)
            rot = cad.RotateEntities(ids, ox + 10.0, oy + 10.0, p.Z, 5.0)
            scl = cad.ScaleEntities(ids, ox + 10.0, oy + 10.0, p.Z, 1.03)
            if (moved + rot + scl) < 3:
                raise Exception("Batch transforms troppo basso")

        def step_modify():
            base = cad.AddLine(ox + 0.0, oy + 90.0, p.Z, ox + 120.0, oy + 90.0, p.Z)
            b1 = cad.AddLine(ox + 30.0, oy + 70.0, p.Z, ox + 30.0, oy + 130.0, p.Z)
            b2 = cad.AddLine(ox + 75.0, oy + 70.0, p.Z, ox + 75.0, oy + 130.0, p.Z)
            parts = cad.BreakCurveAtAllIntersections(base, ArrayList([b1, b2]), True)
            if len(parts) < 2:
                raise Exception("Break parts insufficiente")

            l1 = cad.AddLine(ox + 140.0, oy + 90.0, p.Z, ox + 210.0, oy + 90.0, p.Z)
            l2 = cad.AddLine(ox + 140.0, oy + 105.0, p.Z, ox + 210.0, oy + 105.0, p.Z)
            toward = cad.OffsetEntitiesTowardPoint(ArrayList([l1, l2]), 8.0, ox + 150.0, oy + 135.0, p.Z)
            if len(toward) < 2:
                raise Exception("Offset toward insufficiente")

        def step_database():
            src = "PYLOAD/STRESS/" + str(i) + "/SRC"
            dst = "PYLOAD/STRESS/" + str(i) + "/DST"
            cad.CreateNamedDictionary(src)
            cad.CreateNamedDictionary(dst)

            vals = ArrayList()
            t1 = Hashtable()
            t1["type_code"] = 1000
            t1["value"] = "A" + str(i)
            t2 = Hashtable()
            t2["type_code"] = 1000
            t2["value"] = "B" + str(i)
            vals.Add(t1)
            vals.Add(t2)
            cad.SetNamedXRecord(src, "REC", vals)
            cad.CopyXRecordBetweenNamedDictionaries(src, "REC", dst, "REC_COPY", True)
            if not cad.NamedDictionaryContains(dst, "REC_COPY"):
                raise Exception("Copy XRecord failed")

        def step_region3d():
            p1 = cad.AddPolyline([ox + 0.0, oy + 170.0, ox + 60.0, oy + 170.0, ox + 60.0, oy + 205.0, ox + 0.0, oy + 205.0], True)
            p2 = cad.AddPolyline([ox + 35.0, oy + 182.0, ox + 95.0, oy + 182.0, ox + 95.0, oy + 217.0, ox + 35.0, oy + 217.0], True)
            regs = create_regions_from_entity_ids([p1, p2])
            if len(regs) < 2:
                raise Exception("Region create insufficiente")
            bool_region(regs[0], regs[1], "union")
            ua = region_area(regs[0])
            if ua <= 0.0:
                raise Exception("Union area non valida")

            with db.TransactionManager.StartTransaction() as tr:
                ms_id = SymbolUtilityServices.GetBlockModelSpaceId(db)
                ms = tr.GetObject(ms_id, OpenMode.ForWrite)
                s = Solid3d()
                s.SetDatabaseDefaults()
                s.CreateBox(16.0, 12.0, 8.0)
                s.TransformBy(Matrix3d.Displacement(Vector3d(ox + 250.0, oy + 190.0, p.Z + 5.0)))
                ms.AppendEntity(s)
                tr.AddNewlyCreatedDBObject(s, True)
                tr.Commit()

        for step_name, fn in [
            ("commands", step_commands),
            ("dxf", step_dxf),
            ("transform", step_transform),
            ("modify", step_modify),
            ("database", step_database),
            ("region3d", step_region3d),
        ]:
            ok = run_step(step_name, fn, stats)
            if ok:
                cycle_info["ok"] += 1
            else:
                cycle_info["fail"] += 1

        rows.append(cycle_info)
        log("cycle {0}/{1} -> ok={2}, fail={3}".format(i + 1, cycles, cycle_info["ok"], cycle_info["fail"]))

    # export transcript txt (via API)
    out_txt = cad.ExportShellTranscript("C:\\Users\\user\\Desktop\\PYLOAD\\ultra_stress_transcript.txt")

    # export csv (python)
    out_csv = "C:\\Users\\user\\Desktop\\PYLOAD\\ultra_stress_report.csv"
    with open(out_csv, "w") as f:
        f.write("cycle,ok,fail\n")
        for r in rows:
            f.write("{0},{1},{2}\n".format(r["cycle"], r["ok"], r["fail"]))
        f.write("\n")
        f.write("section,ok,fail\n")
        for k in ["commands", "dxf", "transform", "modify", "database", "region3d"]:
            f.write("{0},{1},{2}\n".format(k, stats[k + "_ok"], stats[k + "_fail"]))

    cad.RegenNative()
    log(
        "Completato | cycles={0} | cmd={1}/{2} dxf={3}/{4} tx={5}/{6} modify={7}/{8} db={9}/{10} region3d={11}/{12}".format(
            cycles,
            stats["commands_ok"], stats["commands_fail"],
            stats["dxf_ok"], stats["dxf_fail"],
            stats["transform_ok"], stats["transform_fail"],
            stats["modify_ok"], stats["modify_fail"],
            stats["database_ok"], stats["database_fail"],
            stats["region3d_ok"], stats["region3d_fail"],
        )
    )
    log("report txt -> " + str(out_txt))
    log("report csv -> " + out_csv)
