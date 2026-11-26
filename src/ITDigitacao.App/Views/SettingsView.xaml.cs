using System;
using System.Windows;
using System.Windows.Controls;
using ITDigitacao.Core;

namespace ITDigitacao.App.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView() { InitializeComponent(); }

        public event Action<Preferences> SaveRequested;
        public event Action ReturnRequested;

        public void Open(Preferences preferences, bool fromEditor)
        {
            ResetFields(preferences);
            ReturnButton.Content = fromEditor ? "Retornar à digitação" : "Retornar";
        }

        private void ResetFields(Preferences value)
        {
            FontSlider.Value = value.FontPoints;
            UpdatePreview(value.FontPoints);
            Sounds.IsChecked = value.Sounds;
            Metrics.IsChecked = value.ShowMetrics;
            HideMetricsInConcentration.IsChecked = value.HideMetricsInConcentration;
            Concentration.IsChecked = value.Concentration;
            VisualFeedback.IsChecked = value.VisualFeedback;
            Animations.IsChecked = value.Animations;
            ReducedEffects.IsChecked = value.ReducedEffects;
            SaveError.Text = "";
        }

        public void ShowSaveError() => SaveError.Text = "Não foi possível salvar no seu perfil. Você pode tentar novamente ou retornar.";

        private void UpdatePreview(double points)
        {
            if (Preview == null)
                return;
            Preview.FontSize = points * 96 / 72;
            FontLabel.Text = $"Tamanho da fonte: {points:0} pt";
        }

        private void Font_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview(e.NewValue);

        private void Options_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                OptionCards.Columns = e.NewSize.Width >= 610 ? 2 : 1;
        }

        private void Default_Click(object sender, RoutedEventArgs e) => ResetFields(new Preferences());
        private void Return_Click(object sender, RoutedEventArgs e) => ReturnRequested?.Invoke();
        private void Save_Click(object sender, RoutedEventArgs e) => SaveRequested?.Invoke(new Preferences
        {
            FontPoints = (int)FontSlider.Value,
            Sounds = Sounds.IsChecked == true,
            ShowMetrics = Metrics.IsChecked == true,
            HideMetricsInConcentration = HideMetricsInConcentration.IsChecked == true,
            Concentration = Concentration.IsChecked == true,
            VisualFeedback = VisualFeedback.IsChecked == true,
            Animations = Animations.IsChecked == true,
            ReducedEffects = ReducedEffects.IsChecked == true
        });
    }
}
