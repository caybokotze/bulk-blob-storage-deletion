# Blob storage bulk deletion tool

### Uses
#### CLI
Use the CLI to provide some options. This allows for more advanced settings like the amount of files which are deleted concurrently.

```shell
  -c, --connection-string    Azure Blob Storage connection string.
  -n, --container            Blob container name.
  --thread-count-limit       (Default: 10) Set the upper thread limit to delete files concurrently. For example a value
                             of 10 means 10 files will be deleted at once to speed up things.
  --dry-run                  List blobs without deleting.
  --help                     Display this help screen.
  --version                  Display version information.
```

#### Just run the Exe
Just run the `BlobStorageBulkDeletion.exe` file and follow the prompts to select what you want to delete.