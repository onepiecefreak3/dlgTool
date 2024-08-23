using System.Globalization;
using dlgTool.Models.Provider;

namespace dlgTool.Provider
{
    abstract class MappingProvider
    {
        private const string DefaultOpCodeFormat_ = "{0:X2}";
        private const int DefaultArgumentCount_ = 0;

        private readonly IOpCodeProvider _opCodeProvider;
        private readonly ICharacterProvider _characterProvider;

        protected MappingProvider(IOpCodeProvider opCodeProvider, ICharacterProvider _characterProvider)
        {
            _opCodeProvider = opCodeProvider;
            this._characterProvider = _characterProvider;
        }

        public bool IsControlCode(int value)
        {
            return _opCodeProvider.TryGet(value, out _) || value < _characterProvider.GetMinCode();
        }

        public bool IsControlCode(string name)
        {
            if (_opCodeProvider.TryGet(name, out _))
                return true;

            if (int.TryParse(name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                return code < _characterProvider.GetMinCode();

            return false;
        }

        public OpCode MapControlCode(int code)
        {
            if (_opCodeProvider.TryGet(code, out OpCode? opCode))
                return opCode!;

            return new OpCode
            {
                Id = code,
                Name = string.Format(DefaultOpCodeFormat_, code),
                ArgumentCount = DefaultArgumentCount_
            };
        }

        public OpCode MapControlCode(string name)
        {
            if (_opCodeProvider.TryGet(name, out OpCode? opCode))
                return opCode!;

            if (name.StartsWith("0x"))
                name = name[2..];

            if (!int.TryParse(name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                throw new InvalidOperationException($"Tried mapping tag {name}.");

            return new OpCode
            {
                Id = code,
                Name = name,
                ArgumentCount = DefaultArgumentCount_
            };
        }

        public string MapCharacter(int code)
        {
            if (_characterProvider.TryGet(code, out string? character))
                return character!;

            if (_characterProvider.TryGet(_characterProvider.GetMinCode(), out character))
                return character!;

            throw new InvalidOperationException($"Tried mapping character {code}.");
        }

        public int MapCharacter(string character)
        {
            if (_characterProvider.TryGet(character, out int code))
                return code;

            return _characterProvider.GetMinCode();
        }
    }
}
