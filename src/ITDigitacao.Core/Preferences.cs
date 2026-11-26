using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace ITDigitacao.Core
{
    [DataContract]
    public sealed class Preferences
    {
        [DataMember(IsRequired = true)] public int FontPoints { get; set; } = 14;
        [DataMember(IsRequired = true)] public bool Sounds { get; set; }
        [DataMember(IsRequired = true)] public bool ShowMetrics { get; set; } = true;
        [DataMember(IsRequired = true)] public bool Concentration { get; set; }
        [DataMember(IsRequired = true)] public bool VisualFeedback { get; set; } = true;
        [DataMember(IsRequired = true)] public bool Animations { get; set; } = true;
        [DataMember(IsRequired = true)] public bool ReducedEffects { get; set; }

        public Preferences Copy() => (Preferences)MemberwiseClone();

        private static DataContractJsonSerializer CreateSerializer() =>
            new DataContractJsonSerializer(typeof(Preferences));

        public static Preferences Load(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var value = (Preferences)CreateSerializer().ReadObject(stream);
                    return value != null && value.FontPoints >= 12 && value.FontPoints <= 28 ? value : new Preferences();
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                                          error is SerializationException || error is XmlException)
            {
                return new Preferences();
            }
        }

        public void Save(string path)
        {
            if (FontPoints < 12 || FontPoints > 28)
                throw new ArgumentOutOfRangeException(nameof(FontPoints));

            var folder = Path.GetDirectoryName(Path.GetFullPath(path));
            Directory.CreateDirectory(folder);
            string pending = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = File.Create(pending))
                    CreateSerializer().WriteObject(stream);
                if (File.Exists(path))
                    File.Replace(pending, path, null);
                else
                    File.Move(pending, path);
            }
            finally
            {
                File.Delete(pending);
            }
        }
    }
}
