using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ITDigitacao.Core;

namespace ITDigitacao.App.Views
{
    public partial class LibraryView : UserControl
    {
        private readonly Random random = new Random();
        private readonly ActivityRepository repository;
        private string loadError;
        private string difficulty;
        private Category category;
        private List<Activity> matches = new List<Activity>();
        private SessionManager session;

        public LibraryView()
        {
            InitializeComponent();
            CategoryCards.Tag = 5;
            AllFilter.IsChecked = true;
            try
            {
                using (var stream = File.OpenRead(Path.Combine(Path.GetDirectoryName(typeof(LibraryView).Assembly.Location), "atividades.json")))
                    repository = ActivityRepository.Load(stream);
            }
            catch (InvalidDataException)
            {
                loadError = "O catálogo de atividades está inválido. Extraia novamente o pacote completo do aplicativo.";
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                loadError = "Não foi possível abrir atividades.json. Mantenha o arquivo na pasta do aplicativo.";
            }
            Render();
        }

        public event Action<Activity> PracticeRequested;
        public event Action ResumeRequested;
        public event Action PageChanged;

        public void Open(SessionManager current)
        {
            session = current;
            Encouragement.Visibility = Visibility.Collapsed;
            category = null;
            RefreshSession();
            Render();
        }

        public void ShowEncouragement() => Encouragement.Visibility = Visibility.Visible;

        public void RefreshSession()
        {
            if (session == null)
                return;
            var time = TimeSpan.FromSeconds(Math.Ceiling(session.Remaining.TotalSeconds));
            SessionText.Text = session.Started
                ? $"Sessão em andamento · {time:mm\\:ss} restantes"
                : $"Tempo de prática selecionado: {session.Duration.TotalMinutes:0} minutos";
            ResumeButton.Visibility = session.Activity == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Layout_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                CategoryCards.Tag = Math.Max(1, Math.Min(5, (int)(e.NewSize.Width / 173)));
        }

        private void Render()
        {
            TitleText.Text = category?.Name ?? "Biblioteca educativa";
            DescriptionText.Text = category?.Description ?? "Descubra coisas incríveis enquanto pratica digitação";
            BannerArt.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(AssetPath(category == null
                ? "mascote-ensinando.png" : "Categorias/" + category.Id + ".png"), UriKind.Absolute));
            BackButton.Visibility = category == null ? Visibility.Collapsed : Visibility.Visible;
            matches = repository?.Find(SearchBox.Text, difficulty, category?.Id) ?? new List<Activity>();
            bool showActivities = category != null || !string.IsNullOrWhiteSpace(SearchBox.Text);
            CategoryCards.ItemsSource = showActivities || repository == null ? null : repository.Categories
                .Select(item => new CategoryCard(item, matches.Count(activity => activity.CategoryId == item.Id)))
                .Where(item => item.Count > 0).ToArray();
            ActivityCards.ItemsSource = showActivities ? matches.Select(activity => new ActivityCard(activity)).ToArray() : null;
            CountsText.Text = repository == null ? "" : category == null
                ? $"{repository.Categories.Count} categorias • {repository.Activities.Count} atividades no catálogo"
                : $"{matches.Count} de {repository.Activities.Count(activity => activity.CategoryId == category.Id)} atividades";
            EmptyCard.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyText.Text = loadError ?? (category != null && repository != null &&
                !repository.Activities.Any(activity => activity.CategoryId == category.Id)
                ? "Esta categoria ainda não tem atividades."
                : "Nenhuma atividade encontrada. Experimente alterar a pesquisa ou a dificuldade.");
            ResetButton.Visibility = loadError == null ? Visibility.Visible : Visibility.Collapsed;
            RandomButton.IsEnabled = matches.Count != 0;
        }

        private static string AssetPath(string relative) => "pack://application:,,,/IT-Digitacao;component/Assets/" + relative;

        private void Search_Changed(object sender, TextChangedEventArgs e)
        {
            if (CategoryCards != null)
                Render();
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            difficulty = (string)((RadioButton)sender).Tag;
            Render();
        }

        private void Category_Click(object sender, RoutedEventArgs e)
        {
            category = ((CategoryCard)((Button)sender).DataContext).Category;
            Render();
            PageChanged?.Invoke();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            category = null;
            Render();
            PageChanged?.Invoke();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            SearchBox.Focus();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            difficulty = null;
            AllFilter.IsChecked = true;
            SearchBox.Clear();
            Render();
        }

        private void Resume_Click(object sender, RoutedEventArgs e) => ResumeRequested?.Invoke();

        private void Practice_Click(object sender, RoutedEventArgs e)
        {
            PracticeRequested?.Invoke(((ActivityCard)((Button)sender).DataContext).Activity);
        }

        private void Random_Click(object sender, RoutedEventArgs e)
        {
            if (matches.Count == 0)
                return;
            var candidates = matches.Where(activity => activity.Id != session?.LastCompletedId).ToList();
            if (candidates.Count == 0)
                candidates = matches;
            PracticeRequested?.Invoke(candidates[random.Next(candidates.Count)]);
        }

        private sealed class CategoryCard
        {
            public CategoryCard(Category category, int count)
            {
                Category = category;
                Count = count;
                Accent = (Brush)new BrushConverter().ConvertFromString(category.Color);
            }

            public Category Category { get; }
            public int Count { get; }
            public Brush Accent { get; }
            public string Name => Category.Name;
            public string Description => Category.Description;
            public string Artwork => AssetPath("Categorias/" + Category.Id + ".png");
            public string CountLabel => Count == 1 ? "1 atividade  →" : $"{Count} atividades  →";
        }

        private sealed class ActivityCard
        {
            public ActivityCard(Activity activity) { Activity = activity; }
            public Activity Activity { get; }
            public string Title => Activity.Title;
            public string Description => Activity.Description;
            public string Difficulty => Activity.Difficulty;
            public string DifficultyTint => Difficulty == "Iniciante" ? "#E1F4E5" :
                Difficulty == "Intermediário" ? "#FFF3D7" : "#FFF1F2";
            public string DifficultyInk => Difficulty == "Iniciante" ? "#176037" :
                Difficulty == "Intermediário" ? "#714900" : "#A60000";
            public string LengthLabel => $"{Activity.WordCount} palavras · aprox. {Activity.EstimatedMinutes} min";
            public string PracticeLabel => "Praticar: " + Activity.Title;
        }
    }
}
