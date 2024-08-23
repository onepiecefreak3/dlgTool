namespace dlgTool.Provider
{
    internal class MappedCharacterProvider: ICharacterProvider
    {
        private readonly IDictionary<int, string> _characters;
        private readonly IDictionary<string, int> _charactersReverse;

        public MappedCharacterProvider(IDictionary<int, string> characters)
        {
            _characters = characters;
            _charactersReverse = characters.GroupBy(p => p.Value).ToDictionary(x => x.Key, y => y.First().Key);
        }

        public int GetMinCode()
        {
            return _characters.Keys.Min();
        }

        public bool TryGet(int value, out string? character)
        {
            return _characters.TryGetValue(value, out character);
        }

        public bool TryGet(string character, out int value)
        {
            return _charactersReverse.TryGetValue(character, out value);
        }
    }
}
