param(
    [string]$OutputDir = ".\\builds\\build_rewrite_2026"
)

$ErrorActionPreference = "Stop"
dotnet build "$PSScriptRoot\\PYLOAD.Rewrite2026.csproj" -c Release -o $OutputDir
