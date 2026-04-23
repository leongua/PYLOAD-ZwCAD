# API Reference 2015 (x86)

Questa pagina è l’indice API per il bridge `PYLOAD` su ZWCAD+ 2015.

## Dove trovare l’elenco completo

L’elenco completo dei metodi esposti su `cad` è mantenuto qui:

- [README.md](./README.md) (sezione `Helper cad`)

Include già:

- comandi/shell/LISP
- geometria 2D/3D
- DXF-like (`EntMake`, `EntGet`, `EntMod`)
- filtri selezione DXF
- blocchi/attributi
- modify avanzato
- database avanzato (dizionari/XRecord/metadata)
- layer/stili/layout/documenti

## Mappa per modulo sorgente (`src/`)

- `PyCad.Core.cs`: core runtime, logging, prompt base
- `PyCad.Commands.cs`: command pipeline, sysvar, transcript, LISP
- `PyCad.Geometry.cs`, `PyCad.Curves.cs`, `PyCad.Arcs.cs`, `PyCad.Polylines.cs`: geometria/curve
- `PyCad.Dxf.cs`, `PyCad.Entmake.cs`: API DXF-like
- `PyCad.Blocks.cs`, `PyCad.BlocksBatch.cs`, `PyCad.Attributes.cs`: blocchi e attributi
- `PyCad.Modify.cs`, `PyCad.ModifyAdvanced.cs`, `PyCad.TransformsAdvanced.cs`: modifica e trasformazioni
- `PyCad.DatabaseAdvanced.cs`: dizionari, XRecord, extension dictionary, copy/delete helpers
- `PyCad.Selection.cs`: selezione e filtri
- `PyCad.Documents.cs`, `PyCad.Layouts.cs`, `PyCad.Layers.cs`: documenti/layout/layer
- `PyCad.TextStyles.cs`, `PyCad.DimStyles.cs`, `PyCad.Groups.cs`: stili e gruppi

## Nota

`2015` e `2026` sono due codebase separate: questa reference riguarda solo il ramo `2015` (32 bit).
