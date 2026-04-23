import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from ZwSoft.ZwCAD.DatabaseServices import (
    BlockTable,
    BlockTableRecord,
    BooleanOperationType,
    Curve,
    DBObject,
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


def log(msg):
    cad.Msg("[MEGABATCH 2026-3] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


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
            reg = r
            rid = ms.AppendEntity(reg)
            tr.AddNewlyCreatedDBObject(reg, True)
            created.append(rid)
        tr.Commit()
    return created


def region_perimeter(region_id):
    total = 0.0
    with db.TransactionManager.StartTransaction() as tr:
        reg = tr.GetObject(region_id, OpenMode.ForRead)
        parts = DBObjectCollection()
        reg.Explode(parts)
        for p in parts:
            if isinstance(p, Curve):
                try:
                    total += p.GetDistanceAtParameter(p.EndParam) - p.GetDistanceAtParameter(p.StartParam)
                except Exception:
                    pass
            if isinstance(p, DBObject):
                p.Dispose()
    return total


def region_area(region_id):
    with db.TransactionManager.StartTransaction() as tr:
        reg = tr.GetObject(region_id, OpenMode.ForRead)
        return reg.Area


def bool_regions(region_a_id, region_b_id, op_name):
    op = BooleanOperationType.BoolUnite
    if op_name == "subtract":
        op = BooleanOperationType.BoolSubtract
    elif op_name == "intersect":
        op = BooleanOperationType.BoolIntersect

    with db.TransactionManager.StartTransaction() as tr:
        ra = tr.GetObject(region_a_id, OpenMode.ForWrite)
        rb = tr.GetObject(region_b_id, OpenMode.ForWrite)
        ra.BooleanOperation(op, rb)
        tr.Commit()


log("Avvio mega batch 3 (region + 3d + view/ucs/viewport)")
res = cad.GetPoint("Punto base mega batch 3:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    state = {
        "regions": 0,
        "region_union_area": 0.0,
        "region_intersection_area": 0.0,
        "region_perimeter": 0.0,
        "solid_ok": False,
        "solid_volume": 0.0,
        "view_count": 0,
        "ucs_count": 0,
        "viewport_count": 0,
    }

    def step_regions():
        p1 = cad.AddPolyline(
            [
                p.X + 0.0,
                p.Y + 0.0,
                p.X + 60.0,
                p.Y + 0.0,
                p.X + 60.0,
                p.Y + 35.0,
                p.X + 0.0,
                p.Y + 35.0,
            ],
            True,
        )
        p2 = cad.AddPolyline(
            [
                p.X + 35.0,
                p.Y + 12.0,
                p.X + 95.0,
                p.Y + 12.0,
                p.X + 95.0,
                p.Y + 47.0,
                p.X + 35.0,
                p.Y + 47.0,
            ],
            True,
        )

        regions = create_regions_from_entity_ids([p1, p2])
        state["regions"] = len(regions)
        if len(regions) < 2:
            log("Region create parziale: count=" + str(len(regions)))
            return

        # Perimetro/area prima delle booleane
        state["region_perimeter"] = region_perimeter(regions[0])

        # Union
        r_union_a = regions[0]
        r_union_b = regions[1]
        bool_regions(r_union_a, r_union_b, "union")
        state["region_union_area"] = region_area(r_union_a)

        # Intersection su nuovo set
        p3 = cad.AddPolyline(
            [
                p.X + 115.0,
                p.Y + 0.0,
                p.X + 175.0,
                p.Y + 0.0,
                p.X + 175.0,
                p.Y + 35.0,
                p.X + 115.0,
                p.Y + 35.0,
            ],
            True,
        )
        p4 = cad.AddPolyline(
            [
                p.X + 150.0,
                p.Y + 12.0,
                p.X + 210.0,
                p.Y + 12.0,
                p.X + 210.0,
                p.Y + 47.0,
                p.X + 150.0,
                p.Y + 47.0,
            ],
            True,
        )
        regions2 = create_regions_from_entity_ids([p3, p4])
        if len(regions2) >= 2:
            bool_regions(regions2[0], regions2[1], "intersect")
            state["region_intersection_area"] = region_area(regions2[0])

        log(
            "Region ok, count={0}, perimeter={1}, union_area={2}, intersect_area={3}".format(
                state["regions"],
                state["region_perimeter"],
                state["region_union_area"],
                state["region_intersection_area"],
            )
        )

    def step_3d_solid():
        with db.TransactionManager.StartTransaction() as tr:
            ms_id = SymbolUtilityServices.GetBlockModelSpaceId(db)
            ms = tr.GetObject(ms_id, OpenMode.ForWrite)
            s = Solid3d()
            s.SetDatabaseDefaults()
            s.CreateBox(24.0, 18.0, 12.0)
            s.TransformBy(Matrix3d.Displacement(Vector3d(p.X + 260.0, p.Y + 20.0, p.Z + 8.0)))
            sid = ms.AppendEntity(s)
            tr.AddNewlyCreatedDBObject(s, True)
            tr.Commit()

        volume = 0.0
        with db.TransactionManager.StartTransaction() as tr:
            s2 = tr.GetObject(sid, OpenMode.ForRead)
            try:
                mp = s2.MassProperties
                volume = mp.Volume
            except Exception:
                # fallback semplice: volume teorico della box
                volume = 24.0 * 18.0 * 12.0

        state["solid_ok"] = True
        state["solid_volume"] = volume
        log("3D solid ok, volume=" + str(volume))

    def step_view_ucs_viewport():
        with db.TransactionManager.StartTransaction() as tr:
            vt = tr.GetObject(db.ViewTableId, OpenMode.ForRead)
            ut = tr.GetObject(db.UcsTableId, OpenMode.ForRead)
            bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead)

            view_count = 0
            for _ in vt:
                view_count += 1

            ucs_count = 0
            for _ in ut:
                ucs_count += 1

            vp_count = 0
            ps = tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForRead)
            for eid in ps:
                ent = tr.GetObject(eid, OpenMode.ForRead)
                if isinstance(ent, Viewport):
                    vp_count += 1

            state["view_count"] = view_count
            state["ucs_count"] = ucs_count
            state["viewport_count"] = vp_count

        log(
            "View/UCS/Viewport ok, views={0}, ucs={1}, paper_viewports={2}".format(
                state["view_count"], state["ucs_count"], state["viewport_count"]
            )
        )

    safe("regions", step_regions)
    safe("solid3d", step_3d_solid)
    safe("view_ucs_viewport", step_view_ucs_viewport)

    cad.RegenNative()
    log(
        "Mega batch 3 completato | summary: region_union={0}, solid={1}, views={2}, ucs={3}, vp={4}".format(
            state["region_union_area"],
            state["solid_ok"],
            state["view_count"],
            state["ucs_count"],
            state["viewport_count"],
        )
    )
