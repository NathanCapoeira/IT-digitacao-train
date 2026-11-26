using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ITDigitacao.Core
{
    public sealed class TypingEngine
    {
        private static readonly Regex UnpairedSurrogate = new Regex(
            @"[\uD800-\uDBFF](?![\uDC00-\uDFFF])|(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]");
        private string[] input = Array.Empty<string>();
        private bool[] unresolvedAttempts = Array.Empty<bool>();

        public TypingEngine(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new ArgumentException("O texto de referência precisa conter texto.", nameof(reference));
            Reference = Array.AsReadOnly(Split(reference));
        }

        public IReadOnlyList<string> Reference { get; }
        public int CorrectAttempts { get; private set; }
        public int ErrorAttempts { get; private set; }
        public int ResolvedErrors { get; private set; }
        public int CurrentCorrect { get; private set; }
        public int CurrentErrors => input.Length - CurrentCorrect;
        public string InputText => string.Concat(input);
        public int Entered => input.Length;
        public bool Complete => CurrentCorrect == Reference.Count && CurrentErrors == 0;
        public double Progress => 100.0 * CurrentCorrect / Reference.Count;

        public bool IsCorrectAt(int index)
        {
            return index >= 0 && index < input.Length && index < Reference.Count && input[index] == Reference[index];
        }

        public int Apply(string text) => ApplyEdit(text, -1, true);

        public int ApplyEdit(string text, int offset, bool countAttempts)
        {
            text = text ?? "";
            var updated = Split(text);
            int limit = offset < 0 ? int.MaxValue : Split(text.Substring(0, Math.Min(offset, text.Length))).Length;
            int prefix = 0;
            while (prefix < limit && prefix < input.Length && prefix < updated.Length && input[prefix] == updated[prefix])
                prefix++;

            int suffix = 0;
            while (suffix < input.Length - prefix && suffix < updated.Length - prefix &&
                input[input.Length - suffix - 1] == updated[updated.Length - suffix - 1])
                suffix++;

            var updatedErrors = new bool[updated.Length];
            Array.Copy(unresolvedAttempts, updatedErrors, prefix);
            Array.Copy(unresolvedAttempts, input.Length - suffix, updatedErrors, updated.Length - suffix, suffix);
            for (int index = prefix; index < input.Length - suffix; index++)
            {
                if (unresolvedAttempts[index])
                    ResolvedErrors++;
            }

            int attempts = countAttempts ? updated.Length - suffix - prefix : 0;
            CurrentCorrect = 0;
            for (int index = 0; index < updated.Length; index++)
            {
                bool correct = index < Reference.Count && updated[index] == Reference[index];
                if (correct)
                {
                    CurrentCorrect++;
                    if (updatedErrors[index])
                    {
                        updatedErrors[index] = false;
                        ResolvedErrors++;
                    }
                }

                if (countAttempts && index >= prefix && index < updated.Length - suffix)
                {
                    if (correct)
                        CorrectAttempts++;
                    else
                    {
                        ErrorAttempts++;
                        updatedErrors[index] = true;
                    }
                }
            }

            input = updated;
            unresolvedAttempts = updatedErrors;
            return attempts;
        }

        private static string[] Split(string text)
        {
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var normalized = UnpairedSurrogate.Replace(lines, "\uFFFD").Normalize(NormalizationForm.FormC);
            var indices = StringInfo.ParseCombiningCharacters(normalized);
            return indices.Select((start, i) =>
                normalized.Substring(start, (i + 1 == indices.Length ? normalized.Length : indices[i + 1]) - start)).ToArray();
        }
    }
}
