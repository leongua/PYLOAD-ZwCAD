# API Reference 2026

Reference completa per il bridge `PYLOAD2026R` (ZWCAD 2026 x64), costruita dalle firme pubbliche in `2026/src`.

## Scope

- Runtime script: `PYLOAD2026R`
- Oggetti disponibili nello script Python: `cad`, `doc`, `db`, `ed`
- Sorgente firme: `2026/src/*.cs`

## Convenzioni variabili (glossario)

Questa sezione spiega i nomi variabili usati in quasi tutte le API.

| Variabile | Tipo | Significato |
|---|---|---|
| `entityId` | `ObjectId` | id di una singola entita |
| `entityIds` | `IList` di `ObjectId` | lista di entita su cui fare batch |
| `x`, `y`, `z` | `double` | coordinate WCS |
| `x1`,`y1`,`z1`,`x2`,`y2`,`z2` | `double` | due punti (linea, finestra, specchio) |
| `baseX`,`baseY`,`baseZ` | `double` | base point per rotate/scale |
| `dx`,`dy`,`dz` | `double` | delta spostamento |
| `angleDegrees` | `double` | angolo in gradi |
| `scaleFactor` | `double` | fattore di scala |
| `radius` | `double` | raggio |
| `height`,`textHeight` | `double` | altezza testo |
| `width` | `double` | larghezza (MText, box, viewport, ecc.) |
| `coordinates` | `IList` | coordinate flat (XY o XYZ, dipende dal metodo) |
| `closed` | `bool` | polilinea chiusa/aperta |
| `dictionaryPath` | `string` | path dizionario con separatori `/` o `\\` |
| `key` | `string` | chiave entry in dizionario/XRecord |
| `typedValues` | `IList` | lista di typed values per XRecord |
| `dxfFilters` | `IList` | lista filtri DXF `{code, value}` |
| `outputPath` | `string` | path file export |
| `spaceName` | `string` | nome spazio/layout (`*Model_Space`, `*Paper_Space`, ecc.) |
| `blockName` | `string` | nome definizione blocco |
| `trimMode`,`extendMode` | `string` | modalita trim/extend (`nearest`, `start`, `end`, `both`) |
| `eraseSource` | `bool` | cancella sorgente dopo operazione |
| `overwrite` | `bool` | sovrascrive target gia esistente |

## Formati input importanti

- `AddPolyline` / `AddLightWeightPolyline`: `coordinates = [x1,y1,x2,y2,...]`
- `AddLeader`: `coordinates = [x1,y1,z1,x2,y2,z2,...]`
- `GetSelectionByDxf` / `DebugDxfFilterMatch`: `dxfFilters = [{"code": 0, "value": "CIRCLE"}, ...]`
- `dictionaryPath`: esempio `PYLOAD/META/TEST`

## Codici DXF supportati direttamente in `SetEntityDxfValue` / `GetEntityDxfValue`

- Generali: `0`, `5`, `8`, `39`, `62`
- Geometria base: `10`,`20`,`30`,`11`,`21`,`31`
- Cerchio: `40`
- Testo: `1`,`41`,`50`,`51`
- Arco: `50`,`51`
- Altri codici possono essere disponibili in sola lettura via mappa DXF interna, ma non tutti sono scrivibili.

## Totale API pubbliche (stato attuale sorgenti)

- Metodi pubblici esposti: **274**
- File sorgente API: 14

## Reference per modulo

### `PyCad2026.Core.cs`

```csharp
public PyCad2026(Document doc, Database db, Editor ed)
public void Msg(string text)
public PromptPointResult GetPoint(string message)
public void ClearShellTranscript()
public string GetBuildMarker()
public string GetEntityHandle(ObjectId entityId)
public ArrayList GetShellTranscript()
public string GetShellTranscriptText()
public string GetLastShellLine()
public void RunLisp(string expression)
public void Princ(string text, bool newLine)
public void RegenNative()
public void ZoomExtents()
```

### `PyCad2026.Commands.cs`

