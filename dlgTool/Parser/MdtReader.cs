using dlgTool.Provider;
using System.Security.Cryptography;

namespace dlgTool.Parser
{
    class MdtReader : IReader
    {
        private const string Password = "u8DurGE2";
        private const string Salt = "6BBGizHE";

        private readonly MappingProvider _provider;

        public MdtReader(MappingProvider provider)
        {
            _provider = provider;
        }

        public void Read(string path)
        {
            byte[] content = File.ReadAllBytes(path);

            // Extract section
            var writeFile = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".txt");

            var reader = new SectionReader(_provider);
            File.WriteAllText(writeFile, reader.Read(new MemoryStream(DecryptData(content))));
        }

        public static byte[] DecryptData(byte[] data)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(Salt);
            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(Password, bytes) { IterationCount = 1000 };

            var rijndaelManaged = new RijndaelManaged { KeySize = 128, BlockSize = 128 };
            rijndaelManaged.Key = rfc2898DeriveBytes.GetBytes(rijndaelManaged.KeySize / 8);
            rijndaelManaged.IV = rfc2898DeriveBytes.GetBytes(rijndaelManaged.BlockSize / 8);

            var cryptoTransform = rijndaelManaged.CreateDecryptor();

            var result = cryptoTransform.TransformFinalBlock(data, 0, data.Length);
            cryptoTransform.Dispose();

            return result;
        }
    }
}
