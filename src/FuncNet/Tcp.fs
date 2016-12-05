namespace FuncNet

/// TCP client and operations
[<RequireQualifiedAccessAttribute>]
module Tcp =
    open System
    open System.Net.Sockets
    open System.Collections.Concurrent
    open System.IO

    /// The request type
    type Request = byte array
    
    /// The request type
    type Response = byte array

    /// Decoder signature
    type Decoder = NetworkStream -> Async<byte array>

    /// Defines the byte order used when decoding, this is handle before sent to the decoder
    type ByteOrder =
        /// Big endian byte order
        | BigEndian
        /// Little endian byte order
        | LittleEndian

    /// Type defining the client configuration
    type ClientConfiguration =
        {
            /// The host name
            Host : string;
            /// The port number
            Port : int;
            /// The decoder used
            Decoder : Decoder;
            /// The byte order used
            ByteOrder : ByteOrder; 
        }

    /// The TCP client
    type Client(config : ClientConfiguration) =
        inherit Service<Request, Response>()
        
        let connectLock = new obj()

        let tcpClient = new TcpClient()

        let ensureClientIsConnected() =
            if not tcpClient.Connected then
                lock connectLock (fun () -> if not tcpClient.Connected then tcpClient.Connect(config.Host, config.Port))

        /// Make a request
        override __.Request(request : Request) =
            ensureClientIsConnected()
            async {
                let s = tcpClient.GetStream()
                let! token = Async.CancellationToken
                do! s.WriteAsync(request, 0, request.Length, token) |> Async.AwaitTask
                let! res = s |> config.Decoder
                return Success res
            }

        /// Closes the client
        override __.Close() =
            tcpClient.Close()

    /// Create configuration. Default byte order is big endian
    let createConfig server port decoder =
        { Host = server; Port = port; Decoder = decoder; ByteOrder = BigEndian }

    /// Set the byte order
    let withByteOrder byteOrder config =
        { config with ByteOrder = byteOrder }

    /// Build client from configuration 
    let buildClient config =
        new Client(config)