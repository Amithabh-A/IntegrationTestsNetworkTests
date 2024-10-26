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
            // Example of broadcasting a message to all clients
            communicator.Send("Hello to all clients from server", "ServerModule", null);
        }
        else
        {
            Console.WriteLine("Failed to start server.");
        }
    }
}

public class ClientNotificationHandler : INotificationHandler
{
    public void OnDataReceived(string serializedData)
    {
        Console.WriteLine($"Client received: {serializedData}");
    }

    public void OnClientJoined(TcpClient socket)
    {
        // Not used on client side
    }

    public void OnClientLeft(string clientId)
    {
        // Not used on client side
    }
}

public class ServerNotificationHandler : INotificationHandler
{
    public void OnDataReceived(string serializedData)
    {
        Console.WriteLine($"Server received: {serializedData}");
    }

    public void OnClientJoined(TcpClient socket)
    {
        Console.WriteLine("A new client has joined.");
    }

    public void OnClientLeft(string clientId)
    {
        Console.WriteLine($"Client {clientId} has left.");
    }
}
