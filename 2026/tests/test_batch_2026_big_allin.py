import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[BATCH 2026 BIG] " + msg)


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


log("Avvio batch unico DXF + Database + Modify advanced")
res = cad.GetPoint("Punto base batch 2026 big:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    ids = {}

    def step_dxf_advanced():
        c = ArrayList()
        c.Add(pair(0, "CIRCLE"))
        c.Add(pair(8, "PYLOAD_DXF26"))
        c.Add(pair(10, p.X + 80.0))
        c.Add(pair(20, p.Y + 10.0))
        c.Add(pair(30, p.Z))
        c.Add(pair(40, 14.0))
        ids["dxf_circle"] = cad.EntMake(c)
        cad.SetEntityDxfValue(ids["dxf_circle"], 40, 20.0)

        l = ArrayList()
        l.Add(pair(0, "LINE"))
        l.Add(pair(8, "PYLOAD_DXF26"))
        l.Add(pair(10, p.X))
        l.Add(pair(20, p.Y))
        l.Add(pair(11, p.X + 50.0))
        l.Add(pair(21, p.Y + 8.0))
        l.Add(pair(39, 2.5))
        ids["dxf_line"] = cad.EntMake(l)

        a = ArrayList()
        a.Add(pair(0, "ARC"))
        a.Add(pair(8, "PYLOAD_DXF26"))
        a.Add(pair(10, p.X + 110.0))
        a.Add(pair(20, p.Y + 40.0))
        a.Add(pair(40, 12.0))
        a.Add(pair(50, 20.0))
        a.Add(pair(51, 170.0))
        ids["dxf_arc"] = cad.EntMake(a)

        f1 = ArrayList()
        f1.Add(pair(0, "CIRCLE"))
        f1.Add(pair(8, "PYLOAD_DXF26"))
        f1.Add(pair(10, p.X + 80.0))
        f1.Add(pair(40, 20.0))
        hits_c = cad.GetSelectionByDxf(f1)
        dbg_c = cad.DebugDxfFilterMatch(ids["dxf_circle"], f1)

        f2 = ArrayList()
        f2.Add(pair(0, "LINE"))
        f2.Add(pair(11, p.X + 50.0))
        f2.Add(pair(39, 2.5))
        hits_l = cad.GetSelectionByDxf(f2)
        dbg_l = cad.DebugDxfFilterMatch(ids["dxf_line"], f2)

        f3 = ArrayList()
        f3.Add(pair(0, "ARC"))
        f3.Add(pair(50, ">=20"))
        f3.Add(pair(51, "<=170"))
        hits_a = cad.GetSelectionByDxf(f3)
        dbg_a = cad.DebugDxfFilterMatch(ids["dxf_arc"], f3)

        log(
            "DXF ok, circle/line/arc hits={0}/{1}/{2}, debug matched={3}/{4}/{5}".format(
                len(hits_c),
                len(hits_l),
                len(hits_a),
                dbg_c["matched"],
                dbg_l["matched"],
                dbg_a["matched"],
            )
        )

    def step_database_path_utils():
        path_src = "PYLOAD/BATCH2026/SRC"
        path_dst = "PYLOAD/BATCH2026/DST"
        cad.CreateNamedDictionary(path_src)
        cad.CreateNamedDictionary(path_dst)

        m = Hashtable()
        m["project"] = "2026R"
        m["phase"] = "BIGBATCH"
        cad.SetNamedStringMap(path_src, "REC_MAP", m)
        rec_map = cad.GetNamedStringMap(path_src, "REC_MAP")

        vals = ArrayList()
        tv1 = Hashtable()
        tv1["type_code"] = 1000
        tv1["value"] = "A"
        vals.Add(tv1)
        tv2 = Hashtable()
        tv2["type_code"] = 1000
        tv2["value"] = "B"
        vals.Add(tv2)
        cad.SetNamedXRecord(path_src, "REC_RAW", vals)
        raw = cad.GetNamedXRecord(path_src, "REC_RAW")

        cad.CopyXRecordBetweenNamedDictionaries(path_src, "REC_RAW", path_dst, "REC_RAW_COPY", True)
        dst_has = cad.NamedDictionaryContains(path_dst, "REC_RAW_COPY")
        tree = cad.ListNamedDictionaryTree("PYLOAD/BATCH2026", 8)
        entries = cad.GetNamedDictionaryEntries(path_src)

        cad.DeleteNamedXRecord(path_src, "REC_RAW", True)
        after_del = cad.NamedDictionaryContains(path_src, "REC_RAW")

        ids["db_line"] = cad.AddLine(p.X + 180.0, p.Y, p.Z, p.X + 230.0, p.Y, p.Z)
        em = Hashtable()
        em["k1"] = "v1"
        em["k2"] = "v2"
        cad.SetEntityStringMap(ids["db_line"], "META/REV", "REC1", em)
        em_read = cad.GetEntityStringMap(ids["db_line"], "META/REV", "REC1")
        ext = cad.GetEntityExtensionDictionaryEntriesAtPath(ids["db_line"], "META/REV")
        cad.DeleteEntityXRecord(ids["db_line"], "META/REV", "REC1", True)
        ext_after = cad.GetEntityExtensionDictionaryEntriesAtPath(ids["db_line"], "META/REV")

        log(
            "DB ok, map/raw/tree/src={0}/{1}/{2}/{3}, dst_copy={4}, raw_deleted={5}, ent_map/ext={6}/{7}->{8}".format(
                rec_map["count"],
                raw["count"],
                len(tree),
                len(entries),
                dst_has,
                not after_del,
                em_read["count"],
                len(ext),
                len(ext_after),
            )
        )

    def step_modify_advanced():
        base = cad.AddLine(p.X + 300.0, p.Y + 10.0, p.Z, p.X + 420.0, p.Y + 10.0, p.Z)
        b1 = cad.AddLine(p.X + 330.0, p.Y - 20.0, p.Z, p.X + 330.0, p.Y + 40.0, p.Z)
        b2 = cad.AddLine(p.X + 380.0, p.Y - 20.0, p.Z, p.X + 380.0, p.Y + 40.0, p.Z)
        parts = cad.BreakCurveAtAllIntersections(base, ArrayList([b1, b2]), True)

        tline = cad.AddLine(p.X + 300.0, p.Y + 70.0, p.Z, p.X + 420.0, p.Y + 70.0, p.Z)
        tb = cad.AddLine(p.X + 360.0, p.Y + 40.0, p.Z, p.X + 360.0, p.Y + 110.0, p.Z)
        trim_id = cad.TrimCurveEndToEntity(tline, tb, True)
        trim_len = cad.GetCurveLength(trim_id)

        eline = cad.AddLine(p.X + 300.0, p.Y + 120.0, p.Z, p.X + 340.0, p.Y + 120.0, p.Z)
        eb = cad.AddLine(p.X + 390.0, p.Y + 90.0, p.Z, p.X + 390.0, p.Y + 150.0, p.Z)
        end_pt = cad.ExtendCurveEndToEntity(eline, eb)

        batch_curves = ArrayList()
        batch_boundaries = ArrayList()
        c1 = cad.AddLine(p.X + 300.0, p.Y + 170.0, p.Z, p.X + 420.0, p.Y + 170.0, p.Z)
        c2 = cad.AddLine(p.X + 300.0, p.Y + 190.0, p.Z, p.X + 350.0, p.Y + 190.0, p.Z)
        bb = cad.AddLine(p.X + 370.0, p.Y + 150.0, p.Z, p.X + 370.0, p.Y + 230.0, p.Z)
        batch_curves.Add(c1)
        batch_curves.Add(c2)
        batch_boundaries.Add(bb)
        trim_info = cad.TrimCurvesToBoundaries(batch_curves, batch_boundaries, "end", False)
        ext_info = cad.ExtendCurvesToBoundaries(batch_curves, batch_boundaries, "end")

        bset = ArrayList()
        e1 = cad.AddLine(p.X + 450.0, p.Y + 10.0, p.Z, p.X + 510.0, p.Y + 10.0, p.Z)
        e2 = cad.AddLine(p.X + 470.0, p.Y - 15.0, p.Z, p.X + 470.0, p.Y + 35.0, p.Z)
        bset.Add(e1)
        bset.Add(e2)
        binfo = cad.BreakEntitiesAtIntersections(bset, True)

        log(
            "Modify ok, break_parts={0}, trim_len={1}, extend_x={2}, trim_changed={3}, extend_changed={4}, break_changed={5}".format(
                len(parts),
                trim_len,
                end_pt.X,
                trim_info["changed"],
                ext_info["changed"],
                binfo["changed"],
            )
        )

    safe("dxf_advanced", step_dxf_advanced)
    safe("database_path_utils", step_database_path_utils)
    safe("modify_advanced", step_modify_advanced)
    cad.RegenNative()
    log("Batch unico completato")
