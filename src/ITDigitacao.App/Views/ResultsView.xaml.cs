using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ITDigitacao.Core;

namespace ITDigitacao.App.Views
{
    public partial class ResultsView : UserControl
    {
        private readonly Random random = new Random();

        public ResultsView()
        {
            InitializeComponent();
            MetricGrid.Tag = 3;
        }

        public event Action NewRequested;
        public event Action BackRequested;
        public event Action ExitRequested;

        public void ShowResult(SessionManager session, Preferences preferences)
        {
            string[] messages = { "Cada palavra é um novo passo.", "Parabéns pela sua dedicação!",
                "Você está desenvolvendo uma nova habilidade.", "Continue descobrindo coisas novas." };
            Message.Text = !session.Started ? "Comece uma atividade quando estiver pronto para praticar." :
                messages[random.Next(messages.Length)];
            int stars = SessionAwards.Stars(session.Evaluated, session.Accuracy);
            StarIcons.Text = stars > 0 ? new string('★', stars) + new string('☆', 3 - stars) : "";
            StarIcons.Visibility = stars == 0 ? Visibility.Collapsed : Visibility.Visible;
            StarExplanation.Text = stars == 0 ? "As estrelas aparecem a partir de 100 caracteres avaliados. Pratique no seu ritmo!" :
                $"{stars} de 3 estrelas • 1 abaixo de 80%, 2 de 80% a menos de 95%, 3 a partir de 95%.";
            MetricGrid.ItemsSource = new[]
            {
                new { Label = "Tempo utilizado", Value = session.Elapsed.ToString(@"mm\:ss") },
                new { Label = "Precisão", Value = PracticeView.Percent(session.Accuracy) },
                new { Label = "Palavras por minuto", Value = PracticeView.Number(session.WordsPerMinute) },
                new { Label = "Erros cometidos", Value = session.TotalErrors.ToString() },
                new { Label = "Caracteres corretos (tentativas)", Value = session.TotalCorrect.ToString() },
                new { Label = "Textos concluídos", Value = session.CompletedCount.ToString() }
            };
            AchievementList.ItemsSource = new[]
            {
                Award("Primeira missão", "Conclua um texto.", SessionAwards.FirstMission(session.CompletedCount)),
                Award("Mira perfeita", "Atinja 95% de precisão com pelo menos 100 caracteres avaliados.", SessionAwards.PerfectAim(session.Evaluated, session.Accuracy)),
                Award("Superpraticante", "Conclua três textos diferentes.", SessionAwards.SuperPractitioner(session.CompletedCount))
            };
            if (preferences.ReducedEffects)
                ResultsBanner.Background = (System.Windows.Media.Brush)FindResource("BrandDarkBrush");
            else
                ResultsBanner.ClearValue(Border.BackgroundProperty);
            StarCard.BeginAnimation(OpacityProperty, null);
            if (preferences.Animations && !preferences.ReducedEffects)
                StarCard.BeginAnimation(OpacityProperty, new DoubleAnimation(0.75, 1, TimeSpan.FromMilliseconds(150)));
        }

        private static object Award(string name, string explanation, bool earned) => new
        {
            Icon = earned ? "★" : "○",
            Label = $"{name} · {(earned ? "Conquistada" : "Em aberto")}",
            Value = explanation
        };

        private void Metrics_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                MetricGrid.Tag = e.NewSize.Width >= 650 ? 3 : 2;
        }

        private void New_Click(object sender, RoutedEventArgs e) => NewRequested?.Invoke();
        private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke();
        private void Exit_Click(object sender, RoutedEventArgs e) => ExitRequested?.Invoke();
    }
}
