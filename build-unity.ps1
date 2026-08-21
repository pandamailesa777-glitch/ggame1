param(
    [ValidateSet('Windows','Android')][string]$Target = 'Windows'
)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$project=Join-Path $root 'UnityProject'
$unity='C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe'
if(-not (Test-Path -LiteralPath $unity)){throw "Unity 6000.5.9f1 not found: $unity"}
if($Target -eq 'Android'){
    $android=Split-Path $unity -Parent | Join-Path -ChildPath 'Data\PlaybackEngines\AndroidPlayer'
    if(-not (Test-Path -LiteralPath $android)){throw 'Unity Android Build Support is not installed yet. Add Android, SDK/NDK and OpenJDK modules in Unity Hub.'}
    $output=Join-Path $project 'Builds\Android\NightfallUnity.apk'
    New-Item -ItemType Directory -Path (Split-Path $output -Parent) -Force | Out-Null
    if(Test-Path -LiteralPath $output){Remove-Item -LiteralPath $output -Force}
    $process=Start-Process -FilePath $unity -ArgumentList @('-batchmode','-quit','-projectPath',$project,'-buildTarget','Android','-executeMethod','Nightfall.UnityMvp.Editor.NightfallAndroidBuild.Build','-logFile',(Join-Path $project 'Logs\build-android.log')) -Wait -PassThru -WindowStyle Hidden
}else{
    $output=Join-Path $project 'Builds\Windows\NightfallUnity.exe'
    New-Item -ItemType Directory -Path (Split-Path $output -Parent) -Force | Out-Null
    $process=Start-Process -FilePath $unity -ArgumentList @('-batchmode','-quit','-projectPath',$project,'-buildWindows64Player',$output,'-logFile',(Join-Path $project 'Logs\build-windows.log')) -Wait -PassThru -WindowStyle Hidden
}
if($process.ExitCode -ne 0){throw "Unity exited with code $($process.ExitCode). See UnityProject\Logs for details."}
if(-not (Test-Path -LiteralPath $output)){throw "Build output was not created: $output"}
Write-Host "Unity build ready: $output"
