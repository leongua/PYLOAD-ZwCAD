# CAD Helper 2026 (`cad`)

Catalogo operativo del bridge esposto negli script `PYLOAD2026R`.

## Oggetti disponibili nello script

| Oggetto | Ruolo |
|---|---|
| `cad` | helper API C# |
| `doc` | documento attivo |
| `db` | database DWG attivo |
| `ed` | editor/command line |

## Moduli helper caricati (sorgente reale)

| Modulo | Focus |
|---|---|
| `PyCad2026.Core.cs` | runtime base, transcript, marker build |
| `PyCad2026.Commands.cs` | command pipeline, LISP, sysvar |
| `PyCad2026.Geometry.cs` | creazione entita 2D base, testo, hatch, leader |
| `PyCad2026.Curves.cs` | info e modifica curve/polilinee |
| `PyCad2026.Selection.cs` | selezione e trasformazioni |
| `PyCad2026.Dxf.cs` | EntMake + read/write DXF + filtri |
| `PyCad2026.Blocks.cs` | blocchi e attributi |
| `PyCad2026.Modify.cs` | trim/extend/break/join/offset/match |
| `PyCad2026.Database.cs` | dictionary/XRecord/extension dictionary/clone |
| `PyCad2026.Advanced.cs` | regioni, solidi 3D, view/UCS, batch avanzati |
| `PyCad2026.Massive.cs` | batch massivi, report/export, deterministic packs |
| `PyCad2026.Fix19.cs` | paperspace viewport, replace blocchi, compat report |

## Quick start minimale

```python
p = cad.GetPoint("Punto base:")
if p and p.Status == 5100:  # PromptStatus.OK
    x, y, z = p.Value.X, p.Value.Y, p.Value.Z
    line_id = cad.AddLine(x, y, z, x + 100, y, z)
    txt_id = cad.AddText("PYLOAD2026R", x, y + 10, z, 2.5)
    cad.RunCommand("_.REGEN")
```

## Reference completa

Per firme complete, variabili e parametri di tutte le API pubbliche:

- [`API_REFERENCE_2026.md`](./API_REFERENCE_2026.md)
