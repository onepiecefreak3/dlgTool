using CommandLine;
using CommandLine.Text;
using dlgTool.Models;
using dlgTool.Parser;
using dlgTool.Provider;

var parser = new Parser(parserSettings => parserSettings.AutoHelp = true);

var parsedResult = parser.ParseArguments<Options>(args);

parsedResult
    .WithNotParsed(_ => DisplayHelp(parsedResult))
    .WithParsed(Execute);

void DisplayHelp<T>(ParserResult<T> result)
{
    var helpText = HelpText.AutoBuild(result, h =>
    {
        h.AdditionalNewLineAfterOption = false;
        return HelpText.DefaultParsingErrorsHandler(result, h);
    }, e => e);

    Console.WriteLine(helpText);
}

void Execute(Options o)
{
    if (!o.TryGetMode(out var mode))
    {
        Console.WriteLine($"Unsupported mode {o.Mode}");
        return;
    }

    if (!o.TryGetFormat(out var format))
    {
        Console.WriteLine($"Unsupported format {o.Format}");
        return;
    }

    if (!o.TryGetGame(out var game))
    {
        Console.WriteLine($"Unsupported game {game}");
        return;
    }

    if (!o.TryGetRegion(out var region))
    {
        Console.WriteLine($"Unsupported region {region}");
        return;
    }

    if (!TryGetMappingProvider(format, game, region, out MappingProvider? provider))
    {
        Console.WriteLine($"No mapping exists for game '{game.ToString().ToLower()}' in region '{region.ToString().ToLower()}'");
        return;
    }

    switch (mode)
    {
        case Mode.Extract:
            Extract(o.Path, format, provider!);
            break;

        case Mode.Create:
            Create(o.Path, format, provider!);
            break;
    }
}

bool TryGetMappingProvider(Format format, Game game, Region region, out MappingProvider? provider)
{
    provider = null;

    switch (format)
    {
        case Format.MesAll:
            return MesAllMappingProvider.TryGet(game, region, out provider);

        case Format.Mdt:
            return MdtMappingProvider.TryGet(game, region, out provider);

        default:
            throw new InvalidOperationException($"Unknown format {format}.");
    }

    return true;
}

void Extract(string path, Format format, MappingProvider provider)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Path {path} has to be an existing file.");
        return;
    }

    IReader reader;
    switch (format)
    {
        case Format.MesAll:
            reader = new MesAllReader(provider);
            break;

        case Format.Mdt:
            reader = new MdtReader(provider);
            break;

        default:
            throw new InvalidOperationException($"Unknown format {format}.");
    }

    reader.Read(path);
}

void Create(string path, Format format, MappingProvider provider)
{
    IWriter writer;
    switch (format)
    {
        case Format.MesAll:
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Path {path} has to be an existing directory.");
                return;
            }

            writer = new MesAllWriter(provider);
            break;

        case Format.Mdt:
            if (!File.Exists(path))
            {
                Console.WriteLine($"Path {path} has to be an existing file.");
                return;
            }

            writer = new MdtWriter(provider);
            break;

        default:
            throw new InvalidOperationException($"Unknown format {format}.");
    }

    writer.Write(path);
}