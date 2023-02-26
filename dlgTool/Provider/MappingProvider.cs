using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using dlgTool.Models;
using dlgTool.Models.Provider;
using Newtonsoft.Json;

namespace dlgTool.Provider
{
    class MappingProvider
    {
        private const string MappingPath_ = "mappings";
        private const string MappingFileFormat_ = "{0}_{1}.json";

        private const string DefaultTagFormat_ = "{0:XX}";
        private const int DefaultArgumentCount_ = 0;

        private readonly IDictionary<int, Tag> _tags;
        private readonly IDictionary<int, string> _characters;
        private readonly IDictionary<string, Tag> _tagsReverse;
        private readonly IDictionary<string, int> _charactersReverse;
        private readonly int _minCharPoint;

        private MappingProvider(Mapping mapping)
        {
            _tags = mapping.Tags;
            _characters = mapping.Characters;
            _tagsReverse = mapping.Tags.ToDictionary(x => x.Value.Name, y => y.Value);
            _charactersReverse = mapping.Characters.GroupBy(p => p.Value).ToDictionary(x => x.Key, y => y.First().Key);
            _minCharPoint = _characters?.Keys.Min() ?? 0;

            foreach (var tag in mapping.Tags)
                tag.Value.Id ??= tag.Key;
        }

        public bool IsControlCode(int value)
        {
            return (_tags?.ContainsKey(value) ?? false) || value < _minCharPoint;
        }

        public bool IsControlCode(string name)
        {
            if (_tagsReverse?.ContainsKey(name) ?? false)
                return true;

            if (int.TryParse(name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                return code < _minCharPoint;

            return false;
        }

        public Tag MapControlCode(int code)
        {
            if (_tags?.TryGetValue(code, out var tag) ?? false)
                return tag;

            return new Tag { Id = code, Name = string.Format(DefaultTagFormat_, code), ArgumentCount = DefaultArgumentCount_ };
        }

        public Tag MapControlCode(string name)
        {
            if (_tagsReverse?.TryGetValue(name, out var tag) ?? false)
                return tag;

            if (name.StartsWith("0x"))
                name = name[2..];

            if (int.TryParse(name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                return new Tag { Id = code, Name = name, ArgumentCount = DefaultArgumentCount_ };

            throw new InvalidOperationException($"Tried mapping tag {name}.");
        }

        public string MapCharacter(int code)
        {
            if (_characters?.TryGetValue(code, out var character) ?? false)
                return character;

            if (_characters?.TryGetValue(_minCharPoint, out character) ?? false)
                return character;

            throw new InvalidOperationException($"Tried mapping character {code}.");
        }

        public int MapCharacter(string character)
        {
            if (_charactersReverse?.TryGetValue(character, out var code) ?? false)
                return code;

            return _minCharPoint;
        }

        #region Static methods

        public static bool Exists(Game game, Region region)
        {
            return File.Exists(GetFilePath(game, region));
        }

        public static MappingProvider Load(Game game, Region region)
        {
            var mapping = JsonConvert.DeserializeObject<Mapping>(File.ReadAllText(GetFilePath(game, region)));
            return new MappingProvider(mapping);
        }

        private static string GetFilePath(Game game, Region region)
        {
            var fileName = string.Format(MappingFileFormat_, game.ToString().ToLower(), region.ToString().ToLower());
            return Path.Combine(MappingPath_, fileName);
        }

        #endregion
    }
}