```csharp
public bool SupportsSystemVariables()
public void RunCommand(string commandText)
public void RunCommand(string commandText, bool activate, bool wrapUpInactiveDoc, bool echoCommand)
public void RunCommands(IList commandTexts)
public void SendEnter()
public void CancelActiveCommand()
public void ZoomAll()
public void ZoomPrevious()
public void ZoomWindow(double x1, double y1, double z1, double x2, double y2, double z2)
public void ZoomCenter(double x, double y, double z, double height)
public void ZoomObject(ObjectId entityId)
public void Audit(bool fixErrors)
public void AuditInteractive()
public void SaveCommand()
public void OpenScript(string scriptFilePath)
public void RunScriptFile(string scriptFilePath)
public object GetVar(string variableName)
public string GetVarString(string variableName)
public int GetVarInt(string variableName)
public double GetVarDouble(string variableName)
public bool GetVarBool(string variableName)
public Hashtable GetVars(IList variableNames)
public void SetVar(string variableName, object value)
public void SetVars(IDictionary variables)
public void LoadLispFile(string lspFilePath)
public void RunLispFile(string lspFilePath)
public void CallLisp(string functionName, IList args)
public void Princ(string text)
public void Command(IList commandArgs)
public string ExportShellTranscript(string filePath)
```

### `PyCad2026.Geometry.cs`

```csharp
public ObjectId AddLine(double x1, double y1, double z1, double x2, double y2, double z2)
public ObjectId AddCircle(double x, double y, double z, double radius)
public ObjectId AddArc(double x, double y, double z, double radius, double startAngleDegrees, double endAngleDegrees)
public ObjectId AddPoint(double x, double y, double z)
public ObjectId AddText(string text, double x, double y, double z, double height)
public ObjectId AddMText(string text, double x, double y, double z, double textHeight, double width)
public ObjectId AddPolyline(IList coordinates, bool closed)
public ObjectId AddLightWeightPolyline(IList coordinates, bool closed)
public ObjectId AddLeader(IList coordinates, ObjectId annotationId)
public ObjectId DrawHatch(IList coordinates, string pattern, double scale, double angleDegrees)
public void SetCircleDiameter(ObjectId entityId, double diameter)
public void SetMTextContents(ObjectId entityId, string contents)
public void SetLeaderHasArrowHead(ObjectId entityId, bool hasArrowHead)
public void SetLeaderHasHookLine(ObjectId entityId, bool hasHookLine)
public void SetBulgeAt(ObjectId entityId, int index, double bulge)
public void SetStartWidthAt(ObjectId entityId, int index, double width)
public void SetEndWidthAt(ObjectId entityId, int index, double width)
public void SetPolylineElevation(ObjectId entityId, double elevation)
public void SetPolylineThickness(ObjectId entityId, double thickness)
public Hashtable GetPolylineInfo(ObjectId entityId)
public Hashtable GetEllipseInfo(ObjectId entityId)
public Hashtable GetHatchInfo(ObjectId entityId)
```

Dettaglio variabili per i metodi piu richiesti:

- `AddText(text,x,y,z,height)`: testo DBText in `Position(x,y,z)`, altezza `height`.
- `AddMText(text,x,y,z,textHeight,width)`: MText in `Location`, con altezza e larghezza box.
- `AddLeader(coordinates,annotationId)`: `coordinates` in triplette XYZ; se `annotationId` punta a MText, il leader viene valutato con annotazione.
- `AddPolyline/AddLightWeightPolyline(coordinates,closed)`: `coordinates` in coppie XY.

### `PyCad2026.Curves.cs`

