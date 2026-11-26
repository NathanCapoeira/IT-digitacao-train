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
$executablePath = (Resolve-Path -LiteralPath $Executable).Path
[Reflection.Assembly]::LoadFrom((Join-Path (Split-Path -Parent $executablePath) 'ITDigitacao.Core.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom($executablePath)
Assert-That ($assembly.GetName().Version.ToString() -eq '1.2.0.0') 'Unexpected executable version.'

$app = [Activator]::CreateInstance($assembly.GetType('ITDigitacao.App.App', $true))
$app.InitializeComponent()
$window = [Activator]::CreateInstance($assembly.GetType('ITDigitacao.App.MainWindow', $true))
try {
    $window.Show()
    $homeView = $window.FindName('HomePage')
    $durationGrid = $homeView.FindName('DurationGrid')
    $library = $window.FindName('LibraryPage')
    $sectionTitle = $window.FindName('SectionTitle')
    Assert-That ($homeView.SelectedMinutes -eq 10) 'Expected a default duration of 10 minutes.'

    foreach ($asset in @('logo-it-digitacao.png', 'logo-instituto.png', 'mascote-boas-vindas.png', 'mascote-ensinando.png', 'mascote-digitando.png', 'Categorias/tecnologia.png')) {
        $uri = [Uri]::new("pack://application:,,,/IT-Digitacao;component/Assets/$asset")
        $bitmap = [System.Windows.Media.Imaging.BitmapImage]::new($uri)
        Assert-That ($bitmap.PixelWidth -gt 0 -and $bitmap.PixelHeight -gt 0) "Could not decode $asset."
    }
    Assert-That ($homeView.FindName('WelcomeMascot').Source.PixelWidth -eq 640) 'Welcome mascot is missing.'

    $window.Width = 660
    $window.UpdateLayout()
    Assert-That ($durationGrid.Columns -eq 3) 'Expected 3 duration columns in a narrow window.'
    $window.Width = 1024
    $window.Height = 720
    $window.UpdateLayout()
    Assert-That ($durationGrid.Columns -eq 6) 'Expected 6 duration columns in a wide window.'
    Assert-That ($homeView.FindName('HeroTitle').ActualHeight -gt 0) 'The heading has no layout.'

    function Capture([string]$FileName) {
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
        $capture = Join-Path (Resolve-Path -LiteralPath $CaptureDirectory).Path $FileName
        $stream = [IO.File]::Create($capture)
        try { $encoder.Save($stream) } finally { $stream.Dispose() }
    }
    Capture 'inicio-wpf.png'

    foreach ($card in $durationGrid.Children) {
        $card.IsChecked = $true
        Assert-That ($homeView.SelectedMinutes -eq [int]$card.Content) 'Duration selection did not update.'
        Assert-That (@($durationGrid.Children | Where-Object IsChecked).Count -eq 1) 'Duration selection is not exclusive.'
    }

    function Send-Click($Control) {
        $Control.RaiseEvent([System.Windows.RoutedEventArgs]::new([System.Windows.Controls.Primitives.ButtonBase]::ClickEvent))
    }

    Send-Click ($homeView.FindName('StartButton'))
    $window.UpdateLayout()
    Assert-That ($window.FindName('LibraryTab').IsChecked -and $library.IsVisible) 'Start did not open the library.'
    Assert-That ($library.FindName('SessionText').Text -match '30 minutos') 'The library lost the selected duration.'
    Assert-That ($library.FindName('CategoryCards').Items.Count -eq 10) 'Library does not show ten categories.'
    Capture 'biblioteca-wpf.png'
    $window.Width = 660
    $window.UpdateLayout()
    Assert-That ($library.FindName('CategoryCards').Tag -lt 5) 'Library did not reduce its column count.'
    $window.Width = 1024
    $window.UpdateLayout()

    function Find-CardButton($Visual) {
        if ($Visual -is [System.Windows.Controls.Button]) { return $Visual }
        for ($i = 0; $i -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($Visual); $i++) {
            $result = Find-CardButton ([System.Windows.Media.VisualTreeHelper]::GetChild($Visual, $i))
            if ($null -ne $result) { return $result }
        }
    }
    $categories = $library.FindName('CategoryCards')
    Send-Click (Find-CardButton ($categories.ItemContainerGenerator.ContainerFromIndex(0)))
    Assert-That ($library.FindName('TitleText').Text -eq 'Informática e tecnologia' -and
        $library.FindName('ActivityCards').Items.Count -eq 3) 'Category did not open its three activities.'
    $window.UpdateLayout()
    Capture 'categoria-wpf.png'
    $intermediate = $library.FindName('IntermediateFilter')
    $intermediate.IsChecked = $true
    Send-Click $intermediate
    Assert-That ($library.FindName('ActivityCards').Items.Count -eq 1) 'Category did not honor the difficulty filter.'
    $activities = $library.FindName('ActivityCards')
    $window.UpdateLayout()
    Send-Click (Find-CardButton ($activities.ItemContainerGenerator.ContainerFromIndex(0)))
    Assert-That ($window.FindName('PracticePage').FindName('ActivityTitle').Text -eq 'Atalhos que facilitam a vida') 'Individual practice did not open the requested activity.'
    Send-Click ($window.FindName('LibraryTab'))
    $all = $library.FindName('AllFilter')
    $all.IsChecked = $true
    Send-Click $all

    $search = $library.FindName('SearchBox')
    $search.Text = 'ZZZ inexistente'
    Assert-That ($library.FindName('EmptyCard').Visibility -eq 'Visible' -and
        -not $library.FindName('RandomButton').IsEnabled) 'Empty state or random guard failed.'
    $search.Text = 'O MUNDO DA INTELIGENCIA ARTIFICIAL'
    Assert-That ($library.FindName('ActivityCards').Items.Count -eq 1) 'Accent-insensitive activity search failed.'
    Send-Click ($library.FindName('RandomButton'))
    $practice = $window.FindName('PracticePage')
    Assert-That ($practice.IsVisible -and $practice.FindName('ActivityTitle').Text -like '*artificial*') 'Random did not respect the search.'
    $entry = $practice.FindName('Entry')
    $paste = [System.Windows.Input.ApplicationCommands]::Paste
    Assert-That (-not $paste.CanExecute($null, $entry) -and -not $entry.AllowDrop) 'Paste or drop is still enabled in the editor.'
    Assert-That ($practice.FindName('ClockText').Text -eq '30:00') 'Session started before the first insertion.'
    $entry.Text = 'X'
    Assert-That ($practice.FindName('MetricText').Text -match 'Erros cometidos 1') 'First wrong attempt was not recorded.'
    $entry.Text = 'I'
    Assert-That ($practice.FindName('MetricText').Text -match 'Corrigidos 1') 'Correction was not reflected.'
    $window.UpdateLayout()
    Capture 'editor-wpf.png'
    $window.Width = 660
    $window.UpdateLayout()
    Assert-That ($practice.FindName('EditorPanels').Columns -eq 1) 'Editor did not stack its panels at narrow width.'
    $window.Width = 1024
    $window.UpdateLayout()
    Send-Click ($window.FindName('LibraryTab'))
    Assert-That ($library.FindName('ResumeButton').IsVisible) 'Library did not offer to resume.'
    Send-Click ($library.FindName('ResumeButton'))
    Assert-That ($practice.IsVisible -and $entry.Text -eq 'I') 'Editor lost the input after navigation.'
    Send-Click ($window.FindName('GuideTab'))
    Assert-That ($sectionTitle.Text -eq 'Como digitar') 'Guide navigation failed.'
    Send-Click ($window.FindName('SettingsTab'))
    Assert-That ($sectionTitle.Text -eq 'Configurações') 'Settings navigation failed.'
    Send-Click ($window.FindName('HomeTab'))
    Assert-That ($homeView.Visibility -eq 'Visible' -and $homeView.SelectedMinutes -eq 30) 'Home lost the session selection.'
    $privateInstance = [Reflection.BindingFlags]'NonPublic, Instance'
    $sessionState = $window.GetType().GetField('session', $privateInstance).GetValue($window)
    $timer = $window.GetType().GetField('displayTimer', $privateInstance).GetValue($window)
    Assert-That ($timer.IsEnabled -and $sessionState.Started) 'The display timer stopped when leaving the editor.'
    $sessionState.End()
    $window.GetType().GetMethod('RefreshSession', $privateInstance).Invoke($window, $null) | Out-Null
    Assert-That ($window.FindName('SummaryPage').IsVisible -and -not $timer.IsEnabled) 'Ending the session did not show results and stop the timer.'
    Assert-That ($window.FindName('SummaryMetrics').Text -match 'Erros cometidos: 1') 'Summary did not preserve the attempt history.'
    $window.UpdateLayout()
    Capture 'resumo-wpf.png'
    Write-Host 'WPF smoke test passed: embedded art, layout, search, library, timer, input, paste, and navigation.'
}
finally {
    $window.Close()
}
