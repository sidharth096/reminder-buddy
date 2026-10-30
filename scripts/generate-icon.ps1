$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path $PSScriptRoot
$output = Join-Path $root 'assets\ReminderBuddy.ico'
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = New-Object System.Collections.Generic.List[byte[]]

function Scale-Point([float]$value, [int]$size) { return $value * $size / 256.0 }

foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $scale = $size / 256.0

        $background = New-Object System.Drawing.Drawing2D.GraphicsPath
        $radius = 52 * $scale
        $rect = New-Object System.Drawing.RectangleF((8 * $scale), (8 * $scale), (240 * $scale), (240 * $scale))
        $diameter = $radius * 2
        $background.AddArc($rect.Left, $rect.Top, $diameter, $diameter, 180, 90)
        $background.AddArc($rect.Right - $diameter, $rect.Top, $diameter, $diameter, 270, 90)
        $background.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
        $background.AddArc($rect.Left, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
        $background.CloseFigure()
        $graphics.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#12372f'))), $background)

        $bell = New-Object System.Drawing.Drawing2D.GraphicsPath
        $bell.StartFigure()
        $bell.AddBezier(70*$scale,144*$scale,82*$scale,132*$scale,89*$scale,115*$scale,89*$scale,96*$scale)
        $bell.AddBezier(89*$scale,96*$scale,89*$scale,68*$scale,106*$scale,46*$scale,128*$scale,46*$scale)
        $bell.AddBezier(128*$scale,46*$scale,150*$scale,46*$scale,167*$scale,68*$scale,167*$scale,96*$scale)
        $bell.AddBezier(167*$scale,96*$scale,167*$scale,115*$scale,174*$scale,132*$scale,186*$scale,144*$scale)
        $bell.CloseFigure()
        $mint = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#5bd5a5'))
        $creamPen = New-Object System.Drawing.Pen([System.Drawing.ColorTranslator]::FromHtml('#e8fff4'), [Math]::Max(1, 8*$scale))
        $creamPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $graphics.FillPath($mint, $bell); $graphics.DrawPath($creamPen, $bell)

        $ink = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#12372f'))
        $eye = [Math]::Max(1.5, 6*$scale)
        $graphics.FillEllipse($ink, 111*$scale-$eye, 103*$scale-$eye, $eye*2, $eye*2)
        $graphics.FillEllipse($ink, 147*$scale-$eye, 103*$scale-$eye, $eye*2, $eye*2)

        $smile = New-Object System.Drawing.Pen([System.Drawing.ColorTranslator]::FromHtml('#12372f'), [Math]::Max(1.2, 7*$scale))
        $smile.StartCap = $smile.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawArc($smile, 113*$scale, 112*$scale, 32*$scale, 22*$scale, 20, 140)

        $dot = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#f06b4f'))
        $graphics.FillEllipse($dot, 162*$scale, 39*$scale, 48*$scale, 48*$scale)
        if ($size -ge 32) {
            $mark = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255,255,247,237), [Math]::Max(1.5, 6*$scale))
            $mark.StartCap = $mark.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $graphics.DrawLine($mark, 186*$scale, 51*$scale, 186*$scale, 67*$scale)
            $graphics.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,255,247,237))), 182*$scale, 73*$scale, 8*$scale, 8*$scale)
            $mark.Dispose()
        }

        $stream = New-Object IO.MemoryStream
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $images.Add($stream.ToArray())
        $stream.Dispose(); $background.Dispose(); $bell.Dispose(); $mint.Dispose(); $ink.Dispose(); $dot.Dispose(); $creamPen.Dispose(); $smile.Dispose()
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}

$directory = Split-Path $output
if (!(Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
$file = New-Object IO.FileStream($output, [IO.FileMode]::Create, [IO.FileAccess]::Write)
$writer = New-Object IO.BinaryWriter($file)
try {
    $writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]$images.Count)
    $offset = 6 + (16 * $images.Count)
    for ($i = 0; $i -lt $images.Count; $i++) {
        $size = $sizes[$i]
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]($(if ($size -eq 256) { 0 } else { $size })))
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([UInt16]1); $writer.Write([UInt16]32)
        $writer.Write([UInt32]$images[$i].Length); $writer.Write([UInt32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($image in $images) { $writer.Write($image) }
}
finally { $writer.Dispose(); $file.Dispose() }

"Generated $output with $($sizes.Count) icon sizes."
