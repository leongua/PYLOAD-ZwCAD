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

## Come usare il repository

Se lavori su ambienti storici:

- entra in [`2015/`](./2015)

Se lavori su ZWCAD 2026:

- entra in [`2026/`](./2026)

## Note

- la cartella `2015/` è mantenuta separata e non deve essere trattata come fallback del ramo 2026
- la cartella `2026/` è una base separata, testata in modo indipendente
- i file `tests/` servono come smoke/regression suite rapida durante lo sviluppo del bridge
