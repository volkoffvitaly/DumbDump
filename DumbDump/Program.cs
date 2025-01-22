#region Input parameters reading

Console.Write(

@"What's file name in '..\input' directory?
- Accepted extensions: .txt or .sql (example: copy-from-query-window.txt)
- Any string or empty if only one file there
> ");

var fileName = Console.ReadLine();

Console.Write(
@"
Target database name
> ");

var databaseName = Console.ReadLine();

Console.Write(
@"
INSERT count for one GO stetament?
- integer, greater than 0
> ");

var go_freq = int.Parse(Console.ReadLine()!);

#endregion


var inputDirectoryPath = "..\\..\\..\\input";
var files = Directory.GetFiles(inputDirectoryPath);
if (files.Length == 1)
    fileName = files[0].Split('\\').Last();

using var reader = File.OpenText(Path.Combine(inputDirectoryPath, fileName!));
using var writer = new StreamWriter(Path.Combine("..\\..\\..\\output", $"output-dump-{DateTime.Now:yyyy-MM-dd}_{go_freq}inserts.sql"));

var currentLine = reader.ReadLine();

// Line #1, replace Database and write: USE [DatabaseName]
writer.WriteLine(currentLine!.Replace("FilterPlanningSystem", databaseName)); 

// Line #2: send package
writer.WriteLine("GO");

// Cleaning after previous applied dump
writer.WriteLine(
$@"
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
END");

// dbo_name / external_name
var dumpedInternalTables = new Dictionary<string, string>() {
    { "Internal_Schedules", "Schedules" }
};

bool iteratingOverInternalTableSchema = false;
bool firstInsertFounded = false;
var insert_counter = 0;

while (null != (currentLine = reader.ReadLine()))
{
    #region Internal table schemas
    if (dumpedInternalTables.Any(tableName => currentLine.Contains($"Object:  Table [dump].[{tableName.Key}]")))
    {
        iteratingOverInternalTableSchema = true;
        continue;
    }
    else if (iteratingOverInternalTableSchema && (currentLine.StartsWith("INSERT ") || currentLine.StartsWith("/****** Object: ")))
        iteratingOverInternalTableSchema = false;
    else if (iteratingOverInternalTableSchema)
        continue;
    #endregion
    
    var newTableName = dumpedInternalTables.FirstOrDefault(tableName => currentLine.Contains($"INSERT [dump].[{tableName.Key}]"));

    if (!newTableName.Equals(default(KeyValuePair<string, string>)))
    {
        currentLine = currentLine
            .Replace("[dump]", "[dbo]")
            .Replace($".[{newTableName.Key}]", $".[{newTableName.Value}]");
    }
    else
    {
        currentLine = currentLine
            .Replace("[dump]", "[external]");
    }

    if (!firstInsertFounded) // CREATE TABLES
    {
        if (currentLine.StartsWith("INSERT "))
        {
            firstInsertFounded = true;
            insert_counter++;
        }

        writer.WriteLine(currentLine);
    }
    else // INSERTS
    {
        if (currentLine.StartsWith("INSERT "))
        {
            writer.WriteLine(currentLine);
            insert_counter++;
        }
        else if (currentLine.StartsWith("GO") && insert_counter == go_freq)
        {
            writer.WriteLine(currentLine);
            insert_counter = 0;
        }
    }
}