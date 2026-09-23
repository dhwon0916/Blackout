param([string]$Version = '1.0.1')
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a version like 1.0.0.' }
$projectRoot = $PSScriptRoot
$publishPath = Join-Path $projectRoot 'artifacts\publish\win-x64'
$packagePath = Join-Path $projectRoot "artifacts\Blackout-$Version-win-x64.zip"
dotnet publish (Join-Path $projectRoot 'Blackout.csproj') -c Release -r win-x64 --self-contained true -o $publishPath -p:DebugType=None -p:DebugSymbols=false "-p:Version=$Version"
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
$testProcess = Start-Process -FilePath (Join-Path $publishPath 'Blackout.exe') -ArgumentList '--self-test' -Wait -PassThru -WindowStyle Hidden
Get-Content (Join-Path $publishPath 'self-test-results.txt')
if ($testProcess.ExitCode -ne 0) { throw 'Self-tests failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $publishPath
Copy-Item -LiteralPath (Join-Path $projectRoot 'RELEASE_NOTES.md') -Destination $publishPath
$packageFiles = Get-ChildItem -LiteralPath $publishPath -File | Where-Object Name -ne 'self-test-results.txt'
Compress-Archive -LiteralPath $packageFiles.FullName -DestinationPath $packagePath -Force
$hash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($packagePath))" | Set-Content -LiteralPath "$packagePath.sha256" -Encoding ascii
Write-Output "Release package: $packagePath"
