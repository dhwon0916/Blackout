$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$frames = @()
foreach ($size in @(16, 20, 24, 32, 40, 48, 64, 128, 256)) {
    $bitmap = [Drawing.Bitmap]::new($size, $size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform($size / 256.0, $size / 256.0)
    $background = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#142331'))
    $screen = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#080e16'))
    $accent = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#6ee7df'))
    $pen = [Drawing.Pen]::new($accent, 12)
    $pen.StartCap = $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $path = [Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc(8,8,104,104,180,90); $path.AddArc(144,8,104,104,270,90)
    $path.AddArc(144,144,104,104,0,90); $path.AddArc(8,144,104,104,90,90); $path.CloseFigure()
    $graphics.FillPath($background,$path)
    $path.Reset()
    $path.AddArc(40,52,28,28,180,90); $path.AddArc(188,52,28,28,270,90)
    $path.AddArc(188,152,28,28,0,90); $path.AddArc(40,152,28,28,90,90); $path.CloseFigure()
    $graphics.FillPath($screen,$path); $graphics.DrawPath($pen,$path)
    $graphics.DrawLine($pen,128,184,128,208); $graphics.DrawLine($pen,88,208,168,208)
    $graphics.FillEllipse($accent,90,75,76,76); $graphics.FillEllipse($screen,112,61,72,72)
    $stream = [IO.MemoryStream]::new()
    $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
    $frames += @{Size=$size; Bytes=$stream.ToArray()}
    if ($size -eq 256) { $bitmap.Save((Join-Path $PSScriptRoot 'Blackout.png'),[Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose(); $graphics.Dispose(); $bitmap.Dispose(); $path.Dispose(); $pen.Dispose()
    $background.Dispose(); $screen.Dispose(); $accent.Dispose()
}
$output = [IO.File]::Create((Join-Path $PSScriptRoot 'Blackout.ico'))
$writer = [IO.BinaryWriter]::new($output)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $frame.Bytes.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
} finally { $writer.Dispose(); $output.Dispose() }
