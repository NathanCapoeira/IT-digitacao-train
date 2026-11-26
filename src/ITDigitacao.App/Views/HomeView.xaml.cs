using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ITDigitacao.App.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            DefaultDuration.IsChecked = true;
            Benefits.Tag = 3;
            Benefits.ItemsSource = new[]
            {
                new Benefit("Aprenda", "Descubra conteúdos úteis e curiosos.", Icon("LibraryIcon"), Brush("BrandTintBrush")),
                new Benefit("Pratique", "Melhore sua digitação de forma divertida.", Icon("KeyboardIcon"), Brush("BrandTintBrush")),
                new Benefit("Evolua", "Supere seus desafios a cada atividade.", Icon("GrowthIcon"), Brush("AchievementTintBrush"))
            };
        }

        public int SelectedMinutes { get; private set; }

        public event EventHandler StartRequested;

        private ImageSource Icon(string key)
        {
            return (ImageSource)FindResource(key);
        }

        private Brush Brush(string key)
        {
            return (Brush)FindResource(key);
        }

        private void Duration_Checked(object sender, RoutedEventArgs e)
        {
            SelectedMinutes = int.Parse((string)((RadioButton)sender).Content, CultureInfo.InvariantCulture);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            StartRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Layout_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.WidthChanged)
            {
                return;
            }

            double width = e.NewSize.Width;
            DurationGrid.Columns = width >= 700 ? 6 : 3;
            Benefits.Tag = width >= 650 ? 3 : 1;
            HeroTitle.FontSize = width >= 690 ? 35 : 27;
            HeroCopy.Margin = width >= 690 ? new Thickness(28, 22, 0, 22) : new Thickness(18, 18, 0, 18);
        }

        private sealed class Benefit
        {
            public Benefit(string title, string description, ImageSource icon, Brush tileBrush)
            {
                Title = title;
                Description = description;
                Icon = icon;
                TileBrush = tileBrush;
            }

            public string Title { get; }
            public string Description { get; }
            public ImageSource Icon { get; }
            public Brush TileBrush { get; }
        }
    }
}
