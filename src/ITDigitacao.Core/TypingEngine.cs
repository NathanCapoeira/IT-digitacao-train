using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ITDigitacao.Core
{
    public sealed class TypingEngine
    {
        private string[] input = Array.Empty<string>();

        public TypingEngine(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new ArgumentException("O texto de referência precisa conter texto.", nameof(reference));
            Reference = Split(reference);
        }

        public string[] Reference { get; }
        public int CorrectAttempts { get; private set; }
        public int ErrorAttempts { get; private set; }
        public int CurrentCorrect { get; private set; }
        public int CurrentErrors => input.Length - CurrentCorrect;
        public string InputText => string.Concat(input);
        public int Entered => input.Length;
        public bool Complete => CurrentCorrect == Reference.Length && CurrentErrors == 0;
        public double Progress => 100.0 * CurrentCorrect / Reference.Length;

        public bool IsCorrectAt(int index)
        {
            return index < input.Length && index < Reference.Length && input[index] == Reference[index];
        }

        public int Apply(string text)
        {
            var updated = Split(text);
            int prefix = 0;
            while (prefix < input.Length && prefix < updated.Length && input[prefix] == updated[prefix])
                prefix++;

            int suffix = 0;
            while (suffix < input.Length - prefix && suffix < updated.Length - prefix &&
                input[input.Length - suffix - 1] == updated[updated.Length - suffix - 1])
                suffix++;

            int attempts = updated.Length - suffix - prefix;
            for (int index = prefix; index < updated.Length - suffix; index++)
            {
                if (index < Reference.Length && updated[index] == Reference[index])
                    CorrectAttempts++;
                else
                    ErrorAttempts++;
            }

            input = updated;
            CurrentCorrect = Enumerable.Range(0, input.Length).Count(IsCorrectAt);
            return attempts;
        }

        private static string[] Split(string text)
        {
            var normalized = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Normalize(NormalizationForm.FormC);
            var indices = StringInfo.ParseCombiningCharacters(normalized);
            return indices.Select((start, i) =>
                normalized.Substring(start, (i + 1 == indices.Length ? normalized.Length : indices[i + 1]) - start)).ToArray();
        }
    }
}
