using dlgTool.Provider;

namespace dlgTool.Models.Parser
{
    struct CharacterEntry
    {
        public string text;
        public int[] args;
        public int byteCount;
        public bool isConditionalJump;

        public CharacterEntry(string text, int byteCount, bool isConditionalJump) : this(text, Array.Empty<int>(), byteCount, isConditionalJump)
        {
        }

        public CharacterEntry(string text, int[] args, int byteCount, bool isConditionalJump)
        {
            this.text = text;
            this.args = args;
            this.byteCount = byteCount;
            this.isConditionalJump = isConditionalJump;
        }
    }
}
