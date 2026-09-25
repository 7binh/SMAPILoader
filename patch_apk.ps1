$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$baseDir = $PSScriptRoot
$apkFile = Join-Path $baseDir "SMAPIGameLoader\bin\Release\net9.0-android\abc.smapi.gameloader-Signed.apk"
$customSo = Join-Path $baseDir "SharedLibs\native\arm64-v8a\libmonosgen-2.0.so"
$tempUnsigned = Join-Path $baseDir "SMAPIGameLoader\bin\Release\net9.0-android\temp-unsigned.apk"
$alignedApk = Join-Path $baseDir "SMAPIGameLoader\bin\Release\net9.0-android\temp-aligned.apk"
$finalApk = Join-Path $baseDir "SMAPIGameLoader\bin\Release\net9.0-android\abc.smapi.gameloader-Signed.apk"
$keystore = Join-Path $baseDir "SMAPIGameLoader\debug.keystore"

# Locate Android build tools (zipalign & apksigner)
$sdkRoots = @(
    $env:ANDROID_HOME,
    $env:ANDROID_SDK_ROOT,
    "$env:LOCALAPPDATA\Android\Sdk",
    "C:\Users\sevenguyen\AppData\Local\Android\Sdk"
) | Where-Object { !([string]::IsNullOrEmpty($_)) -and (Test-Path $_) }

$zipalign = $null
$apksigner = $null

foreach ($root in $sdkRoots) {
    $buildToolsDir = Join-Path $root "build-tools"
    if (Test-Path $buildToolsDir) {
        $versions = Get-ChildItem -Directory $buildToolsDir | Sort-Object Name -Descending
        foreach ($v in $versions) {
            $za = Join-Path $v.FullName "zipalign.exe"
            $as = Join-Path $v.FullName "apksigner.bat"
            if ((Test-Path $za) -and (Test-Path $as)) {
                $zipalign = $za
                $apksigner = $as
                break
            }
        }
    }
    if ($zipalign) { break }
}

if (-not $zipalign -or -not $apksigner) {
    throw "Could not find zipalign and apksigner in Android SDK build-tools."
}

Write-Host "1. Copying APK to temporary file..."
Copy-Item $apkFile $tempUnsigned -Force

Write-Host "2. Updating libmonosgen-2.0.so inside APK..."
$zip = [System.IO.Compression.ZipFile]::Open($tempUnsigned, [System.IO.Compression.ZipArchiveMode]::Update)

$entry = $zip.GetEntry("lib/arm64-v8a/libmonosgen-2.0.so")
if ($entry) {
    Write-Host "   Deleted old stock libmonosgen-2.0.so"
    $entry.Delete()
}

Write-Host "   Adding custom patched libmonosgen-2.0.so..."
[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $customSo, "lib/arm64-v8a/libmonosgen-2.0.so", [System.IO.Compression.CompressionLevel]::Fastest)

Write-Host "   Removing old signatures from META-INF..."
$sigEntries = @($zip.Entries | Where-Object { $_.FullName -like "META-INF/*.SF" -or $_.FullName -like "META-INF/*.RSA" -or $_.FullName -like "META-INF/*.MF" })
foreach ($s in $sigEntries) {
    $s.Delete()
}

$zip.Dispose()
Write-Host "   Done modifying APK archive."

Write-Host "3. Running zipalign..."
if (Test-Path $alignedApk) { Remove-Item $alignedApk -Force }
& $zipalign -p -f 4 $tempUnsigned $alignedApk
if ($LASTEXITCODE -ne 0) { throw "zipalign failed with exit code $LASTEXITCODE" }

Write-Host "4. Signing APK with apksigner..."
& $apksigner sign --ks $keystore --ks-pass pass:android --ks-key-alias androiddebugkey --key-pass pass:android --out $finalApk $alignedApk
if ($LASTEXITCODE -ne 0) { throw "apksigner failed with exit code $LASTEXITCODE" }

Write-Host "5. Verifying signed APK..."
& $apksigner verify -v $finalApk

Write-Host "6. Cleaning up temporary files..."
if (Test-Path $tempUnsigned) { Remove-Item $tempUnsigned -Force }
if (Test-Path $alignedApk) { Remove-Item $alignedApk -Force }

Write-Host "SUCCESS! APK successfully patched with custom libmonosgen-2.0.so."
