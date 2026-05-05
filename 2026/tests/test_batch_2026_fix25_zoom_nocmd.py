import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[FIX25 ZOOM NOCMD] " + msg)


log("Avvio test FIX25 zoom non-interattivo")
res = cad.GetPoint("Punto base FIX25:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    cad.ClearShellTranscript()

    line = cad.AddLine(p.X - 30.0, p.Y - 20.0, p.Z, p.X + 50.0, p.Y + 40.0, p.Z)
    _ = line

    ok_center = cad.TrySetViewCenterNoCmd(p.X, p.Y, 120.0)
    ok_window = cad.TrySetViewWindowNoCmd(p.X - 40.0, p.Y - 25.0, p.X + 60.0, p.Y + 35.0)
    ok_ext = cad.TryZoomExtentsNoCmd()
    st = cad.GetCommandChannelState()

    log("NoCmd center/window/extents = {0}/{1}/{2}".format(ok_center, ok_window, ok_ext))
    log("State cmdactive={0}, transcript={1}".format(st["cmdactive"], st["transcript_lines"]))
    log("Marker=" + cad.GetBuildMarker())
    log("Test FIX25 completato")
