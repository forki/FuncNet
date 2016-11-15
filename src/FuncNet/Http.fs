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
            // HTTP status code
            StatusCode : System.Net.HttpStatusCode
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
    type Client(httpClient : HttpClient) =
        inherit Service<Request, Response>()

        /// Perform a HTTP request
        override __.Request(request : Request) =
            async {
                let httpRequest = new HttpRequestMessage(request.Method.HttpMethod, request.Path)
                match request.Content with
                | Some x -> httpRequest.Content <- x
                | None -> ()
                let! response = httpClient.SendAsync(httpRequest) |> Async.AwaitTask
                return { RawResponse = response; StatusCode = response.StatusCode }
            } |> Future.fromAsync

        /// Close the HTTP service
        override __.Close() =
            httpClient.Dispose()

    /// Create a HTTP client using the specified URL as base address
    let createClientWithBase baseAddress : Service<Request, Response> =
        let client = new HttpClient()
        client.BaseAddress <- new Uri(baseAddress)
        new Client(client) :> Service<Request, Response>

    /// Create a HTTP client using the specified URL as base address
    let createClient() : Service<Request, Response> =
        let client = new HttpClient()
        new Client(client) :> Service<Request, Response>

    /// Create a HTTP request
    let request (m : Method) (path : string) (content : HttpContent option) : Request =
        let msg = { Method = m; Path = path; Content = content }
        msg

    /// Create a HTTP GET request
    let get path = request Get path None
    /// Create a HTTP DELETE request
    let delete path = request Delete path None
    /// Create a HTTP POST request
    let post path content =
        request Post path (Some ((new StringContent(content)) :> HttpContent))
    /// Create a HTTP PUT request
    let put path content =
        request Put path (Some ((new StringContent(content)) :> HttpContent))
    /// Create a HTTP PATCH request
    let patch path content =
        request Patch path (Some ((new StringContent(content)) :> HttpContent))

    /// HTTP classifiers
    [<RequireQualifiedAccessAttribute>]
    module Classifer =
        /// Checks if the HTTP statuscode is a server error, meaning 500 or above
        let isServerError statusCode =
            statusCode >= 500

        /// Checks if the HTTP statuscode is a client error, meaning between 400 and 500
        let isClientError statusCode =
            statusCode >= 400 && statusCode < 500

        /// Generic HTTP status code classifier
        let statusCodeClassifier (failurePredicate : int -> bool ) (service : Service<Request, Response>) request =
            async {
                let! outcome = request |> service.Request
                match outcome with
                | Success x ->
                    let statusCode = x.StatusCode |> int
                    if statusCode |> failurePredicate then return Failure (Classifier.ClassifierException (sprintf "HTTP status code was %i" statusCode))
                    else return Success x
                | Failure e -> return Failure e
            }

        /// Classifier marking the request as a failure if the HTTP status code indicates a server error
        let serverErrorsAsFailure (service : Service<Request, Response>) : Classifier<Request, Response> =
            statusCodeClassifier isServerError service

        /// Classifier marking the request as a failure if the HTTP status code indicates a client error
        let clientErrorsAsFailure (service : Service<Request, Response>) : Classifier<Request, Response> =
            statusCodeClassifier isClientError service
