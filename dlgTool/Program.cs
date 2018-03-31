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
            ["edit"] = Font.Edited,
            ["orig"] = Font.Original
        };

        static Dictionary<string, Game> games = new Dictionary<string, Game>
        {
            ["aatri"] = Game.AATri,
            ["aj3ds"] = Game.AJ3DS,
        };

        class DlgEntry
        {
            public int offset;
            public int size;
        }

        class Options
        {
            public string mode = "";
            public string path = "";
            public Font lang = Font.Original;
            public Game game = Game.AATri;
            public bool isBinary = false;
        }

        static Options ParseArgs(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine($"" +
                        $"Usage: dlgTool.exe [PARAMS]\n\n" +
                        $"PARAMS:\n" +
                        $"\t-m <mode> - The mode to execute in. Can be \"extract\" or \"create\"\n" +
                        $"\t-f <path> - The file or folder path. Needs to be a file for \"extract\" and a folder for \"create\"\n" +
                        $"\t-l <font> - OPTIONAL; Can be one of the following: " + langs.Aggregate("", (o, e) => o + e.Key + ((langs.Last().Key == e.Key) ? "" : ", ")) + "\n" +
                        $"\t\tBy default it's \"orig\"\n" +
                        $"\t-g <game> - OPTIONAL; Can be one of the following: " + games.Aggregate("", (o, e) => o + e.Key + ((games.Last().Key == e.Key) ? "" : ", ")) + "\n" +
                        $"\t\tBy default it's \"aatri\"\n\n" +
                        $"\t-h - Shows this help\n" +
                        $"\t-b - Extracts or recreates by using the binary files instead of txt's");
                Environment.Exit(0);
            }

            var opt = new Options();
            List<string> modes = new List<string> { "-m", "-f", "-l", "-g", "-b", "-h" };

            for (int i = 0; i < args.Count(); i++)
            {
                var param = args[i];
                if (!modes.Contains(param))
                {
                    Console.WriteLine($"{param} is an unknown mode.");
                    Environment.Exit(0);
                }
                if (param != "-b" && param != "-h")
                {
                    if (i + 1 < args.Count())
                    {
                        if (args[i + 1][0] == '-')
                        {
                            Console.WriteLine($"No value is given for {param}");
                            Environment.Exit(0);
                        }
                        else
                        {
                            var value = args[i++ + 1];
                            switch (param)
                            {
                                case "-m":
                                    if (value != "extract" && value != "create")
                                    {
                                        Console.WriteLine($"Mode can only be \"extract\" or \"create\".");
                                        Environment.Exit(0);
                                    }
                                    else
                                    {
                                        opt.mode = value;
                                    }
                                    break;
                                case "-f":
                                    opt.path = value;
                                    break;
                                case "-l":
                                    if (!langs.ContainsKey(value))
                                    {
                                        Console.WriteLine($"Selected Font {value} isn't supported.");
                                        Environment.Exit(0);
                                    }
                                    else
                                    {
                                        opt.lang = langs[value];
                                    }
                                    break;
                                case "-g":
                                    if (!games.ContainsKey(value))
                                    {
                                        Console.WriteLine($"Selected Game {value} isn't supported.");
                                        Environment.Exit(0);
                                    }
                                    else
                                    {
                                        opt.game = games[value];
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No value is given for {param}");
                        Environment.Exit(0);
                    }
                }
                else if (param == "-h")
                {
                    Console.WriteLine($"" +
                        $"Usage: dlgTool.exe [PARAMS]\n\n" +
                        $"PARAMS:\n" +
                        $"\t-m <mode> - The mode to execute in. Can be \"extract\" or \"create\"\n" +
                        $"\t-f <path> - The file or folder path. Needs to be a file for \"extract\" and a folder for \"create\"" +
                        $"\t-l <font> - OPTIONAL; Can be one of the following: " + langs.Aggregate("", (o, e) => o + e.Key + ((langs.Last().Key == e.Key) ? "" : ", ")) + "\n" +
                        $"\t\tBy default it's \"orig\"\n" +
                        $"\t-g <game> - OPTIONAL; Can be one of the following: " + games.Aggregate("", (o, e) => o + e.Key + ((games.Last().Key == e.Key) ? "" : ", ")) + "\n" +
                        $"\t\tBy default it's \"aatri\"\n\n" +
                        $"\t-h - Shows this help\n" +
                        $"\t-b - Extracts or recreates by using the binary files instead of txt's");
                    Environment.Exit(0);
                }
                else if (param == "-b")
                {
                    opt.isBinary = true;
                }

            }

            //Check set options
            if (opt.mode == String.Empty)
            {
                Console.WriteLine($"No Mode is given.");
                Environment.Exit(0);
            }
            if (opt.path == String.Empty)
            {
                Console.WriteLine($"No Path is given.");
                Environment.Exit(0);
            }
            if (opt.mode == "create")
            {
                if ((File.GetAttributes(opt.path) & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    Console.WriteLine($"Path has to be a folder.");
                    Environment.Exit(0);
                }
                if (!Directory.Exists(opt.path))
                {
                    Console.WriteLine($"Folder doesn't exist.");
                    Environment.Exit(0);
                }
            }
            if (opt.mode == "extract")
            {
                if ((File.GetAttributes(opt.path) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    Console.WriteLine($"Path has to be a file.");
                    Environment.Exit(0);
                }
                if (!File.Exists(opt.path))
                {
                    Console.WriteLine($"File doesn't exist.");
                    Environment.Exit(0);
                }
            }

            return opt;
        }

        static void Main(string[] args2)
        {
            var options = ParseArgs(args2);

            if (options.mode == "extract")
            {
                var sections = new List<byte[]>();
                using (var br = new BinaryReaderY(File.OpenRead(options.path)))
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
                var writeDir = Path.Combine(Path.GetDirectoryName(options.path), Path.GetFileNameWithoutExtension(options.path));
                if (!Directory.Exists(writeDir))
                    Directory.CreateDirectory(writeDir);

                var txtCount = 0;
                foreach (var s in sections)
                {
                    using (var br = new BinaryReaderY(new MemoryStream(s)))
                    {
                        if (options.isBinary)
                        {
                            File.WriteAllBytes(Path.Combine(writeDir, $"{txtCount++:000}.bin"), s);
                        }
                        else
                        {
                            var enc = new AAEncoding(options.lang, options.game);
                            File.WriteAllText(Path.Combine(writeDir, $"{txtCount++:000}.txt"), enc.GetSectionText(s));
                        }
                    }
                }
            }
            else if (options.mode == "create")
            {
                var enc = new AAEncoding(options.lang, options.game);
                var files = Directory.GetFiles(options.path).Where(f => Path.GetExtension(f) == ((options.isBinary) ? ".bin" : ".txt")).OrderBy(name => name).ToList();
                var entries = new List<DlgEntry>();

                var ms = new MemoryStream();
                using (var bw = new BinaryWriterY(ms, true))
                {
                    bw.BaseStream.Position = files.Count * 8 + 4;

                    foreach (var file in files)
                    {
                        byte[] compBytes = null;
                        if (options.isBinary)
                        {
                            compBytes = Nintendo.Compress(new MemoryStream(File.ReadAllBytes(file)), Nintendo.Method.LZ10);
                        }
                        else
                        {
                            var sectionText = File.ReadAllText(file).Replace("\r\n", "");
                            var bytes = enc.GetBytes(sectionText);
                            compBytes = Nintendo.Compress(new MemoryStream(bytes), Nintendo.Method.LZ10);
                        }

                        entries.Add(new DlgEntry { offset = (int)bw.BaseStream.Position, size = compBytes.Length });

                        bw.Write(compBytes);
                        bw.BaseStream.Position = (bw.BaseStream.Position + 3) & ~3;
                    }

                    bw.BaseStream.Position = 0;
                    bw.Write(entries.Count);
                    foreach (var entry in entries)
                        bw.WriteStruct(entry);
                }

                File.WriteAllBytes("new.dlg", ms.ToArray());
            }
        }
    }
}
