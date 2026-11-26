using System;
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

        public void Open(SessionManager current)
        {
            session = current;
            var engine = session.Engine;
            ActivityTitle.Text = session.Activity.Title;
            ActivityLevel.Text = session.Activity.Difficulty;
            if (engine != renderedEngine)
            {
                renderedEngine = engine;
                ReferenceText.Inlines.Clear();
                runs = new Run[engine.Reference.Length];
                feedback = new int[runs.Length];
                for (int i = 0; i < runs.Length; i++)
                {
                    var run = new Run(engine.Reference[i] == "\n" ? "\r\n" : engine.Reference[i]);
                    runs[i] = run;
                    ReferenceText.Inlines.Add(run);
                }

                loading = true;
                Entry.Text = engine.InputText.Replace("\n", "\r\n");
                Entry.CaretIndex = Entry.Text.Length;
                Entry.ClearUndo();
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
            WarningText.Visibility = WarningText.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            MetricText.Text = $"Precisão {Percent(session.Accuracy)}  ·  PPM {Number(session.WordsPerMinute)}  ·  " +
                $"Erros cometidos {session.TotalErrors}  ·  Corrigidos {session.CorrectedErrors}  ·  No texto {session.CurrentErrors}";
            ProgressText.Text = $"Progresso do texto: {DisplayProgress(session.Engine)}";
            ProgressMeter.Value = session.Engine.Progress;
            CompleteButton.IsEnabled = !session.Finished && session.Engine.Complete;
            Entry.IsReadOnly = session.Finished;
        }

        private void Entry_Changed(object sender, TextChangedEventArgs e)
        {
            if (loading || session?.Engine == null || session.Finished)
                return;
            session.Apply(Entry.Text);
            UpdateFeedback();
            UpdateMetrics();
            Changed?.Invoke();
        }

        private void UpdateFeedback()
        {
            var engine = session.Engine;
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
            if (e.WidthChanged)
                EditorPanels.Columns = e.NewSize.Width >= 630 ? 2 : 1;
        }

        private void Complete_Click(object sender, RoutedEventArgs e) => Completed?.Invoke();
        private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();
        private void End_Click(object sender, RoutedEventArgs e) => EndRequested?.Invoke();

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
