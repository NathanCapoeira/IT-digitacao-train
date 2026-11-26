using System;
using System.IO;
using System.Media;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ITDigitacao.Core;
using ITDigitacao.App.Views;

namespace ITDigitacao.App
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer displayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        private SessionManager session;
        private readonly string preferencesPath;
        private Preferences preferences;
        private FrameworkElement settingsReturn;
        private FrameworkElement visiblePage;
        private FrameworkElement[] pages;
        private RadioButton settingsReturnTab;
        private SoundPlayer completionSound;
        private HwndSource windowSource;

        public MainWindow() : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Instituto de Transformação", "IT Digitação", "preferencias.json"))
        {
        }

        internal MainWindow(string preferencesPath)
        {
            this.preferencesPath = preferencesPath;
            InitializeComponent();
            preferences = Preferences.Load(preferencesPath);
            pages = new FrameworkElement[] { HomePage, LibraryPage, PracticePage, GuidePage, SettingsPage, SummaryPage, QuestionPage };
            PracticePage.ApplyPreferences(preferences);
            PracticePage.SetAvailableHeight(PageScroller.ViewportHeight);
            PageScroller.SizeChanged += (sender, args) => PracticePage.SetAvailableHeight(PageScroller.ActualHeight);
            SourceInitialized += (sender, args) =>
            {
                windowSource = PresentationSource.FromVisual(this) as HwndSource;
                windowSource?.AddHook(GuideKeyMessage);
            };

            Rect workArea = SystemParameters.WorkArea;
            MinWidth = Math.Min(MinWidth, workArea.Width);
            MinHeight = Math.Min(MinHeight, workArea.Height);
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);

            displayTimer.Tick += (sender, args) => RefreshSession();
            Closed += (sender, args) =>
            {
                displayTimer.Stop();
                windowSource?.RemoveHook(GuideKeyMessage);
                completionSound?.Dispose();
            };
            LibraryPage.PracticeRequested += OpenActivity;
            LibraryPage.ResumeRequested += () => OpenActivity(session.Activity);
            LibraryPage.EndRequested += RequestEnd;
            LibraryPage.PageChanged += () => PageScroller.ScrollToTop();
            PracticePage.Changed += RefreshSession;
            PracticePage.BackRequested += OpenLibrary;
            PracticePage.Completed += CompleteActivity;
            PracticePage.EndRequested += RequestEnd;
            PracticePage.Abandoned += OpenLibrary;
            PracticePage.SettingsRequested += OpenSettings;
            PracticePage.PreferencesChanged += ApplyPreferences;
            SettingsPage.SaveRequested += SavePreferences;
            SettingsPage.ReturnRequested += ReturnFromSettings;
            GuidePage.ExerciseRequested += () => { EnsureSession(); OpenActivity(Activity.HomeRowExercise()); };
            SummaryPage.NewRequested += () => { session = null; OpenLibrary(); };
            SummaryPage.BackRequested += () => ShowPage(HomeTab);
            SummaryPage.ExitRequested += () => Close();
            QuestionPage.ContinueRequested += ShowEncouragement;
            ShowPage(HomeTab);
        }

        private void Navigate_Click(object sender, RoutedEventArgs e) => ShowPage((RadioButton)sender);

        private void StartRequested(object sender, EventArgs e)
        {
            OpenLibrary();
            LibraryTab.Focus();
        }

        private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

        private void OpenSettings()
        {
            if (SettingsPage.IsVisible)
                return;
            settingsReturn = visiblePage;
            settingsReturnTab = HomeTab.IsChecked == true ? HomeTab : GuideTab.IsChecked == true ? GuideTab : LibraryTab;
            if (settingsReturn == HomePage)
                settingsReturnTab = HomeTab;
            else if (settingsReturn == GuidePage)
                settingsReturnTab = GuideTab;
            SettingsPage.Open(preferences, settingsReturn == PracticePage);
            Reveal(SettingsPage, SettingsTab);
            SettingsTab.Focus();
        }

        private void ReturnFromSettings()
        {
            if (settingsReturn == PracticePage && session?.Engine != null && !session.Finished)
                OpenActivity(session.Activity);
            else
                Reveal(settingsReturn ?? HomePage, settingsReturnTab ?? HomeTab);
        }

        private void SavePreferences(Preferences value)
        {
            try
            {
                value.Save(preferencesPath);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                SettingsPage.ShowSaveError();
                return;
            }
            preferences = value;
            ApplyPreferences();
            ReturnFromSettings();
        }

        private void ApplyPreferences()
        {
            if (!preferences.Sounds)
                completionSound?.Stop();
            PracticePage.ApplyPreferences(preferences);
            NavigationBar.Visibility = visiblePage == PracticePage && preferences.Concentration
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private IntPtr GuideKeyMessage(IntPtr handle, int message, IntPtr key, IntPtr details, ref bool handled)
        {
            const int KeyDown = 0x100;
            const int SystemKeyDown = 0x104;
            if ((message == KeyDown || message == SystemKeyDown) && IsActive && GuidePage.IsVisible)
                GuidePage.ShowKey((int)((details.ToInt64() >> 16) & 0x1ff));
            return IntPtr.Zero;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Started != true || session.Finished ||
                MessageBox.Show(this, "Sair e descartar os resultados desta sessão?", "IT Digitação",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Close();
        }

        private void Reveal(FrameworkElement page, RadioButton tab)
        {
            visiblePage = page;
            tab.IsChecked = true;
            foreach (var element in pages)
                element.Visibility = element == page ? Visibility.Visible : Visibility.Collapsed;
            NavigationBar.Visibility = page == PracticePage && preferences.Concentration
                ? Visibility.Collapsed : Visibility.Visible;
            PageScroller.ScrollToTop();
        }

        private void ShowPage(RadioButton tab)
        {
            if (tab == LibraryTab)
                OpenLibrary();
            else if (tab == HomeTab)
                Reveal(HomePage, tab);
            else if (tab == GuideTab)
                Reveal(GuidePage, tab);
            else
                OpenSettings();
        }

        private void EnsureSession()
        {
            if (session == null || session.Finished || !session.Started && session.Activity == null &&
                session.Duration.TotalMinutes != HomePage.SelectedMinutes)
                session = new SessionManager(HomePage.SelectedMinutes, new SessionClock());
        }

        private void OpenLibrary()
        {
            EnsureSession();
            LibraryPage.Open(session);
            Reveal(LibraryPage, LibraryTab);
        }

        private void OpenActivity(Activity activity)
        {
            if (activity == null)
                return;
            if (!session.Open(activity))
            {
                ShowSummary();
                return;
            }
            Reveal(PracticePage, LibraryTab);
            PracticePage.Open(session);
        }

        private void CompleteActivity()
        {
            var question = session.Activity?.Quiz;
            if (!session.CompleteActivity())
            {
                RefreshSession();
                return;
            }
            PlayCompletionSound();
            if (question != null)
            {
                QuestionPage.Open(question);
                Reveal(QuestionPage, LibraryTab);
                QuestionPage.FocusChoices();
            }
            else
                ShowEncouragement();
        }

        private void ShowEncouragement()
        {
            OpenLibrary();
            LibraryPage.ShowEncouragement();
        }

        private void PlayCompletionSound()
        {
            if (!preferences.Sounds)
                return;
            try
            {
                if (completionSound == null)
                {
                    var uri = new Uri("/IT-Digitacao;component/Assets/conclusao.wav", UriKind.Relative);
                    var resource = Application.GetResourceStream(uri);
                    if (resource == null)
                        return;

                    completionSound = new SoundPlayer(resource.Stream);
                    completionSound.Load();
                }
                completionSound.Play();
            }
            catch (Exception error) when (error is InvalidOperationException || error is IOException ||
                                          error is TimeoutException || error is System.ComponentModel.Win32Exception)
            {
                completionSound?.Dispose();
                completionSound = null;
            }
        }

        private void RequestEnd()
        {
            if (MessageBox.Show(this, "Encerrar a sessão e ver seus resultados?", "IT Digitação",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            session.End();
            ShowSummary();
        }

        private void RefreshSession()
        {
            if (session == null)
                return;
            session.Tick();
            if (session.Finished)
            {
                ShowSummary();
                return;
            }
            if (session.Started && !displayTimer.IsEnabled)
                displayTimer.Start();
            if (PracticePage.IsVisible)
                PracticePage.UpdateMetrics();
            if (LibraryPage.IsVisible)
                LibraryPage.RefreshSession();
        }

        private void ShowSummary()
        {
            displayTimer.Stop();
            SummaryPage.ShowResult(session, preferences);
            Reveal(SummaryPage, LibraryTab);
        }
    }
}