```csharp
public Hashtable GetCircleInfo(ObjectId entityId)
public void SetCircleRadius(ObjectId entityId, double radius)
public void SetCircleCenter(ObjectId entityId, double x, double y, double z)
public void SetCircleThickness(ObjectId entityId, double thickness)
public void SetCircleNormal(ObjectId entityId, double x, double y, double z)
public Point3d GetLineStartPoint(ObjectId entityId)
public Point3d GetLineEndPoint(ObjectId entityId)
public double GetLineAngleDegrees(ObjectId entityId)
public void SetLineStartPoint(ObjectId entityId, double x, double y, double z)
public void SetLineEndPoint(ObjectId entityId, double x, double y, double z)
public void SetLineThickness(ObjectId entityId, double thickness)
public void SetLineNormal(ObjectId entityId, double x, double y, double z)
public double GetCurveLength(ObjectId entityId)
public Point3d GetPointAtDist(ObjectId entityId, double distance)
public double GetDistAtPoint(ObjectId entityId, double x, double y, double z)
public Point3d GetClosestPointTo(ObjectId entityId, double x, double y, double z, bool extend)
public Point3d GetPointAtParameter(ObjectId entityId, double parameter)
public double GetParameterAtPoint(ObjectId entityId, double x, double y, double z)
public double GetParameterAtDistance(ObjectId entityId, double distance)
public Hashtable GetCurveFirstDerivativeAtParameter(ObjectId entityId, double parameter)
public Hashtable GetCurveSecondDerivativeAtParameter(ObjectId entityId, double parameter)
public ObjectId[] SplitCurveByParameters(ObjectId entityId, IList parameters)
public int GetPolylineVertexCount(ObjectId entityId)
public ArrayList GetPolylineVertices(ObjectId entityId)
public Point3d GetPolylineVertexAt(ObjectId entityId, int index)
public bool IsPolylineClosed(ObjectId entityId)
public void SetPolylineClosed(ObjectId entityId, bool closed)
public double GetPolylineArea(ObjectId entityId)
public int GetPolylineSegmentCount(ObjectId entityId)
public string GetPolylineSegmentType(ObjectId entityId, int index)
public double GetBulgeAt(ObjectId entityId, int index)
public double GetStartWidthAt(ObjectId entityId, int index)
public double GetEndWidthAt(ObjectId entityId, int index)
public void SetPolylineNormal(ObjectId entityId, double x, double y, double z)
public Point3d GetPolylinePointAtPercent(ObjectId entityId, double percent)
public double GetPolylineLengthToVertex(ObjectId entityId, int index)
public void AddPolylineVertex(ObjectId entityId, int index, double x, double y)
public void RemovePolylineVertex(ObjectId entityId, int index)
```

### `PyCad2026.Selection.cs`

```csharp
public PromptEntityResult SelectEntity(string message)
public ObjectId[] SelectAll()
public ObjectId[] GetSelectionByLayer(string layerName)
public ObjectId[] GetSelectionByType(string typeName)
public ObjectId[] GetSelection(string layerName, string typeName)
public ObjectId[] SelectWindow(double x1, double y1, double z1, double x2, double y2, double z2)
public ObjectId[] SelectCrossingWindow(double x1, double y1, double z1, double x2, double y2, double z2)
public ObjectId CopyEntity(ObjectId entityId, double dx, double dy, double dz)
public void MoveEntity(ObjectId entityId, double dx, double dy, double dz)
public void RotateEntity(ObjectId entityId, double baseX, double baseY, double baseZ, double angleDegrees)
public void ScaleEntity(ObjectId entityId, double baseX, double baseY, double baseZ, double scaleFactor)
public ObjectId MirrorEntity(ObjectId entityId, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
public void EraseEntity(ObjectId entityId)
public ObjectId[] ExplodeEntity(ObjectId entityId, bool eraseSource)
public ObjectId[] OffsetEntity(ObjectId entityId, double offsetDistance)
public int MoveEntities(IList entityIds, double dx, double dy, double dz)
public int RotateEntities(IList entityIds, double baseX, double baseY, double baseZ, double angleDegrees)
public int ScaleEntities(IList entityIds, double baseX, double baseY, double baseZ, double scaleFactor)
public ObjectId[] MirrorEntities(IList entityIds, double x1, double y1, double z1, double x2, double y2, double z2, bool eraseSource)
public int ExplodeEntities(IList entityIds, bool eraseSource)
public int OffsetEntities(IList entityIds, double offsetDistance)
public int EraseEntities(IList entityIds)
```

### `PyCad2026.Dxf.cs`

```csharp
public ObjectId EntMake(IList dxfPairs)
public void SetEntityDxfValue(ObjectId entityId, int code, object value)
public object GetEntityDxfValue(ObjectId entityId, int code)
public ObjectId[] GetSelectionByDxf(IList dxfFilters)
public Hashtable DebugDxfFilterMatch(ObjectId entityId, IList dxfFilters)
```

### `PyCad2026.Blocks.cs`

```csharp
public string[] GetBlockNames()
public Hashtable GetBlockDefinitionInfo(string blockName)
public ObjectId InsertBlock(string blockName, double x, double y, double z)
public Hashtable GetBlockAttributeDefinitions(string blockName)
public int SyncBlockReferenceAttributesBatch(IList blockReferenceIds, bool overwriteExistingText)
public int SyncBlockReferenceAttributes(ObjectId blockReferenceId, bool overwriteExistingText)
public int UpdateBlockAttributeByTagBatch(IList blockReferenceIds, string tag, string value)
public int UpdateBlockAttributesByMapBatch(IList blockReferenceIds, Hashtable values)
public ObjectId ReplaceBlockReference(ObjectId blockReferenceId, string newBlockName, bool preserveAttributeValues, bool eraseSource)
public ObjectId[] ReplaceBlockReferencesBatch(IList blockReferenceIds, string newBlockName, bool preserveAttributeValues, bool eraseSource)
public Hashtable RenameBlockAttributeTag(string blockName, string oldTag, string newTag, bool updateReferences)
public ObjectId[] ExplodeBlockReferenceEx(ObjectId blockReferenceId, bool eraseSource, bool copySourceProperties)
public Hashtable GetBlockReferenceInfo(ObjectId blockReferenceId)
```

