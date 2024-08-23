using dlgTool.Models.Provider;
using dlgTool.Models;
using Newtonsoft.Json;

namespace dlgTool.Provider
{
    class MdtMappingProvider : MappingProvider
    {
        private const string MappingPath_ = "mappings";
        private const string MappingFileFormat_ = "{0}_{1}.json";

        private MdtMappingProvider(Mapping mapping) :
            base(new MappedOpCodeProvider(mapping.OpCodes), new UnicodeCharacterProvider())
        { }

        public static bool TryGet(Game game, Region region, out MappingProvider? provider)
        {
            provider = null;

            if (!File.Exists(GetFilePath(game, region)))
                return false;

            var mapping = JsonConvert.DeserializeObject<Mapping>(File.ReadAllText(GetFilePath(game, region)));
            if (mapping == null)
                return false;

            provider = new MdtMappingProvider(mapping);
            return true;
        }

        private static string GetFilePath(Game game, Region region)
        {
            var fileName = string.Format(MappingFileFormat_, game.ToString().ToLower(), region.ToString().ToLower());
            return Path.Combine(MappingPath_, fileName);
        }
    }
}
