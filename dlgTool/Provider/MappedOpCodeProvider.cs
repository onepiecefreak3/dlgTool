using dlgTool.Models.Provider;

namespace dlgTool.Provider
{
    internal class MappedOpCodeProvider: IOpCodeProvider
    {
        private readonly IDictionary<int, OpCode> _opCodes;
        private readonly IDictionary<string, OpCode> _opCodesReverse;

        public MappedOpCodeProvider(IDictionary<int, OpCode> opCodes)
        {
            _opCodes = opCodes;
            _opCodesReverse = opCodes.ToDictionary(x => x.Value.Name, y => y.Value);

            foreach (var pair in opCodes)
                pair.Value.Id = pair.Key;
        }

        public bool TryGet(int value, out OpCode? opCode)
        {
            return _opCodes.TryGetValue(value, out opCode);
        }

        public bool TryGet(string name, out OpCode? opCode)
        {
            return _opCodesReverse.TryGetValue(name, out opCode);
        }
    }
}
