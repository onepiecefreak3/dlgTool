using dlgTool.Models.Parser;
using dlgTool.Provider;

namespace dlgTool.Parser
{
    class SectionReader
    {
        private const string SectionFormat_ = "<<{0}>>";
        private const string TagFormat_ = "<{0}>";
        private const string PointerTagFormat_ = "<Pointer: @pointer {0}>";
        private const string PointerLabelFormat_ = "@pointer{0}";
        private const string ConditionPointerLabelFormat_ = "@condPointer{0}";

        private readonly MappingProvider _provider;

        public SectionReader(MappingProvider provider)
        {
            _provider = provider;
        }

        public string Read(Stream stream)
        {
            // Read offsets
            var offsets = ReadTextOffsets(stream);

            // Create output
            var pointerCount = 0;
            var conditionalCount = 0;

            var pages = new List<List<CharacterEntry>>();
            for (var i = 0; i < offsets.Count; i++)
            {
                var offset = offsets[i];
                List<CharacterEntry> page;

                if ((offset >= stream.Length || offset < 0) && (offset & 0xFFFF) > 0)
                {
                    // Add pointer hint in character entries
                    InjectPointerHint(pages[(offset >> 16) & 0xFFFF], offset & 0xFFFF, pointerCount);

                    page = new List<CharacterEntry> { new(string.Format(PointerTagFormat_, pointerCount++), 0, false) };
                }
                else
                {
                    // Handle text at offset
                    page = ReadCharacterInfo(stream, offset, i + 1 >= offsets.Count ? (int)stream.Length : offsets[i + 1]);

                    // Resolve conditional jump tags
                    var conditionalJumps = page.Where(ce => ce.isConditionalJump).ToArray();
                    foreach (var conditionalJump in conditionalJumps)
                    {
                        if ((conditionalJump.args[0] & 0x80) != 0)
                            continue;

                        InjectPointerHint(page, conditionalJump.args[1], conditionalCount, true);

                        var index = page.IndexOf(conditionalJump);
                        page[index] = new(page[index].text.Replace(conditionalJump.args[1] + ">", string.Format(ConditionPointerLabelFormat_, conditionalCount++) + ">"), page[index].args, page[index].byteCount, page[index].isConditionalJump);
                    }
                }

                pages.Add(page);
            }

            var result = string.Join(Environment.NewLine + Environment.NewLine, pages.Select((s, i) => string.Format(SectionFormat_, i) + Environment.NewLine + string.Join(string.Empty, s.Select(x => x.text))));

            return result;
        }

        private IList<int> ReadTextOffsets(Stream stream)
        {
            var buffer = new byte[4];

            // Read offset count
            if (stream.Length - stream.Position < 4)
                throw new InvalidOperationException($"Tried to read count of text offsets in section. (Position={stream.Position})");

            stream.Read(buffer);
            var offsetCount = GetInt32(buffer);

            // Read offset
            var offsets = new List<int>(offsetCount);
            for (var i = 0; i < offsetCount; i++)
            {
                if (stream.Length - stream.Position < 8)
                    throw new InvalidOperationException($"Tried to text offset. (Position={stream.Position})");

                stream.Read(buffer);
                var value = GetInt32(buffer);

                offsets.Add(value);
            }

            return offsets;
        }

        private void InjectPointerHint(IList<CharacterEntry> characterEntries, int bytesInto, int pointerId, bool cond = false)
        {
            var byteCount = 0;
            for (var i = 0; i < characterEntries.Count; i++)
            {
                if (byteCount >= bytesInto)
                {
                    var pointerLabel = cond ?
                        string.Format(TagFormat_, string.Format(ConditionPointerLabelFormat_, pointerId)):
                        string.Format(TagFormat_, string.Format(PointerLabelFormat_, pointerId));

                    characterEntries.Insert(i, new CharacterEntry(pointerLabel, 0, false));
                    break;
                }

                byteCount += characterEntries[i].byteCount;
            }
        }

        private List<CharacterEntry> ReadCharacterInfo(Stream stream, int offset, int endOffset)
        {
            var buffer = new byte[2];
            var result = new List<CharacterEntry>();

            stream.Position = offset;
            var cappedEnd = Math.Min(stream.Length, endOffset);

            while (stream.Position < cappedEnd)
            {
                // Read code point
                if (cappedEnd - stream.Position < 2)
                    throw new InvalidOperationException($"Tried to read code point. (Position={stream.Position})");

                stream.Read(buffer);
                var code = GetInt16(buffer);

                if (_provider.IsControlCode(code))
                {
                    // Interpret control code
                    var tag = _provider.MapControlCode(code);

                    // Read arguments
                    var args = new int[tag.ArgumentCount];
                    for (var i = 0; i < tag.ArgumentCount; i++)
                    {
                        if (cappedEnd - stream.Position < 2)
                            throw new InvalidOperationException($"Tried to read tag argument. (Position={stream.Position})");

                        stream.Read(buffer);
                        args[i] = GetInt16(buffer);
                    }

                    // Add character entry
                    var tagName = tag.Name;
                    if (tag.ArgumentCount > 0)
                        tagName += ": " + string.Join(" ", args);

                    tagName = string.Format(TagFormat_, tagName);
                    result.Add(new CharacterEntry(tagName, args.ToArray(), 2 + tag.ArgumentCount * 2, code == 0x35));
                }
                else
                    // Interpret character
                    result.Add(new CharacterEntry(_provider.MapCharacter(code), 2, false));
            }

            return result;
        }

        private int GetInt32(byte[] buffer)
        {
            return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
        }

        private int GetInt16(byte[] buffer)
        {
            return buffer[0] | (buffer[1] << 8);
        }
    }
}
