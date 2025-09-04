using System.Diagnostics.CodeAnalysis;

namespace DumbDump.Parsers;

[method: SetsRequiredMembers]
public class ExternalIntegrationTablesParser(string fileName, string databaseName, int goCommandFrequency) : BaseParser(fileName, databaseName, goCommandFrequency)
{
    private const string SqlToDeletePreviosArtifacts =
    $@"
        BEGIN TRAN;

        DELETE FROM [dbo].[Schedules];

        DROP TABLE IF EXISTS [external].[Prod_LinesDowntimes];
        DROP TABLE IF EXISTS [external].[Prod_OutputDataForKdfExcel];
        DROP TABLE IF EXISTS [external].[Prod_ProductionPlanForKdfExcel];
        DROP TABLE IF EXISTS [external].[Prod_LOCATION_INVENTORY_];
        DROP TABLE IF EXISTS [external].[Prod_FDC_Free_trays];
        DROP TABLE IF EXISTS [external].[Prod_FDC_Stock];

        IF(NOT EXISTS(SELECT * FROM sys.schemas WHERE name = 'external'))
        BEGIN
            EXEC('CREATE SCHEMA [external] AUTHORIZATION [dbo]')
        END

        COMMIT TRAN;
    ";

    public override void Parse()
    {
        // Cleaning after previous applied dump
        StreamWriter.WriteLine(SqlToDeletePreviosArtifacts);

        while (null != (StreamReaderCurrentLine = StreamReader.ReadLine()!) && !StreamReaderCurrentLine.StartsWith(ParserConstants.InsertStatement) && !StreamReaderCurrentLine.StartsWith(ParserConstants.SetIdentityInsertStatement))
        {
            ChangeSchemaAndTableName(isDataStarted: false);
            StreamWriter.WriteLine(StreamReaderCurrentLine);
        }

        ChangeSchemaAndTableName(isDataStarted: true);
        StreamWriter.WriteLine(StreamReaderCurrentLine);

        while (null != (StreamReaderCurrentLine = StreamReader.ReadLine()!))
        {
            if (StreamReaderCurrentLine.StartsWith(ParserConstants.GoCommand) && InsertCounter != GoCommandFrequency)
                continue;

            ChangeSchemaAndTableName(isDataStarted: true);

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

    private void ChangeSchemaAndTableName(bool isDataStarted)
    {
        var isInternalTableFound = TryFindInternalTableNameOccurrence(out string? sourceTableName, out string? targetTableName);

        if (!isDataStarted && isInternalTableFound)
        {
            while (!(StreamReaderCurrentLine = StreamReader.ReadLine()!).Contains("/****** Object:")) { }
        }
        else if (isDataStarted && isInternalTableFound)
        {
            StreamReaderCurrentLine = StreamReaderCurrentLine!
                .Replace($"[{ParserConstants.DumpSchemeName}]", $"[{ParserConstants.DboSchemeName}]")
                .Replace($".[{sourceTableName}]", $".[{targetTableName}]");
        }
        else
        {
            StreamReaderCurrentLine = StreamReaderCurrentLine!
                .Replace($"[{ParserConstants.DumpSchemeName}]", $"[{ParserConstants.ExternalSchemeName}]");
        }
    }

    // [external] / [dbo]
    private readonly Dictionary<string, string> dumpedInternalTables = new() { { "Internal_Schedules", "Schedules" } };

    private bool TryFindInternalTableNameOccurrence(out string? sourceTableName, out string? targetTableName)
    {
        var kvp = dumpedInternalTables.SingleOrDefault(tableName =>
            StreamReaderCurrentLine!.Contains($"[{ParserConstants.DumpSchemeName}].[{tableName.Key}]"));

        sourceTableName = kvp.Key;
        targetTableName = kvp.Value;

        return !kvp.Equals(default(KeyValuePair<string, string>));
    }
}
