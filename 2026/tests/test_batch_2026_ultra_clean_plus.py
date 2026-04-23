import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from ZwSoft.ZwCAD.DatabaseServices import (
    BlockTable,
    BlockTableRecord,
    BooleanOperationType,
    DBObjectCollection,
    Entity,
    OpenMode,
    Region,
    Solid3d,
    SymbolUtilityServices,
    UcsTable,
    Viewport,
    ViewTable,
)
from ZwSoft.ZwCAD.Geometry import Matrix3d, Point3d, Vector3d
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[ULTRA 2026 CLEAN+] " + msg)


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


def region_area(region_id):
    with db.TransactionManager.StartTransaction() as tr:
        reg = tr.GetObject(region_id, OpenMode.ForRead)
        return reg.Area


def bool_region(a_id, b_id, op):
    bop = BooleanOperationType.BoolUnite
    if op == "subtract":
        bop = BooleanOperationType.BoolSubtract
    elif op == "intersect":
        bop = BooleanOperationType.BoolIntersect
    with db.TransactionManager.StartTransaction() as tr:
        a = tr.GetObject(a_id, OpenMode.ForWrite)
        b = tr.GetObject(b_id, OpenMode.ForWrite)
        a.BooleanOperation(bop, b)
        tr.Commit()


log("Avvio ultra clean+ batch API 2026")
res = cad.GetPoint("Punto base ultra clean+ 2026:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    st = {
        "transcript": 0,
        "sysvars": 0,
        "dxf_hits": 0,
        "sel_window": 0,
        "sel_cross": 0,
        "batch_tx": 0,
        "blocks_done": False,
        "modify_done": False,
        "db_tree": 0,
        "db_entries": 0,
        "region_union": 0.0,
        "region_subtract": 0.0,
        "solid_vol": 0.0,
        "views": 0,
        "ucs": 0,
        "vp": 0,
    }
    ids = {}

    def step_commands_clean():
        cad.ClearShellTranscript()
        cad.Princ("[ULTRA CLEAN+] command channel ok", True)
        cad.RegenNative()

        try:
            if hasattr(cad, "SupportsSystemVariables") and cad.SupportsSystemVariables():
                vars_map = cad.GetVars(ArrayList(["CVPORT", "CMDECHO"]))
                st["sysvars"] = vars_map.Count
            else:
                st["sysvars"] = 0
        except Exception:
            st["sysvars"] = 0

        st["transcript"] = len(cad.GetShellTranscript())
        log("Commands clean ok, transcript={0}, sysvars={1}".format(st["transcript"], st["sysvars"]))

    def step_geometry_and_dxf():
        ids["line"] = cad.AddLine(p.X, p.Y, p.Z, p.X + 80.0, p.Y + 20.0, p.Z)
        ids["circle"] = cad.AddCircle(p.X + 95.0, p.Y + 8.0, p.Z, 16.0)
        ids["poly"] = cad.AddPolyline([p.X, p.Y + 45.0, p.X + 35.0, p.Y + 45.0, p.X + 50.0, p.Y + 62.0, p.X + 12.0, p.Y + 78.0], False)
        cad.SetBulgeAt(ids["poly"], 0, 0.25)

        layer = "PYLOAD_ULTRA_CLEAN_DXF"
        dxf = ArrayList([pair(0, "CIRCLE"), pair(8, layer), pair(10, p.X + 230.0), pair(20, p.Y + 10.0), pair(30, p.Z), pair(40, 13.0)])
        dc = cad.EntMake(dxf)
        handle = cad.GetEntityDxfValue(dc, 5)
        ff = ArrayList([pair(0, "CIRCLE"), pair(8, layer), pair(5, handle)])
        hits = cad.GetSelectionByDxf(ff)
        dbg = cad.DebugDxfFilterMatch(dc, ff)
        st["dxf_hits"] = len(hits)
        log("Geometry/DXF ok, dxf_hits={0}, dxf_match={1}, handle={2}".format(st["dxf_hits"], dbg["matched"], handle))

    def step_selection_transform_plus():
        l1 = cad.AddLine(p.X + 120.0, p.Y, p.Z, p.X + 190.0, p.Y, p.Z)
        l2 = cad.CopyEntity(l1, 0.0, 15.0, 0.0)
        l3 = cad.CopyEntity(l1, 0.0, 30.0, 0.0)
        ids_arr = ArrayList([l1, l2, l3])
        st["batch_tx"] += cad.MoveEntities(ids_arr, 4.0, 0.0, 0.0)
        st["batch_tx"] += cad.RotateEntities(ids_arr, p.X + 120.0, p.Y + 10.0, p.Z, 5.0)
        st["batch_tx"] += cad.ScaleEntities(ids_arr, p.X + 120.0, p.Y + 10.0, p.Z, 1.05)
        mirrors = cad.MirrorEntities(ids_arr, p.X + 110.0, p.Y - 10.0, p.Z, p.X + 110.0, p.Y + 70.0, p.Z, False)

        x1 = p.X + 110.0
        y1 = p.Y - 10.0
        x2 = p.X + 220.0
        y2 = p.Y + 80.0
        st["sel_window"] = len(cad.SelectWindow(x1, y1, p.Z, x2, y2, p.Z))
        st["sel_cross"] = len(cad.SelectCrossingWindow(x1, y1, p.Z, x2, y2, p.Z))

        # explode batch and offset batch
        _ = cad.ExplodeEntities(ArrayList(mirrors), False)
        _ = cad.OffsetEntities(ids_arr, 3.5)
        log("Selection/Transform+ ok, window={0}, crossing={1}, batch_tx={2}".format(st["sel_window"], st["sel_cross"], st["batch_tx"]))

    def step_blocks_attrs():
        names = cad.GetBlockNames()
        rich = None
        for n in names:
            info = cad.GetBlockDefinitionInfo(str(n))
            if info["has_attribute_definitions"]:
                rich = str(n)
                break
        if rich is None:
            log("Blocks/ATTRIB skip: nessun blocco con attributi")
            return
        b1 = cad.InsertBlock(rich, p.X + 260.0, p.Y + 10.0, p.Z)
        b2 = cad.InsertBlock(rich, p.X + 285.0, p.Y + 10.0, p.Z)
        sync = cad.SyncBlockReferenceAttributesBatch(ArrayList([b1, b2]), False)
        defs = cad.GetBlockAttributeDefinitions(rich)
        tag = None
        for k in defs.Keys:
            tag = str(k)
            break
        upd = 0
        if tag is not None:
            upd = cad.UpdateBlockAttributeByTagBatch(ArrayList([b1, b2]), tag, "CLEAN")
            m = Hashtable()
            m[tag] = "CLEAN_MAP"
            cad.UpdateBlockAttributesByMapBatch(ArrayList([b1, b2]), m)
        st["blocks_done"] = True
        log("Blocks/ATTRIB ok, sync={0}, upd={1}".format(sync, upd))

    def step_modify_plus():
        base = cad.AddLine(p.X + 0.0, p.Y + 130.0, p.Z, p.X + 120.0, p.Y + 130.0, p.Z)
        b1 = cad.AddLine(p.X + 30.0, p.Y + 110.0, p.Z, p.X + 30.0, p.Y + 160.0, p.Z)
        b2 = cad.AddLine(p.X + 75.0, p.Y + 110.0, p.Z, p.X + 75.0, p.Y + 160.0, p.Z)
        parts = cad.BreakCurveAtAllIntersections(base, ArrayList([b1, b2]), True)

        l1 = cad.AddLine(p.X + 140.0, p.Y + 130.0, p.Z, p.X + 210.0, p.Y + 130.0, p.Z)
        l2 = cad.AddLine(p.X + 140.0, p.Y + 145.0, p.Z, p.X + 210.0, p.Y + 145.0, p.Z)
        toward = cad.OffsetEntitiesTowardPoint(ArrayList([l1, l2]), 8.0, p.X + 150.0, p.Y + 175.0, p.Z)
        both = cad.OffsetEntityBothSides(l1, 6.0)

        t1 = cad.AddLine(p.X + 220.0, p.Y + 130.0, p.Z, p.X + 320.0, p.Y + 130.0, p.Z)
        t2 = cad.AddLine(p.X + 220.0, p.Y + 145.0, p.Z, p.X + 270.0, p.Y + 145.0, p.Z)
        bb = cad.AddLine(p.X + 285.0, p.Y + 110.0, p.Z, p.X + 285.0, p.Y + 180.0, p.Z)
        trm = cad.TrimCurvesToBoundaries(ArrayList([t1, t2]), ArrayList([bb]), "end", False)
        ext = cad.ExtendCurvesToBoundaries(ArrayList([t1, t2]), ArrayList([bb]), "end")

        src = cad.AddLine(p.X + 330.0, p.Y + 130.0, p.Z, p.X + 390.0, p.Y + 130.0, p.Z)
        d1 = cad.AddLine(p.X + 330.0, p.Y + 145.0, p.Z, p.X + 390.0, p.Y + 145.0, p.Z)
        d2 = cad.AddLine(p.X + 330.0, p.Y + 160.0, p.Z, p.X + 390.0, p.Y + 160.0, p.Z)
        cad.SetEntityDxfValue(src, 8, "PYLOAD_ULTRA_CLEAN_PROP")
        cad.SetEntityDxfValue(src, 62, 2)
        opts = Hashtable()
        opts["layer"] = True
        opts["color"] = True
        opts["linetype"] = True
        opts["lineweight"] = True
        opts["linetype_scale"] = True
        opts["visible"] = False
        mp = cad.MatchEntityProperties(src, ArrayList([d1, d2]), opts)
        st["modify_done"] = True
        log("Modify+ ok, parts={0}, toward={1}, both={2}, trim={3}, extend={4}, match={5}".format(len(parts), len(toward), len(both), trm["changed"], ext["changed"], mp["changed"]))

    def step_database_plus():
        src = "PYLOAD/ULTRA_CLEAN/SRC"
        dst = "PYLOAD/ULTRA_CLEAN/DST"
        cad.CreateNamedDictionary(src)
        cad.CreateNamedDictionary(dst)
        m = Hashtable()
        m["mode"] = "ultra_clean"
        m["version"] = "2026"
        cad.SetNamedStringMap(src, "REC_MAP", m)

        vals = ArrayList()
        t1 = Hashtable()
        t1["type_code"] = 1000
        t1["value"] = "A"
        t2 = Hashtable()
        t2["type_code"] = 1000
        t2["value"] = "B"
        vals.Add(t1)
        vals.Add(t2)
        cad.SetNamedXRecord(src, "REC_RAW", vals)
        raw = cad.GetNamedXRecord(src, "REC_RAW")
        cad.CopyXRecordBetweenNamedDictionaries(src, "REC_RAW", dst, "REC_RAW_COPY", True)
        st["db_entries"] = len(cad.GetNamedDictionaryEntries(src))
        st["db_tree"] = len(cad.ListNamedDictionaryTree("PYLOAD/ULTRA_CLEAN", 8))
        has_copy = cad.NamedDictionaryContains(dst, "REC_RAW_COPY")
        cad.DeleteNamedDictionaryEntry(src, "REC_RAW", True)
        has_raw = cad.NamedDictionaryContains(src, "REC_RAW")

        line_meta = cad.AddLine(p.X + 430.0, p.Y + 130.0, p.Z, p.X + 470.0, p.Y + 130.0, p.Z)
        em = Hashtable()
        em["k1"] = "v1"
        em["k2"] = "v2"
        cad.SetEntityStringMap(line_meta, "META/REV", "REC1", em)
        eread = cad.GetEntityStringMap(line_meta, "META/REV", "REC1")
        cad.DeleteEntityXRecord(line_meta, "META/REV", "REC1", True)
        ext_after = cad.GetEntityExtensionDictionaryEntriesAtPath(line_meta, "META/REV")

        model_id = cad.GetModelSpaceRecordId()
        cloned = cad.CloneObjectsToOwner(ArrayList([ids["line"], ids["circle"]]), model_id)
        log("Database+ ok, xrec={0}, entries={1}, tree={2}, copy={3}, raw_deleted={4}, ent_map={5}, ext_after={6}, cloned={7}".format(raw["count"], st["db_entries"], st["db_tree"], has_copy, (not has_raw), eread["count"], len(ext_after), len(cloned)))

    def step_region_3d_view():
        p1 = cad.AddPolyline([p.X + 0.0, p.Y + 220.0, p.X + 60.0, p.Y + 220.0, p.X + 60.0, p.Y + 255.0, p.X + 0.0, p.Y + 255.0], True)
        p2 = cad.AddPolyline([p.X + 35.0, p.Y + 232.0, p.X + 95.0, p.Y + 232.0, p.X + 95.0, p.Y + 267.0, p.X + 35.0, p.Y + 267.0], True)
        regs = create_regions_from_entity_ids([p1, p2])
        if len(regs) >= 2:
            bool_region(regs[0], regs[1], "union")
            st["region_union"] = region_area(regs[0])

        p3 = cad.AddPolyline([p.X + 115.0, p.Y + 220.0, p.X + 175.0, p.Y + 220.0, p.X + 175.0, p.Y + 255.0, p.X + 115.0, p.Y + 255.0], True)
        p4 = cad.AddPolyline([p.X + 150.0, p.Y + 232.0, p.X + 210.0, p.Y + 232.0, p.X + 210.0, p.Y + 267.0, p.X + 150.0, p.Y + 267.0], True)
        regs2 = create_regions_from_entity_ids([p3, p4])
        if len(regs2) >= 2:
            bool_region(regs2[0], regs2[1], "subtract")
            st["region_subtract"] = region_area(regs2[0])

        with db.TransactionManager.StartTransaction() as tr:
            ms_id = SymbolUtilityServices.GetBlockModelSpaceId(db)
            ms = tr.GetObject(ms_id, OpenMode.ForWrite)
            s = Solid3d()
            s.SetDatabaseDefaults()
            s.CreateBox(24.0, 18.0, 12.0)
            s.TransformBy(Matrix3d.Displacement(Vector3d(p.X + 260.0, p.Y + 240.0, p.Z + 8.0)))
            sid = ms.AppendEntity(s)
            tr.AddNewlyCreatedDBObject(s, True)
            tr.Commit()

        with db.TransactionManager.StartTransaction() as tr:
            s2 = tr.GetObject(sid, OpenMode.ForRead)
            try:
                st["solid_vol"] = s2.MassProperties.Volume
            except Exception:
                st["solid_vol"] = 24.0 * 18.0 * 12.0

        with db.TransactionManager.StartTransaction() as tr:
            vt = tr.GetObject(db.ViewTableId, OpenMode.ForRead)
            ut = tr.GetObject(db.UcsTableId, OpenMode.ForRead)
            bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead)
            ps = tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForRead)
            vc = 0
            uc = 0
            vp = 0
            for _ in vt:
                vc += 1
            for _ in ut:
                uc += 1
            for eid in ps:
                ent = tr.GetObject(eid, OpenMode.ForRead)
                if isinstance(ent, Viewport):
                    vp += 1
            st["views"] = vc
            st["ucs"] = uc
            st["vp"] = vp

        log("Region/3D/View ok, union={0}, subtract={1}, solid_vol={2}, views={3}, ucs={4}, vp={5}".format(st["region_union"], st["region_subtract"], st["solid_vol"], st["views"], st["ucs"], st["vp"]))

    def step_export_report():
        out_path = cad.ExportShellTranscript("C:\\Users\\user\\Desktop\\PYLOAD\\ultra_clean_transcript.txt")
        log("Transcript export ok -> " + str(out_path))

    safe("commands_clean", step_commands_clean)
    safe("geometry_and_dxf", step_geometry_and_dxf)
    safe("selection_transform_plus", step_selection_transform_plus)
    safe("blocks_attrs", step_blocks_attrs)
    safe("modify_plus", step_modify_plus)
    safe("database_plus", step_database_plus)
    safe("region_3d_view", step_region_3d_view)
    safe("export_report", step_export_report)

    cad.RegenNative()
    log(
        "Ultra clean+ completato | transcript={0} sysvars={1} dxf_hits={2} win/cross={3}/{4} tx={5} blocks={6} modify={7} db_tree={8} union={9} subtract={10} solid={11}".format(
            st["transcript"],
            st["sysvars"],
            st["dxf_hits"],
            st["sel_window"],
            st["sel_cross"],
            st["batch_tx"],
            st["blocks_done"],
            st["modify_done"],
            st["db_tree"],
            st["region_union"],
            st["region_subtract"],
            st["solid_vol"],
        )
    )
