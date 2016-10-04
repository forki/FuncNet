namespace FuncNet

[<RequireQualifiedAccessAttribute>]
module Http =
    open System
    open System.Net.Http

    type Method =
        | Get
        | Post
        | Put
        | Delete
        | Patch
        member self.HttpMethod =
            match self with
            | Get -> HttpMethod.Get
            | Post -> HttpMethod.Post
            | Put -> HttpMethod.Put
            | Delete -> HttpMethod.Delete
            | Patch -> new HttpMethod("PATCH")

    type Response =
        { RawResponse : HttpResponseMessage }
        override self.ToString() =
            self.ContentString
        member self.ContentString =
            self.RawResponse.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously

    type Request =
        {
            Content : HttpContent option;
            Method : Method;
            Path : string
        }

    type Client =
        { Client : HttpClient }
        member self.Request(request : Request) =
            async {
                let httpRequest = new HttpRequestMessage(request.Method.HttpMethod, request.Path)
                match request.Content with
                | Some x -> httpRequest.Content <- x
                | None -> ()
                let! response = self.Client.SendAsync(httpRequest) |> Async.AwaitTask
                return { RawResponse = response }
            } |> Future.fromAsync

    let createClient baseAddress : Service<Request, Response> =
        let client = new HttpClient()
        client.BaseAddress <- new Uri(baseAddress)
        { Client = client }.Request

    let request (m : Method) (path : string) (content : HttpContent option) : Request =
        let msg = { Method = m; Path = path; Content = content }
        msg

    let get path = request Get path None
    let delete path = request Delete path None
    let post path content = request Post path content
    let put path content = request Put path content
    let patch path content = request Patch path content