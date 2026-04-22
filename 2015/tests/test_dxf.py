import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[DXF TEST] " + msg)


log("Avvio test DXF/XData")

res = cad.GetPoint("Punto base DXF test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    circle_id = cad.AddCircle(p.X, p.Y, p.Z, 12.0)
    log("Cerchio creato. Handle=" + cad.GetEntityHandle(circle_id))

    dxf_name = cad.GetDxfName(circle_id)
    log("DXF name = " + str(dxf_name))

    items = ArrayList()

    item1 = Hashtable()
    item1["type_code"] = 1000
    item1["value"] = "PYLOAD"
    items.Add(item1)

    item2 = Hashtable()
    item2["type_code"] = 1040
    item2["value"] = 42.5
    items.Add(item2)

    cad.SetXData(circle_id, "PYLOAD_APP", items)
    log("XData scritta")

    apps = cad.ListXDataApps(circle_id)
    log("XData apps = " + str(len(apps)))
    for app in apps:
        log(" - " + str(app))

    xdata = cad.GetXData(circle_id, "PYLOAD_APP")
    log("GetXData count = " + str(xdata["count"]))

    summary = cad.GetEntityRawSummary(circle_id)
    log("Raw summary dxf_name = " + str(summary["dxf_name"]))

    cad.ClearXData(circle_id, "PYLOAD_APP")
    log("XData rimossa")

    apps_after = cad.ListXDataApps(circle_id)
    log("XData apps dopo clear = " + str(len(apps_after)))
    xdata_after = cad.GetXData(circle_id, "PYLOAD_APP")
    log("GetXData count dopo clear = " + str(xdata_after["count"]))

    cad.Regen()
    log("Test DXF/XData completato")
