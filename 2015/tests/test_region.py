import clr
clr.AddReference("ZwManaged")

from ZwSoft.ZwCAD.EditorInput import PromptStatus

ed.WriteMessage("\n[REGION TEST] Avvio test region")
res = cad.GetPoint("Punto base region test:")
if res.Status != PromptStatus.OK:
    ed.WriteMessage("\n[REGION TEST] Annullato")
else:
    p = res.Value

    rect1 = cad.DrawRectangle(p.X, p.Y, p.X + 40.0, p.Y + 25.0)
    rect2 = cad.DrawRectangle(p.X + 20.0, p.Y + 10.0, p.X + 60.0, p.Y + 35.0)
    ed.WriteMessage("\n[REGION TEST] Contorni creati")

    region1 = cad.CreateRegionFromEntity(rect1)
    region2 = cad.CreateRegionFromEntity(rect2)
    ed.WriteMessage("\n[REGION TEST] Region create")

    info1 = cad.GetRegionInfo(region1)
    info2 = cad.GetRegionInfo(region2)
    ed.WriteMessage("\n[REGION TEST] Region1 area = %s perimeter = %s" % (info1["area"], info1["perimeter"]))
    ed.WriteMessage("\n[REGION TEST] Region2 area = %s perimeter = %s" % (info2["area"], info2["perimeter"]))

    cad.BooleanRegions(region1, region2, "union")
    union_info = cad.GetRegionInfo(region1)
    ed.WriteMessage("\n[REGION TEST] Union area = %s" % union_info["area"])

    exploded = cad.ExplodeRegion(region1)
    ed.WriteMessage("\n[REGION TEST] ExplodeRegion -> %s entita" % len(exploded))
    ed.WriteMessage("\n[REGION TEST] Test region completato")
