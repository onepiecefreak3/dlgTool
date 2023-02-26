using CommandLine;
using CommandLine.Text;
using dlgTool.Models;
using dlgTool.Parser;
using dlgTool.Provider;
using System.IO;
using System.Text;

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

    if (!MappingProvider.Exists(game, region))
    {
        Console.WriteLine($"No mapping exists for game '{game.ToString().ToLower()}' in region '{region.ToString().ToLower()}'");
        return;
    }

    var provider = MappingProvider.Load(game, region);

    switch (mode)
    {
        case Mode.Extract:
            Extract(o.Path, provider);
            break;

        case Mode.Create:
            Create(o.Path, provider);
            break;
    }
}

void Extract(string path, MappingProvider provider)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Path {path} has to be an existing file.");
        return;
    }
    
    var reader = new MesAllReader(provider);
    reader.Read(path);
}

void Create(string path, MappingProvider provider)
{
    if (!Directory.Exists(path))
    {
        Console.WriteLine($"Path {path} has to be an existing directory.");
        return;
    }

    var reader = new MesAllWriter(provider);
    reader.Write(path);
}