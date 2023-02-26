namespace dlgTool.Models.Parser
{
    struct PointerEntry
    {
        public string text;
        public int sectionIndex;
        public int offset;

        public PointerEntry(string text, int sectionIndex, int offset)
        {
            this.text = text;
            this.sectionIndex = sectionIndex;
            this.offset = offset;
        }
    }
}
