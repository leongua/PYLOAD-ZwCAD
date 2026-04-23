import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[BRIDGE EXPAND 2026] " + msg)


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


log("Avvio test nuove API bridge expansion")
res = cad.GetPoint("Punto base bridge expansion:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def step_views_ucs():
        v_name = "PYLOAD_VIEW_TEST"
        u_name = "PYLOAD_UCS_TEST"
        v_new = cad.EnsureNamedView(v_name, p.X, p.Y, 120.0, 80.0)
        u_new = cad.EnsureNamedUcs(u_name, p.X, p.Y, p.Z, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0)
        stats = cad.GetViewUcsViewportStats()
        views = cad.GetViewNames()
        ucs = cad.GetUcsNames()
        log("View/UCS ok, ensure new={0}/{1}, stats={2}/{3}/{4}, names={5}/{6}".format(v_new, u_new, stats["views"], stats["ucs"], stats["paper_viewports"], len(views), len(ucs)))

    def step_region_api():
        p1 = cad.AddPolyline([p.X + 0.0, p.Y + 30.0, p.X + 50.0, p.Y + 30.0, p.X + 50.0, p.Y + 60.0, p.X + 0.0, p.Y + 60.0], True)
        p2 = cad.AddPolyline([p.X + 20.0, p.Y + 40.0, p.X + 70.0, p.Y + 40.0, p.X + 70.0, p.Y + 70.0, p.X + 20.0, p.Y + 70.0], True)
        regs = cad.CreateRegionsFromEntities(ArrayList([p1, p2]))
        if len(regs) >= 2:
            area0 = cad.GetRegionArea(regs[0])
            cad.BooleanRegions(regs[0], regs[1], "union")
            info = cad.GetRegionInfo(regs[0])
            exp = cad.ExplodeRegion(regs[0], False)
            log("Region API ok, start_area={0}, union_area={1}, exploded={2}".format(area0, info["area"], len(exp)))
        else:
            log("Region API parziale: create count=" + str(len(regs)))

    def step_solid_batch():
        b1 = Hashtable()
        b1["x"] = p.X + 100.0
        b1["y"] = p.Y + 20.0
        b1["z"] = p.Z + 5.0
        b1["length"] = 16.0
        b1["width"] = 12.0
        b1["height"] = 8.0
        b2 = Hashtable()
        b2["x"] = p.X + 130.0
        b2["y"] = p.Y + 20.0
        b2["z"] = p.Z + 5.0
        b2["length"] = 20.0
        b2["width"] = 10.0
        b2["height"] = 6.0
        boxes = cad.AddBoxesBatch(ArrayList([b1, b2]))
        one = cad.AddBox(p.X + 160.0, p.Y + 20.0, p.Z + 5.0, 18.0, 9.0, 7.0)
        sinfo = cad.GetSolid3dInfo(one)
        log("Solid API ok, batch={0}, one_vol={1}".format(len(boxes), sinfo["volume"]))

    def step_blocks_attr_api():
        bn = cad.EnsureTestAttributedBlock("PYLOAD_ATTR_BLOCK", "TAG1", "PROMPT1", "DEF1", 2.5)
        m = Hashtable()
        m["TAG1"] = "VAL_TEST"
        br = cad.InsertBlockWithAttributes(bn, p.X + 210.0, p.Y + 20.0, p.Z, m)
        attrs = cad.GetBlockReferenceAttributes(br)
        changed = cad.SetBlockReferenceAttributes(br, m)
        log("Block attr API ok, block={0}, attr_count={1}, changed={2}".format(bn, attrs["count"], changed))

    def step_entity_counts_db():
        ms = cad.GetModelSpaceEntityIds()
        ps = cad.GetPaperSpaceEntityIds()
        cnt = cad.CountEntitiesInModelSpaceByDxf()

        src = "PYLOAD/EXPAND/SRC"
        cad.CreateNamedDictionary(src)
        vals = ArrayList()
        t1 = Hashtable()
        t1["type_code"] = 1000
        t1["value"] = "EXP"
        vals.Add(t1)
        cad.SetNamedXRecord(src, "REC", vals)
        ecount = cad.GetNamedDictionaryEntriesCount(src)
        log("Counts/DB helpers ok, ms={0}, ps={1}, total={2}, dict_entries={3}".format(len(ms), len(ps), cnt["total"], ecount))

    safe("views_ucs", step_views_ucs)
    safe("region_api", step_region_api)
    safe("solid_batch", step_solid_batch)
    safe("blocks_attr_api", step_blocks_attr_api)
    safe("entity_counts_db", step_entity_counts_db)

    cad.RegenNative()
    log("Test bridge expansion completato")