### `PyCad2026.Modify.cs`

```csharp
public ObjectId[] BreakCurveAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
public void ReverseCurve(ObjectId entityId)
public Point3d ExtendLineEndToPoint(ObjectId entityId, double x, double y, double z)
public Point3d ExtendPolylineEndToPoint(ObjectId entityId, double x, double y)
public Point3d ExtendPolylineEndToPoint(ObjectId entityId, double x, double y, double z)
public Hashtable JoinEntities(IList entityIds, bool eraseJoinedSources)
public Hashtable FilletLines(ObjectId firstLineId, ObjectId secondLineId, double radius, bool trimLines)
public Hashtable ChamferLines(ObjectId firstLineId, ObjectId secondLineId, double firstDistance, double secondDistance, bool trimLines)
public ObjectId[] ArrayRectangularEntityEx(ObjectId entityId, int rows, int columns, int levels, double rowSpacing, double columnSpacing, double levelSpacing, bool eraseSource)
public ObjectId TrimCurveStartAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
public ObjectId TrimCurveEndAtPoint(ObjectId entityId, double x, double y, double z, bool eraseSource)
public ObjectId TrimCurveStartToEntity(ObjectId entityId, ObjectId boundaryId, bool eraseSource)
public ObjectId TrimCurveEndToEntity(ObjectId entityId, ObjectId boundaryId, bool eraseSource)
public Point3d ExtendCurveStartToEntity(ObjectId entityId, ObjectId boundaryId)
public Point3d ExtendCurveEndToEntity(ObjectId entityId, ObjectId boundaryId)
public Hashtable TrimCurvesToBoundaries(IList curveIds, IList boundaryIds, string trimMode, bool eraseSource)
public Hashtable ExtendCurvesToBoundaries(IList curveIds, IList boundaryIds, string extendMode)
public ObjectId[] BreakCurveAtAllIntersections(ObjectId curveId, IList boundaryIds, bool eraseSource)
public Hashtable BreakEntitiesAtIntersections(IList entityIds, bool eraseSource)
public ObjectId[] OffsetEntityBothSides(ObjectId entityId, double offsetDistance)
public ObjectId OffsetEntityTowardPoint(ObjectId entityId, double offsetDistance, double x, double y, double z)
public ObjectId[] OffsetEntitiesTowardPoint(IList entityIds, double offsetDistance, double x, double y, double z)
public Hashtable MatchEntityProperties(ObjectId sourceEntityId, IList targetEntityIds, Hashtable options)
```

Note parametri:

- `trimMode` / `extendMode`: stringhe operative (`nearest`, `start`, `end`, `both`).
- `boundaryIds`: lista di entita di riferimento per intersezioni.
- `eraseSource`: se `true`, l'entita sorgente viene cancellata dopo split/trim/break quando applicabile.

### `PyCad2026.Database.cs`

