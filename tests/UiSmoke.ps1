$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$exe = Join-Path $PSScriptRoot '..\dist\ReminderBuddy.exe'
$process = Start-Process -FilePath $exe -ArgumentList '--smoke-test' -PassThru
try {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $process.Id)
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $settings = $null
    $overlay = $null
    for ($i = 0; $i -lt 30; $i++) {
        $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $condition)
        foreach ($window in $windows) {
            if ($window.Current.Name -eq 'Reminder Buddy') { $settings = $window }
            if ($window.Current.Name -eq 'Reminder Buddy reminder') { $overlay = $window }
        }
        if ($settings -and $overlay) { break }
        Start-Sleep -Milliseconds 100
    }
    if (!$settings -or !$overlay) { throw 'Settings or overlay window did not appear.' }
    'PASS: Real settings and overlay windows opened'
    foreach ($character in @('Buddy', 'Abu', 'Shibu', 'Tom')) {
        $characterCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $character)
        if (!$settings.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $characterCondition)) { throw "Missing character choice: $character" }
    }
    'PASS: All four character choices are accessible'
    $buttonCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    $buttons = $overlay.FindAll([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
    $names = @($buttons | ForEach-Object { $_.Current.Name })
    foreach ($name in @('OK', 'Snooze', 'Close')) {
        if ($names -notcontains $name) { throw "Missing overlay action: $name" }
    }
    'PASS: OK, Snooze and Close are accessible buttons'
    $ok = $buttons | Where-Object { $_.Current.Name -eq 'OK' }
    $ok.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds 200
    $previewCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Preview animation')
    $preview = $settings.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $previewCondition)
    if (!$preview) { throw 'Preview action missing.' }
    $preview.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds 300
    $windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $condition)
    if (!(@($windows | Where-Object { $_.Current.Name -eq 'Reminder Buddy reminder' }).Count)) { throw 'Preview did not reopen overlay.' }
    'PASS: Dismiss and preview actions work'
    if (!$process.WaitForExit(15000)) { throw 'Smoke application did not exit.' }
    if ($process.ExitCode -ne 0) { throw "Smoke application exited with $($process.ExitCode)." }
    'PASS: Application exited cleanly'
}
finally {
    if (!$process.HasExited) { $process.CloseMainWindow() | Out-Null }
}
