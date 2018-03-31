using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dlgTool.IO;
using System.IO;

/* Conditional jump explanation
 * 
 * Structure of first argument
 * 0000 0000 0000 0000
 * |-|| ---| |XXX XXX|
 * 
 * Bit15-13 use variable X
 * Bit12-8  bit to check in variable X
 * Bit7     if 0, jump to current textSection + (2nd argument) bytes
 *          if 1, jump to section pointed by fileStart+4+(2nd argument)*4
 * Bit6-1   Ignore
 * Bit0     if 0, check if variable X bit is 0
 *          if 1, check if variable X bit is 1*/

namespace dlgTool.CustomEncoding
{
    public enum Font : byte
    {
        Original,
        Edited
    }

    public enum Game : byte
    {
        AATri,
        AJ3DS
    }

    public class AAEncoding
    {
        private Font _font;
        private Game _game;
        private string escapeChar = "X";

        public AAEncoding(Font font, Game game)
        {
            _font = font;
            _game = game;
        }

        private string EscapeString(string input) => input.Replace("\\<", ";;;").Replace("\\>", ",,,");
        private string DeEscapeString(string input) => input.Replace(";;;", "\\<").Replace(",,,", "\\>");
        private string DeEscapeStringWithoutBack(string input) => input.Replace(";;;", "<").Replace(",,,", ">");

        public string GetSectionText(byte[] input)
        {
            string result = "";

            using (var br = new BinaryReaderY(new MemoryStream(input)))
            {
                var count = br.ReadInt32();
                var vals = br.ReadMultiple<int>(count);

                int _lastValid = 0;
                var sections = new List<List<(string, int, bool)>>();
                int pointerCount = 0;
                int condCount = 0;
                foreach (var v in vals)
                {
                    if ((v >= br.BaseStream.Length || v < _lastValid) && (v & 0xFFFF) > 0)
                    {
                        //Pointer
                        InjectPointerLabel(sections[(v >> 16) & 0xFFFF], v & 0xFFFF, pointerCount);
                        sections.Add(new List<(string, int, bool)> { ("<Pointer: @pointer" + pointerCount++ + ">", 0, false) });
                    }
                    else
                    {
                        //Offset
                        br.BaseStream.Position = v;
                        var textList = GetTextList(br.BaseStream);

                        //Dissolve condJmp's
                        var condJmps = textList.Where(tl => tl.Item3).ToArray();
                        foreach (var cj in condJmps)
                        {
                            var args = cj.Item1.Split(new string[] { ": " }, StringSplitOptions.None)[1].Split(' ');

                            var check = Convert.ToInt16(args[0]);
                            if ((check & 0x80) == 0)
                            {
                                InjectPointerLabel(textList, Convert.ToInt32(args[1].Split('>')[0]), condCount, true);

                                var index = textList.FindIndex(tl => tl.Item1 == cj.Item1);
                                textList[index] = (textList[index].Item1.Replace(args[1] + ">", "@condPointer" + condCount++ + ">"), textList[index].Item2, textList[index].Item3);
                            }
                        }

                        sections.Add(textList);
                    }
                }

                int pageCount = 0;
                result = sections.Aggregate("", (o, sec) => o + "<<" + pageCount++ + ">>\r\n" + sec.Aggregate("", (o2, p) => o2 + p.Item1) + "\r\n\r\n");
            }

            return result;
        }

        private List<(string, int, bool)> GetTextList(Stream input)
        {
            var result = new List<(string, int, bool)>();

            using (var br = new BinaryReaderY(input, true))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    var code = br.ReadInt16();
                    if (code < 0x80)
                    {
                        //control code
                        var tmp = "<";
                        var byteCount = 2;

                        if (Tables.modes.ContainsKey(code))
                            tmp += Tables.modes[code];
                        else
                            tmp += $"0x{code:X2}";

                        if (Tables.modeValues.ContainsKey(code))
                        {
                            tmp += ": ";
                            byteCount += Tables.modeValues[code] * 2;
                            for (int i = 0; i < Tables.modeValues[code]; i++)
                                tmp += br.ReadInt16() + ((i == Tables.modeValues[code] - 1) ? "" : " ");
                        }

                        tmp += ">";

                        result.Add((tmp, byteCount, code == 0x35));

                        if (code == 0xd)
                            break;
                    }
                    else
                    {
                        //character
                        ReadChar(br.BaseStream, result, code);
                    }
                }
            }

