import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable


def log(msg):
    cad.Msg("[DB ADV TEST] " + msg)


def tv(code, value):
    h = Hashtable()
    h["type_code"] = code
    h["value"] = value
    return h


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


log("Avvio test database avanzato")

res = cad.GetPoint("Punto base database advanced test:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value

    state = {}

    def step_nod():
        state["nod"] = cad.GetNamedObjectsDictionaryId()
        log("NOD id = " + str(state["nod"]))

    def step_dicts():
        state["dict_id"] = cad.CreateNamedDictionary("PYLOAD_DB_ADV/META/SESSION1")
        info = cad.GetNamedDictionaryInfo("PYLOAD_DB_ADV/META/SESSION1")
        log("CreateNamedDictionary count = " + str(info["count"]))

        typed = ArrayList()
        typed.Add(tv(1000, "hello"))
        typed.Add(tv(1070, 42))
        cad.SetNamedXRecord("PYLOAD_DB_ADV/META/SESSION1", "REC1", typed)
        rec = cad.GetNamedXRecord("PYLOAD_DB_ADV/META/SESSION1", "REC1")
        log("GetNamedXRecord count = " + str(rec["count"]))

        smap = Hashtable()
        smap["author"] = "pyload"
        smap["scope"] = "database"
        cad.SetNamedStringMap("PYLOAD_DB_ADV/META/SESSION1", "MAP1", smap)
        map_info = cad.GetNamedStringMap("PYLOAD_DB_ADV/META/SESSION1", "MAP1")
        log("GetNamedStringMap count = " + str(map_info["count"]))

        entries = cad.GetNamedDictionaryEntries("PYLOAD_DB_ADV/META/SESSION1")
        log("GetDictionaryEntries count = " + str(len(entries)))
        log("NamedDictionaryContains REC1 = " + str(cad.NamedDictionaryContains("PYLOAD_DB_ADV/META/SESSION1", "REC1")))

        tree = cad.ListNamedDictionaryTree("PYLOAD_DB_ADV", 3)
        log("ListNamedDictionaryTree count = " + str(len(tree)))

    def step_entity_ext():
        state["line_id"] = cad.AddLine(p.X, p.Y, p.Z, p.X + 40.0, p.Y, p.Z)
        ext_id = cad.EnsureEntityExtensionDictionary(state["line_id"])
        log("EnsureEntityExtensionDictionary id = " + str(ext_id))

        extmap = Hashtable()
        extmap["kind"] = "line"
        extmap["owner"] = "test_database_advanced"
        cad.SetEntityStringMap(state["line_id"], "PYLOAD_META/LOCAL", "INFO", extmap)
        got_ext = cad.GetEntityStringMap(state["line_id"], "PYLOAD_META/LOCAL", "INFO")
        log("GetEntityStringMap count = " + str(got_ext["count"]))

        ext_entries = cad.GetEntityExtensionDictionaryEntries(state["line_id"])
        log("GetEntityExtensionDictionaryEntries count = " + str(len(ext_entries)))
        ext_entries2 = cad.GetEntityExtensionDictionaryEntriesAtPath(state["line_id"], "PYLOAD_META")
        log("GetEntityExtensionDictionaryEntriesAtPath count = " + str(len(ext_entries2)))

    def step_clone_copy():
        ms_id = cad.GetModelSpaceRecordId()
        clone_ids = cad.CloneObjectsToOwner(ArrayList([state["line_id"]]), ms_id)
        log("CloneObjectsToOwner count = " + str(len(clone_ids)))

        state["target_dict"] = cad.CreateNamedDictionary("PYLOAD_DB_ADV/META/SESSION2")
        cad.CopyXRecordBetweenDictionaries(state["dict_id"], "REC1", state["target_dict"], "REC1_COPY", True)
        copied = cad.GetXRecordData(state["target_dict"], "REC1_COPY")
        log("CopyXRecordBetweenDictionaries count = " + str(copied["count"]))

        cad.CopyXRecordBetweenNamedDictionaries("PYLOAD_DB_ADV/META/SESSION1", "MAP1", "PYLOAD_DB_ADV/META/SESSION3", "MAP1_COPY", True)
        copied2 = cad.GetNamedXRecord("PYLOAD_DB_ADV/META/SESSION3", "MAP1_COPY")
        log("CopyXRecordBetweenNamedDictionaries count = " + str(copied2["count"]))

    def step_delete():
        cad.DeleteEntityXRecord(state["line_id"], "PYLOAD_META/LOCAL", "INFO", True)
        after_del = cad.GetEntityXRecord(state["line_id"], "PYLOAD_META/LOCAL", "INFO")
        log("DeleteEntityXRecord count = " + str(after_del["count"]))

        cad.DeleteDictionaryEntry(state["target_dict"], "REC1_COPY", True)
        log("DeleteDictionaryEntry contains = " + str(cad.DictionaryContains(state["target_dict"], "REC1_COPY")))

        cad.DeleteNamedXRecord("PYLOAD_DB_ADV/META/SESSION3", "MAP1_COPY", True)
        log("DeleteNamedXRecord contains = " + str(cad.NamedDictionaryContains("PYLOAD_DB_ADV/META/SESSION3", "MAP1_COPY")))

    safe("step_nod", step_nod)
    safe("step_dicts", step_dicts)
    safe("step_entity_ext", step_entity_ext)
    safe("step_clone_copy", step_clone_copy)
    safe("step_delete", step_delete)

    log("Test database avanzato completato")
