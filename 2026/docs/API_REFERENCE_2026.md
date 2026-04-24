# API Reference 2026

Indice API del progetto `2026/` (ZWCAD 2026 x64).

## Totale API pubbliche

- **247** metodi pubblici nel bridge (cartella `2026/src`).

## API per modulo (`2026/src`)

| File | Metodi pubblici |
|---|---:|
| `PyCad2026.Advanced.cs` | 24 |
| `PyCad2026.Blocks.cs` | 13 |
| `PyCad2026.Commands.cs` | 30 |
| `PyCad2026.Core.cs` | 12 |
| `PyCad2026.Curves.cs` | 38 |
| `PyCad2026.Database.cs` | 32 |
| `PyCad2026.Dxf.cs` | 5 |
| `PyCad2026.Fix19.cs` | 10 |
| `PyCad2026.Geometry.cs` | 22 |
| `PyCad2026.Massive.cs` | 18 |
| `PyCad2026.Modify.cs` | 23 |
| `PyCad2026.Selection.cs` | 22 |
| `PythonLoader2026R.cs` | 1 |

## Famiglie API principali

- Core: `GetBuildMarker`, `Msg`, `GetPoint`, `GetShellTranscript`
- Commands/LISP: `RunCommand`, `RunCommands`, `RunCommandsNoiseFree`, `CommandSilent`, `RunLisp`, `RunLispQuiet`, `ExportShellTranscript`
- Geometry: `AddLine`, `AddCircle`, `AddArc`, `AddPoint`, `AddText`, `AddMText`, `AddLeader`
- Curves/polyline: `GetPointAtParameter`, `GetParameterAtPoint`, `GetSegmentType`, `GetBulgeAt`, `SetBulgeAt`
- Selection/transform: `SelectAll`, `SelectWindow`, `SelectCrossingWindow`, `MoveEntities`, `RotateEntities`, `ScaleEntities`
- DXF: `EntMake`, `GetEntityDxfValue`, `SetEntityDxfValue`, `GetSelectionByDxf`, `DebugDxfFilterMatch`
- Blocks/attributes: `InsertBlock`, `GetBlockAttributeDefinitions`, `SyncBlockReferenceAttributesBatch`, `ReplaceBlockReference`, `RenameBlockAttributeTag`
- Modify: `BreakCurveAtPoint`, `TrimCurveEndToEntity`, `ExtendCurveEndToEntity`, `BreakEntitiesAtIntersections`, `MatchEntityProperties`
- Database: `CreateNamedDictionary`, `SetNamedXRecord`, `GetNamedXRecord`, `SetEntityXRecord`, `CloneObjectsToOwner`, `ListNamedDictionaryTree`
- Advanced/massive: `BuildIntersectionsMatrix`, `CopyTransformBatch`, `ExportEntityAuditCsv`, `ExportDatabaseSnapshot`, `RunDeterministicModifyPack`
- FIX19+: `GetSpaceSummary`, `AddPaperViewportToSpace`, `ReplaceBlockReferencesByMap`, `ExportApiCompatibilityReport`

## Metodi aggiunti nei batch recenti

- command channel pulito: `RunCommandNoiseFree`, `RunCommandsNoiseFree`, `CommandSilent`, `FlushCommandChannel`
- layer/handle tools: `GetCurrentLayerName`, `SetCurrentLayerName`, `GetLayerState`, `SetLayerStatesBatch`, `GetObjectIdsByHandleStrings`, `GetHandleMap`

## Test collegati

- `tests/test_master_2026_fix4.py`
- `tests/test_batch_2026_fix19_megabatch.py`
- `tests/test_batch_2026_fix21_layer_layout.py`
- `tests/test_batch_2026_fix22_command_clean.py`
