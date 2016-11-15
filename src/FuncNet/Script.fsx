#r "System.Net.Http"

#load "Future.fs"
#load "Service.fs"
#load "Filters.fs"
#load "Classifier.fs"
#load "Http.fs"

open FuncNet

// Sample of get without futures
let client = Http.createClientWithBase "http://www.google.dk"
Http.get "/"
|> client.Request
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

// Sample of timeout filter
let timeoutClient =
    Http.createClientWithBase "http://www.google.dk:6666"
    |> TimeoutFilter.create 1000
Http.get "/"
|> timeoutClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

// Sample of retry filter
let retryPolicy =
    RetryFilter.createPolicy (fun o -> match o with | Failure _ -> true | _ -> false)

let retryClient =
    Http.createClientWithBase "http://www.google.dk:6666"
    |> RetryFilter.triesWhile 2 retryPolicy
Http.get "/"
|> retryClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

// Sample of logging filter
let loggingClient =
    Http.createClientWithBase "http://www.google.dk"
    |> LoggingFilter.create (fun x -> printfn "%O" (x); x)
Http.get "/"
|> loggingClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

// Sample of HTTP classifier
let clientErrorClassifierClient =
    Http.createClientWithBase "http://www.google.dk"
    |> Http.Classifer.clientErrorsAsFailure
Http.get "/test"
|> clientErrorClassifierClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

// Sampel of rate limiting
let rateLimitClient =
    Http.createClientWithBase "http://www.google.dk:6666"
    |> RateLimitingFilter.create 1000 1
Http.get "/"
|> rateLimitClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)
Http.get "/"
|> rateLimitClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

/// Sample of drain service
let httpClientWithDrain =
    Http.createClientWithBase "http://www.telenor.dk:6666"
    |> Service.withDrain
let withTimeout =
    httpClientWithDrain
    |> TimeoutFilter.create 10000
Http.get "/"
    |> withTimeout
    |> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)
httpClientWithDrain.Close()