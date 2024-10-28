using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Networking;
using Networking.Communication;
using System.Security.Cryptography;
using System.Text.Json;
using Networking.Serialization;
using System.Net.Sockets;
using Updater;
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
            communicator.Subscribe("ClientModule", new ClientNotificationHandler());
            communicator.Send("Hello from client", "ClientModule", null);
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
            communicator.Subscribe("ServerModule", new ServerNotificationHandler());
            communicator.Send("Hello to all clients from server", "ServerModule", null);
        }
        else
        {
            Console.WriteLine("Failed to start server.");
        }
    }
}

public abstract class NotificationHandler : INotificationHandler
{
    public static string _directoryPath = @"C:\Temp";
    public void OnDataReceived(string serializedData)
    {
        PacketDemultiplexer(serializedData);
    }

    public abstract void OnClientJoined(TcpClient socket);
    public abstract void OnClientLeft(string clientId);

    public static void PacketDemultiplexer(string serializedData)
    {
        // Deserialize data
        DataPacket dataPacket = Utils.DeserializeObject<DataPacket>(serializedData);

        // Check PacketType
        switch (dataPacket.GetPacketType())
        {
            case DataPacket.PacketType.Metadata:
                MetadataHandler(dataPacket);
                break;
            case DataPacket.PacketType.Broadcast:
                BroadcastHandler(dataPacket);
                break;
            case DataPacket.PacketType.ClientFiles:
                ClientFilesHandler(dataPacket);
                break;
            case DataPacket.PacketType.Differences:
                DifferencesHandler(dataPacket);
                break;
            default:
                throw new Exception("Invalid PacketType");
        }
    }

    private static void MetadataHandler(DataPacket dataPacket)
    {
        throw new NotImplementedException();
    }

    private static void BroadcastHandler(DataPacket dataPacket)
    {
        throw new NotImplementedException();
    }

    private static void ClientFilesHandler(DataPacket dataPacket)
    {
        throw new NotImplementedException();
    }

    private static void DifferencesHandler(DataPacket dataPacket)
    {
        throw new NotImplementedException();
    }
}

public class ClientNotificationHandler : NotificationHandler
{
    public override void OnClientJoined(TcpClient socket)
    {
        // Not used on client side
    }

    public override void OnClientLeft(string clientId)
    {
        // Not used on client side
    }

    public static void BroadcastHandler(DataPacket dataPacket)
    {
        // Console.WriteLine("Broadcast packet received.");
    }

    public static void DifferencesHandler(DataPacket dataPacket)
    {
        // file list
        List<FileContent> fileContentList = dataPacket.GetFileContentList();

        // difference file
        FileContent differenceFile = fileContentList[0];

        // deserialize fileContent
        string? serializedDifferences = differenceFile.SerializedContent;
        if (serializedDifferences == null)
        {
            throw new Exception("SerializedContent is null");
        }
        DirectoryMetadataComparer differenceInJson = Utils.DeserializeObject<DirectoryMetadataComparer>(serializedDifferences);

        // get files
        foreach (FileContent fileContent in fileContentList)
        {
            if (fileContent == differenceFile)
            {
                continue;
            }
            string content = Utils.DeserializeObject<string>(fileContent.SerializedContent);
            string filePath = Path.Combine(@"C:\Temp", fileContent.FileName);
            bool status = Utils.WriteToFile(filePath, content);
            if (!status)
            {
                throw new Exception("Failed to write file");
            }
        }

        // Now get client files and send to server.  
        // get filename
        List<string> filenameList = differenceInJson.GetUniqueClientFiles();

        // create list of FileContent
        List<FileContent> fileContentToSend = new List<FileContent>();
        // Adding difference file
        fileContentToSend.Add(dataPacket.GetFileContentList()[0]);

        foreach (string filename in filenameList)
        {
            string filePath = Path.Combine(_directoryPath, filename);
            string? content = Utils.ReadFile(filePath);

            // serialize content and create FileContent string? content = Utils.ReadFile(filePath);
            if (content == null)
            {
                throw new Exception("Failed to read file");
            }

            string? serializedContent = Utils.SerializeObject(content);
            if (serializedContent == null)
            {
                throw new Exception("Failed to serialize content");
            }
            FileContent fileContent = new FileContent(filename, serializedContent);
            fileContentToSend.Add(fileContent);
        }

        // create DataPacket
        DataPacket dataPacketToSend = new DataPacket(DataPacket.PacketType.ClientFiles, fileContentToSend);

        // serialize dataPacket
        string? serializedDataPacket = Utils.SerializeObject(dataPacketToSend);

        // Send NOTE
    }
}

