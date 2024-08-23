namespace dlgTool.Provider
{
    internal interface ICharacterProvider
    {
        public int GetMinCode();

        public bool TryGet(int value, out string? character);
        public bool TryGet(string character, out int value);
    }
}
