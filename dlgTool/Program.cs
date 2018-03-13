using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using dlgTool.CustomEncoding;
using dlgTool.IO;
using dlgTool.Compression;

namespace dlgTool
{
    class Program
    {
        static Dictionary<string, Font> langs = new Dictionary<string, Font>
        {
            ["uni"] = Font.Universal
        };

        static Dictionary<string, Platform> platforms = new Dictionary<string, Platform>
        {
            //["ds"] = Platform.DS
        };

        class DlgEntry
        {
            public int offset;
            public int size;
        }

        static void Main(string[] args)
        {
            Font _font = Font.Default;
            Platform _platform = Platform.ThreeDS;

            if (args.Count() < 2)
            {
                Console.WriteLine("Usage: dlgTool.exe <mode> <input> [lang=default] [platform=3ds]\n\nAvailable modes:" +
                    "\n-e\tExtract a given dlg\n\t<input> needs to be a dlg file" +
                    "\n-c\tCreates a dlg by a given folder of txt's\n\t<input> needs to be a folder of txt's" +
                    "\n\n[lang] is an optional parameter. If not specified the tool uses the charset of the original game to convert the files." +
                    "\nIf specified it uses the given charset to convert the files.\nSupported languages are: " + langs.Aggregate("", (o, e) => o + e.Key + ((langs.Last().Key == e.Key) ? "" : ", ")) +
                    "\n\n[platform] is an optional parameter. If not specified the tool uses the default platform, which is the 3DS, to convert the files." +
                    "\nIf specified it uses the given platform to convert the files.\nSupported platforms are: " + platforms.Aggregate("", (o, e) => o + e.Key + ((platforms.Last().Key == e.Key) ? "" : ", ")));
                return;
            }
            if (args[0] == "-h")
            {
                Console.WriteLine("Usage: dlgTool.exe <mode> <input> [lang=default] [platform=3ds]\n\nAvailable modes:" +
                    "\n-e\tExtract a given dlg\n\t<input> needs to be a dlg file" +
                    "\n-c\tCreates a dlg by a given folder of txt's\n\t<input> needs to be a folder of txt's" +
                    "\n\n[lang] is an optional parameter. If not specified the tool uses the charset of the original game to convert the files." +
                    "\nIf specified it uses the given charset to convert the files.\nSupported languages are: " + langs.Aggregate("", (o, e) => o + e.Key + ((langs.Last().Key == e.Key) ? "" : ", ")) +
                    "\n\n[platform] is an optional parameter. If not specified the tool uses the default platform, which is the 3DS, to convert the files." +
                    "\nIf specified it uses the given platform to convert the files.\nSupported platforms are: " + platforms.Aggregate("", (o, e) => o + e.Key + ((platforms.Last().Key == e.Key) ? "" : ", ")));
                return;
            }
            if (args[0] != "-e" && args[0] != "-c")
            {
                Console.WriteLine("Unknown mode\n\nAvailable modes:" +
                    "\n-e\tExtract a given dlg\n\t<input> needs to be a dlg file" +
                    "\n-c\tCreates a dlg by a given folder of txt's\n\t<input> needs to be a folder of txt's");
                return;
            }

            if (args.Count() > 2)
            {
                if (!langs.ContainsKey(args[2]))
                {
                    Console.WriteLine("Supported languages are: " + langs.Aggregate("", (o, e) => o + e.Key + ((langs.Last().Key == e.Key) ? "" : ", ")) + "\nUsing default language.");
                }
                else
                {
                    _font = langs[args[2]];
                }
            }

            if (args.Count() > 3)
            {
                if (!platforms.ContainsKey(args[3]))
                {
                    Console.WriteLine("Supported platforms are: " + platforms.Aggregate("", (o, e) => o + e.Key + ((platforms.Last().Key == e.Key) ? "" : ", ")) + "\nUsing default platform.");
                }
                else
                {
                    _platform = platforms[args[3]];
                }
            }

            if (args[0] == "-e")
            {
                if (!File.Exists(args[1]))
                {
                    Console.WriteLine($"{args[1]} doesn't exist.");
                    return;
                }

                if ((File.GetAttributes(args[1]) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    Console.WriteLine($"{args[1]} is a directory. Need a dlg file.");
                    return;
                }

                var sections = new List<byte[]>();
                using (var br = new BinaryReaderY(File.OpenRead(args[1])))
                {
                    try
                    {
                        var entryCount = br.ReadInt32();
                        var entryList = br.ReadMultiple<DlgEntry>(entryCount);
                        foreach (var e in entryList)
                        {
                            br.BaseStream.Position = e.offset;
                            sections.Add(Nintendo.Decompress(new MemoryStream(br.ReadBytes(e.size))));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("This file seems a bit corrupted or isn't a dlg. Either way it won't work.");
                        Console.WriteLine(ex.Message + " - " + ex.InnerException);
                        return;
                    }

                }
                var writeDir = Path.Combine(Path.GetDirectoryName(args[1]), Path.GetFileNameWithoutExtension(args[1]));
                if (!Directory.Exists(writeDir))
                    Directory.CreateDirectory(writeDir);

                var txtCount = 0;
                foreach (var s in sections)
                {
                    using (var br = new BinaryReaderY(new MemoryStream(s)))
                    {
                        var enc = new AAEncoding(_font, _platform);

                        File.WriteAllText(Path.Combine(writeDir, $"{txtCount++}.txt"), enc.GetSectionText(s));

                        //Debug
                        //File.WriteAllBytes(Path.Combine(writeDir, $"{txtCount - 1}.bin"), s);
                    }
                }
            }
            else if (args[0] == "-c")
            {
                if (!Directory.Exists(args[1]))
                {
                    Console.WriteLine($"{args[1]} doesn't exist.");
                    return;
                }

                if ((File.GetAttributes(args[1]) & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    Console.WriteLine($"{args[1]} is a file. Need a directory of txt's.");
                    return;
                }

                var enc = new AAEncoding(_font, _platform);
                var files = Directory.GetFiles(args[1]).Where(f => Path.GetExtension(f) == ".txt").ToList();
                var entries = new List<DlgEntry>();

                var ms = new MemoryStream();
                using (var bw = new BinaryWriterY(ms, true))
                {
                    bw.BaseStream.Position = files.Count * 8 + 4;

                    foreach (var file in files)
                    {
                        var sectionText = File.ReadAllText(file).Replace("\r\n", "");

                        var bytes = enc.GetBytes(sectionText);
                        var compBytes = Nintendo.Compress(new MemoryStream(bytes), Nintendo.Method.LZ10);
                        entries.Add(new DlgEntry { offset = (int)bw.BaseStream.Position, size = compBytes.Length });

                        bw.Write(compBytes);
                        bw.BaseStream.Position = (bw.BaseStream.Position + 3) & ~3;

                        //Debug
                        //File.WriteAllBytes(file.Replace(".txt", "_new.bin"), bytes);
                    }

                    bw.BaseStream.Position = 0;
                    bw.Write(entries.Count);
                    foreach (var entry in entries)
                        bw.WriteStruct(entry);
                }

                File.WriteAllBytes("C:\\Users\\Kirito\\Desktop\\new.dlg", ms.ToArray());
            }
        }
    }
}
