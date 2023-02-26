using System.Diagnostics;
using dlgTool.Models.Parser;
using dlgTool.Provider;
using System.Text.RegularExpressions;

namespace dlgTool.Parser
{
    class SectionWriter
    {
        private static readonly Regex SectionNumberPattern = new("<<([^>]*)>>");

        private readonly MappingProvider _provider;

        public SectionWriter(MappingProvider provider)
        {
            _provider = provider;
        }

        //public void Write1(string input, Stream stream)
        //{
        //    //var parsedSections = Parse(input);
        //}

        //private List<List<CharacterEntry2>> Parse(string input)
        //{
        //    var sections = new List<List<CharacterEntry2>>();

        //    var reader = new PeekableStringReader(input);
        //    var currentSection = new List<CharacterEntry2>();

        //    while (!reader.IsEnd)
        //    {
        //        switch (reader.Peek())
        //        {
        //            case '<':
        //                if (reader.Peek(1) == '<')
        //                {
        //                    sections.Add(currentSection = new List<CharacterEntry2>());
        //                    ParseSectionHeader(reader);
        //                }
        //                else
        //                    currentSection.Add(ParseTag(reader));
        //                break;

        //            case '\n':
        //            case '\r':
        //                break;

        //            case '\\':
        //                reader.Read();
        //                currentSection.Add(ParseCharacter(reader));
        //                break;

        //            default:
        //                currentSection.Add(ParseCharacter(reader));
        //                break;
        //        }
        //    }

        //    return sections;
        //}

        //private string ParseSectionHeader(PeekableStringReader reader)
        //{
        //    if (reader.Read() != '<')
        //        throw new InvalidOperationException($"Tried reading start of section header. (Position={reader.Position})");
        //    if (reader.Read() != '<')
        //        throw new InvalidOperationException($"Tried reading start of section header. (Position={reader.Position})");

        //    var sectionName = string.Empty;
        //    while (reader.Peek() != '>')
        //        sectionName += reader.Read();

        //    if (reader.Read() != '>')
        //        throw new InvalidOperationException($"Tried reading end of section header. (Position={reader.Position})");
        //    if (reader.Read() != '>')
        //        throw new InvalidOperationException($"Tried reading end of section header. (Position={reader.Position})");

        //    return sectionName;
        //}

        //private CharacterEntry2 ParseTag(PeekableStringReader reader)
        //{
        //    if (reader.Read() != '<')
        //        throw new InvalidOperationException($"Tried reading start of a tag. (Position={reader.Position})");

        //    // Read tag name
        //    var tagName = string.Empty;
        //    while (reader.Peek() != '>' && reader.Peek() != ':')
        //        tagName += reader.Read();

        //    if (!_provider.IsControlCode(tagName))
        //        throw new InvalidOperationException($"Tried to map tag {tagName}. (Position={reader.Position})");

        //    var tag = _provider.MapControlCode(tagName);

        //    var splitChar = reader.Read();
        //    if (splitChar == '>')
        //        return new CharacterEntry2(tag.Id.Value);

        //    // Read tag arguments
        //    var args = new List<int>();

        //    var currentArg = string.Empty;
        //    while (reader.Peek() != '>')
        //    {
        //        splitChar = reader.Read();
        //        if (splitChar is < '0' or > '9')
        //        {
        //            if (currentArg == string.Empty)
        //                continue;

        //            args.Add(int.Parse(currentArg));
        //            currentArg = string.Empty;
        //            continue;
        //        }

        //        currentArg += splitChar;
        //    }
        //    reader.Read();

        //    if (currentArg != string.Empty)
        //        args.Add(int.Parse(currentArg));

        //    return new CharacterEntry2(tag.Id.Value, args.ToArray());
        //}

        //private CharacterEntry2 ParseCharacter(PeekableStringReader reader)
        //{
        //    return new CharacterEntry2(reader.Read());
        //}

