using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace ITDigitacao.Core
{
    [DataContract]
    public sealed class Category
    {
        [DataMember(IsRequired = true)] public string Id { get; private set; }
        [DataMember(IsRequired = true)] public string Name { get; private set; }
        [DataMember(IsRequired = true)] public string Description { get; private set; }
        [DataMember(IsRequired = true)] public string Color { get; private set; }
    }

    [DataContract]
    public sealed class QuizQuestion
    {
        [DataMember(IsRequired = true)] public string Prompt { get; private set; }
        [DataMember(IsRequired = true)] public string[] Options { get; private set; }
        [DataMember(IsRequired = true)] public int AnswerIndex { get; private set; }
        [DataMember(IsRequired = true)] public string Explanation { get; private set; }
    }

    [DataContract]
    public sealed class Activity
    {
        [DataMember(IsRequired = true)] public string Id { get; private set; }
        [DataMember(IsRequired = true)] public string CategoryId { get; private set; }
        [DataMember(IsRequired = true)] public string Title { get; private set; }
        [DataMember(IsRequired = true)] public string Description { get; private set; }
        [DataMember(IsRequired = true)] public string Difficulty { get; private set; }
        [DataMember(IsRequired = true)] public string Text { get; private set; }
        [DataMember(IsRequired = true)] public string[] Sources { get; private set; }
        [DataMember] public string Question { get; private set; }
        [DataMember] public QuizQuestion Quiz { get; private set; }

        public string CategoryName { get; internal set; }

        public static Activity HomeRowExercise() => new Activity
        {
            Id = "exercicio-posicao-inicial",
            CategoryName = "Guia ABNT2",
            Title = "Posição inicial das mãos",
            Difficulty = "Iniciante",
            Text = "ASDF JKLÇ\nasdf jklç\nasdfg hjklç"
        };

        public int WordCount => Regex.Matches(Text, @"[\p{L}\p{N}]+(?:[-’'][\p{L}\p{N}]+)*").Count;
        public int EstimatedMinutes => (int)Math.Ceiling(WordCount / 20.0);
    }

    public sealed class ActivityRepository
    {
        [DataContract]
#pragma warning disable 0649 // Preenchidos pelo DataContractJsonSerializer durante a leitura do catálogo.
        private sealed class Catalog
        {
            [DataMember(IsRequired = true)] public Category[] Categories;
            [DataMember(IsRequired = true)] public Activity[] Activities;
        }
#pragma warning restore 0649

        private static readonly CompareInfo SearchCulture = CultureInfo.GetCultureInfo("pt-BR").CompareInfo;
        private readonly Dictionary<string, Category> categoriesById;

        private ActivityRepository(Category[] categories, Activity[] activities)
        {
            Categories = Array.AsReadOnly(categories);
            Activities = Array.AsReadOnly(activities);
            categoriesById = categories.ToDictionary(category => category.Id, StringComparer.Ordinal);
            foreach (var activity in activities)
                activity.CategoryName = categoriesById[activity.CategoryId].Name;
        }

        // ReadOnlyCollection<T> implements IList<T> on .NET 3.5; callers get a read-only wrapper.
        public IList<Category> Categories { get; private set; }
        public IList<Activity> Activities { get; private set; }

        public static ActivityRepository Load(Stream stream)
        {
            try
            {
                using (var reader = new StreamReader(new NonClosingStream(stream), new UTF8Encoding(false, true), false, 4096))
                {
                    var json = reader.ReadToEnd().TrimStart('\uFEFF');
                    using (var bytes = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        var catalog = (Catalog)new DataContractJsonSerializer(typeof(Catalog)).ReadObject(bytes);
                        if (catalog == null || catalog.Categories == null || catalog.Categories.Length == 0 ||
                            catalog.Activities == null || catalog.Activities.Length == 0)
                            throw new InvalidDataException("O catálogo precisa conter categorias e atividades.");

                        ActivityValidator.Validate(catalog.Categories, catalog.Activities);
                        return new ActivityRepository(catalog.Categories, catalog.Activities);
                    }
                }
            }
            catch (Exception ex) when (ex is SerializationException || ex is DecoderFallbackException || ex is System.Xml.XmlException)
            {
                throw new InvalidDataException("O catálogo local não contém JSON UTF-8 válido.", ex);
            }
        }

        public List<Activity> Find(string query, string difficulty, string categoryId = null)
        {
            query = query?.Trim() ?? string.Empty;
            return Activities.Where(activity =>
                (string.IsNullOrEmpty(categoryId) || activity.CategoryId == categoryId) &&
                (string.IsNullOrEmpty(difficulty) || activity.Difficulty == difficulty) &&
                (query.Length == 0 || Matches(activity.Title, query) || Matches(activity.Description, query) ||
                    Matches(categoriesById[activity.CategoryId].Name, query) ||
                    Matches(categoriesById[activity.CategoryId].Description, query))).ToList();
        }

        private static bool Matches(string value, string query)
        {
            return SearchCulture.IndexOf(value, query, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
        }
    }

    // .NET 3.5 StreamReader lacks leaveOpen. Disposing this adapter leaves the caller's stream open.
    internal sealed class NonClosingStream : Stream
    {
        private readonly Stream inner;
        internal NonClosingStream(Stream inner) { if (inner == null) throw new ArgumentNullException("inner"); this.inner = inner; }
        public override bool CanRead { get { return inner.CanRead; } }
        public override bool CanSeek { get { return inner.CanSeek; } }
        public override bool CanWrite { get { return inner.CanWrite; } }
        public override long Length { get { return inner.Length; } }
        public override long Position { get { return inner.Position; } set { inner.Position = value; } }
        public override void Flush() { inner.Flush(); }
        public override int Read(byte[] buffer, int offset, int count) { return inner.Read(buffer, offset, count); }
        public override long Seek(long offset, SeekOrigin origin) { return inner.Seek(offset, origin); }
        public override void SetLength(long value) { inner.SetLength(value); }
        public override void Write(byte[] buffer, int offset, int count) { inner.Write(buffer, offset, count); }
        protected override void Dispose(bool disposing) { /* ownership remains with caller */ }
    }

    internal static class ActivityValidator
    {
        public static void Validate(IEnumerable<Category> categories, IEnumerable<Activity> activities)
        {
            var categoryIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var category in categories)
            {
                if (category == null || !ValidId(category.Id) || !categoryIds.Add(category.Id) ||
                    LegacyCompat.IsNullOrWhiteSpace(category.Name) || LegacyCompat.IsNullOrWhiteSpace(category.Description) ||
                    !Regex.IsMatch(category.Color ?? "", "^#[0-9a-fA-F]{6}$"))
                    throw new InvalidDataException("Categoria inválida ou duplicada.");
            }

            var activityIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var activity in activities)
            {
                if (activity == null || !ValidId(activity.Id) || !activityIds.Add(activity.Id) ||
                    !categoryIds.Contains(activity.CategoryId) || LegacyCompat.IsNullOrWhiteSpace(activity.Title) ||
                    LegacyCompat.IsNullOrWhiteSpace(activity.Description) || LegacyCompat.IsNullOrWhiteSpace(activity.Text) ||
                    activity.Sources == null)
                    throw new InvalidDataException("Atividade inválida ou duplicada.");

                var quiz = activity.Quiz;
                if (quiz != null && (LegacyCompat.IsNullOrWhiteSpace(quiz.Prompt) || LegacyCompat.IsNullOrWhiteSpace(quiz.Explanation) ||
                    quiz.Options == null || quiz.Options.Length != 3 || quiz.Options.Any(LegacyCompat.IsNullOrWhiteSpace) ||
                    quiz.Options.Distinct(StringComparer.Ordinal).Count() != quiz.Options.Length ||
                    quiz.AnswerIndex < 0 || quiz.AnswerIndex >= quiz.Options.Length))
                    throw new InvalidDataException("Pergunta educativa inválida em " + activity.Id);

                var words = activity.WordCount;
                switch (activity.Difficulty)
                {
                    case "Iniciante" when words >= 30 && words <= 70:
                    case "Intermediário" when words >= 80 && words <= 150:
                    case "Avançado" when words >= 160 && words <= 300:
                        break;
                    default:
                        throw new InvalidDataException("Dificuldade ou contagem de palavras inválida em " + activity.Id);
                }
            }
        }

        private static bool ValidId(string id) => Regex.IsMatch(id ?? "", "^[a-z0-9]+(?:-[a-z0-9]+)*$");
    }
}