```csharp
public ObjectId GetNamedObjectsDictionaryId()
public ObjectId GetModelSpaceRecordId()
public ObjectId GetPaperSpaceRecordId()
public ObjectId CreateNamedDictionary(string dictionaryPath)
public Hashtable GetDictionaryInfo(ObjectId dictionaryId)
public Hashtable GetNamedDictionaryInfo(string dictionaryPath)
public ArrayList GetDictionaryEntries(ObjectId dictionaryId)
public ObjectId[] GetDictionaryEntryIds(ObjectId dictionaryId)
public ArrayList GetNamedDictionaryEntries(string dictionaryPath)
public bool DictionaryContains(ObjectId dictionaryId, string key)
public bool NamedDictionaryContains(string dictionaryPath, string key)
public void DeleteDictionaryEntry(ObjectId dictionaryId, string key, bool eraseObject)
public void DeleteNamedDictionaryEntry(string dictionaryPath, string key, bool eraseObject)
public void SetXRecordData(ObjectId dictionaryId, string key, IList typedValues)
public Hashtable GetXRecordData(ObjectId dictionaryId, string key)
public void SetNamedXRecord(string dictionaryPath, string key, IList typedValues)
public Hashtable GetNamedXRecord(string dictionaryPath, string key)
public void SetNamedStringMap(string dictionaryPath, string key, Hashtable values)
public Hashtable GetNamedStringMap(string dictionaryPath, string key)
public void DeleteNamedXRecord(string dictionaryPath, string key, bool eraseObject)
public ObjectId EnsureEntityExtensionDictionary(ObjectId entityId)
public ArrayList GetEntityExtensionDictionaryEntries(ObjectId entityId)
public ArrayList GetEntityExtensionDictionaryEntriesAtPath(ObjectId entityId, string subDictionaryPath)
public void SetEntityXRecord(ObjectId entityId, string subDictionaryPath, string key, IList typedValues)
public Hashtable GetEntityXRecord(ObjectId entityId, string subDictionaryPath, string key)
public void SetEntityStringMap(ObjectId entityId, string subDictionaryPath, string key, Hashtable values)
public Hashtable GetEntityStringMap(ObjectId entityId, string subDictionaryPath, string key)
public void DeleteEntityXRecord(ObjectId entityId, string subDictionaryPath, string key, bool eraseObject)
public ObjectId[] CloneObjectsToOwner(IList objectIds, ObjectId ownerId)
public void CopyXRecordBetweenDictionaries(ObjectId sourceDictionaryId, string sourceKey, ObjectId targetDictionaryId, string targetKey, bool overwrite)
public void CopyXRecordBetweenNamedDictionaries(string sourceDictionaryPath, string sourceKey, string targetDictionaryPath, string targetKey, bool overwrite)
public ArrayList ListNamedDictionaryTree(string dictionaryPath, int maxDepth)
```

Note path:

- `dictionaryPath` puo usare sia `/` sia `\\`.
- Path vuoto indica direttamente il dizionario root (NOD o extension dictionary root, secondo contesto).

### `PyCad2026.Advanced.cs`

```csharp
public string[] GetViewNames()
public string[] GetUcsNames()
public Hashtable GetViewUcsViewportStats()
public bool EnsureNamedView(string name, double centerX, double centerY, double width, double height)
public bool EnsureNamedUcs(string name, double ox, double oy, double oz, double xax, double xay, double xaz, double yax, double yay, double yaz)
public ObjectId AddBox(double x, double y, double z, double length, double width, double height)
public Hashtable GetSolid3dInfo(ObjectId solidId)
public ObjectId[] CreateRegionsFromEntities(IList entityIds)
public double GetRegionArea(ObjectId regionId)
public void BooleanRegions(ObjectId primaryRegionId, ObjectId otherRegionId, string operation)
public ObjectId[] ExplodeRegion(ObjectId regionId, bool eraseSource)
public string EnsureTestAttributedBlock(string blockName, string tag, string prompt, string defaultText, double textHeight)
public ObjectId InsertBlockWithAttributes(string blockName, double x, double y, double z, Hashtable values)
public ObjectId[] GetModelSpaceEntityIds()
public ObjectId[] GetPaperSpaceEntityIds()
public Hashtable CountEntitiesInModelSpaceByDxf()
public ObjectId[] AddBoxesBatch(IList boxItems)
public Hashtable GetRegionInfo(ObjectId regionId)
public Hashtable BooleanRegionsBatch(IList operations)
public bool DeleteNamedView(string name)
public bool DeleteNamedUcs(string name)
public Hashtable GetBlockReferenceAttributes(ObjectId blockReferenceId)
public int SetBlockReferenceAttributes(ObjectId blockReferenceId, Hashtable values)
public int GetNamedDictionaryEntriesCount(string dictionaryPath)
```

### `PyCad2026.Massive.cs`

