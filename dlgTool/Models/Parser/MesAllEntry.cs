namespace dlgTool.Models.Parser
{
    class MesAllEntry
    {
        public int Offset { get; }
        public int Size { get; }

        public MesAllEntry(int offset, int size)
        {
            Offset = offset;
            Size = size;
        }
    }
}
