using System;
using System.Windows;
using System.Windows.Automation.Peers;

namespace ITDigitacao.App
{
    public partial class MainWindow : Window
    {
        private const string WelcomeMessage = "Bem-vindo ao IT Digitação!";
        private const string SuccessMessage = "Tudo pronto! A interface está funcionando.";

        public MainWindow()
        {
            InitializeComponent();

            Rect workArea = SystemParameters.WorkArea;
            MinWidth = Math.Min(MinWidth, workArea.Width);
            MinHeight = Math.Min(MinHeight, workArea.Height);
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);

            StatusMessage.Text = WelcomeMessage;
        }

        private void TestInterface_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus(SuccessMessage);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus(WelcomeMessage);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UpdateStatus(string message)
        {
            if (StatusMessage.Text == message)
            {
                return;
            }

            StatusMessage.Text = message;
            UIElementAutomationPeer.CreatePeerForElement(StatusMessage)
                ?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