```csharp
public string[] GetLayoutNamesFromBlockTable()
public Hashtable GetSpaceEntityStats()
public ObjectId[] GetEntitiesInSpace(string spaceName)
public Hashtable EraseByLayerBatch(string layerName, bool onlyModelSpace)
public Hashtable EraseByTypeBatch(string dxfType, bool onlyModelSpace)
public Hashtable EraseByDxfFilterBatch(IList dxfFilters, bool onlyModelSpace)
public Hashtable GetEntityPropertySnapshot(ObjectId entityId)
public int ApplyEntityPropertySnapshot(IList entityIds, Hashtable snapshot)
public Hashtable BuildIntersectionsMatrix(IList curveIds, bool extendThis, bool extendArgument)
public Hashtable BreakCurvesAtAllIntersectionsBatch(IList curveIds, bool eraseSource)
public Hashtable AutoTrimExtendByBoundaries(IList curveIds, IList boundaryIds, string trimMode, string extendMode, bool eraseSource)
public Hashtable OffsetEntitiesTowardSeedsBatch(IList jobs)
public Hashtable CopyTransformBatch(IList jobs)
public string ExportEntityAuditCsv(string outputPath, IList entityIds)
public string ExportDatabaseSnapshot(string outputPath, string dictionaryPath, int maxDepth)
public string[] GetPublicApiMethodNames(string containsFilter)
public string ExportApiMethodsReport(string outputPath, string containsFilter)
public Hashtable RunDeterministicModifyPack(double baseX, double baseY, double baseZ)
```

### `PyCad2026.Fix19.cs`

```csharp
public Hashtable GetSpaceSummary(string spaceName)
public ObjectId AddPaperViewportToSpace(string spaceName, double centerX, double centerY, double width, double height, double viewCenterX, double viewCenterY, double viewHeight)
public ObjectId[] GetViewportIdsInSpace(string spaceName)
public int EraseViewportsInSpace(string spaceName, bool keepFirst)
public ObjectId[] GetBlockReferenceIdsByName(string blockName)
public Hashtable ReplaceBlockReferencesByName(string sourceBlockName, string newBlockName, bool preserveAttributeValues, bool eraseSource)
public Hashtable ReplaceBlockReferencesByMap(Hashtable replacementMap, bool preserveAttributeValues, bool eraseSource)
public int SyncBlockReferenceAttributesByName(string blockName, bool overwriteExistingText)
public int UpdateBlockAttributesByNameMap(string blockName, Hashtable values)
public Hashtable ExportApiCompatibilityReport(string outputPath, IList requiredMethods)
```

### `PyCad2026.Fix23.cs`

```csharp
public void RunCommandNoiseFree(string commandText)
public void RunCommandsNoiseFree(IList commandTexts)
public void FlushCommandChannel()
public void FlushCommandChannel(int cancelCount, int enterCount)
public void RunLispQuiet(string expression)
public void CommandSilent(IList commandArgs)
public string[] GetLayerNames()
public string[] GetLayoutTabNames()
public Hashtable GetLayerEntityCounts()
public Hashtable GetLayerEntityCounts(bool onlyModelSpace)
public Hashtable GetDxfEntityCountsBySpace(string spaceName)
public int MoveEntitiesToLayer(IList entityIds, string layerName)
public int MoveEntitiesByDxfToLayer(string dxfType, string layerName, bool onlyModelSpace)
public int MoveEntitiesByDxfToLayer(IList dxfFilters, string layerName, bool onlyModelSpace)
public int BatchSetEntityVisibility(IList entityIds, bool visible)
public Hashtable GetBlockReferenceCountsByName()
public ObjectId[] GetObjectIdsByHandleStrings(IList handleStrings)
public Hashtable GetHandleMap(IList entityIds)
public string GetCurrentLayerName()
public void SetCurrentLayerName(string layerName)
public Hashtable GetLayerState(string layerName)
public bool SetLayerState(string layerName, Hashtable values)
public Hashtable SetLayerStatesBatch(Hashtable layerStateMap)
```

Note pratiche:

- Le API `*NoiseFree`, `RunLispQuiet`, `CommandSilent` inviano comandi senza logging nel transcript interno.
- `GetObjectIdsByHandleStrings` interpreta gli handle in esadecimale (accetta anche prefisso `0x`).
- `MoveEntitiesByDxfToLayer` supporta sia filtro semplice per tipo DXF sia filtro DXF avanzato via lista `{code,value}`.

### `PythonLoader2026R.cs`

```csharp
public void ExposeAndRun()
```

## Error handling (sintesi)

- Input invalidi -> `ArgumentException` (es. coordinate inconsistenti, entity non valida, dictionary non trovato).
- Operazioni CAD non applicabili -> eccezioni runtime ZWCAD (`eNotApplicable`, `eWasErased`, ecc.).
- API not available in alcune build host -> eccezioni propagate dal bridge.

## File correlati

- Helper operativo: [`CAD_HELPER_2026.md`](./CAD_HELPER_2026.md)
- Guida progetto: [`PYLOAD_2026.md`](./PYLOAD_2026.md)
