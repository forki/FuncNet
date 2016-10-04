namespace FuncNet

/// HTTP client and operations
[<RequireQualifiedAccessAttribute>]
module Http =
    open System
    open System.Net.Http

    /// HTTP method
    type Method =
        /// GET
        | Get
        /// POST
        | Post
        /// PUT
        | Put
        /// DELETE
        | Delete
        /// PATCH
        | Patch
        /// Maps the method to a HttpMethod object
        member self.HttpMethod =
            match self with
            | Get -> HttpMethod.Get
            | Post -> HttpMethod.Post
            | Put -> HttpMethod.Put
            | Delete -> HttpMethod.Delete
            | Patch -> new HttpMethod("PATCH")

    /// The response type
    type Response =
        {
            /// The raw response
            RawResponse : HttpResponseMessage
        }
        override self.ToString() =
            self.ContentString
        /// Get the response content as a string
        member self.ContentString =
            self.RawResponse.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously

    /// The request type
    type Request =
        {
            /// Request content, this is optional, and can be any type to HttpContent
            Content : HttpContent option;
            /// The HTTP method
            Method : Method;
            /// The request path
            Path : string
        }

    /// The HTTP client
    type Client =
        {
            /// The HttpClient used to perform HTTP requests
            Client : HttpClient
        }
        /// Perform a HTTP request
        member self.Request(request : Request) =
            async {
                let httpRequest = new HttpRequestMessage(request.Method.HttpMethod, request.Path)
                match request.Content with
                | Some x -> httpRequest.Content <- x
                | None -> ()
                let! response = self.Client.SendAsync(httpRequest) |> Async.AwaitTask
                return { RawResponse = response }
            } |> Future.fromAsync

    /// Create a HTTP client using the specified URL as base address
    let createClient baseAddress : Service<Request, Response> =
        let client = new HttpClient()
        client.BaseAddress <- new Uri(baseAddress)
        { Client = client }.Request

    /// Create a HTTP request
    let request (m : Method) (path : string) (content : HttpContent option) : Request =
        let msg = { Method = m; Path = path; Content = content }
        msg

    /// Create a HTTP GET request
    let get path = request Get path None
    /// Create a HTTP DELETE request
    let delete path = request Delete path None
    /// Create a HTTP POST request
    let post path content = request Post path content
    /// Create a HTTP PUT request
    let put path content = request Put path content
    /// Create a HTTP PATCH request
    let patch path content = request Patch path content