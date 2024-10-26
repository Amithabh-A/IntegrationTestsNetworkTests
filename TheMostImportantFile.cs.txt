using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Networking;
using Networking.Communication;
using System.Security.Cryptography;
using System.Text.Json;
using Networking.Serialization;

class Program
{
    static void Main(string[] args)
    {


        if (args.Length < 3 && args[0] == "client" || args.Length < 1 && args[0] == "server")
        {
            Console.WriteLine("Usage: dotnet run <client|server> <host> <port>");
            return;
        }

        string mode = args[0];

        ICommunicator communicator = CommunicationFactory.GetCommunicator(mode == "client");

        if (mode == "client")
        {
            Console.WriteLine("Client mode");
            string host = args[1];
            string port = args[2];
            RunClient(communicator, host, port);
        }
        else if (mode == "server")
        {
            RunServer(communicator);
        }
        else
        {
            Console.WriteLine("Invalid mode. Use 'client' or 'server'.");
        }
    }

    static void RunClient(ICommunicator communicator, string host, string port)
    {
        Console.WriteLine("Going to start client");
        string result = communicator.Start(host, port);
        if (result == "success")
        {
            Console.WriteLine("Client connected to server.");
            // Example of sending a message with null as the destination
            communicator.Send("Hello from client", "ExampleModule", null);
        }
        else
        {
            Console.WriteLine("Failed to connect to server.");
        }
    }

    static void RunServer(ICommunicator communicator)
    {
        string result = communicator.Start();
        if (result != "failure")
        {
            Console.WriteLine($"Server started at {result}");
            // Server logic here
        }
        else
        {
            Console.WriteLine("Failed to start server.");
        }
    }
}

public class DirectoryMetadataGenerator
{
    private List<FileMetadata>? _metadata;
    // static void Main(string[] args)
    // {
    //     if (args.Length != 1)
    //     {
    //         Console.WriteLine("Usage: dotnet run <directory-path>");
    //         return;
    //     }

    //     string directoryPath = args[0];

    //     if (!Directory.Exists(directoryPath))
    //     {
    //         Console.WriteLine($"Directory does not exist: {directoryPath}");
    //         return;
    //     }

    //     CreateMetadataFile(directoryPath);
    // }

    /// <summary>
    /// Creates a metadata file in the specified directory containing the hash of each file in the directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory</param>
    /// <param name="writeToFile">Whether to write the metadata to a file.</param>
    /// <returns>List of FileMetadata objects containing the file name and hash of each file in the directory.</returns>
    public DirectoryMetadataGenerator(string directoryPath, bool writeToFile = false)
    {
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Directory does not exist: {directoryPath}");
        }

