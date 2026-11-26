using System;
using System.Windows;
using System.Windows.Controls;
using ITDigitacao.Core;

namespace ITDigitacao.App.Views
{
    public partial class QuestionView : UserControl
    {
        private QuizQuestion question;

        public QuestionView() { InitializeComponent(); }
        public event Action ContinueRequested;

        public void Open(QuizQuestion value)
        {
            question = value;
            Prompt.Text = value.Prompt;
            Choices.ItemsSource = value.Options;
            Choices.IsEnabled = true;
            Feedback.Text = "";
            ContinueButton.Content = "Pular pergunta";
        }

        public void FocusChoices() => ContinueButton.Focus();

        private void Answer_Click(object sender, RoutedEventArgs e)
        {
            bool correct = Equals(((Button)sender).DataContext, question.Options[question.AnswerIndex]);
            Feedback.Text = (correct ? "Isso mesmo! " : "Vamos conferir. ") + question.Explanation;
            Choices.IsEnabled = false;
            ContinueButton.Content = "Continuar";
            ContinueButton.Focus();
        }

        private void Continue_Click(object sender, RoutedEventArgs e) => ContinueRequested?.Invoke();
    }
}
