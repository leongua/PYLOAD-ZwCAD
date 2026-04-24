# PYLOAD 2015 (x86)

Bridge C# + IronPython per esecuzione script Python in **ZWCAD+ 2015 x86**.

## Requisiti

- ZWCAD+ 2015 x86
- .NET Framework 4.6.2
- DLL:
  - `C:\Program Files (x86)\ZWCAD+ 2015\ZwManaged.dll`
  - `C:\Program Files (x86)\ZWCAD+ 2015\ZwDatabaseMgd.dll`

## Build

```powershell
cd 2015
.\build.ps1
```

oppure:

```powershell
dotnet build .\2015\PYLOAD.csproj -c Release
```

## Comando ZWCAD

- `PYLOAD`

## Documentazione

- guida operativa: [`docs/README.md`](./docs/README.md)
- lista API: [`docs/API_REFERENCE_2015.md`](./docs/API_REFERENCE_2015.md)
