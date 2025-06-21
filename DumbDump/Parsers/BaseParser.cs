using System.Diagnostics.CodeAnalysis;

namespace DumbDump.Parsers;

public abstract class BaseParser : IDisposable
{
    public const string inputDirectoryPath = "..\\..\\..\\input";
    private const string outputDirectoryPath = "..\\..\\..\\output";

    public required int InsertCounter { get; set; } = 0;
    public required int GoCommandFrequency { get; init; }

    public required StreamReader StreamReader { get; init; }
    public required StreamWriter StreamWriter { get; init; }
    public required string StreamReaderCurrentLine { get; set; }

    [method: SetsRequiredMembers]
    public BaseParser(string fileName, string databaseName, int goCommandFrequency)
    {
        GoCommandFrequency = goCommandFrequency;

        var outputfileIndex = Directory.GetFiles(outputDirectoryPath)
            .Select(x => x.Split('\\').Last())
            .Select(x => int.Parse(x.Split('_')[0]))
            .OrderByDescending(x => x)
            .FirstOrDefault() + 1;

        // Reading from source and writing to target:
        StreamReader = File.OpenText(Path.Combine(inputDirectoryPath, fileName!));
        StreamReaderCurrentLine = StreamReader.ReadLine()!;

        StreamWriter = new StreamWriter(Path.Combine(outputDirectoryPath, $"{outputfileIndex}_processed-{fileName.Split('.')[0]}-{DateTime.Now:yyyy-MM-dd}.sql"));
        StreamWriter.WriteLine(StreamReaderCurrentLine.Replace("FilterPlanningSystem", databaseName));
        StreamWriter.WriteLine(ParserConstants.GoCommand);
    }

    public void Dispose()
    {
        StreamReader.Dispose();
        StreamWriter.Dispose();

        GC.SuppressFinalize(this);
    }

    public abstract void Parse();
}
