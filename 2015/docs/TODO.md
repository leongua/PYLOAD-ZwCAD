# PYLOAD Roadmap

Roadmap pratica per completare il bridge C# -> IronPython su ZWCAD 2015.

## Fase 1 - Runtime base

- comando `PYLOAD`
- scelta script via path o dialog
- traceback pulito
- scope Python standard (`doc`, `db`, `ed`, `cad`, `script_path`, `script_dir`)
- README iniziale

## Fase 2 - Geometria base

- `AddLine`
- `AddCircle`
- `AddArc`
- `AddPoint`
- `AddText`
- `AddPolyline`
- `AddPolyline3d`

## Fase 3 - Input utente

- `GetPoint`
- `GetString`
- `GetDouble`
- `GetInteger`
- `GetKeyword`
- prompt piu evoluti con default/base point

## Fase 4 - Layer e proprieta

- `LayerExists`
- `EnsureLayer`
- `GetCurrentLayer`
- `SetCurrentLayer`
- `SetEntityColor`
- `SetEntityLayer`
- lineweight / linetype / transparency

## Fase 5 - Selezione e lettura

- `SelectEntity`
- `SelectAll`
- filtri per layer/tipo
- `GetEntityInfo`
- lettura proprieta tipizzate

## Fase 6 - Blocchi e attributi

- `BlockExists`
- `GetBlockNames`
- `InsertBlock`
- lettura attributi
- scrittura attributi
- gestione scala/rotazione/layer

## Fase 7 - Annotazione

- `MText`
- quote
- leader / multileader se disponibili
- stili testo

## Fase 8 - Comandi nativi

- `RunCommand`
- helper dedicati (`ZoomExtents`, `RegenNative`, ecc.)
- controllo echo / async

## Fase 9 - Packaging script

- cartella `Scripts`
- import da `script_dir`
- moduli condivisi
- template script

## Fase 10 - Test e doc

- `test.py`
- `test_geometry.py`
- `test_layers.py`
- `test_selection.py`
- `test_blocks.py`
- cookbook e guida sviluppatore

## Modalita di lavoro consigliata

1. scegliere una fase
2. implementare il minimo utile
3. compilare
4. aggiornare README/TODO
5. aggiungere test dedicato
