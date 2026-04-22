param(
    [string]$OutputDir = ".\\builds\\build_2015"
)

$ErrorActionPreference = "Stop"
dotnet build "$PSScriptRoot\\PYLOAD.csproj" -c Release -o $OutputDir
