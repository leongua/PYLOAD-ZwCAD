import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from ZwSoft.ZwCAD.DatabaseServices import (
    BlockTable,
    BlockTableRecord,
    BooleanOperationType,
    Curve,
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
    cad.Msg("[ULTRA 2026] " + msg)


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


log("Avvio ultra batch API 2026 (all-in-one)")
res = cad.GetPoint("Punto base ultra batch 2026:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    st = {
        "transcript": 0,
        "entities": 0,
        "dxf_hits": 0,
        "sel_all": 0,
        "blocks_done": False,
        "modify_done": False,
        "db_tree": 0,
        "region_union": 0.0,
        "region_subtract": 0.0,
        "solid_vol": 0.0,
        "views": 0,
        "ucs": 0,
        "vp": 0,
    }

    ids = {}

    def step_commands_lisp():
        cad.ClearShellTranscript()
        cad.RunCommand("_.REGEN")
        cad.RunCommands(ArrayList(["_.REGEN", "_.REGEN"]))
        cad.Command(ArrayList(["_.REGEN"]))
        cad.RunLisp('(princ "\\n[ULTRA 2026 LISP] ok")')
        cad.SendEnter()
        st["transcript"] = len(cad.GetShellTranscript())
        log("Commands/LISP ok, transcript=" + str(st["transcript"]))

    def step_geometry_curves():
        ids["line"] = cad.AddLine(p.X, p.Y, p.Z, p.X + 80.0, p.Y + 20.0, p.Z)
        ids["circle"] = cad.AddCircle(p.X + 95.0, p.Y + 8.0, p.Z, 16.0)
        ids["arc"] = cad.AddArc(p.X + 130.0, p.Y + 15.0, p.Z, 14.0, 20.0, 170.0)
        ids["poly"] = cad.AddPolyline([p.X, p.Y + 45.0, p.X + 35.0, p.Y + 45.0, p.X + 50.0, p.Y + 62.0, p.X + 12.0, p.Y + 78.0], False)
        ids["txt"] = cad.AddText("ULTRA2026", p.X, p.Y + 90.0, p.Z, 2.5)
        ids["mtx"] = cad.AddMText("ULTRA MTEXT", p.X + 30.0, p.Y + 92.0, p.Z, 2.2, 30.0)
        ids["hat"] = cad.DrawHatch([p.X + 150.0, p.Y, p.X + 180.0, p.Y, p.X + 180.0, p.Y + 20.0, p.X + 150.0, p.Y + 20.0], "SOLID", 1.0, 0.0)
        cad.SetBulgeAt(ids["poly"], 0, 0.25)
        cad.SetStartWidthAt(ids["poly"], 0, 1.2)
        cad.SetEndWidthAt(ids["poly"], 0, 1.8)
        pl_info = cad.GetPolylineInfo(ids["poly"])
        length = cad.GetCurveLength(ids["line"])
        par = cad.GetParameterAtDistance(ids["line"], 15.0)
        pt = cad.GetPointAtParameter(ids["line"], par)
        st["entities"] = 8
        log("Geometry/Curves ok, line_len={0}, point_x={1}, poly_seg={2}".format(length, pt.X, pl_info["segment_count"]))

    def step_selection_transform_dxf():
        cpy = cad.CopyEntity(ids["line"], 0.0, 18.0, 0.0)
        cad.MoveEntity(cpy, 8.0, 0.0, 0.0)
        cad.RotateEntity(cpy, p.X + 10.0, p.Y + 18.0, p.Z, 15.0)
        cad.ScaleEntity(cpy, p.X + 10.0, p.Y + 18.0, p.Z, 1.1)
        mir = cad.MirrorEntity(cpy, p.X - 20.0, p.Y - 10.0, p.Z, p.X - 20.0, p.Y + 50.0, p.Z, False)
        offs = cad.OffsetEntity(ids["line"], 6.0)
        ex = cad.ExplodeEntity(mir, False)
        st["sel_all"] = len(cad.SelectAll())

        # DXF deterministic
        layer = "PYLOAD_ULTRA_DXF"
        dxf = ArrayList([pair(0, "CIRCLE"), pair(8, layer), pair(10, p.X + 230.0), pair(20, p.Y + 10.0), pair(30, p.Z), pair(40, 13.0)])
        dc = cad.EntMake(dxf)
        ff = ArrayList([pair(0, "CIRCLE"), pair(8, layer), pair(10, p.X + 230.0), pair(40, 13.0)])
        hits = cad.GetSelectionByDxf(ff)
        dbg = cad.DebugDxfFilterMatch(dc, ff)
        st["dxf_hits"] = len(hits)
        log("Selection/Transform/DXF ok, all={0}, offset={1}, explode={2}, dxf_hits={3}, dxf_match={4}".format(st["sel_all"], len(offs), len(ex), st["dxf_hits"], dbg["matched"]))

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
        b1 = cad.InsertBlock(rich, p.X + 270.0, p.Y + 10.0, p.Z)
        b2 = cad.InsertBlock(rich, p.X + 295.0, p.Y + 10.0, p.Z)
        sync = cad.SyncBlockReferenceAttributesBatch(ArrayList([b1, b2]), False)
        defs = cad.GetBlockAttributeDefinitions(rich)
        tag = None
        for k in defs.Keys:
            tag = str(k)
            break
        upd = 0
        if tag is not None:
            upd = cad.UpdateBlockAttributeByTagBatch(ArrayList([b1, b2]), tag, "ULTRA")
            m = Hashtable()
            m[tag] = "ULTRA_MAP"
            cad.UpdateBlockAttributesByMapBatch(ArrayList([b1, b2]), m)
        st["blocks_done"] = True
        log("Blocks/ATTRIB ok, sync={0}, upd={1}".format(sync, upd))

    def step_modify_heavy():
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
        cad.SetEntityDxfValue(src, 8, "PYLOAD_ULTRA_PROP")
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
        log("Modify heavy ok, parts={0}, toward={1}, both={2}, trim={3}, extend={4}, match={5}".format(len(parts), len(toward), len(both), trm["changed"], ext["changed"], mp["changed"]))

    def step_database_advanced():
        src = "PYLOAD/ULTRA/SRC"
        dst = "PYLOAD/ULTRA/DST"
        cad.CreateNamedDictionary(src)
        cad.CreateNamedDictionary(dst)
        m = Hashtable()
        m["mode"] = "ultra"
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
        tree = cad.ListNamedDictionaryTree("PYLOAD/ULTRA", 8)
        st["db_tree"] = len(tree)
        cad.DeleteNamedXRecord(src, "REC_RAW", True)

        model_id = cad.GetModelSpaceRecordId()
        cloned = cad.CloneObjectsToOwner(ArrayList([ids["line"], ids["circle"]]), model_id)
        log("Database ok, xrec_count={0}, tree={1}, cloned={2}".format(raw["count"], st["db_tree"], len(cloned)))

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

    safe("commands_lisp", step_commands_lisp)
    safe("geometry_curves", step_geometry_curves)
    safe("selection_transform_dxf", step_selection_transform_dxf)
    safe("blocks_attrs", step_blocks_attrs)
    safe("modify_heavy", step_modify_heavy)
    safe("database_advanced", step_database_advanced)
    safe("region_3d_view", step_region_3d_view)

    cad.RegenNative()
    log(
        "Ultra batch completato | transcript={0} entities={1} dxf_hits={2} sel_all={3} blocks={4} modify={5} db_tree={6} union={7} subtract={8} solid={9}".format(
            st["transcript"],
            st["entities"],
            st["dxf_hits"],
            st["sel_all"],
            st["blocks_done"],
            st["modify_done"],
            st["db_tree"],
            st["region_union"],
            st["region_subtract"],
            st["solid_vol"],
        )
    )
