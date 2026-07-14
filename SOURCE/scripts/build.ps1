[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pluginProject = Join-Path $repositoryRoot 'src\TruthBar.IL2CPP\TruthBar.IL2CPP.csproj'
$testProject = Join-Path $repositoryRoot 'tests\TruthBar.Tests\TruthBar.Tests.csproj'
$launcherProject = Join-Path $repositoryRoot 'src\TruthBar.Launcher\TruthBar.Launcher.csproj'
$buildRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.build'))
$publishDirectory = Join-Path $repositoryRoot '.build\launcher-publish'
$publishedLauncher = Join-Path $publishDirectory 'RiverRunTruthBar.exe'
$rootLauncher = Join-Path $repositoryRoot 'RiverRunTruthBar.exe'
$stagedLauncher = Join-Path $repositoryRoot 'RiverRunTruthBar.exe.new'

Push-Location $repositoryRoot
try {
    dotnet build $pluginProject -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }

    dotnet run --project $testProject -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Automated tests failed.' }

    $resolvedPublishDirectory = [IO.Path]::GetFullPath($publishDirectory)
    $requiredPrefix = $buildRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (!$resolvedPublishDirectory.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Publish directory escaped the approved build root: $resolvedPublishDirectory"
    }

    if (Test-Path -LiteralPath $resolvedPublishDirectory) {
        Remove-Item -LiteralPath $resolvedPublishDirectory -Recurse -Force
    }

    dotnet publish $launcherProject -c $Configuration -o $publishDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Launcher publish failed.' }

    if (!(Test-Path -LiteralPath $publishedLauncher -PathType Leaf)) {
        throw "Published launcher is missing: $publishedLauncher"
    }

    Copy-Item -LiteralPath $publishedLauncher -Destination $stagedLauncher -Force
    $publishedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $publishedLauncher).Hash
    $stagedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $stagedLauncher).Hash
    if ($publishedHash -ne $stagedHash) {
        throw 'Staged launcher hash does not match the published launcher.'
    }

    Move-Item -LiteralPath $stagedLauncher -Destination $rootLauncher -Force
    Write-Host "Built RiverRunTruthBar: $rootLauncher"
    Write-Host "SHA-256: $publishedHash"
}
finally {
    if (Test-Path -LiteralPath $stagedLauncher) {
        Remove-Item -LiteralPath $stagedLauncher -Force
    }

    Pop-Location
}
