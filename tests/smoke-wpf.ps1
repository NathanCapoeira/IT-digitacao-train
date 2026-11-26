[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$CaptureDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$script:checks = 0
function Assert-That([bool]$Condition, [string]$Message) {
    $script:checks++
    if (-not $Condition) { throw $Message }
}

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase
$executablePath = (Resolve-Path -LiteralPath $Executable).Path
[Reflection.Assembly]::LoadFrom((Join-Path (Split-Path -Parent $executablePath) 'ITDigitacao.Core.dll')) | Out-Null
$assembly = [Reflection.Assembly]::LoadFrom($executablePath)
Assert-That ($assembly.GetName().Version.ToString() -eq '2.0.0.0') 'Unexpected executable version.'

$app = [Activator]::CreateInstance($assembly.GetType('ITDigitacao.App.App', $true))
$app.InitializeComponent()
$testPreferenceFile = [IO.Path]::Combine([IO.Path]::GetTempPath(), [Guid]::NewGuid().ToString() + '.json')
$privateInstance = [Reflection.BindingFlags]'NonPublic, Instance'
$windowType = $assembly.GetType('ITDigitacao.App.MainWindow', $true)
$testConstructor = $windowType.GetConstructor($privateInstance, $null, [type[]]@([string]), $null)
$window = $testConstructor.Invoke([object[]]@($testPreferenceFile))
try {
    $window.Show()
    $homeView = $window.FindName('HomePage')
    $durationGrid = $homeView.FindName('DurationGrid')
    $library = $window.FindName('LibraryPage')
    Assert-That ($homeView.SelectedMinutes -eq 10) 'Expected a default duration of 10 minutes.'

    foreach ($asset in @('logo-it-digitacao.png', 'logo-instituto.png', 'mascote-boas-vindas.png', 'mascote-ensinando.png', 'mascote-celebrando.png', 'mascote-digitando.png', 'Categorias/tecnologia.png')) {
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
        $library.FindName('ActivityCards').Items.Count -eq 5) 'Category did not open its five activities.'
    $window.UpdateLayout()
    Capture 'categoria-wpf.png'
    $intermediate = $library.FindName('IntermediateFilter')
    $intermediate.IsChecked = $true
    Send-Click $intermediate
    Assert-That ($library.FindName('ActivityCards').Items.Count -eq 2) 'Category did not honor the difficulty filter.'
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
    $focusButton = $practice.FindName('FocusButton')
    Send-Click $focusButton
    Assert-That ($practice.FindName('MetricCards').Visibility -eq 'Collapsed' -and $entry.Text -eq 'I') 'Focus mode lost text or left metric cards visible.'
    Send-Click $focusButton
    Assert-That ($practice.FindName('MetricCards').IsVisible -and $entry.Text -eq 'I') 'Normal mode did not restore metrics.'
    $originalFont = $entry.FontSize
    Send-Click ($practice.FindName('LargerButton'))
    Assert-That ($entry.FontSize -gt $originalFont -and $entry.Text -eq 'I') 'Editor font adjustment lost input.'
    Send-Click ($practice.FindName('SettingsButton'))
    $editorSettings = $window.FindName('SettingsPage')
    $editorSettings.FindName('FontSlider').Value = 28
    $editorSettings.FindName('Animations').IsChecked = $false
    Send-Click ($editorSettings.FindName('SaveButton'))
    Assert-That ($practice.IsVisible -and $entry.FontSize -gt 35 -and $entry.Text -eq 'I' -and
        [IO.File]::Exists($testPreferenceFile)) 'Saving from the editor lost input or did not persist settings.'
    Send-Click ($window.FindName('GuideTab'))
    $guide = $window.FindName('GuidePage')
    Assert-That $guide.IsVisible 'Guide navigation failed.'
    $guide.ShowKey(0x21)
    Assert-That ($guide.FindName('KeyHint').Text -match 'Indicador esquerdo') 'The guide did not identify F.'
    $guide.ShowKey(0x138)
    Assert-That ($guide.FindName('KeyHint').Text -match 'AltGr') 'The guide did not identify AltGr.'
    $window.UpdateLayout()
    Capture 'guia-abnt2-wpf.png'
    Send-Click ($window.FindName('SettingsTab'))
    $settings = $window.FindName('SettingsPage')
    Assert-That $settings.IsVisible 'Settings navigation failed.'
    $settings.FindName('FontSlider').Value = 28
    Assert-That ($settings.FindName('Preview').FontSize -gt 35) 'The font preview did not update.'
    Send-Click ($settings.FindName('DefaultButton'))
    Assert-That ($settings.FindName('FontSlider').Value -eq 14 -and -not $settings.FindName('Sounds').IsChecked) 'Restore defaults failed.'
    $window.UpdateLayout()
    Capture 'configuracoes-wpf.png'
    Send-Click ($window.FindName('HomeTab'))
    Assert-That ($homeView.Visibility -eq 'Visible' -and $homeView.SelectedMinutes -eq 30) 'Home lost the session selection.'
    $sessionState = $window.GetType().GetField('session', $privateInstance).GetValue($window)
    $timer = $window.GetType().GetField('displayTimer', $privateInstance).GetValue($window)
    Assert-That ($timer.IsEnabled -and $sessionState.Started) 'The display timer stopped when leaving the editor.'
    $sessionState.End()
    $window.GetType().GetMethod('RefreshSession', $privateInstance).Invoke($window, $null) | Out-Null
    Assert-That ($window.FindName('SummaryPage').IsVisible -and -not $timer.IsEnabled) 'Ending the session did not show results and stop the timer.'
    $summary = $window.FindName('SummaryPage')
    $errorCard = $summary.FindName('MetricGrid').Items | Where-Object Label -eq 'Erros cometidos'
    Assert-That ($errorCard.Value -eq '1' -and $summary.FindName('StarIcons').Visibility -eq 'Collapsed') 'Summary displayed incorrect errors or premature stars.'
    $window.UpdateLayout()
    Capture 'resumo-wpf.png'
    Send-Click ($window.FindName('GuideTab'))
    Send-Click ($guide.FindName('ExerciseButton'))
    Assert-That ($practice.IsVisible -and $practice.FindName('ActivityTitle').Text -eq 'Posição inicial das mãos') 'Guide exercise did not use the editor.'
    $practiceSession = $window.GetType().GetField('session', $privateInstance).GetValue($window)
    Assert-That (-not $practiceSession.Started -and $practiceSession.CompletedCount -eq 0) 'New session kept previous metrics.'
    $entry.Text = $practiceSession.Activity.Text
    Assert-That $practice.FindName('CompleteButton').IsEnabled 'The guide exercise could not be completed.'
    Send-Click ($practice.FindName('CompleteButton'))
    Assert-That ($library.IsVisible -and $practiceSession.CompletedCount -eq 1) 'Exercise completion did not return to the library.'

    $search.Text = 'Atalhos que facilitam a vida'
    Send-Click ($library.FindName('RandomButton'))
    $entry.Text = $practiceSession.Activity.Text
    Send-Click ($practice.FindName('CompleteButton'))
    $question = $window.FindName('QuestionPage')
    Assert-That ($question.IsVisible -and $question.FindName('Choices').Items.Count -eq 3 -and $timer.IsEnabled) 'The optional question or running timer is missing.'
    $window.UpdateLayout()
    Send-Click (Find-CardButton ($question.FindName('Choices').ItemContainerGenerator.ContainerFromIndex(1)))
    Assert-That ($question.FindName('Feedback').Text -like 'Isso mesmo!*') 'The question did not explain the correct answer.'
    Send-Click ($question.FindName('ContinueButton'))
    Assert-That ($library.IsVisible -and $practiceSession.CompletedCount -eq 2) 'Question exit altered the session results.'
    Write-Host "WPF checks passed: $script:checks"
    Write-Host 'WPF smoke test passed: embedded art, layout, search, library, timer, input, paste, and navigation.'
}
finally {
    $window.Close()
    Remove-Item -LiteralPath $testPreferenceFile -ErrorAction SilentlyContinue
}
