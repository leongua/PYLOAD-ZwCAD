import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList


def log(msg):
    cad.Msg("[FIX24 CMD PIPE] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio FIX24 command pipeline hardening")
res = cad.GetPoint("Punto base FIX24:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    def step_state_before():
        st = cad.GetCommandChannelState()
        log("State before, cmdactive={0}, transcript={1}".format(st["cmdactive"], st["transcript_lines"]))

    def step_safe_commands():
        cad.ClearShellTranscript()
        cad.ZoomExtentsSafe()
        cad.ZoomPreviousSafe()
        cad.ZoomCenterSafe(p.X, p.Y, p.Z, 80.0)
        cad.ZoomWindowSafe(p.X - 40.0, p.Y - 20.0, p.Z, p.X + 40.0, p.Y + 20.0, p.Z)
        cad.RegenSafe()
        st = cad.GetCommandChannelState()
        log("Safe commands ok, cmdactive={0}, transcript={1}".format(st["cmdactive"], st["transcript_lines"]))

    def step_macro():
        macro = ArrayList([
            "_.ZOOM _E",
            "_.ZOOM _P",
            "_.REGEN",
        ])
        rep = cad.RunCommandMacro(macro, True, True, 1)
        log("Macro ok, sent={0}, cmdactive={1}".format(rep["sent"], rep["cmdactive"]))

    def step_lisp():
        l = ArrayList([
            '(princ "\\n[FIX24 LISP1] ok")',
            '(princ "\\n[FIX24 LISP2] ok")',
        ])
        cad.RunLispMacro(l, True, False)
        cad.PrincQuiet("[FIX24 PRINC] ok")
        st = cad.GetCommandChannelState()
        log("Lisp macro ok, cmdactive={0}, last='{1}'".format(st["cmdactive"], st["last_line"]))

    def step_validate():
        smoke = ArrayList([
            "_.REGEN",
            "_.ZOOM _E",
        ])
        rep = cad.ValidateCommandPipeline(smoke)
        log("Validate ok={0}, cmdactive before/after={1}/{2}".format(
            rep["ok"], rep["cmdactive_before"], rep["cmdactive_after"]))

    safe("state_before", step_state_before)
    safe("safe_commands", step_safe_commands)
    safe("macro", step_macro)
    safe("lisp", step_lisp)
    safe("validate", step_validate)

    cad.FlushCommandChannelHard()
    log("Marker=" + cad.GetBuildMarker())
    log("FIX24 command pipeline completato")
