using System.Buffers.Binary;
using System.Text;

namespace dlgTool.Provider
{
    internal class UnicodeCharacterProvider : ICharacterProvider
    {
        private const ushort MinCode_ = 128;

        private readonly byte[] _charBuffer = new byte[2];

        public int GetMinCode() => MinCode_;

        public bool TryGet(int value, out string? character)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_charBuffer, (ushort)(value - MinCode_));
            character = Encoding.Unicode.GetString(_charBuffer);

            return true;
        }

        public bool TryGet(string character, out int value)
        {
            _ = Encoding.Unicode.GetBytes(character, _charBuffer);
            value = BinaryPrimitives.ReadUInt16LittleEndian(_charBuffer) + MinCode_;

            return true;
        }
    }
}
