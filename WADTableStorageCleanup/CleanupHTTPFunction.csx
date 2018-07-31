#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    log.LogDebug("Im debugging");
    log.LogInformation("C# HTTP trigger function processed a request.");

    try
    {       
        // Retrieve storage account from connection-string
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=sfdiagsccdicthce3fe4;AccountKey=715VyJtuBZzmpYIIEhUbyZwfvfqxmMWWYL3FZGtOcNYcdYGa1hD+dRPg3TSuiI3JIFva1a9ghsZdlN2U8J0PiQ==;EndpointSuffix=core.windows.net");
        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

        var tables = new string[] {"WADServiceFabricSystemEventTable", "WADServiceFabricReliableServiceEventTable", "WADServiceFabricReliableServiceEventTable", "WADPerformanceCountersTable", "WADWindowsEventLogsTable"};
        foreach(var table in tables)
        {
            CloudTable cloudTable = tableClient.GetTableReference( table );

            // keep a 14 days of logs
            DateTime keepThreshold = DateTime.UtcNow.AddDays( -14 );

            // Initialize the continuation token to null to start from the beginning of the table.
            TableContinuationToken continuationToken = null;

            log.LogInformation("query for stuff");
            TableQuery query = new TableQuery();
            query.FilterString = string.Format( "PartitionKey lt '0{0}'", keepThreshold.Ticks );
            //var items = cloudTable.ExecuteQuery( query ).Take( 1000 );
            do {
                var queryResult = await cloudTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = queryResult.ContinuationToken;
                var items = queryResult.Results;
                if ( items.Count == 0)
                {
                    log.LogInformation("Nothing to cleanup");
                }
                
                Dictionary<string, TableBatchOperation> batches = new Dictionary<string, TableBatchOperation>();
                foreach ( var entity in items )
                {
                    TableOperation tableOperation = TableOperation.Delete( entity );

                    // need a new batch?
                    if ( !batches.ContainsKey( entity.PartitionKey ) )
                        batches.Add( entity.PartitionKey, new TableBatchOperation() );

                    // can have only 100 per batch
                    if ( batches[entity.PartitionKey].Count < 100)
                        batches[entity.PartitionKey].Add( tableOperation );
                }

                // execute!
                foreach ( var batch in batches.Values )
                {
                    await cloudTable.ExecuteBatchAsync( batch );
                }

                log.LogInformation( table + " truncated: filterstring={FilterString}", query.FilterString);

            } while (continuationToken != null);
        }
    }
    catch ( Exception ex )
    {
        log.LogError("Truncate WADLogsTable exception exception={ex}", ex);
    }

    return (ActionResult)new OkObjectResult($"Truncated table");
}