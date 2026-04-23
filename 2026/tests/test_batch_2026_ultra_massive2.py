import clr

clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable
from System import DateTime
from System.IO import File, Path


def log(msg):
    cad.Msg("[ULTRA MASSIVE 2026-2] " + msg)


def safe(label, fn):
    try:
        fn()
    except Exception as ex:
        log(label + " -> ERRORE: " + str(ex))


def pair(code, value):
    h = Hashtable()
    h["code"] = code
    h["value"] = value
    return h


def to_typed_values(items):
    arr = ArrayList()
    for code, value in items:
        t = Hashtable()
        t["type_code"] = code
        t["value"] = value
        arr.Add(t)
    return arr


def find_block_refs_by_name(block_name):
    refs = ArrayList()
    all_insert = cad.GetSelectionByType("INSERT")
    for rid in all_insert:
        try:
            info = cad.GetBlockReferenceInfo(rid)
            if str(info["name"]).lower() == block_name.lower():
                refs.Add(rid)
        except:
            pass
    return refs


log("Avvio ULTRA MASSIVE 2026-2 (blocks+db+modify+report)")
res = cad.GetPoint("Punto base ultra massive 2026-2:")
if res.Status != PromptStatus.OK:
    log("Operazione annullata.")
else:
    p = res.Value
    base = r"C:\Users\user\Desktop\PYLOAD"
    summary_lines = ArrayList()
    summary_lines.Add("ULTRA MASSIVE 2026-2")
    summary_lines.Add("timestamp=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
    summary_lines.Add("build=" + str(cad.GetBuildMarker()))

    def step_command_channel():
        cad.CancelActiveCommand()
        cad.ClearShellTranscript()
        cad.Princ("[ULTRA MASSIVE 2026-2] command channel ok", True)
        cad.RunLisp('(progn (princ "\\n[ULTRA MASSIVE 2026-2 LISP] ok") (princ))')
        txt = cad.ExportShellTranscript(base + r"\ultra_massive2_transcript.txt")
        summary_lines.Add("transcript=" + txt)
        log("Command channel ok, transcript export=" + txt)

    def step_blocks_attributes():
        blk_a = cad.EnsureTestAttributedBlock("PYLOAD_UM2_A", "TAG1", "PromptA", "DEF_A", 2.5)
        blk_b = cad.EnsureTestAttributedBlock("PYLOAD_UM2_B", "TAG1", "PromptB", "DEF_B", 2.5)

        refs = ArrayList()
        for i in range(4):
            values = Hashtable()
            values["TAG1"] = "A_" + str(i + 1)
            r = cad.InsertBlockWithAttributes(blk_a, p.X + 20.0 + i * 18.0, p.Y + 20.0, p.Z, values)
            refs.Add(r)

        sync = cad.SyncBlockReferenceAttributesBatch(refs, False)
        up1 = cad.UpdateBlockAttributeByTagBatch(refs, "TAG1", "SYNCED")
        values_map = Hashtable()
        values_map["TAG1"] = "MAP_SET"
        up2 = cad.UpdateBlockAttributesByMapBatch(refs, values_map)

        ren = cad.RenameBlockAttributeTag(blk_a, "TAG1", "TAGX", True)
        up3 = cad.UpdateBlockAttributeByTagBatch(refs, "TAGX", "RENAMED")

        replaced = cad.ReplaceBlockReference(refs[0], blk_b, True, True)
        exploded = cad.ExplodeBlockReferenceEx(refs[1], False, True)
        attrs_rep = cad.GetBlockReferenceAttributes(replaced)

        refs_a = find_block_refs_by_name("PYLOAD_UM2_A")
        refs_b = find_block_refs_by_name("PYLOAD_UM2_B")

        summary_lines.Add("blocks_sync={0}".format(sync))
        summary_lines.Add("blocks_update_tag={0}".format(up1))
        summary_lines.Add("blocks_update_map={0}".format(up2))
        summary_lines.Add("blocks_rename_defs_refs={0}/{1}".format(ren["changed_definitions"], ren["changed_references"]))
        summary_lines.Add("blocks_update_renamed={0}".format(up3))
        summary_lines.Add("blocks_refs_A_B={0}/{1}".format(refs_a.Count, refs_b.Count))
        summary_lines.Add("blocks_exploded={0}".format(len(exploded)))
        summary_lines.Add("blocks_replaced_attr_count={0}".format(attrs_rep["count"]))
        log("Blocks/ATTRIB ok, sync={0}, upd={1}/{2}/{3}, refs A/B={4}/{5}, exploded={6}".format(sync, up1, up2, up3, refs_a.Count, refs_b.Count, len(exploded)))

    def step_database_deep():
        src = "PYLOAD/UM2/SRC"
        dst = "PYLOAD/UM2/DST"
        cad.CreateNamedDictionary(src)
        cad.CreateNamedDictionary(dst)

        smap = Hashtable()
        smap["PROJECT"] = "PYLOAD"
        smap["RUN"] = "UM2"
        cad.SetNamedStringMap(src, "META", smap)
        map_back = cad.GetNamedStringMap(src, "META")

        raw = to_typed_values([(1000, "alpha"), (1070, 2026), (1040, 3.14)])
        cad.SetNamedXRecord(src, "RAW", raw)
        raw_back = cad.GetNamedXRecord(src, "RAW")
        cad.CopyXRecordBetweenNamedDictionaries(src, "RAW", dst, "RAW_COPY", True)
        dst_back = cad.GetNamedXRecord(dst, "RAW_COPY")
        tree = cad.ListNamedDictionaryTree("PYLOAD/UM2", 4)

        line = cad.AddLine(p.X + 0.0, p.Y + 70.0, p.Z, p.X + 60.0, p.Y + 70.0, p.Z)
        cad.EnsureEntityExtensionDictionary(line)
        emap = Hashtable()
        emap["owner"] = "ultra_massive2"
        emap["state"] = "active"
        cad.SetEntityStringMap(line, "UM2/ENTITY", "MAP", emap)
        ent_map = cad.GetEntityStringMap(line, "UM2/ENTITY", "MAP")
        ext_before = cad.GetEntityExtensionDictionaryEntriesAtPath(line, "UM2/ENTITY")
        cad.DeleteEntityXRecord(line, "UM2/ENTITY", "MAP", True)
        ext_after = cad.GetEntityExtensionDictionaryEntriesAtPath(line, "UM2/ENTITY")

        cad.DeleteNamedXRecord(src, "RAW", True)
        contains_after = cad.NamedDictionaryContains(src, "RAW")

        summary_lines.Add("db_map_count={0}".format(map_back["count"]))
        summary_lines.Add("db_raw_src_dst={0}/{1}".format(raw_back["count"], dst_back["count"]))
        summary_lines.Add("db_tree={0}".format(tree.Count))
        summary_lines.Add("db_ent_map={0}".format(ent_map["count"]))
        summary_lines.Add("db_ext_before_after={0}/{1}".format(ext_before.Count, ext_after.Count))
        summary_lines.Add("db_src_contains_raw_after_delete={0}".format(contains_after))
        log("Database deep ok, map/raw/dst={0}/{1}/{2}, tree={3}, ent_ext={4}->{5}".format(map_back["count"], raw_back["count"], dst_back["count"], tree.Count, ext_before.Count, ext_after.Count))

    def step_modify_heavy():
        l1 = cad.AddLine(p.X + 0.0, p.Y + 110.0, p.Z, p.X + 100.0, p.Y + 110.0, p.Z)
        l2 = cad.AddLine(p.X + 20.0, p.Y + 80.0, p.Z, p.X + 20.0, p.Y + 140.0, p.Z)
        l3 = cad.AddLine(p.X + 80.0, p.Y + 80.0, p.Z, p.X + 80.0, p.Y + 140.0, p.Z)
        circle = cad.AddCircle(p.X + 50.0, p.Y + 110.0, p.Z, 24.0)

        curves = ArrayList([l1, l2, l3, circle])
        matrix = cad.BuildIntersectionsMatrix(curves, False, False)
        brk = cad.BreakCurvesAtAllIntersectionsBatch(curves, False)
        auto = cad.AutoTrimExtendByBoundaries(ArrayList([l2, l3]), ArrayList([circle]), "nearest", "nearest", False)

        jobs = ArrayList()
        j1 = Hashtable()
        j1["entity_id"] = l1
        j1["distance"] = 3.0
        j1["x"] = p.X + 50.0
        j1["y"] = p.Y + 130.0
        j1["z"] = p.Z
        jobs.Add(j1)
        off = cad.OffsetEntitiesTowardSeedsBatch(jobs)

        src_snap = cad.GetEntityPropertySnapshot(l1)
        targets = cad.GetSelectionByType("LINE")
        changed = cad.ApplyEntityPropertySnapshot(ArrayList(targets), src_snap)

        summary_lines.Add("modify_pairs_hits={0}/{1}".format(matrix["pairs"], matrix["intersections"]))
        summary_lines.Add("modify_break_parts={0}".format(brk["parts"]))
        summary_lines.Add("modify_trim_extend={0}/{1}".format(auto["trim_changed"], auto["extend_changed"]))
        summary_lines.Add("modify_offset_ok_fail={0}/{1}".format(off["ok"], off["fail"]))
        summary_lines.Add("modify_props_changed={0}".format(changed))
        log("Modify heavy ok, hits={0}, break={1}, trim/extend={2}/{3}, offset={4}/{5}, props={6}".format(matrix["intersections"], brk["parts"], auto["trim_changed"], auto["extend_changed"], off["ok"], off["fail"], changed))

    def step_exports_and_summary():
        all_ids = cad.GetModelSpaceEntityIds()
        csv_path = cad.ExportEntityAuditCsv(base + r"\ultra_massive2_entity_audit.csv", ArrayList(all_ids))
        db_path = cad.ExportDatabaseSnapshot(base + r"\ultra_massive2_db_snapshot.txt", "PYLOAD/UM2", 5)
        summary_path = base + r"\ultra_massive2_summary.txt"

        text = ""
        for line in summary_lines:
            text += str(line) + "\r\n"
        text += "entity_count=" + str(len(all_ids)) + "\r\n"
        text += "csv=" + csv_path + "\r\n"
        text += "db_snapshot=" + db_path + "\r\n"
        File.WriteAllText(summary_path, text)
        log("Export summary ok, csv/db/summary pronti")
        log("summary -> " + summary_path)

    safe("command_channel", step_command_channel)
    safe("blocks_attributes", step_blocks_attributes)
    safe("database_deep", step_database_deep)
    safe("modify_heavy", step_modify_heavy)
    safe("exports_summary", step_exports_and_summary)

    cad.RegenNative()
    log("ULTRA MASSIVE 2026-2 completato")
