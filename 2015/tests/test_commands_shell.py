import clr
clr.AddReference("ZwManaged")

from ZwSoft.ZwCAD.EditorInput import PromptStatus
from System.Collections import ArrayList, Hashtable

ed.WriteMessage("\n[COMMAND TEST] Avvio test comandi/shell")
res = cad.GetPoint("Punto base command test:")
if res.Status != PromptStatus.OK:
    ed.WriteMessage("\n[COMMAND TEST] Annullato")
else:
    p = res.Value

    line_id = cad.AddLine(p.X - 25.0, p.Y, p.Z, p.X + 25.0, p.Y, p.Z)
    ed.WriteMessage("\n[COMMAND TEST] Entita base creata, handle=%s" % cad.GetEntityHandle(line_id))

    cad.ZoomWindow(p.X - 40.0, p.Y - 20.0, p.Z, p.X + 40.0, p.Y + 20.0, p.Z)
    ed.WriteMessage("\n[COMMAND TEST] ZoomWindow inviato")

    cad.ZoomCenter(p.X, p.Y, p.Z, 80.0)
    ed.WriteMessage("\n[COMMAND TEST] ZoomCenter inviato")

    cad.ZoomPrevious()
    ed.WriteMessage("\n[COMMAND TEST] ZoomPrevious inviato")

    cad.RegenNative()
    ed.WriteMessage("\n[COMMAND TEST] RegenNative inviato")

    cad.RunCommands(["_REGEN", "_ZOOM _E"])
    ed.WriteMessage("\n[COMMAND TEST] RunCommands eseguito")

    cad.Command(["_.ZOOM", "_P"])
    ed.WriteMessage("\n[COMMAND TEST] Command([...]) eseguito")

    cad.RunLisp('(princ "\\n[PYLOAD LISP] ok")')
    ed.WriteMessage("\n[COMMAND TEST] RunLisp eseguito")

    cvport = cad.GetVar("CVPORT")
    ed.WriteMessage("\n[COMMAND TEST] GetVar(CVPORT) = %s" % cvport)
    cvport_i = cad.GetVarInt("CVPORT")
    ed.WriteMessage("\n[COMMAND TEST] GetVarInt(CVPORT) = %s" % cvport_i)

    sysvars = ArrayList()
    sysvars.Add("CVPORT")
    sysvars.Add("CMDECHO")
    sysvars_map = cad.GetVars(sysvars)
    ed.WriteMessage("\n[COMMAND TEST] GetVars count = %s" % len(sysvars_map))

    args = ArrayList()
    args.Add("\n[PYLOAD CALLLISP] ok")
    cad.CallLisp("princ", args)
    ed.WriteMessage("\n[COMMAND TEST] CallLisp eseguito")

    setvars = Hashtable()
    setvars["CMDECHO"] = 0
    cad.SetVars(setvars)
    ed.WriteMessage("\n[COMMAND TEST] SetVars eseguito")

    cad.Princ("[PYLOAD PRINC] ok", True)
    ed.WriteMessage("\n[COMMAND TEST] Princ eseguito")

    transcript = cad.GetShellTranscript()
    ed.WriteMessage("\n[COMMAND TEST] Shell transcript righe = %s" % len(transcript))
    ed.WriteMessage("\n[COMMAND TEST] Last shell line = %s" % cad.GetLastShellLine())

    out_path = cad.ExportShellTranscript(script_dir + "\\command_transcript.txt")
    ed.WriteMessage("\n[COMMAND TEST] ExportShellTranscript = %s" % out_path)

    ed.WriteMessage("\n[COMMAND TEST] Test comandi/shell completato")
