using CommandLine;

namespace dlgTool.Models
{
    /*                        $"Usage: dlgTool.exe [PARAMS]\n\n" +
                        $"PARAMS:\n" +
                        $"\t-m <mode> - The mode to execute in. Can be \"extract\" or \"create\"\n" +
                        $"\t-f <path> - The file or folder path. Needs to be a file for \"extract\" and a folder for \"create\"\n" +
                        $"\t-l <font> - OPTIONAL; Can be one of the following: " + langs.Aggregate("", (o, e) => o + e.Key + ((langs.Last().Key == e.Key) ? "" : ", ")) + "\n" +
                        $"\t\tBy default it's \"orig\"\n" +
                        $"\t-g <game> - OPTIONAL; Can be one of the following: " + games.Aggregate("", (o, e) => o + e.Key + ((games.Last().Key == e.Key) ? "" : ", ")) + "\n" +
                        $"\t\tBy default it's \"aatri\"\n\n" +
                        $"\t-h - Shows this help\n" +
                        $"\t-b - Extracts or recreates by using the binary files instead of txt's");*/

    class Options
    {
        [Option('m', "mode", Required = true, HelpText = "Set the mode to execute in\n  extract: Extract .bin-Files to .txt-Files in a given path\n  create: Create .bin-Files from .txt-Files in a given path")]
        public string Mode { get; set; }

        [Option('p', "path", Required = true, HelpText = "The path to process on. Has to be a file for 'extract' and a directory for 'create'")]
        public string Path { get; set; }

        [Option('g', "game", Required = true, HelpText = "The game to process files from\n  aa1\n  aa2\n  aa3\n  aa4")]
        public string Game { get; set; }

        [Option('r', "region", Required = true, HelpText = "The region of the game to process files from\n  jp\n  us\n  eu")]
        public string Region { get; set; }

        public bool TryGetMode(out Mode mode) => Enum.TryParse(Mode, true, out mode);

        public bool TryGetGame(out Game game) => Enum.TryParse(Game, true, out game);

        public bool TryGetRegion(out Region region) => Enum.TryParse(Region, true, out region);
    }
}
