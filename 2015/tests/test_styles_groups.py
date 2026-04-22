import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus


def log(msg):
    cad.Msg("[STYLE/GROUP TEST] " + msg)


log("Avvio test styles/groups")

log("Current TextStyle = " + str(cad.GetCurrentTextStyle()))
log("Current DimStyle = " + str(cad.GetCurrentDimensionStyle()))
log("Current Layout = " + str(cad.GetCurrentLayoutName()))

text_style_name = "PYLOAD_TXT_STYLE"
dim_style_name = "PYLOAD_DIM_STYLE"
group_name = "PYLOAD_GROUP_TMP"

cad.CreateTextStyle(text_style_name, "", 0.0, 1.0, 0.0)
cad.CreateDimensionStyle(dim_style_name)
log("Creati/garantiti TextStyle e DimStyle")

txt_info = cad.GetTextStyleInfo(text_style_name)
dim_info = cad.GetDimensionStyleInfo(dim_style_name)
log("TextStyle info -> x_scale=" + str(txt_info["x_scale"]))
log("DimStyle info -> dimtxt=" + str(dim_info["dimtxt"]))

cad.SetCurrentTextStyle(text_style_name)
cad.SetCurrentDimensionStyle(dim_style_name)
log("Current TextStyle dopo set = " + str(cad.GetCurrentTextStyle()))
log("Current DimStyle dopo set = " + str(cad.GetCurrentDimensionStyle()))

layouts = cad.GetLayoutNames()
if len(layouts) > 0:
    first_layout = str(layouts[0])
    layout_info = cad.GetLayoutInfo(first_layout)
    log("Layout info -> " + str(layout_info["name"]) + " model=" + str(layout_info["is_model"]))

res = cad.GetPoint("Punto base styles/groups test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    line_id = cad.AddLine(p.X, p.Y, p.Z, p.X + 50.0, p.Y, p.Z)
    circle_id = cad.AddCircle(p.X + 20.0, p.Y + 20.0, p.Z, 10.0)
    log("Entita create")

    if cad.GroupExists(group_name):
        cad.DeleteGroup(group_name)

    cad.CreateGroup(group_name, "Gruppo temporaneo PYLOAD")
    cad.AddEntitiesToGroup(group_name, [line_id, circle_id])
    group_info = cad.GetGroupInfo(group_name)
    group_ids = cad.GetGroupEntityIds(group_name)
    log("Group info -> count=" + str(group_info["count"]))
    log("Group entity ids -> " + str(len(group_ids)))

    cad.DeleteGroup(group_name)
    log("Group eliminato")

    cad.Regen()
    log("Test styles/groups completato")
