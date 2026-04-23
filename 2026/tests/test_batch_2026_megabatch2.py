import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[MEGABATCH 2026-2] " + msg)


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


log("Avvio mega batch 2 (deterministic dxf + heavy modify + props)")
res = cad.GetPoint("Punto base mega batch 2:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def step_dxf_deterministic():
        layer = "PYLOAD_DXF26_DET"
        c = ArrayList()
        c.Add(pair(0, "CIRCLE"))
        c.Add(pair(8, layer))
        c.Add(pair(10, p.X + 40.0))
        c.Add(pair(20, p.Y + 20.0))
        c.Add(pair(30, p.Z))
        c.Add(pair(40, 11.0))
        cid = cad.EntMake(c)
        handle = cad.GetEntityHandle(cid)

        f = ArrayList()
        f.Add(pair(0, "CIRCLE"))
        f.Add(pair(8, layer))
        f.Add(pair(330, "29"))  # owner typical model space handle in this build family
        f.Add(pair(40, 11.0))
        hits = cad.GetSelectionByDxf(f)
        dbg = cad.DebugDxfFilterMatch(cid, f)
        log("DXF deterministic hits={0}, debug={1}, handle={2}".format(len(hits), dbg["matched"], handle))

    def step_modify_heavy():
        base = cad.AddLine(p.X + 120.0, p.Y + 0.0, p.Z, p.X + 250.0, p.Y + 0.0, p.Z)
        b1 = cad.AddLine(p.X + 150.0, p.Y - 20.0, p.Z, p.X + 150.0, p.Y + 40.0, p.Z)
        b2 = cad.AddLine(p.X + 200.0, p.Y - 20.0, p.Z, p.X + 200.0, p.Y + 40.0, p.Z)
        parts = cad.BreakCurveAtAllIntersections(base, ArrayList([b1, b2]), True)

        l1 = cad.AddLine(p.X + 120.0, p.Y + 70.0, p.Z, p.X + 190.0, p.Y + 70.0, p.Z)
        l2 = cad.AddLine(p.X + 120.0, p.Y + 85.0, p.Z, p.X + 190.0, p.Y + 85.0, p.Z)
        offsets = cad.OffsetEntitiesTowardPoint(ArrayList([l1, l2]), 8.0, p.X + 130.0, p.Y + 120.0, p.Z)
        both = cad.OffsetEntityBothSides(l1, 6.0)

        c1 = cad.AddLine(p.X + 120.0, p.Y + 140.0, p.Z, p.X + 250.0, p.Y + 140.0, p.Z)
        c2 = cad.AddLine(p.X + 120.0, p.Y + 160.0, p.Z, p.X + 250.0, p.Y + 160.0, p.Z)
        bb = cad.AddLine(p.X + 210.0, p.Y + 120.0, p.Z, p.X + 210.0, p.Y + 200.0, p.Z)
        trim = cad.TrimCurvesToBoundaries(ArrayList([c1, c2]), ArrayList([bb]), "end", False)
        ext = cad.ExtendCurvesToBoundaries(ArrayList([c1, c2]), ArrayList([bb]), "end")

        log(
            "Modify heavy ok, break_parts={0}, toward={1}, both_sides={2}, trim={3}, extend={4}".format(
                len(parts), len(offsets), len(both), trim["changed"], ext["changed"]
            )
        )

    def step_match_props():
        src = cad.AddLine(p.X + 300.0, p.Y + 20.0, p.Z, p.X + 360.0, p.Y + 20.0, p.Z)
        t1 = cad.AddLine(p.X + 300.0, p.Y + 35.0, p.Z, p.X + 360.0, p.Y + 35.0, p.Z)
        t2 = cad.AddLine(p.X + 300.0, p.Y + 50.0, p.Z, p.X + 360.0, p.Y + 50.0, p.Z)
        cad.SetEntityDxfValue(src, 8, "PYLOAD_PROP_SRC")
        cad.SetEntityDxfValue(src, 62, 3)
        opts = Hashtable()
        opts["layer"] = True
        opts["color"] = True
        opts["linetype"] = True
        opts["lineweight"] = True
        opts["linetype_scale"] = True
        opts["visible"] = False
        info = cad.MatchEntityProperties(src, ArrayList([t1, t2]), opts)
        hits = cad.GetSelectionByDxf(ArrayList([pair(0, "LINE"), pair(8, "PYLOAD_PROP_SRC"), pair(62, 3)]))
        log("Match props ok, changed={0}, hits_same_props={1}".format(info["changed"], len(hits)))

    safe("dxf_deterministic", step_dxf_deterministic)
    safe("modify_heavy", step_modify_heavy)
    safe("match_props", step_match_props)
    cad.RegenNative()
    log("Mega batch 2 completato")
