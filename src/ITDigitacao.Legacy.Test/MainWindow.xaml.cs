using System;
using System.Windows;
namespace ITDigitacao.Legacy.Test {
  public partial class MainWindow : Window {
    public MainWindow() { InitializeComponent(); StatusText.Text = "CLR: " + Environment.Version.ToString() + " — confira que inicia com CLR 2.0.50727."; }
    private void TestButton_Click(object sender, RoutedEventArgs e) { StatusText.Text = "Botão funcional. Texto recebido: " + TypingBox.Text + " | CLR " + Environment.Version.ToString(); }
  }
}
