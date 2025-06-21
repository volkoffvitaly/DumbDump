using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DumbDump.Parsers;

[method: SetsRequiredMembers]
public class AnyInternalDataParser(string fileName, string databaseName, int goCommandFrequency) : BaseParser(fileName, databaseName, goCommandFrequency)
{
    public override void Parse()
    {
        while (null != (StreamReaderCurrentLine = StreamReader.ReadLine()!))
        {
            if (StreamReaderCurrentLine.StartsWith(ParserConstants.GoCommand) && InsertCounter != GoCommandFrequency)
                continue;

            ChangeSchemaName();

            if (!(StreamReaderCurrentLine.StartsWith(ParserConstants.GoCommand) || StreamReaderCurrentLine.StartsWith(ParserConstants.InsertStatement) || StreamReaderCurrentLine.StartsWith(ParserConstants.SetIdentityInsertStatement)))
            {
                StreamWriter.WriteLine(StreamReaderCurrentLine);
            }
            else if (StreamReaderCurrentLine.StartsWith(ParserConstants.SetIdentityInsertStatement))
            {
                StreamWriter.WriteLine(StreamReaderCurrentLine);
            }
            else if (StreamReaderCurrentLine.StartsWith(ParserConstants.InsertStatement + ' '))
            {
                StreamReaderCurrentLine = Regex.Replace(StreamReaderCurrentLine, "(, )?\\[ValidFrom\\], \\[ValidTo\\]", "");
                StreamReaderCurrentLine = Regex.Replace(StreamReaderCurrentLine, "(, )?(CAST.{45}, )(?=CAST\\(N'9999-12-31T23:59:59\\.9999999' AS DateTime2\\))(CAST\\(N'9999-12-31T23:59:59\\.9999999' AS DateTime2\\))", "");
                StreamWriter.WriteLine(StreamReaderCurrentLine);
                InsertCounter++;
            }
            else if (StreamReaderCurrentLine.StartsWith(ParserConstants.GoCommand) && InsertCounter == GoCommandFrequency)
            {
                StreamWriter.WriteLine(ParserConstants.GoCommand);
                InsertCounter = 0;
            }
        }
    }

    private void ChangeSchemaName()
    {
        StreamReaderCurrentLine = StreamReaderCurrentLine!
            .Replace($"[{ParserConstants.DumpSchemeName}]", $"[{ParserConstants.ExternalSchemeName}]");
    }
}
