using dlgTool.Models.Provider;

namespace dlgTool.Provider
{
    internal interface IOpCodeProvider
    {
        bool TryGet(int value, out OpCode? opCode);
        bool TryGet(string name, out OpCode? opCode);
    }
}
