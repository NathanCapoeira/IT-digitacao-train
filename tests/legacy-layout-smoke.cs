using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class LegacyLayoutSmoke
{
    private static object Field(object owner, string name)
    {
        return owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Layout(Window window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        window.UpdateLayout();
    }

    [STAThread]
    private static void Main()
    {
        new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Assembly app = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IT-Digitacao.exe"));
        Type mainWindow = app.GetType("ITDigitacao.Legacy.App.MainWindow", true);
        Window window = (Window)Activator.CreateInstance(mainWindow, true);
        try
        {
            Rect workArea = SystemParameters.WorkArea;
            Require(window.Width <= workArea.Width && window.Height <= workArea.Height, "Initial size exceeds the work area.");
            window.ShowActivated = false;
            window.Show();
            object repository = Field(window, "repo");
            IEnumerable activities = (IEnumerable)repository.GetType().GetProperty("Activities").GetValue(repository, null);
            object longest = activities.Cast<object>().OrderByDescending(activity =>
                ((string)activity.GetType().GetProperty("Text").GetValue(activity, null)).Length).First();
            mainWindow.GetMethod("OpenActivity", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(window, new[] { longest });

            TextBox entry = (TextBox)Field(window, "entry");
            TextBlock reference = (TextBlock)Field(window, "referenceBlock");
            ScrollViewer referenceScroll = (ScrollViewer)reference.Parent;
            Border inputCard = (Border)((DockPanel)((Grid)entry.Parent).Parent).Parent;
            Border referenceCard = (Border)((DockPanel)referenceScroll.Parent).Parent;
            Grid editor = (Grid)inputCard.Parent;
            ScrollViewer page = (ScrollViewer)Field(window, "pageScroll");
            int[,] sizes = { { 800, 560 }, { 1024, 728 }, { 1280, 680 }, { 1366, 728 }, { 470, 500 } };
            for (int i = 0; i < sizes.GetLength(0); i++)
            {
                window.Width = sizes[i, 0];
                window.Height = sizes[i, 1];
                Layout(window);
                string size = window.Width + "x" + window.Height;
                Require(entry.ActualHeight >= 55 && referenceScroll.ActualHeight >= 55, size + ": typing viewport collapsed.");
                Require(Math.Abs(inputCard.ActualHeight - referenceCard.ActualHeight) <= 1, size + ": panel heights differ.");
                Require(entry.FontSize == reference.FontSize, size + ": editor font sizes differ.");
                Require(editor.ActualWidth <= page.ViewportWidth + 1, size + ": editor exceeds page width.");
                if (i < 4)
                {
                    double referenceShare = referenceCard.ActualWidth / (referenceCard.ActualWidth + inputCard.ActualWidth);
                    Require(Grid.GetColumn(inputCard) == 1 && Math.Abs(referenceShare - .4) < .03, size + ": editor columns differ from 40/60.");
                }
                else
                {
                    Require(Grid.GetRow(inputCard) == 1 && Grid.GetColumn(inputCard) == 0, size + ": editor should stack on a narrow window.");
                }
            }

            window.Width = 800;
            window.Height = 560;
            Layout(window);
            entry.Text = "a";
            Layout(window);
            entry.ScrollToHome();
            referenceScroll.ScrollToBottom();
            Layout(window);
            Require(referenceScroll.ScrollableHeight > 0 && referenceScroll.VerticalOffset > 0 && entry.VerticalOffset == 0,
                "Scrolling the reference should leave the input at the top.");
            double editorHeight = editor.ActualHeight;
            double referenceOffset = referenceScroll.VerticalOffset;
            entry.Text = string.Join("\n", Enumerable.Repeat("Linha de teste com texto suficiente para rolar.", 40).ToArray());
            entry.ScrollToEnd();
            Layout(window);
            Require(entry.ExtentHeight > entry.ViewportHeight && entry.VerticalOffset > 0, "The input should scroll independently.");
            Require(Math.Abs(referenceScroll.VerticalOffset - referenceOffset) <= 1, "Scrolling the input moved the reference.");
            Require(Math.Abs(editor.ActualHeight - editorHeight) <= 1, "Long input expanded the editor.");
            Console.WriteLine("Legacy layout checks passed: four work-area sizes, narrow stacking, equal font and height, and independent scrolling.");
        }
        finally
        {
            window.Close();
            Application.Current.Shutdown();
        }
    }
}