            return result;
        }

        private void InjectPointerLabel(List<(string, int, bool)> input, int bytesInto, int pointerCount, bool cond = false)
        {
            var byteCount = 0;
            for (int i = 0; i < input.Count; i++)
                if (byteCount != bytesInto)
                    byteCount += input[i].Item2;
                else
                {
                    input.Insert(i, ("<@" + ((cond) ? "condP" : "p") + "ointer" + pointerCount + ">", 0, false));
                    break;
                }
        }

        public byte[] GetBytes(string input)
        {
            var texts = EscapeString(input).Split(new string[] { "<<" }, StringSplitOptions.RemoveEmptyEntries);

            var list = new List<List<(string, int, bool)>>();
            var pointList = new List<(string, int, int)>();

            for (int j = 0; j < texts.Count(); j++)
            {
                var split = texts[j].Split(new string[] { ">>" }, StringSplitOptions.None)[1].Split(new string[] { "<" }, StringSplitOptions.RemoveEmptyEntries);
                list.Add(new List<(string, int, bool)>());
                var tList = list.Last();

                for (int i = 0; i < split.Count(); i++)
                {
                    var parts = split[i].Split('>');

                    if (("<" + parts[0]).Contains("<@"))
                    {
                        pointList.Add(
                            (parts[0],
                            j,
                            tList.Aggregate(0, (o, l) => o + l.Item2))
                            );
                    }
                    else if (("<" + parts[0]).Contains("<Pointer"))
                    {
                        tList.Add(("<" + parts[0] + ">", 0, false));
                    }
                    else
                    {
                        tList.Add(
                            ("<" + split[i].Split('>')[0] + ">",
                            (split[i].Split('>')[0].Count(c => c == ' ') + 1) * 2,
                            ("<" + split[i].Split('>')[0]).Contains("<cond_jmp"))
                            );

                        if (split[i].Split('>')[1] != "")
                            foreach (var c in DeEscapeStringWithoutBack(split[i].Split('>')[1]))
                                tList.Add(($"{c}", 2, false));
                    }
                }
            }

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterY(ms, true))
            {
                bw.Write(list.Count);

                var offset = list.Count * 4 + 4;
                var offsets = new List<int>();

                bw.BaseStream.Position = offset;

                foreach (var l in list)
                {
                    if (l[0].Item1.Contains("<Pointer"))
                    {
                        var pointerName = l[0].Item1.Split(new string[] { "<Pointer: " }, StringSplitOptions.RemoveEmptyEntries)[0].Split('>')[0];
                        var pointEntry = pointList.Where(pl => pl.Item1 == pointerName).ToList()[0];
                        offsets.Add((pointEntry.Item2 << 16) | pointEntry.Item3);
                    }
                    else
                    {
                        offsets.Add(offset);
                        var secB = GetSectionBytes(l, pointList);
                        bw.Write(secB);
                        offset += secB.Length;
                    }
                }

                bw.BaseStream.Position = 4;
                foreach (var o in offsets)
                    bw.Write(o);
            }

            ;

            return ms.ToArray();
        }

        private byte[] GetSectionBytes(List<(string, int, bool)> list, List<(string, int, int)> pointList)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterY(ms, true))
            {
                foreach (var l in list)
                {
                    if (l.Item1[0] == '<' && l.Item1.Length > 1)
                    {
                        //control code
                        var s = l.Item1.Split('>')[0].Split('<')[1].Split(new string[] { ": " }, StringSplitOptions.None);

                        if (Tables.modes.ContainsValue(s[0]))
                        {
                            bw.Write(Tables.modes.Where(m => m.Value == s[0]).ToList()[0].Key);
                        }
                        else
                        {
                            try
                            {
                                bw.Write(Convert.ToInt16(s[0].Split(new string[] { "0x" }, StringSplitOptions.RemoveEmptyEntries)[0], 16));
                            }
                            catch (Exception)
                            {
                                throw new Exception($"{s[0]} is no valid control code.");
                            }
                        }

                        if (s.Count() > 1)
                        {
                            //args detected
                            var args = s[1].Split(' ');
                            foreach (var a in args)
                            {
                                if (a[0] == '@')
                                {
                                    bw.Write((short)pointList.Where(pl => pl.Item1 == a).ToList()[0].Item3);
                                }
                                else
                                {
                                    bw.Write(Convert.ToInt16(a));
                                }
                            }
                        }
                    }
                    else
                    {
                        //character
                        WriteChar(bw.BaseStream, l.Item1);
                    }
                }
            }

            return ms.ToArray();
        }

        private void ReadChar(Stream input, List<(string, int, bool)> result, short code)
        {
            switch (_game)
            {
                case Game.AATri:
                    switch (_font)
                    {
                        case Font.Original:
                        default:
                            result.Add((Tables.triOrigCharConv.ContainsKey(code) ? Tables.triOrigCharConv[code] : escapeChar, 2, false));
                            break;
                        case Font.Edited:
                            result.Add(
                                (Tables.triEditCharConv.ContainsKey(code) ?
                                    Tables.triEditCharConv[code] :
                                    Tables.triOrigCharConv.ContainsKey(code) ?
                                        Tables.triOrigCharConv[code] :
                                        escapeChar,
                                 2, false));
                            break;
                    }
                    break;
                case Game.AJ3DS:
                    switch (_font)
                    {
                        case Font.Original:
                        default:
                            result.Add((Tables.aj3dsOrigCharConv.ContainsKey(code) ? Tables.aj3dsOrigCharConv[code] : escapeChar, 2, false));
                            break;
                        case Font.Edited:
                            result.Add(
                                (Tables.aj3dsEditCharConv.ContainsKey(code) ?
                                    Tables.aj3dsEditCharConv[code] :
                                    Tables.aj3dsOrigCharConv.ContainsKey(code) ?
                                        Tables.aj3dsOrigCharConv[code] :
                                        escapeChar,
                                 2, false));
                            break;
                    }
                    break;
            }
        }

        private void WriteChar(Stream input, string character)
        {
            using (var bw = new BinaryWriterY(input, true))
            {
                switch (_game)
                {
                    case Game.AATri:
                        switch (_font)
                        {
                            case Font.Original:
                            default:
                                bw.Write(Tables.triOrigCharConv.ContainsValue(character) ?
                                    Tables.triOrigCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                    Tables.triOrigCharConv.Where(d => d.Value == escapeChar).ToList()[0].Key);
                                break;
                            case Font.Edited:
                                bw.Write(Tables.triEditCharConv.ContainsValue(character) ?
                                    Tables.triEditCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                    Tables.triOrigCharConv.ContainsValue(character) ?
                                        Tables.triOrigCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                        Tables.triOrigCharConv.Where(d => d.Value == escapeChar).ToList()[0].Key);
                                break;
                        }
                        break;
                    case Game.AJ3DS:
                        switch (_font)
                        {
                            case Font.Original:
                            default:
                                bw.Write(Tables.aj3dsOrigCharConv.ContainsValue(character) ?
                                    Tables.aj3dsOrigCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                    Tables.aj3dsOrigCharConv.Where(d => d.Value == escapeChar).ToList()[0].Key);
                                break;
                            case Font.Edited:
                                bw.Write(Tables.aj3dsEditCharConv.ContainsValue(character) ?
                                    Tables.aj3dsEditCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                    Tables.aj3dsOrigCharConv.ContainsValue(character) ?
                                        Tables.aj3dsOrigCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                        Tables.aj3dsOrigCharConv.Where(d => d.Value == escapeChar).ToList()[0].Key);
                                break;
                        }
                        break;
                }
            }
        }
    }
}
