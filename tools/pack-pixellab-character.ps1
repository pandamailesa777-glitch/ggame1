param(
    [Parameter(Mandatory=$true)][string]$ExportDirectory,
    [Parameter(Mandatory=$true)][string]$EntityId,
    [string]$OutputDirectory,
    [string]$StateName,
    [string]$AnimationName = 'move',
    [string]$OutputClip = 'move',
    [switch]$NoLoop,
    [int]$Fps = 7
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
if (-not $OutputDirectory) { $OutputDirectory = Split-Path -Parent $ExportDirectory }
$metadata = Get-Content -LiteralPath (Join-Path $ExportDirectory 'metadata.json') -Raw | ConvertFrom-Json
$state = if ($StateName) { $metadata.states | Where-Object { $_.name -eq $StateName -or $_.folder -eq $StateName } | Select-Object -First 1 } else { $metadata.states[0] }
if (-not $state) { throw "State '$StateName' not found in PixelLab export." }
$stateRoot = Join-Path $ExportDirectory $state.folder
$directions = @('east','south-east','south','south-west','west','north-west','north','north-east')

function Build-Sheet([string]$animation, [int]$frames, [string]$outputName) {
    $sheet = New-Object System.Drawing.Bitmap ($state.character.size.width*$frames),($state.character.size.height*8),([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($sheet)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    try {
        for ($row=0; $row -lt $directions.Count; $row++) {
            $direction=$directions[$row]
            for ($frame=0; $frame -lt $frames; $frame++) {
                if ($animation -eq 'idle') {
                    $path=Join-Path $stateRoot ("rotations\$direction.png")
                } else {
                    $path=Join-Path $stateRoot ("animations\$animation\$direction\frame_{0:D3}.png" -f $frame)
                }
                $image=[System.Drawing.Image]::FromFile($path)
                try { $graphics.DrawImageUnscaled($image,$frame*$state.character.size.width,$row*$state.character.size.height) }
                finally { $image.Dispose() }
            }
        }
        $out=Join-Path $OutputDirectory $outputName
        $sheet.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "Packed $animation -> $out"
    } finally { $graphics.Dispose(); $sheet.Dispose() }
}

Build-Sheet 'idle' 1 ("${EntityId}__idle__8dir__1fps__loop.png")
$move = $state.frames.animations.$AnimationName
if ($move) {
    $frameCount = @($move.south).Count
    if ($frameCount -lt 1) { throw 'PixelLab export contains an empty move animation.' }
    $suffix = if ($NoLoop) { 'once' } else { 'loop' }
    Build-Sheet $AnimationName $frameCount ("${EntityId}__${OutputClip}__8dir__${Fps}fps__${suffix}.png")
}
