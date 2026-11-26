using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ITDigitacao.Core;

namespace ITDigitacao.App.Views
{
    public partial class LibraryView : UserControl
    {
        private readonly Random random = new Random();
        private ActivityRepository repository;
        private bool catalogLoaded;
        private readonly Dictionary<string, BitmapSource> artwork = new Dictionary<string, BitmapSource>();
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
        }

        private void LoadCatalog()
        {
            if (catalogLoaded)
                return;
            catalogLoaded = true;
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
        }

        public event Action<Activity> PracticeRequested;
        public event Action ResumeRequested;
        public event Action PageChanged;
        public event Action EndRequested;

        public void Open(SessionManager current)
        {
            LoadCatalog();
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
            SessionActions.Visibility = session.Activity != null || session.Started ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Layout_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.WidthChanged)
                return;

            CategoryCards.Tag = Math.Max(1, Math.Min(5, (int)(e.NewSize.Width / 173)));
            RandomMascot.Visibility = e.NewSize.Width >= 610 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Render()
        {
            TitleText.Text = category?.Name ?? "Biblioteca educativa";
            DescriptionText.Text = category?.Description ?? "Descubra coisas incríveis enquanto pratica digitação";
            BannerArt.Source = Artwork(category == null ? "mascote-ensinando.png" : "Categorias/" + category.Id + ".png",
                category == null ? 384 : 448);
            BackButton.Visibility = category == null ? Visibility.Collapsed : Visibility.Visible;
            matches = repository?.Find(SearchBox.Text, difficulty, category?.Id) ?? new List<Activity>();
            bool showActivities = category != null || !string.IsNullOrWhiteSpace(SearchBox.Text);
            CategoryCards.ItemsSource = showActivities || repository == null ? null : repository.Categories
                .Select(item => new CategoryCard(item, matches.Count(activity => activity.CategoryId == item.Id), this))
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

        private BitmapSource Artwork(string relative, int width)
        {
            if (artwork.TryGetValue(relative, out var existing))
                return existing;
            BitmapSource result = null;
            try
            {
                var uri = new Uri("/IT-Digitacao;component/Assets/" + relative, UriKind.Relative);
                var resource = Application.GetResourceStream(uri);
                if (resource != null)
                {
                    using (var stream = resource.Stream)
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.DecodePixelWidth = width;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        result = image;
                    }
                }
            }
            catch (Exception error) when (error is IOException || error is NotSupportedException || error is FormatException)
            {
                // The category text and its accent surface remain available if the illustration cannot load.
            }
            artwork[relative] = result;
            return result;
        }

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

        private void End_Click(object sender, RoutedEventArgs e) => EndRequested?.Invoke();

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
            public CategoryCard(Category category, int count, LibraryView view)
            {
                Category = category;
                Count = count;
                Accent = (Brush)new BrushConverter().ConvertFromString(category.Color);
                Artwork = view.Artwork("Categorias/" + category.Id + ".png", 448);
            }

            public Category Category { get; }
            public int Count { get; }
            public Brush Accent { get; }
            public string Name => Category.Name;
            public string Description => Category.Description;
            public BitmapSource Artwork { get; }
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
