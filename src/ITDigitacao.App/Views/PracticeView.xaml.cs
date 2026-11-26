using System;
using System.Linq;
using System.Globalization;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ITDigitacao.Core;

namespace ITDigitacao.App.Views
{
    public partial class PracticeView : UserControl
    {
        private static readonly Brush CorrectBrush = Frozen("#22834C");
        private static readonly Brush ErrorBrush = Frozen("#A60000");
        private static readonly Brush NextBrush = Frozen("#FFF0BC");
        private SessionManager session;
        private TypingEngine renderedEngine;
        private Run[] runs;
        private int[] feedback;
        private bool loading;
        private bool followPending;
        private Preferences preferences = new Preferences();

        public PracticeView()
        {
            InitializeComponent();
            DataObject.AddPastingHandler(Entry, (sender, args) => args.CancelCommand());
            Entry.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste,
                (sender, args) => args.Handled = true,
                (sender, args) => { args.CanExecute = false; args.Handled = true; }));
        }

        public event Action Changed;
        public event Action Completed;
        public event Action BackRequested;
        public event Action EndRequested;
        public event Action SettingsRequested;
        public event Action Abandoned;
        public event Action PreferencesChanged;

        public void ApplyPreferences(Preferences value)
        {
            preferences = value;
            ReferenceText.FontSize = Entry.FontSize = value.FontPoints * 96.0 / 72.0;
            ReferenceText.LineHeight = ReferenceText.FontSize * 1.45;
            FontValue.Text = value.FontPoints + " pt";
            SoundButton.Content = value.Sounds ? "Som ligado" : "Som desligado";
            FocusButton.Content = value.Concentration ? "Voltar ao modo normal" : "Concentração";
            WarningText.Visibility = value.Concentration ? Visibility.Collapsed : Visibility.Visible;
            MetricCards.Visibility = MetricText.Visibility = value.ShowMetrics && (!value.Concentration || !value.HideMetricsInConcentration)
                ? Visibility.Visible : Visibility.Collapsed;
            UpdatePanelHeight();
            if (runs != null && session?.Engine == renderedEngine)
            {
                Array.Clear(feedback, 0, feedback.Length);
                foreach (var run in runs)
                {
                    run.ClearValue(TextElement.ForegroundProperty);
                    run.ClearValue(TextElement.BackgroundProperty);
                    run.ClearValue(Inline.TextDecorationsProperty);
                }
                UpdateFeedback();
            }
        }

        public void SetAvailableHeight(double height)
        {
            availableHeight = height;
            UpdatePanelHeight();
        }

        private double availableHeight;

        private void UpdatePanelHeight()
        {
            var height = EditorPanels.Columns == 1 ? 180 : Math.Max(150, Math.Min(500,
                availableHeight - (preferences.Concentration ? 220 : 355)));
            ReferenceScroller.Height = Entry.Height = height;
        }

        public void Open(SessionManager current)
        {
            session = current;
            var engine = session.Engine;
            ActivityTitle.Text = session.Activity.Title;
            ActivityLevel.Text = session.Activity.CategoryName + " · " + session.Activity.Difficulty;
            if (engine != renderedEngine)
            {
                renderedEngine = engine;
                ReferenceText.Inlines.Clear();
                runs = new Run[engine.Reference.Count];
                feedback = new int[runs.Length];
                for (int i = 0; i < runs.Length; i++)
                {
                    var run = new Run(engine.Reference[i] == "\n" ? "\r\n" : engine.Reference[i]);
                    runs[i] = run;
                    ReferenceText.Inlines.Add(run);
                }

                loading = true;
                Entry.IsUndoEnabled = false;
                Entry.Text = engine.InputText.Replace("\n", "\r\n");
                Entry.CaretIndex = Entry.Text.Length;
                Entry.IsUndoEnabled = true;
                loading = false;
                ReferenceScroller.ScrollToTop();
            }
            UpdateFeedback();
            UpdateMetrics();
            Entry.Focus();
        }

        public void UpdateMetrics()
        {
            if (session?.Engine == null)
                return;
            var remaining = TimeSpan.FromSeconds(Math.Ceiling(session.Remaining.TotalSeconds));
            ClockText.Text = remaining.ToString(@"mm\:ss");
            WarningText.Text = !session.Started ? "O tempo começa quando você digitar." :
                remaining.TotalSeconds <= 60 ? "Último minuto de prática." :
                remaining.TotalMinutes <= 5 ? "Restam até cinco minutos de prática." : "";
            WarningText.Visibility = WarningText.Text.Length == 0 || preferences.Concentration ? Visibility.Collapsed : Visibility.Visible;
            SpeedValue.Text = Number(session.WordsPerMinute);
            AccuracyValue.Text = Percent(session.Accuracy);
            HistoricalErrors.Text = session.TotalErrors.ToString();
            CurrentErrors.Text = session.CurrentErrors.ToString();
            MetricText.Text = $"Precisão {Percent(session.Accuracy)}  ·  PPM {Number(session.WordsPerMinute)}  ·  " +
                $"Erros cometidos {session.TotalErrors}  ·  Corrigidos {session.CorrectedErrors}  ·  No texto {session.CurrentErrors}";
            ProgressText.Text = session.Engine.Complete ? "100% · Texto pronto para concluir" :
                $"Progresso do texto: {DisplayProgress(session.Engine)}";
            ProgressMeter.Value = session.Engine.Progress;
            CompleteButton.IsEnabled = !session.Finished && session.Engine.Complete;
            Entry.IsReadOnly = session.Finished;
        }

        private void Entry_Changed(object sender, TextChangedEventArgs e)
        {
            if (loading || session?.Engine == null || session.Finished)
                return;
            int offset = e.Changes.Count == 1 ? e.Changes.First().Offset : -1;
            session.ApplyEdit(Entry.Text, offset, e.UndoAction != UndoAction.Undo && e.UndoAction != UndoAction.Redo);
            UpdateFeedback();
            UpdateMetrics();
            FollowReference();
            Changed?.Invoke();
        }

        private void UpdateFeedback()
        {
            var engine = session.Engine;
            if (!preferences.VisualFeedback)
                return;

            for (int i = 0; i < runs.Length; i++)
            {
                int state = i == engine.Entered ? 3 : i > engine.Entered ? 0 : engine.IsCorrectAt(i) ? 1 : 2;
                if (feedback[i] == state)
                    continue;
                feedback[i] = state;
                runs[i].Foreground = state == 1 ? CorrectBrush : state == 2 ? ErrorBrush : ReferenceText.Foreground;
                runs[i].TextDecorations = state == 2 ? TextDecorations.Underline : null;
                runs[i].Background = state == 3 ? NextBrush : null;
            }
        }

        private void Panels_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.WidthChanged)
                return;
            EditorPanels.Columns = e.NewSize.Width >= 630 ? 2 : 1;
            MetricCards.Columns = EditorPanels.Columns == 1 ? 2 : 4;
            UpdatePanelHeight();
        }

        private void Complete_Click(object sender, RoutedEventArgs e) => Completed?.Invoke();
        private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();
        private void End_Click(object sender, RoutedEventArgs e) => EndRequested?.Invoke();

        private void Entry_SelectionChanged(object sender, RoutedEventArgs e) => FollowReference();

        private void FollowReference()
        {
            if (followPending || loading || !IsVisible || runs == null)
                return;
            followPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                followPending = false;
                if (!IsVisible || !ReferenceText.IsArrangeValid || runs.Length == 0)
                    return;
                string prefix = Entry.Text.Substring(0, Math.Min(Entry.SelectionStart, Entry.Text.Length))
                    .Replace("\r\n", "\n");
                int index = Math.Min(runs.Length - 1, new StringInfo(prefix).LengthInTextElements);
                Rect bounds = runs[index].ContentStart.GetCharacterRect(LogicalDirection.Forward);
                if (bounds.IsEmpty)
                    return;
                double top = ReferenceText.TranslatePoint(bounds.TopLeft, ReferenceScroller).Y;
                double bottom = top + bounds.Height;
                if (top < 0)
                    ReferenceScroller.ScrollToVerticalOffset(ReferenceScroller.VerticalOffset + top);
                else if (bottom > ReferenceScroller.ViewportHeight)
                    ReferenceScroller.ScrollToVerticalOffset(ReferenceScroller.VerticalOffset + bottom - ReferenceScroller.ViewportHeight);
            }));
        }

        private void ChangeFont(int delta)
        {
            preferences.FontPoints = Math.Max(12, Math.Min(28, preferences.FontPoints + delta));
            PreferencesChanged?.Invoke();
            Entry.Focus();
        }

        private void Smaller_Click(object sender, RoutedEventArgs e) => ChangeFont(-1);
        private void Larger_Click(object sender, RoutedEventArgs e) => ChangeFont(1);
        private void Settings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

        private void Sound_Click(object sender, RoutedEventArgs e)
        {
            preferences.Sounds = !preferences.Sounds;
            PreferencesChanged?.Invoke();
            Entry.Focus();
        }

        private void Focus_Click(object sender, RoutedEventArgs e)
        {
            preferences.Concentration = !preferences.Concentration;
            PreferencesChanged?.Invoke();
            Entry.Focus();
        }

        private bool Confirm(string question) => MessageBox.Show(Window.GetWindow(this), question, "IT Digitação",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            if (Confirm("Reiniciar este texto? O tempo e as tentativas já registradas serão mantidos.") && session.RestartActivity())
                Open(session);
        }

        private void Abandon_Click(object sender, RoutedEventArgs e)
        {
            if (!Confirm("Abandonar este texto e escolher outro? As tentativas da sessão serão mantidas."))
                return;
            session.AbandonActivity();
            if (!session.Finished)
                Abandoned?.Invoke();
        }

        private static string DisplayProgress(TypingEngine engine) =>
            !engine.Complete && engine.Progress >= 100 ? "100% preenchido · remova o conteúdo excedente" :
            Math.Floor(engine.Progress).ToString("0") + "%";
        public static string Percent(double? value) => value.HasValue ? value.Value.ToString("0.#") + "%" : "—";
        public static string Number(double? value) => value.HasValue ? value.Value.ToString("0.#") : "—";

        private static Brush Frozen(string color)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(color);
            brush.Freeze();
            return brush;
        }
    }
}
