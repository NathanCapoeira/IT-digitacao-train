using System;
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

        public MainWindow()
        {
            InitializeComponent();

            Rect workArea = SystemParameters.WorkArea;
            MinWidth = Math.Min(MinWidth, workArea.Width);
            MinHeight = Math.Min(MinHeight, workArea.Height);
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);

            displayTimer.Tick += (sender, args) => RefreshSession();
            Closed += (sender, args) => displayTimer.Stop();
            LibraryPage.PracticeRequested += OpenActivity;
            LibraryPage.ResumeRequested += () => OpenActivity(session.Activity);
            LibraryPage.EndRequested += RequestEnd;
            LibraryPage.PageChanged += () => PageScroller.ScrollToTop();
            PracticePage.Changed += RefreshSession;
            PracticePage.BackRequested += OpenLibrary;
            PracticePage.Completed += CompleteActivity;
            PracticePage.EndRequested += RequestEnd;
            ShowPage(HomeTab);
        }

        private void Navigate_Click(object sender, RoutedEventArgs e) => ShowPage((RadioButton)sender);

        private void StartRequested(object sender, EventArgs e)
        {
            OpenLibrary();
            LibraryTab.Focus();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(SettingsTab);
            SettingsTab.Focus();
        }

        private void ReturnHome_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(HomeTab);
            HomeTab.Focus();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Started != true || session.Finished ||
                MessageBox.Show(this, "Sair e descartar os resultados desta sessão?", "IT Digitação",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Close();
        }

        private void NewSession_Click(object sender, RoutedEventArgs e)
        {
            session = null;
            ReturnHome_Click(sender, e);
        }

        private void Reveal(FrameworkElement page, RadioButton tab)
        {
            tab.IsChecked = true;
            foreach (var element in new FrameworkElement[] { HomePage, LibraryPage, PracticePage, SectionPage, SummaryPage })
                element.Visibility = element == page ? Visibility.Visible : Visibility.Collapsed;
            PageScroller.ScrollToTop();
        }

        private void ShowPage(RadioButton tab)
        {
            if (tab == LibraryTab)
            {
                OpenLibrary();
                return;
            }

            if (tab == HomeTab)
            {
                Reveal(HomePage, tab);
                return;
            }

            bool guide = tab == GuideTab;
            SectionTitle.Text = guide ? "Como digitar" : "Configurações";
            SectionDescription.Text = guide ? "Conheça o teclado e desenvolva bons hábitos de digitação." :
                "Personalize sua experiência de aprendizado.";
            SectionStatus.Text = guide ? "O guia ABNT2 e as orientações de postura chegarão em uma fase futura." :
                "As preferências de leitura e prática chegarão em uma fase futura.";
            Reveal(SectionPage, tab);
        }

        private void OpenLibrary()
        {
            if (session == null || session.Finished || !session.Started && session.Activity == null &&
                session.Duration.TotalMinutes != HomePage.SelectedMinutes)
                session = new SessionManager(HomePage.SelectedMinutes, new SessionClock());
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
            if (!session.CompleteActivity())
            {
                RefreshSession();
                return;
            }
            OpenLibrary();
            LibraryPage.ShowEncouragement();
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
            SummaryMessage.Text = session.Started ? "Cada tentativa ajuda você a evoluir. Continue praticando!" :
                "A sessão terminou antes da primeira tentativa. Comece outra quando estiver pronto.";
            SummaryMetrics.Text =
                $"Tempo utilizado: {session.Elapsed:mm\\:ss}\nPrecisão: {PracticeView.Percent(session.Accuracy)}\n" +
                $"Palavras por minuto: {PracticeView.Number(session.WordsPerMinute)}\nErros cometidos: {session.TotalErrors}\n" +
                $"Erros corrigidos: {session.CorrectedErrors}\nTextos concluídos: {session.CompletedCount}";
            Reveal(SummaryPage, LibraryTab);
        }
    }
}
