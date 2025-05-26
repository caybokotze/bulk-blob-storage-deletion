// See https://aka.ms/new-console-template for more information

using Azure.Storage.Blobs;
using CommandLine;
using Sharprompt;

namespace BlobStorageBulkDeletion;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var arguments = new CliArguments();
        var cliMode = args.Length != 0;
        var cliParseFailed = false;

        Parser.Default
            .ParseArguments<CliArguments>(args)
            .WithParsed(opts =>
            {
                arguments = opts;
                cliMode = true;
            })
            .WithNotParsed(_ =>
            {
                cliParseFailed = true;
            });

        if (cliMode && cliParseFailed)
        {
            throw new NullReferenceException("The options to execute this tool was not set correctly. Try running again with CLI options set like -h or --help");
        }

        if (arguments.DryRun)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Currently DryRun is enabled, which means the delete logic will be skipped");
            Console.ForegroundColor = ConsoleColor.Black;
        }

        Console.WriteLine($"Running with `--thread-count-limit` set to {arguments.ConcurrentDeleteThreadLimit}");

        await HandleDeletionAsync(arguments);
    }

    private static async Task HandleDeletionAsync(CliArguments arguments)
    {
        if (string.IsNullOrEmpty(arguments.ConnectionString))
        {
            arguments.ConnectionString = Prompt.Input<string>("Enter your Azure Blob Storage connection string",
                placeholder: "DefaultEndpointsProtocol=https;AccountName=...").Trim();
        }

        try
        {
            var serviceClient = new BlobServiceClient(arguments.ConnectionString);

            // Fetch containers
            var containers = new List<string>();
            await foreach (var container in serviceClient.GetBlobContainersAsync())
            {
                containers.Add(container.Name);
            }

            if (containers.Count == 0)
            {
                Console.WriteLine("No containers found.");
                return;
            }

            // Let user select a container
            var selectedContainer = Prompt.Select("Select a container to delete:", containers);

            var containerClient = serviceClient.GetBlobContainerClient(selectedContainer);

            // List blobs
            var blobs = new List<string>();
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobs.Add(blob.Name);
            }

            if (blobs.Count == 0)
            {
                Console.WriteLine("No blobs found in this container.");
                return;
            }

            Console.WriteLine($"\nFound {blobs.Count} blobs in '{selectedContainer}':");
            foreach (var blob in blobs)
            {
                Console.WriteLine($"- {blob}");
            }

            if (arguments.DryRun)
            {
                Console.WriteLine("\nDry run complete. No blobs were deleted.");
                return;
            }

            // Confirm deletion
            var confirm = Prompt.Confirm($"\nAre you sure you want to delete all {blobs.Count} blobs?");
            if (!confirm)
            {
                Console.WriteLine("Operation cancelled.");
                return;
            }

            // Delete blobs concurrently.
            var semaphore = new SemaphoreSlim(arguments.ConcurrentDeleteThreadLimit);
            var deleteTasks = blobs.Select(async blob =>
            {
                try
                {
                    await semaphore.WaitAsync();
                    var blobClient = containerClient.GetBlobClient(blob);
                    await blobClient.DeleteIfExistsAsync();
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"Deleted: {blob}");
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                catch
                {
                    Console.WriteLine("Something went wrong while the file was being deleted.");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(deleteTasks);

            Console.WriteLine("✅ All blobs deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}