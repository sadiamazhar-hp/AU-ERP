# Fixes common Windows Gradle cache/lock issues, then builds the Android app.
# Usage: powershell -ExecutionPolicy Bypass -File scripts/android-build.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$AndroidDir = Join-Path $ProjectRoot "android"
$SubstDrive = "A:"
$SubstPath = $ProjectRoot

Write-Host "==> Stopping Gradle/Java processes..."
Get-Process -Name java -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
if (Test-Path (Join-Path $AndroidDir "gradlew.bat")) {
  Push-Location $AndroidDir
  .\gradlew.bat --stop 2>$null | Out-Null
  Pop-Location
}
Start-Sleep -Seconds 2

Write-Host "==> Clearing corrupted Gradle caches..."
Remove-Item -Recurse -Force "$env:USERPROFILE\.gradle\caches\transforms-4" -ErrorAction SilentlyContinue

$AccessorsDir = Join-Path $AndroidDir ".gradle\8.6\dependencies-accessors"
$TargetName = "423f0288fa7dffe069445ffa4b72952b4629a15a"
$TargetPath = Join-Path $AccessorsDir $TargetName

if (Test-Path $AccessorsDir) {
  if (-not (Test-Path $TargetPath)) {
    $temp = Get-ChildItem $AccessorsDir -Directory -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -like "$TargetName-*" } |
      Select-Object -First 1
    if ($temp) {
      Write-Host "==> Repairing dependencies-accessors workspace..."
      Move-Item -LiteralPath $temp.FullName -Destination $TargetPath -Force
    }
  }
  Get-ChildItem $AccessorsDir -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "$TargetName-*" } |
    ForEach-Object { Remove-Item -Recurse -Force $_.FullName -ErrorAction SilentlyContinue }
}

Write-Host "==> Mapping subst drive $SubstDrive -> $SubstPath"
subst $SubstDrive /d 2>$null | Out-Null
cmd /c "subst $SubstDrive `"$SubstPath`"" | Out-Null
$BuildDir = "$SubstDrive\android"

try {
  Write-Host "==> Building (this may take several minutes on first run)..."
  Push-Location $BuildDir
  .\gradlew.bat app:installDebug -PreactNativeDevServerPort=8081
  if ($LASTEXITCODE -ne 0) {
    # Retry once after accessors repair if Gradle failed on move
    $accessorsOnSubst = Join-Path $BuildDir ".gradle\8.6\dependencies-accessors"
    $targetOnSubst = Join-Path $accessorsOnSubst $TargetName
    if (-not (Test-Path $targetOnSubst)) {
      $temp = Get-ChildItem $accessorsOnSubst -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "$TargetName-*" } |
        Select-Object -First 1
      if ($temp) {
        Write-Host "==> Retrying after accessors repair..."
        Move-Item -LiteralPath $temp.FullName -Destination $targetOnSubst -Force
        .\gradlew.bat app:installDebug -PreactNativeDevServerPort=8081
      }
    }
  }
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  Write-Host "==> Build succeeded."
}
finally {
  Pop-Location
  subst $SubstDrive /d 2>$null | Out-Null
}
