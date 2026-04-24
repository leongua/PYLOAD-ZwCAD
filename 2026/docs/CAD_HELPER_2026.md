# CAD Helper 2026 (`cad`)

Catalogo operativo dell'helper `cad` esposto dagli script Python lanciati con `PYLOAD2026R`.

## Oggetti disponibili nello script

| Oggetto | Tipo | Scopo |
|---|---|---|
| `doc` | Documento ZWCAD | accesso al documento attivo |
| `db` | Database ZWCAD | accesso al database DWG attivo |
| `ed` | Editor ZWCAD | prompt e messaggi command line |
| `cad` | Bridge C# 2026 | API ad alto livello per script |

## Metodi `cad` (tabella)

| Metodo / Pattern | Modulo | Descrizione breve |
|---|---|---|
| `GetBuildMarker`, `Msg`, `GetPoint` | `PyCad2026.Core.cs` | funzioni base runtime e prompt |
| `GetShellTranscript*`, `ClearShellTranscript` | `PyCad2026.Core.cs` | lettura/gestione transcript shell |
| `RunCommand`, `RunCommands`, `SendEnter`, `CancelActiveCommand` | `PyCad2026.Commands.cs` | invio comandi al command line |
| `RunCommandNoiseFree`, `RunCommandsNoiseFree`, `FlushCommandChannel` | `PyCad2026.Commands.cs` | esecuzione comandi con riduzione rumore prompt |
| `Command`, `CommandSilent`, `RunLisp`, `RunLispQuiet`, `CallLisp` | `PyCad2026.Commands.cs` | pipeline LISP/command |
| `GetVar*`, `SetVar*`, `SupportsSystemVariables` | `PyCad2026.Commands.cs` | accesso system variables |
| `ExportShellTranscript` | `PyCad2026.Commands.cs` | export transcript su file |
| `Zoom*`, `Audit*`, `SaveCommand`, `RunScriptFile`, `OpenScript` | `PyCad2026.Commands.cs` | utility comandi nativi |
| `AddLine`, `AddCircle`, `AddArc`, `AddPoint` | `PyCad2026.Geometry.cs` | creazione entità geometriche base |
| `AddText`, `AddMText`, `AddLeader`, `AddLightWeightPolyline`, `AddPolyline` | `PyCad2026.Geometry.cs` | creazione testo/polilinee/leader |
| `GetLineInfo`, `GetCircleInfo`, `GetArcInfo`, `GetMTextInfo`, `GetLeaderInfo` | `PyCad2026.Geometry.cs` | lettura info geometriche |
| `SetMText*`, `SetLeaderHasArrowHead`, `SetLeaderHasHookLine` | `PyCad2026.Geometry.cs` | modifica entità testo/leader |
| `GetCurveLength`, `GetCurvePointAtDistance`, `GetCurveParameterAtPoint` | `PyCad2026.Curves.cs` | query curva (lunghezza/parametri) |
| `GetPointAtParameter`, `GetParameterAtPoint`, `GetParameterAtDistance` | `PyCad2026.Curves.cs` | conversione punto-parametro |
| `SplitCurveByParameters`, `BreakCurveAtPoint` | `PyCad2026.Curves.cs` | split e break curve |
| `GetPolyline*`, `SetPolyline*`, `GetBulgeAt`, `SetBulgeAt` | `PyCad2026.Curves.cs` | API avanzate polilinea |
| `GetSelectionByLayer`, `GetSelectionByType`, `SelectAll` | `PyCad2026.Selection.cs` | selezioni base |
| `SelectWindow`, `SelectCrossingWindow` | `PyCad2026.Selection.cs` | selezione per area |
| `MoveEntity`, `RotateEntity`, `ScaleEntity`, `MirrorEntity`, `CopyEntity` | `PyCad2026.Selection.cs` | trasformazioni singola entità |
| `MoveEntities`, `RotateEntities`, `ScaleEntities`, `MirrorEntities` | `PyCad2026.Selection.cs` | trasformazioni batch |
| `OffsetEntity`, `OffsetEntities`, `ExplodeEntity`, `EraseEntity` | `PyCad2026.Selection.cs` | operazioni dirette su entità |
| `EntMake`, `GetEntityDxfValue`, `SetEntityDxfValue` | `PyCad2026.Dxf.cs` | bridge DXF-like |
| `GetSelectionByDxf`, `DebugDxfFilterMatch` | `PyCad2026.Dxf.cs` | filtri e debug match DXF |
| `GetBlockNames`, `GetBlockDefinitionInfo` | `PyCad2026.Blocks.cs` | introspezione blocchi |
| `InsertBlock`, `GetBlockReferenceInfo` | `PyCad2026.Blocks.cs` | gestione block reference |
| `GetBlockAttributeDefinitions`, `GetBlockReferenceAttributes`, `SetBlockReferenceAttributes` | `PyCad2026.Blocks.cs` / `PyCad2026.Advanced.cs` | gestione attributi |
| `SyncBlockReferenceAttributes*`, `UpdateBlockAttributeByTagBatch`, `UpdateBlockAttributesByMapBatch` | `PyCad2026.Blocks.cs` | sync/update batch attributi |
| `ReplaceBlockReference*`, `RenameBlockAttributeTag`, `ExplodeBlockReferenceEx` | `PyCad2026.Blocks.cs` | replace/rename/explode blocchi |
| `BreakCurveAtAllIntersections`, `BreakEntitiesAtIntersections` | `PyCad2026.Modify.cs` | break automatico su intersezioni |
| `TrimCurve*`, `ExtendCurve*`, `TrimCurvesToBoundaries`, `ExtendCurvesToBoundaries` | `PyCad2026.Modify.cs` | trim/extend automatici |
| `ReverseCurve`, `JoinEntities`, `FilletLines`, `ChamferLines` | `PyCad2026.Modify.cs` | modifica geometrica avanzata |
| `OffsetEntityTowardPoint`, `OffsetEntityBothSides`, `MatchEntityProperties` | `PyCad2026.Modify.cs` | offset evoluto e match properties |
| `CreateNamedDictionary`, `GetNamedDictionaryInfo`, `GetNamedDictionaryEntries` | `PyCad2026.Database.cs` | gestione dictionary NOD |
| `SetNamedXRecord`, `GetNamedXRecord`, `DeleteNamedXRecord` | `PyCad2026.Database.cs` | gestione XRecord nominati |
| `SetNamedStringMap`, `GetNamedStringMap` | `PyCad2026.Database.cs` | key/value persistenti in dictionary |
| `SetEntityXRecord`, `GetEntityXRecord`, `SetEntityStringMap`, `GetEntityStringMap` | `PyCad2026.Database.cs` | metadata su extension dictionary entità |
| `CloneObjectsToOwner`, `CopyXRecordBetween*`, `ListNamedDictionaryTree` | `PyCad2026.Database.cs` | utility clone/copy/traversal DB |
| `GetViewNames`, `GetUcsNames`, `GetViewUcsViewportStats` | `PyCad2026.Advanced.cs` | overview viste/UCS/viewport |
| `EnsureNamedView`, `EnsureNamedUcs`, `DeleteNamedView`, `DeleteNamedUcs` | `PyCad2026.Advanced.cs` | gestione named view/UCS |
| `AddBox`, `AddBoxesBatch`, `GetSolid3dInfo` | `PyCad2026.Advanced.cs` | 3D solids base |
| `CreateRegionsFromEntities`, `BooleanRegions*`, `ExplodeRegion`, `GetRegionInfo` | `PyCad2026.Advanced.cs` | region API |
| `GetModelSpaceEntityIds`, `GetPaperSpaceEntityIds`, `CountEntitiesInModelSpaceByDxf` | `PyCad2026.Advanced.cs` | conteggi e scansioni spazi |
| `GetLayoutNamesFromBlockTable`, `GetEntitiesInSpace`, `GetSpaceEntityStats` | `PyCad2026.Massive.cs` | utility spazio/layout |
| `EraseByLayerBatch`, `EraseByTypeBatch`, `EraseByDxfFilterBatch` | `PyCad2026.Massive.cs` | cancellazione batch |
| `GetEntityPropertySnapshot`, `ApplyEntityPropertySnapshot` | `PyCad2026.Massive.cs` | snapshot/apply proprietà |
| `BuildIntersectionsMatrix`, `BreakCurvesAtAllIntersectionsBatch`, `AutoTrimExtendByBoundaries` | `PyCad2026.Massive.cs` | modify batch deterministic |
| `CopyTransformBatch`, `OffsetEntitiesTowardSeedsBatch` | `PyCad2026.Massive.cs` | trasformazioni/offset batch |
| `ExportEntityAuditCsv`, `ExportDatabaseSnapshot`, `ExportApiMethodsReport`, `GetPublicApiMethodNames` | `PyCad2026.Massive.cs` | export/report API |
| `RunDeterministicModifyPack` | `PyCad2026.Massive.cs` | pack modifica deterministico per test |
| `GetSpaceSummary`, `AddPaperViewportToSpace`, `GetViewportIdsInSpace`, `EraseViewportsInSpace` | `PyCad2026.Fix19.cs` | tools paperspace/viewport |
| `GetBlockReferenceIdsByName`, `ReplaceBlockReferencesByName`, `ReplaceBlockReferencesByMap` | `PyCad2026.Fix19.cs` | replace blocchi per nome/mappa |
| `SyncBlockReferenceAttributesByName`, `UpdateBlockAttributesByNameMap` | `PyCad2026.Fix19.cs` | batch attributi per nome blocco |
| `ExportApiCompatibilityReport` | `PyCad2026.Fix19.cs` | report compatibilità metodi richiesti |
| `GetLayerNames`, `GetLayoutTabNames`, `GetLayerEntityCounts`, `GetDxfEntityCountsBySpace` | `PyCad2026.Fix21.cs` | report layer/layout |
| `MoveEntitiesToLayer`, `MoveEntitiesByDxfToLayer`, `BatchSetEntityVisibility` | `PyCad2026.Fix21.cs` | batch layer/visibility |
| `GetBlockReferenceCountsByName` | `PyCad2026.Fix21.cs` | conteggio block reference per nome |
| `GetCurrentLayerName`, `SetCurrentLayerName`, `GetLayerState`, `SetLayerState`, `SetLayerStatesBatch` | `PyCad2026.Fix22.cs` | stato layer e current layer |
| `GetObjectIdsByHandleStrings`, `GetHandleMap` | `PyCad2026.Fix22.cs` | mapping handle <-> object id |

## Note

- Marker build corrente usato nei test: `cad.GetBuildMarker()`
- Catalogo API esteso: [`API_REFERENCE_2026.md`](./API_REFERENCE_2026.md)
