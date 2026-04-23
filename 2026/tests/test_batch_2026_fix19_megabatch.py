import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[FIX19 MEGABATCH] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio mega batch FIX19 (layout + blocks advanced + compat report)")
res = cad.GetPoint("Punto base FIX19 megabatch:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    base = r"C:\Users\user\Desktop\PYLOAD"

    def step_layout_paperspace():
        layouts = cad.GetLayoutNamesFromBlockTable()
        target = "*Paper_Space"
        for n in layouts:
            if str(n).lower() != "*model_space":
                target = str(n)
                break

        before = cad.GetSpaceSummary(target)
        try:
            vp = cad.AddPaperViewportToSpace(target, p.X + 30.0, p.Y + 20.0, 80.0, 50.0, p.X, p.Y, 120.0)
        except:
            target = "*Paper_Space"
            vp = cad.AddPaperViewportToSpace(target, p.X + 30.0, p.Y + 20.0, 80.0, 50.0, p.X, p.Y, 120.0)
        ids = cad.GetViewportIdsInSpace(target)
        erased = cad.EraseViewportsInSpace(target, True)
        after = cad.GetSpaceSummary(target)
        log("Layout/Paper ok, target={0}, vp_add={1}, vp_ids={2}, erased={3}, before/after={4}/{5}".format(
            target, vp, len(ids), erased, before["viewports"], after["viewports"]))

    def step_block_advanced_replace():
        a = cad.EnsureTestAttributedBlock("PYLOAD_F19_A", "TAG1", "P", "A_DEF", 2.5)
        b = cad.EnsureTestAttributedBlock("PYLOAD_F19_B", "TAG1", "P", "B_DEF", 2.5)
        c = cad.EnsureTestAttributedBlock("PYLOAD_F19_C", "TAG1", "P", "C_DEF", 2.5)

        for i in range(3):
            m = Hashtable()
            m["TAG1"] = "A_" + str(i + 1)
            cad.InsertBlockWithAttributes(a, p.X + 200.0 + i * 20.0, p.Y + 0.0, p.Z, m)
        for i in range(2):
            m = Hashtable()
            m["TAG1"] = "B_" + str(i + 1)
            cad.InsertBlockWithAttributes(b, p.X + 200.0 + i * 20.0, p.Y + 30.0, p.Z, m)

        ids_a = cad.GetBlockReferenceIdsByName(a)
        rep_a = cad.ReplaceBlockReferencesByName(a, c, True, True)

        rmap = Hashtable()
        rmap[b] = c
        rep_map = cad.ReplaceBlockReferencesByMap(rmap, True, True)

        sync = cad.SyncBlockReferenceAttributesByName(c, False)
        vals = Hashtable()
        vals["TAG1"] = "FIX19"
        upd = cad.UpdateBlockAttributesByNameMap(c, vals)
        ids_c = cad.GetBlockReferenceIdsByName(c)

        log("Blocks advanced ok, refsA_before={0}, repA={1}, repMap={2}/{3}, sync={4}, upd={5}, refsC={6}".format(
            len(ids_a), rep_a["replaced_count"], rep_map["replaced"], rep_map["failed"], sync, upd, len(ids_c)))

    def step_compatibility_report():
        required = ArrayList([
            "RunDeterministicModifyPack",
            "ExportApiMethodsReport",
            "GetLayoutNamesFromBlockTable",
            "GetSpaceSummary",
            "AddPaperViewportToSpace",
            "GetViewportIdsInSpace",
            "ReplaceBlockReferencesByName",
            "ReplaceBlockReferencesByMap",
            "SyncBlockReferenceAttributesByName",
            "UpdateBlockAttributesByNameMap",
            "ExportApiCompatibilityReport",
        ])
        rep = cad.ExportApiCompatibilityReport(base + r"\fix19_api_compatibility.csv", required)
        api_all = cad.ExportApiMethodsReport(base + r"\fix19_api_methods.txt", "")
        log("Compat report ok, requested/present/missing={0}/{1}/{2}".format(rep["requested"], rep["present"], rep["missing"]))
        log("Compat csv -> " + rep["path"])
        log("API methods -> " + api_all)

    safe("layout_paperspace", step_layout_paperspace)
    safe("block_advanced_replace", step_block_advanced_replace)
    safe("compatibility_report", step_compatibility_report)

    cad.RegenNative()
    log("Mega batch FIX19 completato")
