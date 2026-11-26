[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$CaptureDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-That([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $Executable).Path)
Assert-That ($assembly.GetName().Version.ToString() -eq '1.1.0.0') 'Unexpected executable version.'
[System.Windows.Application]::ResourceAssembly = $assembly

$app = [Activator]::CreateInstance($assembly.GetType('ITDigitacao.App.App', $true))
$app.InitializeComponent()
$window = [Activator]::CreateInstance($assembly.GetType('ITDigitacao.App.MainWindow', $true))
try {
    $window.Show()
    $home = $window.FindName('HomePage')
    $durationGrid = $home.FindName('DurationGrid')
    $plan = $window.FindName('SelectedPlan')
    $sectionTitle = $window.FindName('SectionTitle')
    Assert-That ($home.SelectedMinutes -eq 10) 'Expected a default duration of 10 minutes.'

    foreach ($asset in @('logo-it-digitacao.png', 'logo-instituto.png', 'mascote-boas-vindas.png')) {
        $uri = [Uri]::new("pack://application:,,,/IT-Digitacao;component/Assets/$asset")
        $bitmap = [System.Windows.Media.Imaging.BitmapImage]::new($uri)
        Assert-That ($bitmap.PixelWidth -gt 0 -and $bitmap.PixelHeight -gt 0) "Could not decode $asset."
    }
    Assert-That ($home.FindName('WelcomeMascot').Source.PixelWidth -eq 640) 'Welcome mascot is missing.'

    $window.Width = 660
    $window.UpdateLayout()
    Assert-That ($durationGrid.Columns -eq 3) 'Expected 3 duration columns in a narrow window.'
    $window.Width = 1024
    $window.Height = 720
    $window.UpdateLayout()
    Assert-That ($durationGrid.Columns -eq 6) 'Expected 6 duration columns in a wide window.'
    Assert-That ($home.FindName('HeroTitle').ActualHeight -gt 0) 'The heading has no layout.'

    New-Item -ItemType Directory -Path $CaptureDirectory -Force | Out-Null
    $surface = $window.Content
    $visualDpi = [System.Windows.Media.VisualTreeHelper]::GetDpi($surface)
    $pixelWidth = [int][Math]::Ceiling($surface.ActualWidth * $visualDpi.DpiScaleX)
    $pixelHeight = [int][Math]::Ceiling($surface.ActualHeight * $visualDpi.DpiScaleY)
    $image = [System.Windows.Media.Imaging.RenderTargetBitmap]::new(
        $pixelWidth, $pixelHeight, $visualDpi.PixelsPerInchX, $visualDpi.PixelsPerInchY,
        [System.Windows.Media.PixelFormats]::Pbgra32
    )
    $image.Render($surface)
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($image))
    $capture = Join-Path (Resolve-Path -LiteralPath $CaptureDirectory).Path 'inicio-wpf.png'
    $stream = [IO.File]::Create($capture)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }

    foreach ($card in $durationGrid.Children) {
        $card.IsChecked = $true
        Assert-That ($home.SelectedMinutes -eq [int]$card.Content) 'Duration selection did not update.'
        Assert-That (@($durationGrid.Children | Where-Object IsChecked).Count -eq 1) 'Duration selection is not exclusive.'
    }

    function Send-Click($Control) {
        $Control.RaiseEvent([System.Windows.RoutedEventArgs]::new([System.Windows.Controls.Primitives.ButtonBase]::ClickEvent))
    }

    Send-Click ($home.FindName('StartButton'))
    Assert-That ($window.FindName('LibraryTab').IsChecked -and $sectionTitle.Text -eq 'Biblioteca educativa') 'Start did not open the library.'
    Assert-That ($plan.Text -match '30 minutos' -and $plan.Visibility -eq 'Visible') 'The library lost the selected duration.'

    Send-Click ($window.FindName('GuideTab'))
    Assert-That ($sectionTitle.Text -eq 'Como digitar' -and $plan.Visibility -eq 'Collapsed') 'Guide navigation failed.'
    Send-Click ($window.FindName('SettingsTab'))
    Assert-That ($sectionTitle.Text -eq 'Configurações') 'Settings navigation failed.'
    Send-Click ($window.FindName('HomeTab'))
    Assert-That ($home.Visibility -eq 'Visible' -and $home.SelectedMinutes -eq 30) 'Home lost the session selection.'
    Write-Host 'WPF smoke test passed: embedded art, layout, duration and navigation.'
}
finally {
    $window.Close()
}
