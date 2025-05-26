using CommandLine;

namespace BlobStorageBulkDeletion;

public class CliArguments
{
    [Option('c', "connection-string", Required = false, HelpText = "Azure Blob Storage connection string.")]
    public string ConnectionString { get; set; } = string.Empty;

    [Option('n', "container", Required = false, HelpText = "Blob container name.")]
    public string Container { get; set; } = string.Empty;

    [Option("thread-count-limit",
        HelpText =
            "Set the upper thread limit to delete files concurrently. For example a value of 10 means 10 files will be deleted at once to speed up things.", Default = 10)]
    public int ConcurrentDeleteThreadLimit { get; set; } = 10;


    [Option("dry-run", HelpText = "List blobs without deleting.")]
    public bool DryRun { get; set; }
}