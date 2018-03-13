﻿using System;
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
        Default,
        Universal
    }

    public enum Platform : byte
    {
        DS,
        ThreeDS
    }

    public class AAEncoding
    {
        private Font _font;
        private Platform _platform;
        private string escapeChar = "X";

        public AAEncoding(Font font, Platform platform)
        {
            _font = font;
            _platform = platform;
        }


        Dictionary<short, string> modes = new Dictionary<short, string>
        {
            [0x0] = "bop",
            [0x1] = "b",
            [0x2] = "p",
            [0x3] = "color",
            [0x4] = "pause",
            [0x5] = "music",
            [0x6] = "sound",
            [0x7] = "fullscreen_text",
            [0x8] = "2args_choice_jmp",         //jumps to 2-argument choice
            [0x9] = "3args_choice_jmp",         //jumps to 3-argument choice
            [0xa] = "rejmp",                    //???
            [0xb] = "speed",
            [0xc] = "wait",
            [0xd] = "endjmp",                   //ends a current section
            [0xe] = "name",
            [0xf] = "testimony_box",
            [0x11] = "evd_window_plain",
            [0x12] = "bgcolor",
            [0x13] = "showphoto",
            [0x14] = "removephoto",
            [0x15] = "testimony_jmp",         //jmp used in testimonies, special due to independent working
            [0x16] = "savegame",
            [0x17] = "newevidence",
            [0x1a] = "swoosh",
            [0x1b] = "bg",
            [0x1c] = "hidetextbox",
            [0x1e] = "person",
            [0x1f] = "hideperson",
            [0x21] = "evidence_window_lifebar",
            [0x22] = "fademusic",
            [0x24] = "reset",
            [0x27] = "shake",
            [0x28] = "testimony_animation",
            [0x2c] = "jmp",                     //jumps to a section specified by value
            [0x2d] = "nextpage_button",
            [0x2e] = "nextpage_nobutton",
            [0x2f] = "animation",
            [0x31] = "personvanish",
            [0x34] = "fadetoblack",
            [0x35] = "cond_jmp",                //conditional jump, explained at the top of this class
            [0x39] = "littlesprite",
            [0x42] = "soundtoggle",
            [0x43] = "lifebar",
            [0x44] = "guilty",
            [0x46] = "bgtile",
            [0x49] = "wingame",
            [0x4e] = "wait_noanim",
            [0x5d] = "center",
            [0x69] = "bganim",
            [0x6a] = "switchscript"
        };

        Dictionary<short, int> modeValues = new Dictionary<short, int>
        {
            [0x3] = 1,
            [0x5] = 2,
            [0x6] = 2,
            [0x8] = 2,
            [0x9] = 3,
            [0xa] = 1,
            [0xb] = 1,
            [0xc] = 1,
            [0xe] = 1,
            [0xf] = 2,
            [0x10] = 1,
            [0x12] = 3,
            [0x13] = 1,
            [0x17] = 1,
            [0x18] = 1,
            [0x19] = 2,
            [0x1a] = 4,
            [0x1b] = 1,
            [0x1c] = 1,
            [0x1d] = 1,
            [0x1e] = 3,
            [0x20] = 1,
            [0x22] = 2,
            [0x23] = 2,
            [0x26] = 1,
            [0x27] = 2,
            [0x28] = 1,
            [0x29] = 1,
            [0x2a] = 3,
            [0x2c] = 1,
            [0x2f] = 2,
            [0x30] = 1,
            [0x31] = 2,
            [0x32] = 2,
            [0x33] = 5,
            [0x34] = 1,
            [0x35] = 2,
            [0x36] = 1,
            [0x37] = 2,
            [0x38] = 1,
            [0x39] = 1,
            [0x3a] = 3,
            [0x3b] = 2,
            [0x3d] = 1,
            [0x3e] = 1,
            [0x42] = 1,
            [0x43] = 1,
            [0x44] = 1,
            [0x46] = 1,
            [0x47] = 2,
            [0x48] = 2,
            [0x4b] = 1,
            [0x4d] = 2,
            [0x4e] = 1,
            [0x55] = 2,
            [0x5a] = 2,
            [0x5d] = 1,
            [0x5f] = 3,
            [0x60] = 2,
            [0x65] = 2,
            [0x69] = 2,
            [0x6a] = 1,
            [0x6b] = 3,
            [0x6c] = 1,
            [0x6d] = 1,
            [0x6f] = 1,
            [0x70] = 2,
            [0x74] = 2,
            [0x76] = 2,
            [0x77] = 1,
            [0x78] = 1,
            [0x7a] = 1,
            [0x7c] = 1
        };

        Dictionary<short, string> triDefCharConv = new Dictionary<short, string>
        {
            [0x80] = "0",
            [0x81] = "1",
            [0x82] = "2",
            [0x83] = "3",
            [0x84] = "4",
            [0x85] = "5",
            [0x86] = "6",
            [0x87] = "7",
            [0x88] = "8",
            [0x89] = "9",

            [0x8a] = "A",
            [0x8b] = "B",
            [0x8c] = "C",
            [0x8d] = "D",
            [0x8e] = "E",
            [0x8f] = "F",
            [0x90] = "G",
            [0x91] = "H",
            [0x92] = "I",
            [0x93] = "J",
            [0x94] = "K",
            [0x95] = "L",
            [0x96] = "M",
            [0x97] = "N",
            [0x98] = "O",
            [0x99] = "P",
            [0x9a] = "Q",
            [0x9b] = "R",
            [0x9c] = "S",
            [0x9d] = "T",
            [0x9e] = "U",
            [0x9f] = "V",
            [0xa0] = "W",
            [0xa1] = "X",
            [0xa2] = "Y",
            [0xa3] = "Z",

            [0xa4] = "a",
            [0xa5] = "b",
            [0xa6] = "c",
            [0xa7] = "d",
            [0xa8] = "e",
            [0xa9] = "f",
            [0xaa] = "g",
            [0xab] = "h",
            [0xac] = "i",
            [0xad] = "j",
            [0xae] = "k",
            [0xaf] = "l",
            [0xb0] = "m",
            [0xb1] = "n",
            [0xb2] = "o",
            [0xb3] = "p",
            [0xb4] = "q",
            [0xb5] = "r",
            [0xb6] = "s",
            [0xb7] = "t",
            [0xb8] = "u",
            [0xb9] = "v",
            [0xba] = "w",
            [0xbb] = "x",
            [0xbc] = "y",
            [0xbd] = "z",

            [0xbe] = "!",
            [0xbf] = "?",

            [0xc0] = "あ",
            [0xc1] = "い",
            [0xc2] = "う",
            [0xc3] = "え",
            [0xc4] = "お",
            [0xc5] = "か",
            [0xc6] = "き",
            [0xc7] = "く",
            [0xc8] = "け",
            [0xc9] = "こ",
            [0xca] = "さ",
            [0xcb] = "し",
            [0xcc] = "す",
            [0xcd] = "せ",
            [0xce] = "そ",
            [0xcf] = "た",
            [0xd0] = "ち",
            [0xd1] = "つ",
            [0xd2] = "て",
            [0xd3] = "と",
            [0xd4] = "な",
            [0xd5] = "に",
            [0xd6] = "ぬ",
            [0xd7] = "ね",
            [0xd8] = "の",
            [0xd9] = "は",
            [0xda] = "ひ",
            [0xdb] = "ふ",
            [0xdc] = "へ",
            [0xdd] = "ほ",
            [0xde] = "ま",
            [0xdf] = "み",
            [0xe0] = "む",
            [0xe1] = "め",
            [0xe2] = "も",
            [0xe3] = "や",
            [0xe4] = "ゆ",
            [0xe5] = "よ",
            [0xe6] = "ら",
            [0xe7] = "り",
            [0xe8] = "る",
            [0xe9] = "れ",
            [0xea] = "ろ",
            [0xeb] = "わ",
            [0xec] = "を",
            [0xed] = "ん",
            [0xee] = "が",
            [0xef] = "ぎ",
            [0xf0] = "ぐ",
            [0xf1] = "げ",
            [0xf2] = "ご",
            [0xf3] = "ざ",
            [0xf4] = "じ",
            [0xf5] = "ず",
            [0xf6] = "ぜ",
            [0xf7] = "ぞ",
            [0xf8] = "だ",
            [0xf9] = "ぢ",
            [0xfa] = "づ",
            [0xfb] = "で",
            [0xfc] = "ど",
            [0xfd] = "ば",
            [0xfe] = "び",
            [0xff] = "ぶ",
            [0x100] = "べ",
            [0x101] = "ぼ",
            [0x102] = "ぱ",
            [0x103] = "ぴ",
            [0x104] = "ぷ",
            [0x105] = "ぺ",
            [0x106] = "ぽ",
            [0x107] = "ぁ",
            [0x108] = "ぃ",
            [0x109] = "ぅ",
            [0x10a] = "ぇ",
            [0x10b] = "ぉ",
            [0x10c] = "ゃ",
            [0x10d] = "ゅ",
            [0x10e] = "ょ",
            [0x10f] = "っ",

            [0x110] = "ア",
            [0x111] = "イ",
            [0x112] = "ウ",
            [0x113] = "エ",
            [0x114] = "オ",
            [0x115] = "カ",
            [0x116] = "キ",
            [0x117] = "ク",
            [0x118] = "ケ",
            [0x119] = "コ",
            [0x11a] = "サ",
            [0x11b] = "シ",
            [0x11c] = "ス",
            [0x11d] = "セ",
            [0x11e] = "ソ",
            [0x11f] = "タ",
            [0x120] = "チ",
            [0x121] = "ツ",
            [0x122] = "テ",
            [0x123] = "ト",
            [0x124] = "ナ",
            [0x125] = "ニ",
            [0x126] = "ヌ",
            [0x127] = "ネ",
            [0x128] = "ノ",
            [0x129] = "ハ",
            [0x12a] = "ヒ",
            [0x12b] = "フ",
            [0x12c] = "ヘ",
            [0x12d] = "ホ",
            [0x12e] = "マ",
            [0x12f] = "ミ",
            [0x130] = "ム",
            [0x131] = "メ",
            [0x132] = "モ",
            [0x133] = "ヤ",
            [0x134] = "ユ",
            [0x135] = "ヨ",
            [0x136] = "ラ",
            [0x137] = "リ",
            [0x138] = "ル",
            [0x139] = "レ",
            [0x13a] = "ロ",
            [0x13b] = "ワ",
            [0x13c] = "ヲ",
            [0x13d] = "ン",
            [0x13e] = "ガ",
            [0x13f] = "ギ",
            [0x140] = "グ",
            [0x141] = "ゲ",
            [0x142] = "ゴ",
            [0x143] = "ザ",
            [0x144] = "ジ",
            [0x145] = "ズ",
            [0x146] = "ゼ",
            [0x147] = "ゾ",
            [0x148] = "ダ",
            [0x149] = "ヂ",
            [0x14a] = "ヅ",
            [0x14b] = "デ",
            [0x14c] = "ド",
            [0x14d] = "バ",
            [0x14e] = "ビ",
            [0x14f] = "ブ",
            [0x150] = "ベ",
            [0x151] = "ボ",
            [0x152] = "パ",
            [0x153] = "ピ",
            [0x154] = "プ",
            [0x155] = "ペ",
            [0x156] = "ポ",
            [0x157] = "ァ",
            [0x158] = "ィ",
            [0x159] = "ゥ",
            [0x15a] = "ェ",
            [0x15b] = "ォ",
            [0x15c] = "ャ",
            [0x15d] = "ュ",
            [0x15e] = "ョ",
            [0x15f] = "ッ",
            [0x160] = "ヴ",

            [0x161] = ".",
            [0x162] = " ",
            [0x163] = "「",
            [0x164] = "」",
            [0x165] = "(",
            [0x166] = ")",
            [0x167] = "『",
            [0x168] = "』",
            [0x169] = "\"",
            [0x16a] = "\"",
            [0x16b] = "▼",
            [0x16c] = "▲",
            [0x16d] = ":",
            [0x16e] = "、",
            [0x16f] = ",",
            [0x170] = "+",
            [0x171] = "/",
            [0x172] = "*",
            [0x173] = "'",
            [0x174] = "-",
            [0x175] = "・",
            [0x176] = "。",
            [0x177] = "%",
            [0x178] = "‥",
            [0x179] = "~",
            [0x17a] = "《",
            [0x17b] = "》",
            [0x17c] = "&",
            [0x17d] = "★",
            [0x17e] = "♪",
            [0x17f] = " ",
            [0x17f] = "-",
            [0x180] = "\"",
            [0x181] = "[",
            [0x182] = "]",
            [0x183] = "$",
            [0x184] = "#",
            [0x185] = "\\>",
            [0x186] = "\\<",
            [0x187] = "=",
            [0x188] = " ",
            [0x189] = "é",
            [0x18a] = "á",
            [0x18b] = ";",
            [0x18c] = "è",
            [0x18d] = " ",
            [0x18e] = "Ç"
        };

        Dictionary<short, string> triUniCharConv = new Dictionary<short, string>
        {
            [0xc0] = "Á",
            [0xc1] = "À",
            [0xc2] = "Â",
            [0xc3] = "Ä",
            [0xc4] = "Ã",
            [0xc5] = "Ç",
            [0xc6] = "É",
            [0xc7] = "È",
            [0xc8] = "Ê",
            [0xc9] = "Ë",
            [0xca] = "Ẽ",
            [0xcb] = "Í",
            [0xcc] = "Ì",
            [0xcd] = "Î",
            [0xce] = "Ï",
            [0xcf] = "Ñ",
            [0xd0] = "Ó",
            [0xd1] = "Ò",
            [0xd2] = "Ô",
            [0xd3] = "Ö",
            [0xd4] = "Õ",
            [0xd5] = "Œ",
            [0xd6] = "Ú",
            [0xd7] = "Ù",
            [0xd8] = "Û",
            [0xd9] = "Ü",
            [0xda] = "Ÿ",
            [0xdb] = "á",
            [0xdc] = "à",
            [0xdd] = "â",
            [0xde] = "ä",
            [0xdf] = "ã",
            [0xe0] = "æ",
            [0xe1] = "ç",
            [0xe2] = "é",
            [0xe3] = "è",
            [0xe4] = "ê",
            [0xe5] = "ë",
            [0xe6] = "ẽ",
            [0xe7] = "í",
            [0xe8] = "ì",
            [0xe9] = "î",
            [0xea] = "ï",
            [0xeb] = "ñ",
            [0xec] = "ó",
            [0xed] = "ò",
            [0xee] = "ô",
            [0xef] = "ö",
            [0xf0] = "õ",
            [0xf1] = "œ",
            [0xf2] = "ú",
            [0xf3] = "ù",
            [0xf4] = "û",
            [0xf5] = "ü",
            [0xf6] = "ÿ",
            [0xf7] = "¿",
            [0xf8] = "¡",
            [0xf9] = "ß",
            [0xfa] = "€",
            [0xfb] = "º",
            [0xfc] = "ª",
            [0xfd] = "ű",
            [0xfe] = "ő",
            [0xff] = "Ű",
            [0x100] = "Ő",
            [0x101] = "„"
        };

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

                        if (modes.ContainsKey(code))
                            tmp += modes[code];
                        else
                            tmp += $"0x{code:X2}";

                        if (modeValues.ContainsKey(code))
                        {
                            tmp += ": ";
                            byteCount += modeValues[code] * 2;
                            for (int i = 0; i < modeValues[code]; i++)
                                tmp += br.ReadInt16() + ((i == modeValues[code] - 1) ? "" : " ");
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

                        if (modes.ContainsValue(s[0]))
                        {
                            bw.Write(modes.Where(m => m.Value == s[0]).ToList()[0].Key);
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
            switch (_platform)
            {
                case Platform.ThreeDS:
                    switch (_font)
                    {
                        case Font.Default:
                        default:
                            result.Add((triDefCharConv.ContainsKey(code) ? triDefCharConv[code] : escapeChar, 2, false));
                            break;
                        case Font.Universal:
                            result.Add(
                                (triUniCharConv.ContainsKey(code) ?
                                    triUniCharConv[code] :
                                    triDefCharConv.ContainsKey(code) ?
                                        triDefCharConv[code] :
                                        escapeChar,
                                 2, false));
                            break;
                    }
                    break;
                case Platform.DS:
                    /*switch (_font)
                    {
                        case Font.Default:
                        default:
                            result.Add((dsDefCharConv.ContainsKey(code) ? dsDefCharConv[code] : escapeChar, 2, false));
                            break;
                        case Font.Universal:
                            result.Add(
                                (dsUniCharConv.ContainsKey(code) ?
                                    dsUniCharConv[code] :
                                    dsDefCharConv.ContainsKey(code) ?
                                        dsDefCharConv[code] :
                                        escapeChar,
                                 2, false));
                            break;
                    }*/
                    break;
            }
        }

        private void WriteChar(Stream input, string character)
        {
            using (var bw = new BinaryWriterY(input, true))
            {
                switch (_platform)
                {
                    case Platform.ThreeDS:
                        switch (_font)
                        {
                            case Font.Default:
                            default:
                                bw.Write(triDefCharConv.ContainsValue(character) ?
                                    triDefCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                    triDefCharConv.Where(d => d.Value == escapeChar).ToList()[0].Key);
                                break;
                            case Font.Universal:
                                bw.Write(triUniCharConv.ContainsValue(character) ?
                                    triUniCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                    triDefCharConv.ContainsValue(character) ?
                                        triDefCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                        triDefCharConv.Where(d => d.Value == escapeChar).ToList()[0].Key);
                                break;
                        }
                        break;
                    case Platform.DS:
                        /*switch (_font)
                        {
                            case Font.Default:
                            default:
                                bw.Write(dsDefCharConv.ContainsValue(character) ?
                                    dsDefCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                    dsDefCharConv.Where(d => d.Value == escapeChar).ToList()[0].Key);
                                break;
                            case Font.Universal:
                                bw.Write(dsUniCharConv.ContainsValue(character) ?
                                    dsUniCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                    dsDefCharConv.ContainsValue(character) ?
                                        dsDefCharConv.Where(d => d.Value == character).ToList()[0].Key :
                                        dsDefCharConv.Where(d => d.Value == escapeChar).ToList()[0].Key);
                                break;
                        }*/
                        break;
                }
            }
        }
    }
}
