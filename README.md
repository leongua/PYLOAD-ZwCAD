# PYLOAD-ZwCAD

<div align="center">

[![ZWCAD 2015](https://img.shields.io/badge/ZWCAD-2015-blue)](#2015-legacy-bridge)
[![ZWCAD 2026](https://img.shields.io/badge/ZWCAD-2026-0a7f5a)](#2026-rewrite-bridge)
[![IronPython](https://img.shields.io/badge/IronPython-3.4.1-6a5acd)](https://ironpython.net/)
[![.NET](https://img.shields.io/badge/.NET-4.6.2%20%7C%204.7-purple)](https://dotnet.microsoft.com/)

Bridge C# + IronPython per eseguire script Python dentro ZWCAD, con due varianti separate:
**una per ZWCAD+ 2015 x86** e **una per ZWCAD 2026 x64**.

</div>

---

## Perché questo repository

Questo repository raccoglie due linee di lavoro che abbiamo mantenuto volutamente separate:

- `2015/` contiene il bridge storico, molto esteso, pensato per **ZWCAD+ 2015 x86**
- `2026/` contiene il bridge **riscritto da zero** per **ZWCAD 2026 x64**

L'obiettivo è evitare di mischiare ambienti e runtime molto diversi nello stesso progetto, mantenendo
una separazione chiara tra il target 2015 e il target 2026.

---

## Struttura

```text
PYLOAD-ZwCAD/
├─ 2015/
│  ├─ docs/
│  ├─ src/
│  ├─ tests/
│  ├─ build.ps1
│  └─ PYLOAD.csproj
├─ 2026/
│  ├─ docs/
│  ├─ src/
│  ├─ tests/
│  ├─ build.ps1
│  └─ PYLOAD.Rewrite2026.csproj
└─ README.md
```

---

## Confronto rapido

| Cartella | Target | Architettura | Profilo | Note |
|---|---|---:|---|---|
| `2015/` | ZWCAD+ 2015 | x86 | dedicato | bridge separato per runtime 32 bit |
| `2026/` | ZWCAD 2026 | x64 | dedicato | bridge separato per runtime 64 bit |

---

## 2015 Bridge

### Cosa contiene

La cartella [`2015/`](./2015) contiene il bridge storico completo per ZWCAD+ 2015:

- loader `PYLOAD`
- API helper molto ampia per entità, layer, blocchi, DXF-like, modify, shell/LISP e database
- molti smoke test e test settoriali
- documentazione tecnica storica

### Build

Requisiti principali:

- **ZWCAD+ 2015 x86**
- **.NET Framework 4.6.2**
- DLL attese:
  - `C:\Program Files (x86)\ZWCAD+ 2015\ZwManaged.dll`
  - `C:\Program Files (x86)\ZWCAD+ 2015\ZwDatabaseMgd.dll`

Build rapida:

```powershell
cd 2015
.\build.ps1
```

Oppure:

```powershell
dotnet build .\2015\PYLOAD.csproj -c Release
```

### Entrypoint

- comando ZWCAD: `PYLOAD`

### Riferimenti

- doc principale: [`2015/docs/README.md`](./2015/docs/README.md)
- reference API: [`2015/docs/API_REFERENCE_2015.md`](./2015/docs/API_REFERENCE_2015.md)
- note operative: [`2015/docs/TODO.md`](./2015/docs/TODO.md)

---

## 2026 Bridge

### Cosa contiene

La cartella [`2026/`](./2026) contiene la riscrittura dedicata a ZWCAD 2026:

- loader `PYLOAD2026R`
- bridge riprogettato per la documentazione 2026
- moduli separati per `Core`, `Geometry`, `Dxf`, `Blocks`, `Modify`, `Database`
- smoke test master unico per validazione rapida

### Verifica

Il bridge 2026 ha gia passato il master smoke su:

- commands / editor / transcript
- geometry
- DXF-like API
- blocks / attributes
- modify
- database advanced

### Build

Requisiti principali:

- **ZWCAD 2026 x64**
- **.NET Framework 4.7**
- DLL attese:
  - `C:\Program Files\ZWSOFT\ZWCAD 2026\ZwManaged.dll`
  - `C:\Program Files\ZWSOFT\ZWCAD 2026\ZwDatabaseMgd.dll`

Build rapida:

```powershell
cd 2026
.\build.ps1
```

Oppure:

```powershell
dotnet build .\2026\PYLOAD.Rewrite2026.csproj -c Release
```

### Entrypoint

- comando ZWCAD: `PYLOAD2026R`

### Riferimenti

- guida 2026: [`2026/docs/PYLOAD_2026.md`](./2026/docs/PYLOAD_2026.md)
- reference API: [`2026/docs/API_REFERENCE_2026.md`](./2026/docs/API_REFERENCE_2026.md)
- smoke test master: [`2026/tests/test_master_2026_fix4.py`](./2026/tests/test_master_2026_fix4.py)

---

## Filosofia del progetto

Invece di forzare un'unica codebase a coprire tutto, questo repository mantiene due codebase distinte,
ognuna allineata alla propria versione di ZWCAD.

Questo rende il repository piu semplice da mantenere e piu onesto rispetto alle differenze reali tra
le API delle due versioni.

---
# PYLOAD (ZWCAD + IronPython)

Bridge C# per eseguire script Python dentro ZWCAD 2015.

## Documentazione API

- indice API 2015: [`API_REFERENCE_2015.md`](./API_REFERENCE_2015.md)

## Scopo

`PYLOAD` carica un file `.py` scelto dall'utente ed esegue lo script nel contesto del disegno attivo, esponendo oggetti ZWCAD utili allo scripting.

Questo repository contiene anche file di test (`a.py`, `cozy_profile.js`) usati durante sviluppo/prototipazione.  
Non sono parte obbligatoria del runtime di `PYLOAD`.

## Requisiti

- ZWCAD 2015 (x86)
- .NET Framework 4.6.2
- DLL ZWCAD installate in:
  - `C:\Program Files (x86)\ZWCAD+ 2015\ZwManaged.dll`
  - `C:\Program Files (x86)\ZWCAD+ 2015\ZwDatabaseMgd.dll`

## Build

Progetto: `net462`, `x86`.

Comando:

```powershell
dotnet build PYLOAD.csproj -c Release
```

Output principale:

`bin\Release\net462\PYLOAD.dll`

Runtime consigliato da caricare in ZWCAD:

`dist\PYLOAD-runtime\PYLOAD.dll`

La cartella `dist\PYLOAD-runtime` viene preparata automaticamente dalla build e include:

- `PYLOAD.dll`
- dipendenze .NET/IronPython
- moduli Python standard necessari (`os.py`, ecc.)
- script di test `test*.py`
- documentazione principale

## Installazione in ZWCAD

1. Apri ZWCAD 2015.
2. Carica `PYLOAD.dll` (tipicamente con `NETLOAD` o comando equivalente della tua installazione).
3. Esegui comando: `PYLOAD`.
4. Seleziona lo script Python da eseguire.

## Uso comando (con o senza argomento)

`PYLOAD` supporta due modalita:

- Senza argomento: premi `Invio` e si apre il file dialog.
- Con argomento: passa subito il percorso `.py` (utile in macro/script).

Esempi:

- `PYLOAD` poi `Invio` (apre dialog)
- `PYLOAD` poi `C:\scripts\hello_zwcad.py`
- `PYLOAD` poi `"C:\mia cartella\hello_zwcad.py"` (con spazi)

## Contratto script Python

Durante l'esecuzione, `PYLOAD` rende disponibili nello scope Python:

- `doc`: documento attivo
- `db`: database del documento attivo
- `ed`: editor/command line del documento attivo
- `cad`: helper C# per operazioni frequenti
- `script_path`: percorso completo dello script in esecuzione
- `script_dir`: cartella dello script in esecuzione

Lo script deve gestire in autonomia la transazione:

```python
with db.TransactionManager.StartTransaction() as tr:
    # scrittura entita
    tr.Commit()
```

## Esempio minimo (`hello_zwcad.py`)

```python
import clr
clr.AddReference("ZwManaged")
clr.AddReference("ZwDatabaseMgd")

from ZwSoft.ZwCAD.DatabaseServices import *
from ZwSoft.ZwCAD.Geometry import *

ed.WriteMessage("\n[PYLOAD] Script avviato.")

with db.TransactionManager.StartTransaction() as tr:
    bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead)
    btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite)

    p1 = Point3d(0, 0, 0)
    p2 = Point3d(100, 100, 0)
    ln = Line(p1, p2)
    btr.AppendEntity(ln)
    tr.AddNewlyCreatedDBObject(ln, True)
    tr.Commit()

ed.Regen()
ed.WriteMessage("\n[PYLOAD] OK.")
```

## Helper `cad`

Metodi disponibili nel bridge attuale:

- `cad.Msg("testo")`
- `cad.Regen()`
- `cad.RunCommand("testo comando")`
- `cad.RunCommand("testo comando", activate, wrapUpInactiveDoc, echoCommand)`
- `cad.RunCommands(["_REGEN", "_ZOOM _E"])`
- `cad.Command(["_.ZOOM", "_E"])`
- `cad.RunLisp('(princ "\\nhello")')`
- `cad.LoadLispFile("C:\\temp\\file.lsp")`
- `cad.RunLispFile("C:\\temp\\file.lsp")`
- `cad.CallLisp("princ", ["ciao"])`
- `cad.Princ("ciao")`
- `cad.Princ("ciao", True)`
- `cad.GetVar("CVPORT")`
- `cad.GetVarString("LOGINNAME")`
- `cad.GetVarInt("CVPORT")`
- `cad.GetVarDouble("VIEWSIZE")`
- `cad.GetVarBool("TILEMODE")`
- `cad.GetVars(["CVPORT", "CMDECHO"])`
- `cad.SetVar("CMDECHO", 0)`
- `cad.SetVars({"CMDECHO": 0})`
- `cad.GetShellTranscript()`
- `cad.GetShellTranscriptText()`
- `cad.GetLastShellLine()`
- `cad.ClearShellTranscript()`
- `cad.ExportShellTranscript("C:\\temp\\shell.txt")`
- `cad.SendEnter()`
- `cad.CancelActiveCommand()`
- `cad.RegenNative()`
- `cad.ZoomAll()`
- `cad.ZoomExtents()`
- `cad.ZoomPrevious()`
- `cad.ZoomWindow(x1, y1, z1, x2, y2, z2)`
- `cad.ZoomCenter(x, y, z, height)`
- `cad.Audit(False)`
- `cad.AuditInteractive()`
- `cad.SaveCommand()`
- `cad.OpenScript("C:\\temp\\cmd.scr")`
- `cad.RunScriptFile("C:\\temp\\cmd.scr")`
- `cad.GetPoint("messaggio")`
- `cad.GetString("messaggio")`
- `cad.GetDouble("messaggio")`
- `cad.GetInteger("messaggio")`
- `cad.GetKeyword("messaggio", "Si No")`
- `cad.AddLine(x1, y1, z1, x2, y2, z2)`
- `cad.AddCircle(x, y, z, radius)`
- `cad.AddArc(x, y, z, radius, startAngleDeg, endAngleDeg)`
- `cad.GetCircleInfo(objectId)`
- `cad.GetCircleDiameter(objectId)`
- `cad.GetCircleCircumference(objectId)`
- `cad.GetCircleStartPoint(objectId)`
- `cad.GetCircleEndPoint(objectId)`
- `cad.GetCirclePointAtAngleDegrees(objectId, angleDeg)`
- `cad.SetCircleRadius(objectId, radius)`
- `cad.SetCircleDiameter(objectId, diameter)`
- `cad.SetCircleCenter(objectId, x, y, z)`
- `cad.SetCircleThickness(objectId, thickness)`
- `cad.SetCircleNormal(objectId, x, y, z)`
- `cad.OffsetCircle(objectId, distance)`
- `cad.GetLineInfo(objectId)`
- `cad.GetLineMidPoint(objectId)`
- `cad.GetLineStartPoint(objectId)`
- `cad.GetLineEndPoint(objectId)`
- `cad.GetLineAngleDegrees(objectId)`
- `cad.SetLineStartPoint(objectId, x, y, z)`
- `cad.SetLineEndPoint(objectId, x, y, z)`
- `cad.SetLineThickness(objectId, thickness)`
- `cad.SetLineNormal(objectId, x, y, z)`
- `cad.OffsetLine(objectId, distance)`
- `cad.GetArcInfo(objectId)`
- `cad.GetArcLength(objectId)`
- `cad.GetArcArea(objectId)`
- `cad.GetArcStartPoint(objectId)`
- `cad.GetArcEndPoint(objectId)`
- `cad.GetArcMidPoint(objectId)`
- `cad.GetArcTotalAngle(objectId)`
- `cad.GetArcTotalAngleDegrees(objectId)`
- `cad.SetArcRadius(objectId, radius)`
- `cad.SetArcAngles(objectId, startAngleRad, endAngleRad)`
- `cad.SetArcAnglesDegrees(objectId, startAngleDeg, endAngleDeg)`
- `cad.SetArcCenter(objectId, x, y, z)`
- `cad.SetArcThickness(objectId, thickness)`
- `cad.SetArcNormal(objectId, x, y, z)`
- `cad.OffsetArc(objectId, distance)`
- `cad.AddPoint(x, y, z)`
- `cad.Add3DFace(x1, y1, z1, x2, y2, z2, x3, y3, z3, x4, y4, z4)`
- `cad.Add3DPoly([x1, y1, z1, x2, y2, z2, ...], closed)`
- `cad.AddPolyfaceMesh(vertexCoordinates, faceIndices)`
- `cad.AddTrace(x1, y1, z1, x2, y2, z2, x3, y3, z3, x4, y4, z4)`
- `cad.AddSolid(x1, y1, z1, x2, y2, z2, x3, y3, z3, x4, y4, z4)`
- `cad.AddRay(x1, y1, z1, x2, y2, z2)`
- `cad.AddLightWeightPolyline([x1, y1, x2, y2, ...], closed)`
- `cad.AddPolyline([x1, y1, x2, y2, ...], closed)`
- `cad.AddPolyline3d([x1, y1, z1, x2, y2, z2, ...], closed)`
- `cad.DrawRectangle(x1, y1, x2, y2)`
- `cad.DrawHatch([x1, y1, x2, y2, ...], "SOLID", scale, angleDeg)`
- `cad.GetCurveLength(objectId)`
- `cad.GetBoundingBox(objectId)`
- `cad.GetIntersections(id1, id2)`
- `cad.GetIntersectionsEx(id1, id2, "both|extend_this|extend_other|extend_both")`
- `cad.CountIntersections(id1, id2, mode)`
- `cad.GetCurveStartPoint(objectId)`
- `cad.GetCurveEndPoint(objectId)`
- `cad.GetCurveMidPoint(objectId)`
- `cad.GetCurveSamplePoints(objectId, divisions)`
- `cad.IsPointOnCurve(objectId, x, y, z, tolerance)`
- `cad.GetEntityArea(objectId)`
- `cad.GetEntityPerimeter(objectId)`
- `cad.GetEntityMetrics(objectId)`
- `cad.SumEntityAreas([id1, id2])`
- `cad.SumEntityPerimeters([id1, id2])`
- `cad.BuildMetricsSummary([id1, id2])`
- `cad.GetPointAtDist(objectId, dist)`
- `cad.GetDistAtPoint(objectId, x, y, z)`
- `cad.GetClosestPointTo(objectId, x, y, z, extend)`
- `cad.GetPointAtParameter(objectId, parameter)`
- `cad.GetParameterAtPoint(objectId, x, y, z)`
- `cad.GetParameterAtDistance(objectId, distance)`
- `cad.GetCurveFirstDerivativeAtParameter(objectId, parameter)`
- `cad.GetCurveFirstDerivativeAtPoint(objectId, x, y, z)`
- `cad.GetCurveSecondDerivativeAtParameter(objectId, parameter)`
- `cad.GetCurveSecondDerivativeAtPoint(objectId, x, y, z)`
- `cad.SplitCurveByParameters(objectId, [param1, param2])`
- `cad.SplitCurveByPoints(objectId, [x1, y1, z1, x2, y2, z2])`
- `cad.BreakCurveAtPoint(objectId, x, y, z, eraseSource)`
- `cad.BreakCurveAtDistance(objectId, distance, eraseSource)`
- `cad.BreakCurveAtTwoPoints(objectId, x1, y1, z1, x2, y2, z2, eraseSource)`
- `cad.BreakCurveAtTwoDistances(objectId, d1, d2, eraseSource)`
- `cad.TrimCurveStartAtPoint(objectId, x, y, z, eraseSource)`
- `cad.TrimCurveEndAtPoint(objectId, x, y, z, eraseSource)`
- `cad.TrimCurveStartAtDistance(objectId, distance, eraseSource)`
- `cad.TrimCurveEndAtDistance(objectId, distance, eraseSource)`
- `cad.KeepCurveSegmentBetweenPoints(objectId, x1, y1, z1, x2, y2, z2, eraseSource)`
- `cad.KeepCurveSegmentBetweenDistances(objectId, d1, d2, eraseSource)`
- `cad.RemoveCurveSegmentBetweenPoints(objectId, x1, y1, z1, x2, y2, z2, eraseSource)`
- `cad.RemoveCurveSegmentBetweenDistances(objectId, d1, d2, eraseSource)`
- `cad.ReverseCurve(objectId)`
- `cad.ReverseCurves([id1, id2])`
- `cad.JoinEntities(primaryId, [id2, id3], eraseJoinedSources)`
- `cad.JoinAllEntities([id1, id2, id3], eraseJoinedSources)`
- `cad.ExtendLineStartToPoint(objectId, x, y, z)`
- `cad.ExtendLineEndToPoint(objectId, x, y, z)`
- `cad.ExtendPolylineStartToPoint(objectId, x, y)`
- `cad.ExtendPolylineEndToPoint(objectId, x, y)`
- `cad.FilletLines(line1Id, line2Id, radius, trimLines)`
- `cad.ChamferLines(line1Id, line2Id, d1, d2, trimLines)`
- `cad.StretchEntitiesCrossingWindow([ids], x1, y1, z1, x2, y2, z2, dx, dy, dz, moveContainedEntities)`
- `cad.TrimCurveStartToEntity(curveId, boundaryId, eraseSource)`
- `cad.TrimCurveEndToEntity(curveId, boundaryId, eraseSource)`
- `cad.ExtendCurveStartToEntity(curveId, boundaryId)`
- `cad.ExtendCurveEndToEntity(curveId, boundaryId)`
- `cad.TrimCurvesToBoundaries([curveIds], [boundaryIds], "start|end|nearest", eraseSource)`
- `cad.ExtendCurvesToBoundaries([curveIds], [boundaryIds], "start|end|nearest")`
- `cad.BreakCurveAtAllIntersections(curveId, [boundaryIds], eraseSource)`
- `cad.BreakEntitiesAtIntersections([entityIds], eraseSource)`
- `cad.OffsetEntityBothSides(objectId, distance)`
- `cad.OffsetEntitiesBothSides([id1, id2], distance)`
- `cad.OffsetEntityTowardPoint(objectId, distance, x, y, z)`
- `cad.OffsetEntitiesTowardPoint([ids], distance, x, y, z)`
- `cad.CopyEntities([id1, id2], dx, dy, dz)`
- `cad.CopyEntityMultiple(objectId, [dx1, dy1, dz1, dx2, dy2, dz2, ...])`
- `cad.CopyEntitiesMultiple([id1, id2], [dx1, dy1, dz1, ...])`
- `cad.MatchEntityProperties(sourceId, [targetIds], options)`
- `cad.ArrayRectangularEntityEx(objectId, rows, cols, levels, rowSpacing, colSpacing, levelSpacing, eraseSource)`
- `cad.ArrayPolarEntityEx(objectId, itemCount, cx, cy, cz, fillAngleDeg, rotateItems, eraseSource)`
- `cad.SyncBlockReferenceAttributes(blockRefId, overwriteExistingText)`
- `cad.SyncBlockReferenceAttributesBatch([blockRefIds], overwriteExistingText)`
- `cad.UpdateBlockAttributeByTagBatch([blockRefIds], "TAG", "VALUE")`
- `cad.UpdateBlockAttributesByMapBatch([blockRefIds], {"TAG": "VALUE"})`
- `cad.RenameBlockAttributeTag("BLOCK", "OLDTAG", "NEWTAG", updateReferences)`
- `cad.ReplaceBlockReference(blockRefId, "NEWBLOCK", preserveAttributeValues, eraseSource)`
- `cad.ReplaceBlockReferencesBatch([blockRefIds], "NEWBLOCK", preserveAttributeValues, eraseSource)`
- `cad.ExplodeBlockReferenceEx(blockRefId, eraseSource, copySourceProperties)`
- `cad.GetNamedObjectsDictionaryId()`
- `cad.GetModelSpaceRecordId()`
- `cad.GetPaperSpaceRecordId()`
- `cad.CreateNamedDictionary("A/B/C")`
- `cad.GetDictionaryInfo(dictId)`
- `cad.GetNamedDictionaryInfo("A/B/C")`
- `cad.GetDictionaryEntries(dictId)`
- `cad.GetNamedDictionaryEntries("A/B/C")`
- `cad.GetDictionaryEntryIds(dictId)`
- `cad.DictionaryContains(dictId, "KEY")`
- `cad.NamedDictionaryContains("A/B/C", "KEY")`
- `cad.DeleteDictionaryEntry(dictId, "KEY", eraseObject)`
- `cad.DeleteNamedDictionaryEntry("A/B/C", "KEY", eraseObject)`
- `cad.SetXRecordData(dictId, "KEY", typedValues)`
- `cad.GetXRecordData(dictId, "KEY")`
- `cad.SetNamedXRecord("A/B", "KEY", typedValues)`
- `cad.GetNamedXRecord("A/B", "KEY")`
- `cad.SetNamedStringMap("A/B", "KEY", {"k": "v"})`
- `cad.GetNamedStringMap("A/B", "KEY")`
- `cad.DeleteNamedXRecord("A/B", "KEY", eraseObject)`
- `cad.EnsureEntityExtensionDictionary(entityId)`
- `cad.GetExtensionDictionaryInfo(entityId)`
- `cad.GetEntityExtensionDictionaryEntries(entityId)`
- `cad.GetEntityExtensionDictionaryEntriesAtPath(entityId, "A/B")`
- `cad.SetEntityXRecord(entityId, "A/B", "KEY", typedValues)`
- `cad.GetEntityXRecord(entityId, "A/B", "KEY")`
- `cad.SetEntityStringMap(entityId, "A/B", "KEY", {"k": "v"})`
- `cad.GetEntityStringMap(entityId, "A/B", "KEY")`
- `cad.DeleteEntityXRecord(entityId, "A/B", "KEY", eraseObject)`
- `cad.CloneObjectsToOwner([ids], ownerId)`
- `cad.CopyXRecordBetweenDictionaries(sourceDictId, "SRC", targetDictId, "DST", overwrite)`
- `cad.CopyXRecordBetweenNamedDictionaries("A/B", "SRC", "C/D", "DST", overwrite)`
- `cad.ListNamedDictionaryTree("A/B", maxDepth)`
- `cad.GetPolylineVertexCount(objectId)`
- `cad.GetPolylineVertices(objectId)`
- `cad.GetPolylineVertexAt(objectId, index)`
- `cad.GetPolylineInfo(objectId)`
- `cad.IsPolylineClosed(objectId)`
- `cad.SetPolylineClosed(objectId, True)`
- `cad.GetPolylineArea(objectId)`
- `cad.GetPolylineSegmentCount(objectId)`
- `cad.GetPolylineSegmentType(objectId, index)`
- `cad.GetLineSegment2dAt(objectId, index)`
- `cad.GetArcSegment2dAt(objectId, index)`
- `cad.GetBulgeAt(objectId, index)`
- `cad.SetBulgeAt(objectId, index, value)`
- `cad.GetStartWidthAt(objectId, index)`
- `cad.SetStartWidthAt(objectId, index, value)`
- `cad.GetEndWidthAt(objectId, index)`
- `cad.SetEndWidthAt(objectId, index, value)`
- `cad.SetPolylineElevation(objectId, value)`
- `cad.SetPolylineThickness(objectId, value)`
- `cad.SetPolylineNormal(objectId, x, y, z)`
- `cad.GetPolylinePointAtPercent(objectId, percent01)`
- `cad.GetPolylineLengthToVertex(objectId, index)`
- `cad.AddPolylineVertex(objectId, index, x, y)`
- `cad.RemovePolylineVertex(objectId, index)`
- `cad.AddText("ciao", x, y, z, height)`
- `cad.AddMText("testo", x, y, z, textHeight, width)`
- `cad.GetMTextInfo(objectId)`
- `cad.SetMTextContents(objectId, "testo")`
- `cad.SetMTextHeight(objectId, value)`
- `cad.SetMTextWidth(objectId, value)`
- `cad.SetMTextLocation(objectId, x, y, z)`
- `cad.SetMTextRotation(objectId, angleDeg)`
- `cad.AddSpline([x1, y1, z1, x2, y2, z2, ...])`
- `cad.GetSplineInfo(objectId)`
- `cad.GetSplineStartPoint(objectId)`
- `cad.GetSplineEndPoint(objectId)`
- `cad.GetSplineStartParameter(objectId)`
- `cad.GetSplineEndParameter(objectId)`
- `cad.GetSplinePointAtParameter(objectId, parameter)`
- `cad.GetSplineParameterAtPoint(objectId, x, y, z)`
- `cad.GetSplineParameterAtDistance(objectId, distance)`
- `cad.GetSplinePointAtPercent(objectId, percent01)`
- `cad.GetSplineStartTangent(objectId)`
- `cad.GetSplineEndTangent(objectId)`
- `cad.GetSplineControlPointCount(objectId)`
- `cad.GetSplineFitPointCount(objectId)`
- `cad.IsSplineClosed(objectId)`
- `cad.OffsetSpline(objectId, distance)`
- `cad.GetEllipseInfo(objectId)`
- `cad.SetEllipseCenter(objectId, x, y, z)`
- `cad.SetEllipseRadiusRatio(objectId, value)`
- `cad.SetEllipseAngles(objectId, startDeg, endDeg)`
- `cad.CreateRegionFromEntity(objectId)`
- `cad.CreateRegionsFromEntities([id1, id2])`
- `cad.GetRegionInfo(objectId)`
- `cad.BooleanRegions(regionId, otherRegionId, "union|subtract|intersect")`
- `cad.ExplodeRegion(regionId)`
- `cad.SetRegionNormal(regionId, x, y, z)`
- `cad.GetHatchInfo(objectId)`
- `cad.SetHatchPattern(objectId, "SOLID")`
- `cad.SetHatchScale(objectId, value)`
- `cad.SetHatchAngle(objectId, angleDeg)`
- `cad.SetHatchAssociative(objectId, True)`
- `cad.SetHatchElevation(objectId, value)`
- `cad.SetHatchNormal(objectId, x, y, z)`
- `cad.AddAlignedDimension(x1, y1, z1, x2, y2, z2, dimLineX, dimLineY, dimLineZ, text)`
- `cad.AddLeader([x1, y1, z1, x2, y2, z2, ...], annotationId)`
- `cad.GetLeaderInfo(objectId)`
- `cad.GetLeaderVertices(objectId)`
- `cad.SetLeaderHasArrowHead(objectId, True)`
- `cad.SetLeaderHasHookLine(objectId, False)`
- `cad.SetLeaderAnnotation(objectId, annotationId)`
- `cad.AddTolerance(text, x, y, z, directionX, directionY, directionZ)`
- `cad.AddTable(x, y, z, rows, columns, rowHeight, columnWidth)`
- `cad.Get3DFaceInfo(objectId)`
- `cad.GetPolyfaceMeshInfo(objectId)`
- `cad.LayerExists("LAYER")`
- `cad.BlockExists("BLOCK_NAME")`
- `cad.GetBlockNames()`
- `cad.FindBlockNames("part_of_name")`
- `cad.GetBlockDefinitionInfo("BLOCK_NAME")`
- `cad.GetOpenDrawings()`
- `cad.SaveDrawing()`
- `cad.SaveDrawingAs("C:\\temp\\copy.dwg")`
- `cad.NewDrawing()`
- `cad.OpenDrawing("C:\\temp\\file.dwg")`
- `cad.SwitchDrawing("Disegno1.dwg")`
- `cad.GetModelSpaceEntityIds()`
- `cad.GetPaperSpaceEntityIds()`
- `cad.GetLayoutNames()`
- `cad.GetCurrentLayoutName()`
- `cad.GetLayoutInfo("Layout1")`
- `cad.GetTextStyleNames()`
- `cad.GetCurrentTextStyle()`
- `cad.CreateTextStyle(styleName, fontFile, textSize, xScale, obliqueAngleDeg)`
- `cad.RenameTextStyle("OLD", "NEW")`
- `cad.SetCurrentTextStyle("STYLE")`
- `cad.GetTextStyleInfo("STYLE")`
- `cad.GetDimensionStyleNames()`
- `cad.GetCurrentDimensionStyle()`
- `cad.CreateDimensionStyle("STYLE")`
- `cad.RenameDimensionStyle("OLD", "NEW")`
- `cad.SetCurrentDimensionStyle("STYLE")`
- `cad.GetDimensionStyleInfo("STYLE")`
- `cad.GetGroupNames()`
- `cad.GroupExists("GROUP")`
- `cad.CreateGroup("GROUP", "description")`
- `cad.DeleteGroup("GROUP")`
- `cad.AddEntitiesToGroup("GROUP", [id1, id2])`
- `cad.GetGroupEntityIds("GROUP")`
- `cad.GetGroupInfo("GROUP")`
- `cad.GetDictionaryNames()`
- `cad.GetRegisteredApplicationNames()`
- `cad.GetUcsNames()`
- `cad.GetViewNames()`
- `cad.GetCollectionsSummary()`
- `cad.EnsureLayer("LAYER", colorIndex)`
- `cad.ListLayers()`
- `cad.GetCurrentLayer()`
- `cad.SetCurrentLayer("LAYER")`
- `cad.SetLayerColor("LAYER", colorIndex)`
- `cad.RenameLayer("OLD", "NEW")`
- `cad.DeleteLayer("LAYER")`
- `cad.TurnLayerOn("LAYER")`
- `cad.TurnLayerOff("LAYER")`
- `cad.IsLayerOn("LAYER")`
- `cad.LockLayer("LAYER")`
- `cad.UnlockLayer("LAYER")`
- `cad.IsLayerLocked("LAYER")`
- `cad.FreezeLayer("LAYER")`
- `cad.ThawLayer("LAYER")`
- `cad.IsLayerFrozen("LAYER")`
- `cad.SetLayerLineWeight("LAYER", value)`
- `cad.GetLayerInfo("LAYER")`
- `cad.ListLinetypes()`
- `cad.SetEntityColor(objectId, colorIndex)`
- `cad.SetEntityLayer(objectId, "LAYER")`
- `cad.SetEntityLineWeight(objectId, value)`
- `cad.SetEntityLinetype(objectId, "Continuous")`
- `cad.GetEntityHandle(objectId)`
- `cad.GetEntityTypeName(objectId)`
- `cad.GetEntityOwnerId(objectId)`
- `cad.IsEntityVisible(objectId)`
- `cad.SetEntityVisible(objectId, True)`
- `cad.GetEntityCommonInfo(objectId)`
- `cad.GetEntitiesCommonInfo([id1, id2])`
- `cad.GetDxfName(objectId)`
- `cad.EntMake(dxfPairs)`
- `cad.EntMakeMany([dxfPairs1, dxfPairs2])`
- `cad.EntLast()`
- `cad.EntNext(objectId)`
- `cad.EntPrevious(objectId)`
- `cad.GetOwnerEntityIds(objectId)`
- `cad.EntFirstAttribute(blockReferenceId)`
- `cad.GetAttributeOwnerBlockReference(attributeId)`
- `cad.GetEntityByHandle("3BC26")`
- `cad.EntParent(objectId)`
- `cad.EntChildren(objectId)`
- `cad.EntAncestors(objectId)`
- `cad.EntDescendants(objectId, maxDepth)`
- `cad.EntOwnerInfo(objectId)`
- `cad.EntRootOwner(objectId)`
- `cad.EntOwnerChainInfo(objectId)`
- `cad.EntOwningBlockReference(objectId)`
- `cad.EntAttributeSiblings(attributeId)`
- `cad.EntNextAttribute(attributeId)`
- `cad.EntPreviousAttribute(attributeId)`
- `cad.EntDel(objectId)`
- `cad.EntCopy(objectId, dx, dy, dz)`
- `cad.EntGet(objectId)`
- `cad.EntGetMap(objectId)`
- `cad.GetEntityDxfValue(objectId, code)`
- `cad.HasEntityDxfCode(objectId, code)`
- `cad.GetEntityDxfCodes(objectId)`
- `cad.SetEntityDxfValue(objectId, code, value)`
- `cad.EntMod(objectId, dxfPairs)`
- `cad.ListXDataApps(objectId)`
- `cad.GetXData(objectId, "APPNAME")`
- `cad.SetXData(objectId, typedValues)`
- `cad.ClearXData(objectId, "APPNAME")`
- `cad.GetEntityRawSummary(objectId)`
- `cad.SetEntitiesColorByLayer([id1, id2, ...])`
- `cad.SetEntitiesLineWeight([id1, id2], value)`
- `cad.SetEntitiesLinetype([id1, id2], "Continuous")`
- `cad.GetSelectionByDxf(filters)`
- `cad.FilterEntitiesByDxf(ids, filters)`
- `cad.SelectWindowByDxf(x1, y1, z1, x2, y2, z2, filters)`
- `cad.SelectCrossingWindowByDxf(x1, y1, z1, x2, y2, z2, filters)`
- `cad.GetSelectionByArea(minArea, maxArea, layerName, typeName, onlyClosed)`
- `cad.GetSelectionByLength(minLength, maxLength, layerName, typeName, closedOnly)`
- `cad.GetSelectionByClosed(True, layerName, typeName)`
- `cad.InsertBlock("BLOCK_NAME", x, y, z)`
- `cad.InsertBlockScaled("BLOCK_NAME", x, y, z, sx, sy, sz, rotationDeg)`
- `cad.InsertBlocks([{...}, {...}])`
- `cad.BlockReferenceHasAttributes(blockRefId)`
- `cad.GetBlockAttributes(blockRefId)`
- `cad.GetBlockAttributeReferenceIds(blockRefId)`
- `cad.GetBlockDefinitionEntityIds(blockRefId)`
- `cad.GetInsertAttributeTraversalInfo(blockRefId)`
- `cad.GetConstantBlockAttributes(blockRefId)`
- `cad.GetConstantBlockAttributeDefinitionIds(blockRefId)`
- `cad.GetBlockAttributeDefinitions("BLOCK_NAME")`
- `cad.GetBlockAttributeDefinitionIds("BLOCK_NAME")`
- `cad.FindBlockAttributeReferenceId(blockRefId, "TAG")`
- `cad.GetAttributeInfo(attributeId)`
- `cad.SetAttributeText(attributeId, "value")`
- `cad.SetAttributeTag(attributeId, "TAG")`

## DXF-like API (`EntMake` / `EntGet` / `EntMod`)

`PYLOAD` espone anche una superficie stile `entmake/entget/entmod` per lavorare con coppie `code/value`.

Entita supportate nel subset attuale:

- `LINE`
- `RAY`
- `CIRCLE`
- `ARC`
- `POINT`
- `3DFACE`
- `TRACE`
- `SOLID`
- `ATTDEF`
- `ATTRIB` tramite `EntGet` / `EntMod` / `SetEntityDxfValue` su riferimenti esistenti e `EntMake` controllato con owner `330`
- `TEXT`
- `MTEXT`
- `ELLIPSE`
- `HATCH`
- `LEADER`
- `INSERT`
- `LWPOLYLINE`

Codici comuni gestiti:

- `0` tipo DXF
- `5` handle
- `330` owner handle
- `67` flag model/paper space
- `410` layout name quando disponibile
- `8` layer
- `6` linetype
- `48` linetype scale
- `60` visibilita (`0` visibile, `1` invisibile)
- `62` color index
- `370` lineweight

Codici geometrici coperti:

- `LINE`: `10/20/30`, `11/21/31`, `39`, `210/220/230`
- `RAY`: `10/20/30`, `11/21/31`
- `CIRCLE`: `10/20/30`, `40`, `39`, `210/220/230`
- `ARC`: `10/20/30`, `40`, `50`, `51`, `39`, `210/220/230`
- `POINT`: `10/20/30`, `39`, `210/220/230`
- `3DFACE`: `10/20/30`, `11/21/31`, `12/22/32`, `13/23/33`
- `TRACE`: `10/20/30`, `11/21/31`, `12/22/32`, `13/23/33`
- `SOLID`: `10/20/30`, `11/21/31`, `12/22/32`, `13/23/33`
- `ATTDEF`: `10/20/30`, `11/21/31`, `1`, `2`, `3`, `7`, `40`, `41`, `50`, `51`, `70`, `71`, `72`, `73`, `39`, `210/220/230`
- `ATTRIB`: `330`, `10/20/30`, `11/21/31`, `1`, `2`, `7`, `40`, `41`, `50`, `51`, `70`, `71`, `72`, `73`, `39`, `210/220/230`
- `TEXT`: `10/20/30`, `11/21/31`, `1`, `7`, `40`, `41`, `50`, `51`, `71`, `72`, `73`, `39`, `210/220/230`
- `MTEXT`: `10/20/30`, `1`, `7`, `40`, `41`, `44`, `50`, `71`, `72`, `73`, `210/220/230`
- `ELLIPSE`: `10/20/30`, `11/21/31`, `40`, `41`, `42`, `210/220/230`
- `HATCH`: `2`, `10`, `20`, `41`, `52`, `70`, `75`, `76`, `77`
- `LEADER`: `10/20/30`, `71`, `76`, `340`
- `INSERT`: `2`, `10/20/30`, `41`, `42`, `43`, `50`, `66`
- `LWPOLYLINE`: `10`, `20`, `38`, `39`, `40`, `41`, `42`, `43`, `70`, `90`, `210/220/230`

Filtri DXF-style attualmente supportati in selezione:

- `0` DXF name
- `8` layer
- `62` color index
- `6` linetype
- `60` visibilita
- `370` lineweight
- `2` block name per `INSERT`
- `2` pattern name per `HATCH`
- `2` tag per `ATTDEF` / `ATTRIB`
- `1` testo per `TEXT` / `MTEXT` / `ATTRIB`
- `3` prompt per `ATTDEF`
- `5` handle
- `330` owner handle
- `410` layout name
- `10/20/30` punto principale per `LINE`, `RAY`, `CIRCLE`, `ARC`, `POINT`, `TEXT`, `MTEXT`, `ATTRIB`, `ATTDEF`, `ELLIPSE`, `INSERT`
- `11/21/31` punto secondario o vettore per `LINE`, `RAY`, `ELLIPSE`, e alignment point testo quando disponibile
- `210/220/230` normal per entita che la espongono
- `7` text style per `TEXT` / `MTEXT` / `ATTDEF`
- `7` text style per `ATTRIB`
- `67` model/paper space
- `66` presenza attributi su `INSERT`
- `71` text generation flags per `TEXT` / `ATTDEF` / `ATTRIB`
- `71` attachment per `MTEXT`
- `52` pattern angle per `HATCH`
- `71` arrow head per `LEADER`
- `72` allineamento orizzontale per `TEXT` / `ATTDEF` / `ATTRIB`
- `72` flow direction per `MTEXT`
- `73` allineamento verticale per `TEXT` / `ATTDEF` / `ATTRIB`
- `73` line spacing style per `MTEXT`
- `44` line spacing factor per `MTEXT`
- `75` hatch style per `HATCH`
- `76` vertex count per `LEADER`
- `76` pattern type per `HATCH`
- `77` pattern double per `HATCH`

Supportano anche:

- wildcard stringa con `*` e `?`
- confronti numerici con `>`, `<`, `>=`, `<=`

Codici numerici gia filtrabili anche con confronti:

- `38`, `39`, `40`, `41`, `42`, `43`, `44`, `48`, `50`, `51`, `52`, `70`, `71`, `72`, `73`, `75`, `76`, `77`, `90`, `370`
- `10`, `20`, `30`, `11`, `21`, `31`, `210`, `220`, `230`

Estensioni recenti del subset:

- `RAY`, `3DFACE`, `TRACE`, `SOLID`, `LEADER` supportati anche via `EntMake` / `EntGet` / `EntMod`
- `LWPOLYLINE` ora supporta anche `bulge` (`42`), `start width` (`40`), `end width` (`41`) e `constant width` (`43`)
- su `LWPOLYLINE`, `43` viene emesso solo quando la polyline ha davvero larghezza costante; se usi `40/41` per-vertice, il bridge privilegia il dato per-vertice
- `HATCH` espone anche `70` per l'associativita
- `TEXT`, `ATTDEF`, `ATTRIB` espongono anche `41` (width factor), `51` (oblique), `72/73` (alignment) e `11/21/31` se presente un alignment point
- `TEXT`, `ATTDEF`, `ATTRIB` espongono anche `71` (text generation flags) quando disponibile nella tua API
- `MTEXT` espone anche `71` (attachment), `72` (flow direction), `73` (line spacing style), `44` (line spacing factor) quando disponibili nella tua API
- `INSERT` espone anche `66` in `EntGet`
- `ATTRIB` espone e modifica anche `7` (text style) e `70` (flags base, oggi soprattutto invisibilita)
- `HATCH` espone anche `75` (style), `76` (pattern type), `77` (pattern double) quando disponibili nella tua API
- traversal DXF piu ricco con `EntAncestors`, `EntDescendants`, `EntOwnerInfo`, `GetBlockDefinitionEntityIds`
- `EntGet` espone anche i codici comuni `5`, `330`, `67`, `410` per lavorare meglio in stile database/layout/owner
- traversal piu profondo su blocchi/attributi con `EntRootOwner`, `EntOwnerChainInfo`, `EntOwningBlockReference`, `EntAttributeSiblings`, `EntNextAttribute`, `EntPreviousAttribute`, `GetInsertAttributeTraversalInfo`

Test consigliati per questo settore:

- `test_dxf.py`
- `test_entmake.py`
- `test_dxf_insert_select.py`
- `test_dxf_hatch_traversal.py`
- `test_dxf_attdef_handle_select.py`
- `test_dxf_attrref_filters.py`
- `test_dxf_advanced_filters.py`
- `test_dxf_master.py`
- `test_2d_lowlevel.py`

Proprieta utili:

- `cad.DrawingPath`
- `cad.DrawingName`
- `cad.DrawingDirectory`
- `cad.DatabaseFilename`
- `cad.HasFullDrawingPath`
- `cad.IsDrawingSaved`

Esempio rapido:

```python
cad.Msg("Script path: " + script_path)

res = cad.GetPoint("Punto iniziale:")
if res.Status == PromptStatus.OK:
    p = res.Value
    cad.AddCircle(p.X, p.Y, p.Z, 20.0)
    cad.AddText("PYLOAD", p.X, p.Y, p.Z, 5.0)
    cad.Regen()
```

## Note operative

- `PYLOAD` blocca il documento durante l'esecuzione script (`DocumentLock`).
- Le eccezioni Python vengono riportate in command line con traceback (`[PYLOAD TRACEBACK]`).
- Se usi WinForms in Python, ricordati:
  - `clr.AddReference("System.Windows.Forms")`
  - poi `import System.Windows.Forms as WinForms`

## Troubleshooting rapido

- `ImportError: No module named Forms`
  - manca `clr.AddReference("System.Windows.Forms")` prima dell'import.

- Non vedi entita a schermo
  - verifica `tr.Commit()` nello script.
  - esegui `ed.Regen()` a fine operazione.

- Errore riferimenti ZWCAD in build
  - controlla i `HintPath` in `PYLOAD.csproj` verso le DLL di ZWCAD 2015.

## Come usare il repository

Se lavori su ambienti storici:

- entra in [`2015/`](./2015)

Se lavori su ZWCAD 2026:

- entra in [`2026/`](./2026)

## Note

- la cartella `2015/` è mantenuta separata e non deve essere trattata come fallback del ramo 2026
- la cartella `2026/` è una base separata, testata in modo indipendente
- i file `tests/` servono come smoke/regression suite rapida durante lo sviluppo del bridge
