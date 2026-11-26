using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ITDigitacao.App.Views
{
    public partial class GuideView : UserControl
    {
        private const double Unit = 42;
        private const double RowHeight = 43;
        private readonly Dictionary<int, KeyTile> keys = new Dictionary<int, KeyTile>();
        private KeyTile highlighted;
        private readonly string[] fingers = {
            "Mínimo esquerdo", "Anelar esquerdo", "Médio esquerdo", "Indicador esquerdo",
            "Indicador direito", "Médio direito", "Anelar direito", "Mínimo direito", "Polegares"
        };
        private readonly Brush[] colors = {
            Brushes.MistyRose, Brushes.LemonChiffon, Brushes.Honeydew, Brushes.LightCyan,
            Brushes.Lavender, Brushes.PaleTurquoise, Brushes.PeachPuff, Brushes.Thistle, Brushes.Gainsboro
        };

        public GuideView()
        {
            InitializeComponent();
            Add(0, 0, 1, 0x29, "'  \"", 0);
            string[] digits = { "1 !", "2 @", "3 #", "4 $", "5 %", "6 ¨", "7 &", "8 *", "9 (", "0 )", "- _", "= +" };
            int[] digitFingers = { 0, 1, 2, 3, 3, 4, 4, 5, 6, 7, 7, 7 };
            for (int i = 0; i < digits.Length; i++)
                Add(0, i + 1, 1, i + 2, digits[i], digitFingers[i]);
            Add(0, 13, 2, 0x0E, "Backspace", 7);

            Add(1, 0, 1.5, 0x0F, "Tab", 0);
            Letters(1, 1.5, 0x10, "QWERTYUIOP");
            Add(1, 11.5, 1, 0x1A, "´  `", 7);
            Add(1, 12.5, 1, 0x1B, "[  {", 7);

            Add(2, 0, 1.75, 0x3A, "Caps Lock", 0);
            Letters(2, 1.75, 0x1E, "ASDFGHJKLÇ");
            Add(2, 11.75, 1, 0x28, "~  ^", 7);
            Add(2, 12.75, 1, 0x2B, "]  }", 7);
            AddEnter();

            Add(3, 0, 1.25, 0x2A, "Shift", 0);
            Add(3, 1.25, 1, 0x56, "\\  |", 0);
            Letters(3, 2.25, 0x2C, "ZXCVBNM", new[] { 0, 1, 2, 3, 3, 4, 4 });
            Add(3, 9.25, 1, 0x33, ",  <", 5);
            Add(3, 10.25, 1, 0x34, ".  >", 6);
            Add(3, 11.25, 1, 0x35, ";  :", 7);
            Add(3, 12.25, 1, 0x73, "/  ?", 7);
            Add(3, 13.25, 1.75, 0x36, "Shift", 7);

            Add(4, 0, 1.5, 0x1D, "Ctrl", 0);
            Add(4, 1.5, 1.15, 0x15B, "Win", 0);
            Add(4, 2.65, 1.15, 0x38, "Alt", 8);
            Add(4, 3.8, 6.6, 0x39, "Espaço", 8);
            Add(4, 10.4, 1.15, 0x138, "AltGr", 8);
            Add(4, 11.55, 1.15, 0x15C, "Win", 7);
            Add(4, 12.7, 1.15, 0x15D, "Menu", 7);
            Add(4, 13.85, 1.15, 0x11D, "Ctrl", 7);

            FingerLegend.ItemsSource = fingers.Select((label, index) => new { Label = label, Color = colors[index] }).ToArray();
        }

        public event Action ExerciseRequested;

        public void ShowKey(int scanCode)
        {
            if (highlighted != null)
                highlighted.Background(highlighted.Color);
            if (!keys.TryGetValue(scanCode, out highlighted))
            {
                KeyHint.Text = "Esta tecla fica fora da área principal ilustrada.";
                return;
            }

            highlighted.Background(Brushes.Gold);
            var tip = highlighted.Label == "F" || highlighted.Label == "J" ? "Use a marca em relevo como referência."
                : scanCode == 0x1A || scanCode == 0x28 ? "Pressione o acento e depois a letra para combiná-los."
                : scanCode == 0x138 ? "Use AltGr junto com uma tecla que tenha um terceiro símbolo."
                : "Depois, volte à posição inicial.";
            KeyHint.Text = $"{highlighted.Label} — {fingers[highlighted.Finger]}. {tip}";
        }

        private void Letters(int row, double x, int scan, string letters, int[] map = null)
        {
            map = map ?? new[] { 0, 1, 2, 3, 3, 4, 4, 5, 6, 7 };
            for (int i = 0; i < letters.Length; i++)
                Add(row, x + i, 1, scan + i, letters[i].ToString(), map[i]);
        }

        private void Add(int row, double x, double width, int scan, string label, int finger)
        {
            bool anchor = label == "F" || label == "J";
            var tile = new Border
            {
                Width = width * Unit - 4, Height = RowHeight - 4, Background = colors[finger],
                CornerRadius = new CornerRadius(5), BorderThickness = new Thickness(anchor ? 2 : 1),
                BorderBrush = anchor ? Brushes.DarkRed : Brushes.DarkGray,
                Child = new TextBlock
                {
                    Text = anchor ? label + "  ·" : label, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                }
            };
            Canvas.SetLeft(tile, x * Unit + 2);
            Canvas.SetTop(tile, row * RowHeight + 2);
            KeyboardCanvas.Children.Add(tile);
            keys.Add(scan, new KeyTile(label, finger, colors[finger], brush => tile.Background = brush));
        }

        private void AddEnter()
        {
            var shape = new Path
            {
                Data = Geometry.Parse("M 0,0 H 59 V 82 H 10.5 V 39 H 0 Z"),
                Fill = colors[7], Stroke = Brushes.DarkGray, StrokeThickness = 1, StrokeLineJoin = PenLineJoin.Round
            };
            Canvas.SetLeft(shape, 13.5 * Unit + 2);
            Canvas.SetTop(shape, RowHeight + 2);
            KeyboardCanvas.Children.Add(shape);
            var label = new TextBlock { Text = "Enter", FontSize = 12, FontWeight = FontWeights.SemiBold };
            Canvas.SetLeft(label, 13.5 * Unit + 14);
            Canvas.SetTop(label, RowHeight + 25);
            KeyboardCanvas.Children.Add(label);
            keys.Add(0x1C, new KeyTile("Enter", 7, colors[7], brush => shape.Fill = brush));
        }

        private void Exercise_Click(object sender, RoutedEventArgs e) => ExerciseRequested?.Invoke();

        private sealed class KeyTile
        {
            public KeyTile(string label, int finger, Brush color, Action<Brush> background)
            {
                Label = label;
                Finger = finger;
                Color = color;
                Background = background;
            }

            public string Label { get; }
            public int Finger { get; }
            public Brush Color { get; }
            public Action<Brush> Background { get; }
        }
    }
}