        public void Write(string input, Stream stream)
        {
            var escapedText = EscapeString(input);
            (int offset, int nextOffset, string sectionNumber)[] sectionIndices = SectionNumberPattern.Matches(escapedText).Select(m => (m.Index, m.Index + m.Length, m.Groups[1].Value)).ToArray();

            // Parse text into characters and pointers
            var sections = new List<List<CharacterEntry>>();
            var pointers = new List<PointerEntry>();

            for (var i = 0; i < sectionIndices.Length; i++)
            {
                var sectionIndex = sectionIndices[i];
                var endOffset = i + 1 >= sectionIndices.Length ? escapedText.Length : sectionIndices[i + 1].offset;

                var currentSection = new List<CharacterEntry>();
                sections.Add(currentSection);

                var tags = escapedText[sectionIndex.nextOffset..endOffset].Split(new[] { "<" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tags)
                {
                    var tagSplit = tag.Split('>');
                    var (tagName, clearText) = (tagSplit[0], tagSplit[1]);

                    // If tag is pointer reference
                    if (("<" + tagName).Contains("<@"))
                        pointers.Add(new PointerEntry(tagName, i, currentSection.Sum(x => x.byteCount)));

                    // If tag is global pointer
                    else if (("<" + tagName).Contains("<Pointer"))
                        currentSection.Add(new CharacterEntry("<" + tagName + ">", 0, false));

                    else
                        currentSection.Add(new CharacterEntry("<" + tagName + ">", (tagName.Count(c => c == ' ') + 1) * 2, ("<" + tagName).Contains("<cond_jmp")));

                        // Get text after tag
                    if (string.IsNullOrEmpty(clearText))
                        continue;

                    // Add every character
                    foreach (var c in DeEscapeStringWithoutBack(clearText))
                        currentSection.Add(new CharacterEntry($"{c}", 2, false));
                }
            }

            // Collect section data
            var sectionsData = new List<Stream>();
            var offsets = new List<(int, bool)>();

            var sectionOffset = 0;
            foreach (var section in sections)
            {
                if (section[0].text.Contains("<Pointer"))
                {
                    var pointerName = section[0].text.Split(new[] { "<Pointer: " }, StringSplitOptions.RemoveEmptyEntries)[0].Split('>')[0];
                    var pointEntry = pointers.First(pl => pl.text == pointerName);

                    offsets.Add(((pointEntry.sectionIndex << 16) | pointEntry.offset, true));
                }
                else
                {
                    var sectionMs = new MemoryStream();
                    WriteSection(section, pointers, sectionMs);

                    sectionsData.Add(sectionMs);

                    offsets.Add((sectionOffset, false));
                    sectionOffset += (int)sectionMs.Length;
                }
            }

            // Write section data
            var buffer = new byte[4];

            //  Write section count
            SetInt32(sections.Count, buffer);
            stream.Write(buffer);

            //  Write offset list
            var startOffset = 4 + offsets.Count * 4;
            foreach (var offset in offsets.Select(x => x.Item2 ? x.Item1 : x.Item1 + startOffset))
            {
                SetInt32(offset, buffer);
                stream.Write(buffer);
            }

            //  Write section bytes
            foreach (var sectionData in sectionsData)
            {
                sectionData.Position = 0;
                sectionData.CopyTo(stream);
            }
        }

        private void WriteSection(List<CharacterEntry> characters, List<PointerEntry> pointers, Stream stream)
        {
            var buffer = new byte[2];

            foreach (var character in characters)
            {
                if (character.text.Length == 1 && character.text[0] != '<')
                {
                    // Write character
                    SetInt16(_provider.MapCharacter(character.text), buffer);
                    stream.Write(buffer);

                    continue;
                }

                // Write control code
                var argText = character.text.Split('>')[0].Split('<')[1].Split(new[] { ": " }, StringSplitOptions.None);
                var tagName = argText[0];

                if (_provider.IsControlCode(tagName))
                {
                    var tag = _provider.MapControlCode(tagName);

                    SetInt16(tag.Id.Value, buffer);
                    stream.Write(buffer);
                }

                if (argText.Length <= 1)
                    continue;

                // Write arguments
                var args = argText[1].Split(' ');
                foreach (var arg in args)
                {
                    if (arg[0] == '@')
                    {
                        // Resolve pointer
                        SetInt16(pointers.First(x => x.text == arg).offset, buffer);
                        stream.Write(buffer);
                    }
                    else
                    {
                        // Resolve simple argument
                        SetInt16(int.Parse(arg), buffer);
                        stream.Write(buffer);
                    }
                }
            }
        }

        private string EscapeString(string input) => input.Replace("\n", "").Replace("\r", "").Replace("\\<", ";;;").Replace("\\>", ",,,");

        private string DeEscapeStringWithoutBack(string input) => input.Replace(";;;", "<").Replace(",,,", ">");

        private void SetInt32(int value, byte[] buffer)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
        }

        private void SetInt16(int value, byte[] buffer)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
        }
    }
}
