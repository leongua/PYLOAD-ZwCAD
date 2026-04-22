import clr
import math

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[ARC TEST] " + msg)


log("Avvio test archi")

res = cad.GetPoint("Centro arco:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    cad.EnsureLayer("PYLOAD_ARCS", 1)
    cad.SetCurrentLayer("PYLOAD_ARCS")

    arc_id = cad.AddArc(p.X, p.Y, p.Z, 25.0, 20.0, 210.0)
    log("Arco creato")

    info = cad.GetArcInfo(arc_id)
    log("Handle = " + str(info["handle"]))
    log("Radius = " + str(info["radius"]))
    log("StartAngle = " + str(info["start_angle"]))
    log("EndAngle = " + str(info["end_angle"]))
    log("TotalAngle = " + str(info["total_angle"]))
    log("ArcLength = " + str(info["arc_length"]))
    log("Area = " + str(info["area"]))

    sp = cad.GetArcStartPoint(arc_id)
    ep = cad.GetArcEndPoint(arc_id)
    log("StartPoint = (" + str(sp.X) + ", " + str(sp.Y) + ", " + str(sp.Z) + ")")
    log("EndPoint = (" + str(ep.X) + ", " + str(ep.Y) + ", " + str(ep.Z) + ")")

    cad.SetArcRadius(arc_id, 32.0)
    cad.SetArcAngles(arc_id, math.radians(30.0), math.radians(240.0))
    cad.SetArcThickness(arc_id, 2.0)
    log("Raggio, angoli e thickness modificati")

    offsets = cad.OffsetArc(arc_id, 6.0)
    log("OffsetArc ha creato: " + str(len(offsets)) + " entita")

    info2 = cad.GetArcInfo(arc_id)
    log("Nuovo Radius = " + str(info2["radius"]))
    log("Nuovo ArcLength = " + str(info2["arc_length"]))

    cad.Regen()
    cad.ZoomExtents()
    log("Test archi completato")
