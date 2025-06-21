using DumbDump;
using DumbDump.Parsers;

class Program
{
    static void Main()
    {
        var parseType = ParseTypeInit();
        var fileName = FileNameInit();
        var databaseName = DatabaseNameInit();
        var goCommandFrequency = GoCommandFrequencyInit(parseType);

        using BaseParser parser = parseType switch
        {
            ParserType.ExternalIntegrationTables => new ExternalIntegrationTablesParser(fileName, databaseName, goCommandFrequency),
            ParserType.AnyInternalData => new AnyInternalDataParser(fileName, databaseName, goCommandFrequency),
            _ => throw new NotImplementedException(),
        };

        parser.Parse();
    }

    private static ParserType ParseTypeInit()
    {
        Console.WriteLine(
@$"1. What's we are parsing? 
- Parser types:
{nameof(ParserType.ExternalIntegrationTables)}: {(int)ParserType.ExternalIntegrationTables}
{nameof(ParserType.AnyInternalData)}:           {(int)ParserType.AnyInternalData}");

    ParseTypeInit:
        Console.Write("> ");

        var parsingType = ReadLine();

        if (ushort.TryParse(parsingType, out ushort result))
        {
            var parsingTypeResult = Enum.GetValues<ParserType>().SingleOrDefault(x => (int)x == result);

            if (parsingTypeResult == default)
            {
                InputError("Provide a digit from list above");
                goto ParseTypeInit;
            }

            Console.WriteLine();

            return parsingTypeResult;
        }
        else
        {
            InputError("Provide a digit from list above");
            goto ParseTypeInit;
        }
    }

    private static string FileNameInit()
    {
        Console.WriteLine(
@"2. What's file name in '..\input' directory?
- Accepted extensions: .txt or .sql (example: copy-from-query-window.txt)
- Any string or empty if only one file there");

    FileNameInit:
        Console.Write("> ");
        
        var fileName = ReadLine();

        var fileNames = Directory.GetFiles(BaseParser.inputDirectoryPath).Select(x => x.Split('\\').Last()).ToList();
        if (string.IsNullOrEmpty(fileName) && fileNames.Count != 1)
        {
            InputError("More than 1 file in 'input' directory, specify file name directly or clean 'input' directory'");
            goto FileNameInit;
        }
        else if (!string.IsNullOrEmpty(fileName) && !fileNames.Contains(fileName))
        {
            InputError("No file with such name in 'input' directory'");
            goto FileNameInit;
        }
        else if (string.IsNullOrEmpty(fileName) && fileNames.Count == 1)
            fileName = fileNames[0];

        Console.WriteLine();

        return fileName!;
    }

    private static string DatabaseNameInit()
    {
        Console.WriteLine("3. Target database name?");

    DatabaseNameInit:
        Console.Write("> ");

        var databaseName = ReadLine();
        if (string.IsNullOrEmpty(databaseName))
        {
            InputError("Database name can not be empty");
            goto DatabaseNameInit;
        }

        Console.WriteLine();

        return databaseName!;
    }

    private static int GoCommandFrequencyInit(ParserType parserType)
    {
        var defaultGoCommandFrequency = parserType switch
        {
            ParserType.ExternalIntegrationTables => 1000,
            ParserType.AnyInternalData => int.MaxValue,
            _ => throw new NotImplementedException(),
        };

        Console.WriteLine(
$@"4. INSERT count for one GO command?
- integer, greater than 0
- default value for ParserType = {parserType} is {defaultGoCommandFrequency}");

    GoCommandFrequencyInit:
        Console.Write("> ");

        var input = ReadLine();
        if (string.IsNullOrEmpty(input))
            input = defaultGoCommandFrequency.ToString();
        if (!int.TryParse(input, out int value))
        {
            InputError("Provide a digit");
            goto GoCommandFrequencyInit;
        }

        Console.WriteLine();

        return value;
    }

    private static string? ReadLine()
    {
        return Console.ReadLine()?.Trim();
    }

    private static void InputError(string errorDetails)
    {
        Console.Error.WriteLine($"\n{errorDetails}.");
    }
}