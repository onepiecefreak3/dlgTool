using dlgTool.Provider;
using dlgTool.Models.Parser;
using Kompression.Implementations;

namespace dlgTool.Parser
{
    class MesAllReader
    {
        private readonly MappingProvider _provider;

        public MesAllReader(MappingProvider provider)
        {
            _provider = provider;
        }

        public void Read(string path)
        {
            using var fs = File.OpenRead(path);

            // Read sections
            var sections = ReadSections(fs);

            // Extract sections
            var writeDir = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            if (!Directory.Exists(writeDir))
                Directory.CreateDirectory(writeDir);

            var reader = new SectionReader(_provider);
            var txtCount = 0;

            foreach (var section in sections)
                File.WriteAllText(Path.Combine(writeDir, $"{txtCount++:000}.txt"), reader.Read(section));
        }

        private IList<Stream> ReadSections(Stream stream)
        {
            var buffer = new byte[4];

            // Read section count
            if (stream.Length - stream.Position < 4)
                throw new InvalidOperationException($"Tried to read count of sections. (Position={stream.Position})");

            stream.Read(buffer);
            var sectionCount = GetInt32(buffer);

            // Read section entries
            var entries = new List<MesAllEntry>(sectionCount);
            for (var i = 0; i < sectionCount; i++)
            {
                if (stream.Length - stream.Position < 8)
                    throw new InvalidOperationException($"Tried to read section. (Position={stream.Position})");

                stream.Read(buffer);
                var offset = GetInt32(buffer);

                stream.Read(buffer);
                var size = GetInt32(buffer);

                entries.Add(new MesAllEntry(offset, size));
            }

            // Read sections
            var lz10 = Compressions.Nintendo.Lz10.Build();

            var sections = new List<Stream>();
            foreach (var entry in entries)
            {
                if (stream.Length - entry.Offset < entry.Size)
                    throw new InvalidOperationException($"Tried to read section. (Position={entry.Offset}, Size={entry.Size})");

                using var ss = new MemoryStream();
                var ms = new MemoryStream();

                var sectionBuffer = new byte[entry.Size];
                stream.Position = entry.Offset;

                stream.Read(sectionBuffer);
                ss.Write(sectionBuffer);
                ss.Position = 0;

                lz10.Decompress(ss, ms);

                ms.Position = 0;
                sections.Add(ms);
            }

            return sections;
        }

        private int GetInt32(byte[] buffer)
        {
            return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
        }
    }
}
