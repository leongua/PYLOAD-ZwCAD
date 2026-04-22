import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")


def log(msg):
    cad.Msg("[COLLECTION TEST] " + msg)


log("Avvio test collections")

model_ids = cad.GetModelSpaceEntityIds()
paper_ids = cad.GetPaperSpaceEntityIds()
layouts = cad.GetLayoutNames()
textstyles = cad.GetTextStyleNames()
dimstyles = cad.GetDimensionStyleNames()
groups = cad.GetGroupNames()
dictionaries = cad.GetDictionaryNames()
regapps = cad.GetRegisteredApplicationNames()
ucss = cad.GetUcsNames()
views = cad.GetViewNames()
summary = cad.GetCollectionsSummary()

log("ModelSpace entities: " + str(len(model_ids)))
log("PaperSpace entities: " + str(len(paper_ids)))
log("Layouts: " + str(len(layouts)))
for name in layouts[:10]:
    log(" - layout: " + str(name))

log("TextStyles: " + str(len(textstyles)))
for name in textstyles[:10]:
    log(" - textstyle: " + str(name))

log("DimStyles: " + str(len(dimstyles)))
for name in dimstyles[:10]:
    log(" - dimstyle: " + str(name))

log("Groups: " + str(len(groups)))
log("Dictionaries: " + str(len(dictionaries)))
log("RegisteredApplications: " + str(len(regapps)))
log("UCSs: " + str(len(ucss)))
log("Views: " + str(len(views)))

for key in summary.Keys:
    log(" summary " + str(key) + " = " + str(summary[key]))

log("Test collections completato")
