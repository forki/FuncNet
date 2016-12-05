namespace FuncNet.Tests

open Xunit
open FuncNet
open System
open System.Net
open System.Net.Http
open Swensen.Unquote
open System.Net.Sockets
open System.Threading

type TcpFixture() =
    let listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 1234)

    do
        listener.Start()

    member __.TcpServer = listener

    interface IDisposable with
        member __.Dispose() = listener.Server.Dispose() 

type TcpTests(fixture : TcpFixture) =
    interface IClassFixture<TcpFixture>

    [<Fact>]
    member __.``Making a request sends the specified data to the TCP endpoint``() =
        let dataReceived = new ManualResetEvent(false)
        let expectedData = [| 0uy; 0uy; 1uy;|]
        let buffer = Array.zeroCreate 3
        let client = Tcp.createConfig "localhost" 1234 (fun s -> async { s.Read(buffer, 0, buffer.Length) |> ignore; return buffer }) |> Tcp.buildClient
        
        async {
            use! c = fixture.TcpServer.AcceptTcpClientAsync() |> Async.AwaitTask
            use stream = c.GetStream()
            let rec readLoop() =
                if stream.Read(buffer, 0, buffer.Length) = 0 then
                    readLoop()
                else dataReceived.Set() |> ignore
            readLoop()
            c.Close()
            return ()
        } |> Async.Start
         
        client.Request expectedData
        |> Future.await
        |> ignore

        test <@ dataReceived.WaitOne(3000) @>
        test <@ buffer |> Seq.compareWith Operators.compare expectedData = 0 @>

    [<Fact>]
    member __.``Client receives response`` () =
        let expectedData = [| 0uy; 0uy; 1uy;|]
        let buffer = Array.zeroCreate 3
        let client = Tcp.createConfig "localhost" 1234 (fun s -> async { s.Read(buffer, 0, buffer.Length) |> ignore; return buffer }) |> Tcp.buildClient

        async {
            use! c = fixture.TcpServer.AcceptTcpClientAsync() |> Async.AwaitTask
            use stream = c.GetStream()
            let rec readLoop() =
                if stream.Read(buffer, 0, buffer.Length) = 0 then readLoop()
            readLoop()
            stream.Write(buffer, 0, buffer.Length)
            stream.Flush()
            c.Close()
            return ()
        } |> Async.Start

        let res =
            client.Request expectedData
            |> Future.await

        test <@ res |> Seq.compareWith Operators.compare expectedData = 0 @>