import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList


def log(msg):
    cad.Msg("[ULTRA MASSIVE 2026-3] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio ULTRA MASSIVE 2026-3 (cs deterministic + api report + stress)")
res = cad.GetPoint("Punto base ultra massive 2026-3:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    base = r"C:\Users\user\Desktop\PYLOAD"

    def step_marker_and_api():
        marker = cad.GetBuildMarker()
        names_all = cad.GetPublicApiMethodNames("")
        names_mod = cad.GetPublicApiMethodNames("Modify")
        api_path = cad.ExportApiMethodsReport(base + r"\ultra_massive3_api_methods.txt", "")
        log("Build/API ok, marker={0}, methods={1}, modify_like={2}".format(marker, len(names_all), len(names_mod)))
        log("API report -> " + api_path)

    def step_deterministic_modify():
        info = cad.RunDeterministicModifyPack(p.X, p.Y, p.Z)
        log("Deterministic modify ok, break_parts={0}, trim_len={1}, extend_x={2}, poly_x={3}, pairs/hits={4}/{5}".format(
            info["break_parts"], info["trim_len"], info["extend_x"], info["extend_poly_x"], info["matrix_pairs"], info["matrix_hits"]))

    def step_mini_stress():
        ok = 0
        fail = 0
        for i in range(5):
            try:
                x = p.X + 600.0 + i * 260.0
                y = p.Y
                info = cad.RunDeterministicModifyPack(x, y, p.Z)
                if int(info["break_parts"]) >= 3:
                    ok += 1
                else:
                    fail += 1
            except:
                fail += 1
        log("Mini stress ok/fail = {0}/{1}".format(ok, fail))

    def step_exports():
        ids = cad.GetModelSpaceEntityIds()
        csv = cad.ExportEntityAuditCsv(base + r"\ultra_massive3_entity_audit.csv", ArrayList(ids))
        snap = cad.ExportDatabaseSnapshot(base + r"\ultra_massive3_db_snapshot.txt", "PYLOAD", 3)
        log("Export ok, csv={0}, snap={1}".format(csv, snap))

    safe("marker_api", step_marker_and_api)
    safe("deterministic_modify", step_deterministic_modify)
    safe("mini_stress", step_mini_stress)
    safe("exports", step_exports)

    cad.RegenNative()
    log("ULTRA MASSIVE 2026-3 completato")
