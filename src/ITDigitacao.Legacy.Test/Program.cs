using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ITDigitacao.Legacy.Test
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application application = new Application();
            application.Run(new CompatibilityWindow());
        }
    }

    internal sealed class CompatibilityWindow : Window
    {
        private readonly TextBox typingBox;
        private readonly TextBlock status;

        public CompatibilityWindow()
        {
            Title = "IT Digitação — Teste Legacy";
            Width = 760;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

            Border frame = new Border();
            frame.Margin = new Thickness(24);
            frame.Background = Brushes.White;
            frame.BorderBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
            frame.BorderThickness = new Thickness(1);
            frame.CornerRadius = new CornerRadius(14);

            DockPanel layout = new DockPanel();
            Border banner = new Border();
            banner.Background = new SolidColorBrush(Color.FromRgb(181, 18, 27));
            banner.CornerRadius = new CornerRadius(14, 14, 0, 0);
            banner.Padding = new Thickness(20);
            TextBlock heading = new TextBlock();
            heading.Text = "IT DIGITAÇÃO • TESTE DE COMPATIBILIDADE";
            heading.Foreground = Brushes.White;
            heading.FontSize = 22;
            heading.FontWeight = FontWeights.Bold;
            banner.Child = heading;
            DockPanel.SetDock(banner, Dock.Top);
            layout.Children.Add(banner);

            StackPanel content = new StackPanel();
            content.Margin = new Thickness(28);
            content.VerticalAlignment = VerticalAlignment.Center;

            Image logo = new Image();
            logo.Source = new BitmapImage(new Uri("Assets/logo-instituto.png", UriKind.Relative));
            logo.Height = 74;
            logo.HorizontalAlignment = HorizontalAlignment.Left;
            logo.Margin = new Thickness(0, 0, 0, 18);
            content.Children.Add(logo);

            TextBlock prompt = new TextBlock();
            prompt.Text = "Digite uma frase com acentuação brasileira:";
            prompt.FontSize = 18;
            prompt.Margin = new Thickness(0, 0, 0, 10);
            content.Children.Add(prompt);

            typingBox = new TextBox();
            typingBox.FontSize = 20;
            typingBox.Padding = new Thickness(10);
            typingBox.Text = "Ação, coração, ônibus e informação.";
            content.Children.Add(typingBox);

            Button testButton = new Button();
            testButton.Content = "Testar botão";
            testButton.Width = 150;
            testButton.Height = 42;
            testButton.Margin = new Thickness(0, 18, 0, 12);
            testButton.HorizontalAlignment = HorizontalAlignment.Left;
            testButton.Click += TestButton_Click;
            content.Children.Add(testButton);

            status = new TextBlock();
            status.FontSize = 15;
            status.TextWrapping = TextWrapping.Wrap;
            status.Text = "CLR: " + Environment.Version.ToString() + " — confirme CLR 2.0.50727.";
            content.Children.Add(status);

            layout.Children.Add(content);
            frame.Child = layout;
            Content = frame;
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            status.Text = "Botão funcional. Texto recebido: " + typingBox.Text + " | CLR " + Environment.Version.ToString();
        }
    }
}
