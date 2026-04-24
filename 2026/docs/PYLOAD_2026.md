# PYLOAD 2026 - Guida Operativa

Questa guida descrive solo il progetto `2026/`.

## Obiettivo

Eseguire script Python in ZWCAD 2026 con comando `PYLOAD2026R`, esponendo un oggetto `cad` con API per:

- comandi e shell transcript
- geometria e curve
- filtri DXF e selezioni
- blocchi/attributi
- modify batch
- database (dizionari/XRecord)

## Struttura

```text
2026/
├─ src/      # bridge C#
├─ tests/    # smoke e batch test Python
├─ docs/     # documentazione
├─ build.ps1
└─ PYLOAD.Rewrite2026.csproj
```

## Runtime

Quando uno script viene avviato dal loader, sono disponibili:

- `doc`
- `db`
- `ed`
- `cad`

## Helper `cad` (2026)

L'helper `cad` è l'API Python del bridge 2026.

Esempi rapidi:

```python
cad.Msg("[PY] start")
line_id = cad.AddLine(0, 0, 0, 100, 0, 0)
cad.RunCommandsNoiseFree(["_.REGEN"], True, True, 1)
```

```python
hits = cad.GetSelectionByDxf([{"code": 0, "value": "LINE"}])
cad.Msg("linee trovate: " + str(len(hits)))
```

```python
cad.SetNamedStringMap("PYLOAD/META", "RUN", {"batch": "fix22"})
meta = cad.GetNamedStringMap("PYLOAD/META", "RUN")
```

## Build e deploy

```powershell
cd 2026
.\build.ps1
```

Oppure:

```powershell
dotnet build .\2026\PYLOAD.Rewrite2026.csproj -c Release
```

Carica la DLL in ZWCAD con `NETLOAD` e poi esegui `PYLOAD2026R`.

## Test consigliati

- `tests/test_master_2026_fix4.py`
- `tests/test_batch_2026_fix19_megabatch.py`
- `tests/test_batch_2026_fix21_layer_layout.py`
- `tests/test_batch_2026_fix22_command_clean.py`

## API

Per la lista API del progetto 2026:

- helper `cad` tabellare: [`CAD_HELPER_2026.md`](./CAD_HELPER_2026.md)
- [`API_REFERENCE_2026.md`](./API_REFERENCE_2026.md)
