# PYLOAD 2026 (x64)

Bridge C# + IronPython per esecuzione script Python in **ZWCAD 2026 x64**.

## Requisiti

- ZWCAD 2026 x64
- .NET Framework 4.7
- DLL:
  - `C:\Program Files\ZWSOFT\ZWCAD 2026\ZwManaged.dll`
  - `C:\Program Files\ZWSOFT\ZWCAD 2026\ZwDatabaseMgd.dll`

## Build

```powershell
cd 2026
.\build.ps1
```

oppure:

```powershell
dotnet build .\2026\PYLOAD.Rewrite2026.csproj -c Release
```

## Comando ZWCAD

- `PYLOAD2026R`

## Documentazione

- guida operativa: [`docs/PYLOAD_2026.md`](./docs/PYLOAD_2026.md)
- helper `cad`: [`docs/CAD_HELPER_2026.md`](./docs/CAD_HELPER_2026.md)
- lista API: [`docs/API_REFERENCE_2026.md`](./docs/API_REFERENCE_2026.md)
