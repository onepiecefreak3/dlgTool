using dlgTool.Provider;

namespace dlgTool.Models.Parser
{
    struct CharacterEntry2
    {
        public int code;
        public int[] args;

        public CharacterEntry2(int code)
        {
            this.code = code;
            this.args = Array.Empty<int>();
        }

        public CharacterEntry2(int code, int[] args)
        {
            this.code = code;
            this.args = args;
        }

        public string ToString(MappingProvider provider)
        {
            // Map character
            if (!provider.IsControlCode(code))
                return provider.MapCharacter(code);

            // Map control code
            var tag = provider.MapControlCode(code);

            var tagString = $"<{tag.Name}";
            if (args?.Length > 0)
                tagString += ":" + string.Join(" ", args);
            tagString += ">";

            return tagString;
        }
    }
}
