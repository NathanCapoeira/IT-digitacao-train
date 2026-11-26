using System;
using System.Windows;
using System.Windows.Controls;

namespace ITDigitacao.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Rect workArea = SystemParameters.WorkArea;
            MinWidth = Math.Min(MinWidth, workArea.Width);
            MinHeight = Math.Min(MinHeight, workArea.Height);
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);

            ShowPage(HomeTab);
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            ShowPage((RadioButton)sender);
        }

        private void StartRequested(object sender, EventArgs e)
        {
            ShowPage(LibraryTab);
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
            Close();
        }

        private void ShowPage(RadioButton tab)
        {
            tab.IsChecked = true;
            bool isHome = tab == HomeTab;
            HomePage.Visibility = isHome ? Visibility.Visible : Visibility.Collapsed;
            SectionPage.Visibility = isHome ? Visibility.Collapsed : Visibility.Visible;
            PageScroller.ScrollToTop();

            SelectedPlan.Visibility = Visibility.Collapsed;
            if (tab == LibraryTab)
            {
                SectionTitle.Text = "Biblioteca educativa";
                SectionDescription.Text = "Aqui você encontrará assuntos para praticar e aprender.";
                SelectedPlan.Text = $"Tempo de prática selecionado: {HomePage.SelectedMinutes} minutos";
                SelectedPlan.Visibility = Visibility.Visible;
                SectionStatus.Text = "As categorias e atividades chegarão na próxima fase.";
            }
            else if (tab == GuideTab)
            {
                SectionTitle.Text = "Como digitar";
                SectionDescription.Text = "Conheça o teclado e desenvolva bons hábitos de digitação.";
                SectionStatus.Text = "O guia do teclado ABNT2 e as orientações de postura chegarão em uma fase futura.";
            }
            else if (tab == SettingsTab)
            {
                SectionTitle.Text = "Configurações";
                SectionDescription.Text = "Personalize sua experiência de aprendizado.";
                SectionStatus.Text = "As preferências de leitura e prática chegarão em uma fase futura.";
            }
        }
    }
}
