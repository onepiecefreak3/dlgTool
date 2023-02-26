using dlgTool.Models.Parser;
using dlgTool.Provider;
using Kompression.Implementations;

namespace dlgTool.Parser
{
    class MesAllWriter
    {
        private const string MesAllName_ = "mes_all.bin";

        private readonly MappingProvider _provider;

        public MesAllWriter(MappingProvider provider)
        {
            _provider = provider;
        }

        public void Write(string path)
        {
            // Collect sections
            var sections = new List<Stream>();

            var writer = new SectionWriter(_provider);
            foreach (var sectionFile in Directory.EnumerateFiles(path).Where(x => Path.GetExtension(x) == ".txt"))
            {
                var sectionText = File.ReadAllText(sectionFile);

                var ms = new MemoryStream();
                sections.Add(ms);

                writer.Write(sectionText, ms);
            }

            // Write sections
            using var output = File.Create(Path.Combine(path, MesAllName_));
            WriteSections(output, sections);
        }

        private void WriteSections(Stream output, IList<Stream> sections)
        {
            var buffer = new byte[4];

            // Write section count
            SetInt32(sections.Count, buffer);
            output.Write(buffer);

            // Write sections
            var lz10 = Compressions.Nintendo.Lz10.Build();

            var sectionEntries = new List<MesAllEntry>();
            var sectionOffset = 4 + sections.Count * 8;
            foreach (var section in sections)
            {
                section.Position = 0;
                var compressedMs = new MemoryStream();
                lz10.Compress(section, compressedMs);

                compressedMs.Position = 0;
                sectionEntries.Add(new MesAllEntry(sectionOffset, (int)compressedMs.Length));

                output.Position = sectionOffset;
                compressedMs.CopyTo(output);

                sectionOffset += (int)compressedMs.Length + 3 & ~3;
            }

            // Write section entries
            output.Position = 4;

            foreach (var sectionEntry in sectionEntries)
            {
                SetInt32(sectionEntry.Offset, buffer);
                output.Write(buffer);

                SetInt32(sectionEntry.Size, buffer);
                output.Write(buffer);
            }
        }

        private void SetInt32(int value, byte[] buffer)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
        }
    }
}
