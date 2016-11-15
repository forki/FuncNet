namespace FuncNet.Tests

open Xunit
open FuncNet
open System.Net
open System.Net.Http
open Swensen.Unquote

type HttpTests() =
    [<Fact>]
    member __.``get creates a HTTP GET request`` () =
        let request = Http.get "/"

        test <@ request.Method = Http.Get @>

    [<Theory>]
    [<InlineData("/")>]
    [<InlineData("http://www.google.com/")>]
    member __.``get creates a HTTP GET with the specified path`` (path) =
        let request = Http.get path

        test <@ request.Path = path @>

    [<Fact>]
    member __.``delete creates a HTTP DELETE request`` () =
        let request = Http.delete "/"

        test <@ request.Method = Http.Delete @>

    [<Theory>]
    [<InlineData("/")>]
    [<InlineData("http://www.google.com/")>]
    member __.``delete creates a HTTP DELETE with the specified path`` (path) =
        let request = Http.delete path

        test <@ request.Path = path @>
        
    [<Fact>]
    member __.``post creates a HTTP POST request`` () =
        let request = Http.post "/" ""

        test <@ request.Method = Http.Post @>

    [<Theory>]
    [<InlineData("/")>]
    [<InlineData("http://www.google.com/")>]
    member __.``post creates a HTTP GET with the specified path`` (path) =
        let request = Http.post path ""

        test <@ request.Path = path @>
        
    [<Fact>]
    member __.``put creates a HTTP PUT request`` () =
        let request = Http.put "/" ""

        test <@ request.Method = Http.Put @>

    [<Theory>]
    [<InlineData("/")>]
    [<InlineData("http://www.google.com/")>]
    member __.``put creates a HTTP PUT with the specified path`` (path) =
        let request = Http.put path ""

        test <@ request.Path = path @>
        
    [<Fact>]
    member __.``patch creates a HTTP PATCH request`` () =
        let request = Http.patch "/" ""

        test <@ request.Method = Http.Patch @>

    [<Theory>]
    [<InlineData("/")>]
    [<InlineData("http://www.google.com/")>]
    member __.``patch creates a HTTP PATCH with the specified path`` (path) =
        let request = Http.patch path ""

        test <@ request.Path = path @>

    [<Fact>]
    member __.``Request creates a request to the specified URL`` () =
        let url = "http://localhost:8080/"
        let listener = new HttpListener()
        listener.Prefixes.Add(url)
        listener.Start()
        async {
            let! context = listener.GetContextAsync() |> Async.AwaitTask
            let response = context.Response
            response.StatusCode <- 200
            response.ContentLength64 <- 0L
            response.OutputStream.Close()
        } |> Async.Start
        let client = Http.createClient()
        
        let response = Http.get url |> client.Request |> Future.await

        test <@ response.StatusCode = HttpStatusCode.OK @>
