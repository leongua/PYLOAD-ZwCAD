# API Reference 2015

Indice API del progetto `2015/` (ZWCAD+ 2015 x86).

## Totale API pubbliche

- **459** metodi pubblici nel bridge (cartella `2015/src`).

## API per modulo (`2015/src`)

| File | Metodi pubblici |
|---|---:|
| `PyCad.Analysis.cs` | 14 |
| `PyCad.Arcs.cs` | 15 |
| `PyCad.Attributes.cs` | 10 |
| `PyCad.Blocks.cs` | 20 |
| `PyCad.BlocksBatch.cs` | 8 |
| `PyCad.Collections.cs` | 11 |
| `PyCad.Commands.cs` | 33 |
| `PyCad.Core.cs` | 11 |
| `PyCad.Curves.cs` | 37 |
| `PyCad.DatabaseAdvanced.cs` | 33 |
| `PyCad.DimStyles.cs` | 5 |
| `PyCad.Documents.cs` | 6 |
| `PyCad.Dxf.cs` | 6 |
| `PyCad.Entities.cs` | 16 |
| `PyCad.Entmake.cs` | 31 |
| `PyCad.Geometry.cs` | 19 |
| `PyCad.Groups.cs` | 6 |
| `PyCad.Layers.cs` | 20 |
| `PyCad.Layouts.cs` | 2 |
| `PyCad.Modify.cs` | 20 |
| `PyCad.ModifyAdvanced.cs` | 21 |
| `PyCad.Polylines.cs` | 37 |
| `PyCad.Regions.cs` | 6 |
| `PyCad.Reporting.cs` | 5 |
| `PyCad.Selection.cs` | 32 |
| `PyCad.TextStyles.cs` | 5 |
| `PyCad.ThreeD.cs` | 5 |
| `PyCad.TransformsAdvanced.cs` | 4 |
| `PyCad.TwoDLowLevel.cs` | 22 |
| `PythonLoader.cs` | 1 |

## Famiglie API principali

- Comandi/LISP/shell: `RunCommand`, `RunCommands`, `Command`, `RunLisp`, `CallLisp`, `ExportShellTranscript`
- Geometria: `AddLine`, `AddCircle`, `AddArc`, `AddPolyline`, `AddSpline`, `AddMText`
- Curva e parametri: `GetPointAtParameter`, `GetParameterAtPoint`, `GetParameterAtDistance`
- DXF-like: `EntMake`, `EntGet`, `EntGetMap`, `EntMod`, `GetEntityDxfValue`, `SetEntityDxfValue`
- Selezione: `GetSelectionByDxf`, `GetSelectionByArea`, `GetSelectionByLength`, `SelectWindowByDxf`
- Blocchi/attributi: `InsertBlock`, `GetBlockAttributes`, `SyncBlockReferenceAttributesBatch`, `UpdateBlockAttributesByMapBatch`, `ReplaceBlockReference`
- Modify: `BreakCurveAtPoint`, `TrimCurvesToBoundaries`, `ExtendCurvesToBoundaries`, `JoinEntities`, `FilletLines`, `ChamferLines`
- Database avanzato: `CreateNamedDictionary`, `SetNamedXRecord`, `GetNamedXRecord`, `SetEntityXRecord`, `ListNamedDictionaryTree`
- Layer/layout/stili: `EnsureLayer`, `SetCurrentLayer`, `GetLayoutNames`, `CreateTextStyle`, `CreateDimensionStyle`

## Lista completa metodi

La lista estesa metodo-per-metodo è mantenuta in:

- [`README.md`](./README.md) sezione **Helper `cad`**
