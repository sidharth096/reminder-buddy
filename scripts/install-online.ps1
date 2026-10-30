$ErrorActionPreference = 'Stop'
$repository = 'sidharth096/reminder-buddy'
$headers = @{ 'User-Agent' = 'ReminderBuddy-Installer' }
$temporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) ("ReminderBuddy-" + [Guid]::NewGuid().ToString('N'))

try {
    New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repository/releases/latest" -Headers $headers
    $setupAsset = $release.assets | Where-Object { $_.name -match '^ReminderBuddy-Setup-\d+\.\d+\.\d+\.exe$' } | Select-Object -First 1
    $checksumAsset = $release.assets | Where-Object { $_.name -eq 'SHA256SUMS.txt' } | Select-Object -First 1
    if (!$setupAsset -or !$checksumAsset) { throw 'The latest release does not contain the installer and checksum files.' }

    $setupPath = Join-Path $temporaryDirectory $setupAsset.name
    $checksumPath = Join-Path $temporaryDirectory 'SHA256SUMS.txt'
    Invoke-WebRequest -Uri $setupAsset.browser_download_url -Headers $headers -OutFile $setupPath
    Invoke-WebRequest -Uri $checksumAsset.browser_download_url -Headers $headers -OutFile $checksumPath

    $escapedName = [Regex]::Escape($setupAsset.name)
    $checksumLine = Get-Content -LiteralPath $checksumPath | Where-Object { $_ -match "^([a-fA-F0-9]{64})\s+$escapedName$" } | Select-Object -First 1
    if (!$checksumLine) { throw 'The installer checksum is missing from SHA256SUMS.txt.' }
    $expectedHash = ([Regex]::Match($checksumLine, '^[a-fA-F0-9]{64}')).Value
    $actualHash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
    if ($actualHash -ne $expectedHash) { throw 'Installer verification failed. The downloaded file was not opened.' }

    "Verified Reminder Buddy $($release.tag_name). Opening the installer..."
    $process = Start-Process -FilePath $setupPath -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "The installer exited with code $($process.ExitCode)." }
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) { Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue }
}
