param(
    [string]$SourceDirectory,
    [string]$ArtRoot,
    [switch]$AllowHeroOverwrite
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
if (-not $ArtRoot) { $ArtRoot = Join-Path $root 'art\entities' }
$drawable = Join-Path $root 'app\src\main\res\drawable-nodpi'
$unityResources = Join-Path $root 'UnityProject\Assets\Resources\Art\Generated'
$manifestPath = Join-Path $root 'app\src\main\assets\sprite_factory\generated_entities.json'
$pattern = '^(?<entity>[a-z0-9_]+)__(?<animation>[a-z0-9_]+)__(?<dirs>1|4|8)dir__(?<fps>\d+)fps__(?<loop>loop|once)\.png$'
$protectedHeroes = @('hero_amelia','hero_sam','hero_zike')
New-Item -ItemType Directory -Path $unityResources -Force | Out-Null

if ($SourceDirectory) {
    $files = @(Get-ChildItem -LiteralPath $SourceDirectory -Filter '*.png' -File)
} elseif (Test-Path -LiteralPath $ArtRoot) {
    $files = @(Get-ChildItem -LiteralPath $ArtRoot -Filter '*.png' -File -Recurse | Where-Object { $_.Directory.Name -eq 'Source' })
} else {
    $files = @()
}

$entities = [ordered]@{}
foreach ($file in $files) {
    if ($file.Name -notmatch $pattern) { Write-Warning "Skipped (bad name): $($file.FullName)"; continue }
    $entity=$Matches.entity; $animation=$Matches.animation; $dirs=[int]$Matches.dirs; $fps=[int]$Matches.fps; $loop=$Matches.loop -eq 'loop'
    if ($protectedHeroes -contains $entity -and -not $AllowHeroOverwrite) {
        throw "Protected hero '$entity' cannot be imported without -AllowHeroOverwrite."
    }
    $image=[System.Drawing.Image]::FromFile($file.FullName)
    try {
        if ($image.Height % $dirs -ne 0) { throw "$($file.Name): height must be divisible by direction count" }
        $cell=[int]($image.Height/$dirs)
        if ($image.Width % $cell -ne 0) { throw "$($file.Name): width must be a multiple of square frame size $cell" }
        $frames=[int]($image.Width/$cell)
    } finally { $image.Dispose() }

    $resource=($entity+'_'+$animation+'_'+$dirs+'dir').ToLowerInvariant()
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $drawable ($resource+'.png')) -Force
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $unityResources ($resource+'.png')) -Force
    if (-not $entities.Contains($entity)) {
        $definitionPath = Join-Path $file.Directory.Parent.FullName 'entity.json'
        $meta = if (Test-Path -LiteralPath $definitionPath) { Get-Content -LiteralPath $definitionPath -Raw | ConvertFrom-Json } else { $null }
        $entities[$entity]=[ordered]@{
            id=$entity
            entityType=if ($meta.entityType) { $meta.entityType } else { 'enemy' }
            scale=if ($null -ne $meta.scale) { [double]$meta.scale } else { 1.0 }
            pivotX=if ($null -ne $meta.pivotX) { [double]$meta.pivotX } else { .5 }
            pivotY=if ($null -ne $meta.pivotY) { [double]$meta.pivotY } else { .9 }
            sortOffset=if ($null -ne $meta.sortOffset) { [int]$meta.sortOffset } else { 0 }
            fallbackColor=if ($meta.fallbackColor) { $meta.fallbackColor } else { '#ff00ff' }
            animations=@()
        }
    }
    $entities[$entity].animations += [ordered]@{id=$animation;sheet=$resource;layout='direction_rows';columns=$frames;rows=$dirs;frames=$frames;directions=$dirs;fps=$fps;loop=$loop}
}

$out=[ordered]@{schemaVersion=1;entities=@($entities.Values)} | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($manifestPath,$out,(New-Object System.Text.UTF8Encoding($false)))
Write-Host "Imported $($files.Count) sheets for $($entities.Count) entities -> $manifestPath"
