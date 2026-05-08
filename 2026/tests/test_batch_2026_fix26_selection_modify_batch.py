import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[FIX26 SEL/MOD BATCH] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio FIX26 selection/modify batch")
res = cad.GetPoint("Punto base FIX26:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def step_setup():
        l1 = cad.AddLine(p.X + 0.0, p.Y + 0.0, p.Z, p.X + 40.0, p.Y + 0.0, p.Z)
        l2 = cad.AddLine(p.X + 10.0, p.Y - 10.0, p.Z, p.X + 10.0, p.Y + 20.0, p.Z)
        l3 = cad.AddLine(p.X + 25.0, p.Y - 10.0, p.Z, p.X + 25.0, p.Y + 20.0, p.Z)
        c1 = cad.AddCircle(p.X + 70.0, p.Y + 0.0, p.Z, 8.0)
        log("Setup ok, handles={0}/{1}/{2}/{3}".format(
            cad.GetEntityHandle(l1), cad.GetEntityHandle(l2), cad.GetEntityHandle(l3), cad.GetEntityHandle(c1)))
        return l1, l2, l3, c1

    ids = step_setup()
    l1, l2, l3, c1 = ids

    def step_select_and_stats():
        h = ArrayList([cad.GetEntityHandle(l1), cad.GetEntityHandle(c1), "BAD"])
        by_h = cad.SelectByHandles(h)
        by_dxf = cad.SelectByDxfInSpace(ArrayList([{"code": 0, "value": "LINE"}]), "*Model_Space")
        st = cad.GetSelectionStats(ArrayList(by_h))
        log("Select/stats ok, by_handle={0}, lines_in_space={1}, stats_count={2}".format(
            len(by_h), len(by_dxf), st["count"]))

    def step_copy_and_transform():
        disps = ArrayList()
        d1 = Hashtable(); d1["dx"] = 0.0; d1["dy"] = 30.0; d1["dz"] = 0.0; disps.Add(d1)
        d2 = Hashtable(); d2["dx"] = 20.0; d2["dy"] = 30.0; d2["dz"] = 0.0; disps.Add(d2)
        copied = cad.CopyEntitiesMultiple(ArrayList([l1, c1]), disps)

        jobs = ArrayList()
        j1 = Hashtable()
        j1["entity_id"] = l1
        j1["dx"] = 50.0; j1["dy"] = 0.0; j1["dz"] = 0.0
        j1["angle_degrees"] = 15.0
        j1["scale"] = 1.1
        j1["base_x"] = p.X; j1["base_y"] = p.Y; j1["base_z"] = p.Z
        jobs.Add(j1)
        tr = cad.CopyRotateScaleBatch(jobs)
        log("Copy/transform ok, copied={0}, batch_ok/fail={1}/{2}".format(len(copied), tr["ok"], tr["fail"]))

    def step_break_offset():
        break_jobs = ArrayList()
        b1 = Hashtable(); b1["entity_id"] = l1; b1["x"] = p.X + 20.0; b1["y"] = p.Y; b1["z"] = p.Z; break_jobs.Add(b1)
        b2 = Hashtable(); b2["entity_id"] = l2; b2["x"] = p.X + 10.0; b2["y"] = p.Y + 5.0; b2["z"] = p.Z; break_jobs.Add(b2)
        br = cad.BreakCurvesAtPointsBatch(break_jobs, False)

        off = cad.OffsetEntitiesBothSidesBatch(ArrayList([l3]), 2.5)
        log("Break/offset ok, break_ok/fail/created={0}/{1}/{2}, offset_ok/fail/created={3}/{4}/{5}".format(
            br["ok"], br["fail"], br["created"], off["ok"], off["fail"], off["created"]))

    safe("select_and_stats", step_select_and_stats)
    safe("copy_and_transform", step_copy_and_transform)
    safe("break_offset", step_break_offset)

    log("Marker=" + cad.GetBuildMarker())
    cad.RegenSafe()
    log("FIX26 selection/modify batch completato")