        List<FileMetadata> metadata = CreateFileMetadata(directoryPath, writeToFile);
        _metadata = metadata;
    }


    /// <summary>
    /// Gets the metadata of the files in the directory.
    /// </summary>
    /// <returns>List of FileMetadata objects. </returns>
    public List<FileMetadata>? GetMetadata()
    {
        return _metadata;
    }

    /// <summary>
    /// Creates a metadata file in the specified directory containing the hash of each file in the directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the files for which metadata is to be created.</param>
    /// <param name="writeToFile">Whether to write the metadata to a file.</param>
    /// <returns>List of FileMetadata objects containing the file name and hash of each file in the directory.</returns>
    private static List<FileMetadata> CreateFileMetadata(string directoryPath, bool writeToFile = false)
    {
        List<FileMetadata> metadata = new List<FileMetadata>();
        string metadataFilePath = Path.Combine(directoryPath, "metadata.json");

        foreach (string filePath in Directory.GetFiles(directoryPath))
        {
            // Skip the metadata file itself
            if (Path.GetFileName(filePath).Equals("metadata.json", StringComparison.OrdinalIgnoreCase))
                continue;

            string fileHash = ComputeFileHash(filePath);
            metadata.Add(new FileMetadata
            {
                FileName = Path.GetFileName(filePath),
                FileHash = fileHash
            });
        }

        // Write (or overwrite) the metadata.json file
        if (writeToFile)
        {
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(metadata, options));
            Console.WriteLine($"Metadata file created/overwritten at: {metadataFilePath}");
        }

        return metadata;
    }

    /// <summary>
    /// Computes the SHA-256 hash of the file residing at the specified path.
    /// </summary>
    /// <param name="filePath">Path of file</param>
    /// <returns>SHA-256 hash of the file</returns>
    private static string ComputeFileHash(string filePath)
    {
        using SHA256 sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(filePath);
        Byte[] hashBytes = sha256.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

public class DirectoryMetadataComparer
{
    private Dictionary<int, List<object>>? _differences;
    // static void Main(string[] args)
    // {
    //     if (!ValidateArguments(args))
    //     {
    //         return;
    //     }

    //     string metadataFilePathA = args[0];
    //     string metadataFilePathB = args[1];

    //     if (!FilesExist(metadataFilePathA, metadataFilePathB))
    //     {
    //         return;
    //     }

    //     List<FileMetadata> metadataA = LoadMetadata(metadataFilePathA);
    //     List<FileMetadata> metadataB = LoadMetadata(metadataFilePathB);

    //     Dictionary<int, List<object>> differences = CompareMetadata(metadataA, metadataB);
    //     WriteDifferencesToFile(differences, "differences.json");
    // }


    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryMetadataComparer"/> class.
    /// </summary>
    /// <param name="metadataFilePathA">Path to the metadata file for directory A.</param>
    /// <param name="metadataFilePathB">Path to the metadata file for directory B.</param>
    /// <returns> A new instance of the <see cref="DirectoryMetadataComparer"/> class.</returns>
    public DirectoryMetadataComparer(string metadataFilePathA, string metadataFilePathB)
    {

        if (!FilesExist(metadataFilePathA, metadataFilePathB))
        {
            return;
        }

        List<FileMetadata> metadataA = LoadMetadata(metadataFilePathA);
        List<FileMetadata> metadataB = LoadMetadata(metadataFilePathB);

        Dictionary<int, List<object>> differences = CompareMetadata(metadataA, metadataB);
        WriteDifferencesToFile(differences, "differences.json");
    }


    /// <summary>
    /// Gets the differences between the metadata of two directories.
    /// </summary>
    /// <returns>A dictionary containing the differences between the metadata of two directories.</returns>
    public Dictionary<int, List<object>>? GetDifferences()
    {
        return _differences;
    }


    /// <summary>
    /// Checks whether necessary arguments are provided. 
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>True if the arguments are valid; otherwise, false.</returns>
    private static bool ValidateArguments(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: dotnet run <path-to-metadata.json-fA> <path-to-metadata.json-fB>");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Checks whether the files exist.
    /// </summary>
    /// <param name="pathA">Path of the first file.</param>
    /// <param name="pathB">Path of the second file.</param>
    /// <returns>True if the files exist; otherwise, false.</returns>
    private static bool FilesExist(string pathA, string pathB)
    {
        if (!File.Exists(pathA))
        {
            Console.WriteLine($"Metadata file does not exist: {pathA}");
            return false;
        }

        if (!File.Exists(pathB))
        {
            Console.WriteLine($"Metadata file does not exist: {pathB}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Loads metadata from specified metadatafile
    /// </summary>
    /// <param name="filePath">Path to the metadata file.</param>
    /// <returns>List of FileMetadata objects.</returns>
    private static List<FileMetadata> LoadMetadata(string filePath)
    {
        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        // if fileStream is null, metadata will be null. 
        List<FileMetadata> metadata = JsonSerializer.Deserialize<List<FileMetadata>>(fileStream);
        return metadata ?? new List<FileMetadata>();
    }

    /// <summary>
    /// Compares metadata from two <see cref="List<FileMetadata>"/> objects and returns the differences.
    /// </summary>
    /// <param name="metadataA">The first list of metadata.</param>
    /// <param name="metadataB">The second list of metadata.</param>
    /// <returns>A dictionary containing the differences between the two lists of metadata.</returns>
    private static Dictionary<int, List<object>> CompareMetadata(List<FileMetadata> metadataA, List<FileMetadata> metadataB)
    {
        Dictionary<int, List<object>> differences = new Dictionary<int, List<object>>
        {
            { -1, new List<object>() }, // In B but not in A
            { 0, new List<object>() },  // Files with same hash but different names
            { 1, new List<object>() }    // In A but not in B
        };

        Dictionary<string, string> hashToFileA = CreateHashToFileDictionary(metadataA);
        Dictionary<string, string> hashToFileB = CreateHashToFileDictionary(metadataB);

        CheckForRenamesAndMissingFiles(metadataB, hashToFileA, differences);
        CheckForOnlyInAFiles(metadataA, hashToFileB, differences);

        return differences;
    }

    /// <summary>
    /// Creates a dictionary that maps file hashes to file names.
    /// </summary>
    /// <param name="metadata">The list of metadata.</param>
    /// <returns>A dictionary that maps file hashes to file names.</returns>
    private static Dictionary<string, string> CreateHashToFileDictionary(List<FileMetadata> metadata)
    {
        var hashToFile = new Dictionary<string, string>();
        foreach (var file in metadata)
        {
            hashToFile[file.FileHash] = file.FileName;
        }
        return hashToFile;
    }

    /// <summary>
    /// Checks for files in directory B that have been renamed or missing in directory A.
    /// </summary>
    /// <param name="metadataB">The list of metadata for directory B.</param>
    /// <param name="hashToFileA">Dictionary that maps file hashes to file names in directory A.</param>
    /// <param name="differences">The dictionary that stores the differences.</param>
    /// <returns> The dictionary that stores the differences.</returns>
    private static void CheckForRenamesAndMissingFiles(List<FileMetadata> metadataB, Dictionary<string, string> hashToFileA, Dictionary<int, List<object>> differences)
    {
        foreach (FileMetadata fileB in metadataB)
        {
            if (hashToFileA.ContainsKey(fileB.FileHash))
            {
                if (hashToFileA[fileB.FileHash] != fileB.FileName)
                {
                    differences[0].Add(new Dictionary<string, string>
                    {
                        { "RenameFrom", fileB.FileName },
                        { "RenameTo", hashToFileA[fileB.FileHash] },
                        { "FileHash", fileB.FileHash }
                    });
                }
            }
            else
            {
                differences[-1].Add(new Dictionary<string, string>
                {
                    { "FileName", fileB.FileName },
                    { "FileHash", fileB.FileHash }
                });
            }
        }
    }

    /// <summary>
    /// Checks for files in directory A that are missing in directory B.
    /// </summary>
    /// <param name="metadataA">The list of metadata for directory A.</param>
    /// <param name="hashToFileB">Dictionary that maps file hashes to file names in directory B.</param>
    /// <param name="differences">The dictionary that stores the differences.</param>
    /// <returns> The dictionary that stores the differences.</returns>
    private static void CheckForOnlyInAFiles(List<FileMetadata> metadataA, Dictionary<string, string> hashToFileB, Dictionary<int, List<object>> differences)
    {
        foreach (FileMetadata fileA in metadataA)
        {
            if (!hashToFileB.ContainsKey(fileA.FileHash))
            {
                differences[1].Add(new Dictionary<string, string>
                {
                    { "FileName", fileA.FileName },
                    { "FileHash", fileA.FileHash }
                });
            }
        }
    }

    /// <summary>
    /// Writes the differences dictionary to json file.
    /// </summary>
    /// <param name="differences">The dictionary containing the differences.</param>
    private static void WriteDifferencesToFile(Dictionary<int, List<object>> differences, string outputFilePath)
    {
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(outputFilePath, JsonSerializer.Serialize(differences, options));
        Console.WriteLine($"Differences written to: {outputFilePath}");
    }
}

/// <summary>
/// Represents metadata for a file, including its name and hash.
/// </summary>
public class FileMetadata
{
    public string FileName { get; set; }
    public string FileHash { get; set; }
}

public class Utils
{
    // please check whether filePath is valid first. 
    static string ReadFileContent(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    // please check whether everything is valid. 
    static bool WriteContentToFile(string content, string outputFilePath)
    {
        try
        {
            File.WriteAllText(outputFilePath, content);
            return true;
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"An error occurred while writing to file: {ex.Message}");
            return false;
        }
    }

    /*
     * Serialization of objects are done in NetworkSerialization class. 
     *
     */
}

