# PYLOAD 2026 Rewrite

<div align="center">

[![ZWCAD](https://img.shields.io/badge/ZWCAD-2026-0a7f5a)](https://www.zwsoft.com/)
[![Platform](https://img.shields.io/badge/platform-x64-blue)](https://www.zwsoft.com/)
[![.NET](https://img.shields.io/badge/.NET-4.7-purple)](https://dotnet.microsoft.com/)
[![IronPython](https://img.shields.io/badge/IronPython-3.4.1-6a5acd)](https://ironpython.net/)

Ramo **riscritto da zero** per ZWCAD 2026, separato dal bridge storico 2015.

</div>

---

## Visione

`2026/` non e un semplice porting del vecchio progetto.
E una base nuova, piu pulita, pensata per:

- seguire piu da vicino la documentazione ZWCAD 2026
- ridurre il peso dei workaround storici del ramo 2015
- crescere come bridge principale per il runtime moderno

---

## Cosa contiene

```text
2026/
├─ docs/
│  └─ PYLOAD_2026.md
├─ src/
│  ├─ PyCad2026.Core.cs
│  ├─ PyCad2026.Geometry.cs
│  ├─ PyCad2026.Dxf.cs
│  ├─ PyCad2026.Blocks.cs
│  ├─ PyCad2026.Modify.cs
│  ├─ PyCad2026.Database.cs
│  └─ PythonLoader2026R.cs
├─ tests/
│  └─ test_master_2026_fix4.py
├─ build.ps1
└─ PYLOAD.Rewrite2026.csproj
```

---

## Stato del ramo

Questo ramo e gia stato validato con uno smoke test master unico su ZWCAD 2026 reale.

Settori gia coperti:

- editor / command line / transcript
- geometria base
- DXF-like API
- blocchi e attributi
- modify
- database avanzato

Entrypoint ZWCAD:

- `PYLOAD2026R`

---

## Build

### Requisiti

- **ZWCAD 2026 x64**
- **.NET Framework 4.7**
- DLL installate in:
  - `C:\Program Files\ZWSOFT\ZWCAD 2026\ZwManaged.dll`
  - `C:\Program Files\ZWSOFT\ZWCAD 2026\ZwDatabaseMgd.dll`

### Build rapida

```powershell
cd 2026
.\build.ps1
```

Oppure:

```powershell
dotnet build .\2026\PYLOAD.Rewrite2026.csproj -c Release
```

Output tipico:

- `bin\Release\net47\PYLOAD2026R.dll`

Package runtime:

- `dist\PYLOAD2026R-runtime\`

---

## Test rapido

Lo smoke test principale del ramo e:

- [`tests/test_master_2026_fix4.py`](./tests/test_master_2026_fix4.py)

Flusso consigliato:

1. `NETLOAD` della DLL `PYLOAD2026R.dll`
2. lancia `PYLOAD2026R`
3. esegui `tests/test_master_2026_fix4.py`

Questo test verifica in un colpo:

- commands / lisp
- geometry
- dxf
- blocks
- modify
- database

---

## Differenza rispetto al 2015

Il ramo `2026/` va considerato come:

- **base attiva per il futuro**
- **progetto separato**
- **rewrite mantenuto indipendente**

Il ramo `2015/` invece resta:

- piu ampio come copertura storica
- utile per ambienti legacy
- riferimento comportamentale, non base architetturale del rewrite

---

## Documentazione

Per il contesto tecnico completo:

- guida specifica del ramo: [`docs/PYLOAD_2026.md`](./docs/PYLOAD_2026.md)

Quella guida spiega:

- come usare la documentazione ZWCAD 2026
- quali settori del bridge sono gia coerenti
- quali aree conviene ampliare dopo

---

## Obiettivo pratico

La direzione consigliata per questo ramo e semplice:

- mantenere `2026/` piccolo, leggibile e solido
- aggiungere funzionalita solo dopo test reali in ZWCAD
- evitare di trascinare dentro automaticamente tutta la complessita del 2015

In breve:

**meno compatibilita implicita, piu comportamento verificato.**
