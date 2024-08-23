using dlgTool.Provider;
using System.Security.Cryptography;

namespace dlgTool.Parser
{
    internal class MdtWriter : IWriter
    {
        private const string Password = "u8DurGE2";
        private const string Salt = "6BBGizHE";

        private readonly MappingProvider _provider;

        public MdtWriter(MappingProvider provider)
        {
            _provider = provider;
        }

        public void Write(string path)
        {
            // Write section
            var writer = new SectionWriter(_provider);
            var ms = new MemoryStream();

            writer.Write(File.ReadAllText(path), ms);

            // Encrypt section
            var data = EncryptData(ms.ToArray());

            // Write file
            var writeFile = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".mdt");
            File.WriteAllBytes(writeFile, data);
        }

        public static byte[] EncryptData(byte[] data)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(Salt);
            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(Password, bytes) { IterationCount = 1000 };

            var rijndaelManaged = new RijndaelManaged { KeySize = 128, BlockSize = 128 };
            rijndaelManaged.Key = rfc2898DeriveBytes.GetBytes(rijndaelManaged.KeySize / 8);
            rijndaelManaged.IV = rfc2898DeriveBytes.GetBytes(rijndaelManaged.BlockSize / 8);

            var cryptoTransform = rijndaelManaged.CreateEncryptor();

            var result = cryptoTransform.TransformFinalBlock(data, 0, data.Length);
            cryptoTransform.Dispose();

            return result;
        }
    }
}
