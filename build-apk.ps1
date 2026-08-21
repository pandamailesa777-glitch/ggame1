$ErrorActionPreference = 'Stop'
$sdk = 'C:\Program Files (x86)\Android\android-sdk'
$bt = "$sdk\build-tools\36.0.0"
$java = 'C:\Program Files\Android\openjdk\jdk-21.0.8\bin'
$env:JAVA_HOME = 'C:\Program Files\Android\openjdk\jdk-21.0.8'
$env:Path = "$java;$env:Path"

New-Item -ItemType Directory -Force build\compiled, build\gen, build\classes, build\dex | Out-Null
& "$bt\aapt2.exe" compile --dir app\src\main\res -o build\res.zip
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& "$bt\aapt2.exe" link -o build\unsigned.apk -I "$sdk\platforms\android-36\android.jar" --manifest app\src\main\AndroidManifest.xml --java build\gen build\res.zip --min-sdk-version 26 --target-sdk-version 36
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$unsignedApk = (Resolve-Path build\unsigned.apk).Path
Push-Location app\src\main
& "$bt\aapt.exe" add $unsignedApk 'assets/sprite_factory/entities.json' 'assets/sprite_factory/generated_entities.json'
$assetExit = $LASTEXITCODE
Pop-Location
if ($assetExit) { exit $assetExit }
& "$java\javac.exe" -source 8 -target 8 -encoding UTF-8 -classpath "$sdk\platforms\android-36\android.jar" -d build\classes app\src\main\java\com\nightfall\protocol\SpriteSystem.java app\src\main\java\com\nightfall\protocol\GameActivity.java build\gen\com\nightfall\protocol\R.java
if ($LASTEXITCODE) { exit $LASTEXITCODE }
& "$bt\d8.bat" --lib "$sdk\platforms\android-36\android.jar" --min-api 26 --output build\dex build\classes\com\nightfall\protocol\*.class
if ($LASTEXITCODE) { exit $LASTEXITCODE }
Copy-Item -LiteralPath build\dex\classes.dex -Destination build\classes.dex -Force
Push-Location build
& "$bt\aapt.exe" add unsigned.apk classes.dex
$aaptExit = $LASTEXITCODE
Pop-Location
if ($aaptExit) { exit $aaptExit }
& "$bt\zipalign.exe" -f 4 build\unsigned.apk build\aligned.apk
if (!(Test-Path build\debug.keystore)) {
    & "$java\keytool.exe" -genkeypair -keystore build\debug.keystore -storepass android -alias androiddebugkey -keypass android -dname 'CN=Android Debug,O=Android,C=US' -keyalg RSA -keysize 2048 -validity 10000
}
& "$bt\apksigner.bat" sign --ks build\debug.keystore --ks-pass pass:android --key-pass pass:android --out build\NightfallProtocol-debug.apk build\aligned.apk
& "$bt\apksigner.bat" verify --verbose build\NightfallProtocol-debug.apk