public class ServerNotificationHandler : NotificationHandler
{
    public override void OnClientJoined(TcpClient socket)
    {
        // Console.WriteLine("A new client has joined.");
    }

    public override void OnClientLeft(string clientId)
    {
        // Console.WriteLine($"Client {clientId} has left.");
    }

    private static void MetadataHandler(DataPacket dataPacket)
    {

        // get metadata of client directory
        List<FileContent> fileContents = dataPacket.GetFileContentList();
        FileContent fileContent = fileContents[0];
        string? serializedContent = fileContent.SerializedContent;
        if (serializedContent == null)
        {
            throw new Exception("Serialized content is null");
        }
        List<FileMetadata>? metadataClient = Utils.DeserializeObject<List<FileMetadata>>(serializedContent);

        // generate metadata of server
        List<FileMetadata>? metadataServer = new DirectoryMetadataGenerator().GetMetadata();
        if (metadataServer == null)
        {
            throw new Exception("Metadata server is null");
        }

        // compare metadata
        DirectoryMetadataComparer comparerInstance = new DirectoryMetadataComparer(metadataServer, metadataClient);
        Dictionary<int, List<object>>? differences = comparerInstance.GetDifferences();


        // List<FileContent>
        List<FileContent> fileContentsToSend = new List<FileContent>();

        // comparer object
        string serializedDifferences = Utils.SerializeObject(comparerInstance);
        FileContent differenceFile = new FileContent("differences.json", serializedDifferences);
        fileContentsToSend.Add(differenceFile);

        // Get filename list
        List<string> filenameList = comparerInstance.GetUniqueServerFiles();
        foreach (string filename in filenameList)
        {
            string filePath = Path.Combine(_directoryPath, filename);
            string? content = Utils.ReadFile(filePath);
            if (content == null)
            {
                throw new Exception("Content is null");
            }

            // serialize
            serializedContent = Utils.SerializeObject(content);

            // create FileContent object
            FileContent fileContentToSend = new FileContent(filename, serializedContent);
            fileContentsToSend.Add(fileContentToSend);
        }


        // create datapacket
        DataPacket dataPacketToSend = new DataPacket(DataPacket.PacketType.Differences, fileContentsToSend);

        // serialize datapacket
        string serializedDataPacket = Utils.SerializeObject(dataPacketToSend);

        // Send  NOTE
    }

    private static void ClientFilesHandler(DataPacket dataPacket)
    {
        // file list
        List<FileContent> fileContentList = dataPacket.GetFileContentList();

        // difference file
        FileContent differenceFile = fileContentList[0];

        // deserialize fileContent
        string? serializedDifferences = differenceFile.SerializedContent;
        if (serializedDifferences == null)
        {
            throw new Exception("SerializedContent is null");
        }
        DirectoryMetadataComparer differenceInJson = Utils.DeserializeObject<DirectoryMetadataComparer>(serializedDifferences);

        // get files
        foreach (FileContent fileContent in fileContentList)
        {
            if (fileContent == differenceFile)
            {
                continue;
            }
            string content = Utils.DeserializeObject<string>(fileContent.SerializedContent);
            string filePath = Path.Combine(@"C:\Temp", fileContent.FileName);
            bool status = Utils.WriteToFile(filePath, content);
            if (!status)
            {
                throw new Exception("Failed to write file");
            }
        }

        // broadcast client's new files to all clients: 
        dataPacket.SetPacketType(DataPacket.PacketType.Broadcast);

        // serialize packet 
        string serializedPacket = Utils.SerializeObject(dataPacket);

        // SendAll

    }

}

