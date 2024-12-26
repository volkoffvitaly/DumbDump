Console.Write("File name with extension in 'input' directory? (.txt or .sql) > ");
var fileName = Console.ReadLine();

Console.Write("Target database name > ");
var databaseName = Console.ReadLine();

Console.Write("INSERT count for one GO stetament? (integer) > ");
var go_freq = int.Parse(Console.ReadLine()!);
var insert_counter = 0;

string? currentLine;
bool firstInsertFounded = false;

using var reader = File.OpenText(Path.Combine("..\\..\\..\\input", fileName!));
using var writer = new StreamWriter(Path.Combine("..\\..\\..\\output", $"output-dump-{DateTime.Now.ToString("yyyy-MM-dd")}_{go_freq}inserts.sql"));

currentLine = reader.ReadLine();
var useDbLine = currentLine!.Replace("FilterPlanningSystem", databaseName);
writer.WriteLine(useDbLine);
writer.WriteLine("GO");
writer.WriteLine(
    $@"
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
;
while (null != (currentLine = reader.ReadLine()))
{
    currentLine = currentLine.Replace("[dump]", "[external]");
    if (!firstInsertFounded)
    {
        if (currentLine.StartsWith("INSERT "))
        {
            firstInsertFounded = true;
            insert_counter++;
        }

        writer.WriteLine(currentLine);
    }
    else
    {
        if (currentLine.StartsWith("INSERT ")) {
            insert_counter++;
            writer.WriteLine(currentLine);
        }
        else if (currentLine.StartsWith("GO") && insert_counter == go_freq)
        {
            writer.WriteLine(currentLine);
            insert_counter = 0;
        }
    }
}