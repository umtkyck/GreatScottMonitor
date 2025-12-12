# PowerShell script to create a simple icon for MedSecure Vision
# This creates a shield-shaped icon with biometric theme

Add-Type -AssemblyName System.Drawing

$iconSizes = @(16, 32, 48, 256)
$outputPath = $PSScriptRoot

function Create-IconBitmap {
    param([int]$size)
    
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)
    
    # Background gradient (blue to purple)
    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 88, 166, 255),  # #58A6FF
        [System.Drawing.Color]::FromArgb(255, 163, 113, 247), # #A371F7
        45
    )
    
    # Draw rounded rectangle as background
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = [int]($size * 0.2)
    $path.AddArc(0, 0, $radius * 2, $radius * 2, 180, 90)
    $path.AddArc($size - $radius * 2, 0, $radius * 2, $radius * 2, 270, 90)
    $path.AddArc($size - $radius * 2, $size - $radius * 2, $radius * 2, $radius * 2, 0, 90)
    $path.AddArc(0, $size - $radius * 2, $radius * 2, $radius * 2, 90, 90)
    $path.CloseFigure()
    
    $graphics.FillPath($gradientBrush, $path)
    
    # Draw shield icon (white)
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $shieldSize = [int]($size * 0.6)
    $shieldX = [int](($size - $shieldSize) / 2)
    $shieldY = [int](($size - $shieldSize) / 2)
    
    # Simple shield shape
    $shieldPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $shieldPath.AddArc($shieldX, $shieldY, $shieldSize, [int]($shieldSize * 0.5), 180, 180)
    $shieldPath.AddLine($shieldX + $shieldSize, $shieldY + [int]($shieldSize * 0.25), $shieldX + $shieldSize, $shieldY + [int]($shieldSize * 0.6))
    $shieldPath.AddLine($shieldX + $shieldSize, $shieldY + [int]($shieldSize * 0.6), $shieldX + [int]($shieldSize / 2), $shieldY + $shieldSize)
    $shieldPath.AddLine($shieldX + [int]($shieldSize / 2), $shieldY + $shieldSize, $shieldX, $shieldY + [int]($shieldSize * 0.6))
    $shieldPath.AddLine($shieldX, $shieldY + [int]($shieldSize * 0.6), $shieldX, $shieldY + [int]($shieldSize * 0.25))
    $shieldPath.CloseFigure()
    
    $graphics.FillPath($whiteBrush, $shieldPath)
    
    # Draw face circle in center
    $faceBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 88, 166, 255))
    $faceSize = [int]($size * 0.2)
    $faceX = [int](($size - $faceSize) / 2)
    $faceY = [int](($size - $faceSize) / 2) - [int]($size * 0.05)
    $graphics.FillEllipse($faceBrush, $faceX, $faceY, $faceSize, $faceSize)
    
    $graphics.Dispose()
    return $bitmap
}

# Create 256x256 PNG first
$bitmap256 = Create-IconBitmap -size 256
$pngPath = Join-Path $outputPath "icon.png"
$bitmap256.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Created: icon.png (256x256)" -ForegroundColor Green

# Create ICO file manually
$icoPath = Join-Path $outputPath "icon.ico"

# For simplicity, we'll save as PNG and convert
# Create a 32x32 version for the ICO
$bitmap32 = Create-IconBitmap -size 32
$icon = [System.Drawing.Icon]::FromHandle($bitmap32.GetHicon())

$fs = [System.IO.File]::Create($icoPath)
$icon.Save($fs)
$fs.Close()

Write-Host "Created: icon.ico" -ForegroundColor Green
Write-Host "Icons saved to: $outputPath" -ForegroundColor Cyan

$bitmap256.Dispose()
$bitmap32.Dispose()

