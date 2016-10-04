#r "System.Net.Http"

#load "Future.fs"
#load "Service.fs"
#load "Filters.fs"
#load "HttpService.fs"

open FuncNet

// Sample of get without futures
let client = Http.createClient "http://www.google.dk"
Http.get "/"
|> client
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

// Sample of timeout filter
let timeoutClient =
    Http.createClient "http://www.google.dk:6666"
    |> TimeoutFilter.create 1000
Http.get "/"
|> timeoutClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

// Sample of retry filter
let retryClient =
    Http.createClient "http://www.google.dk:6666"
    |> RetryFilter.triesWhile 2 (fun o -> match o with | Failure _ -> true | _ -> false)
Http.get "/"
|> retryClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)

// Sample of logging filter
let loggingClient =
    Http.createClient "http://www.google.dk"
    |> LoggingFilter.create (fun x -> printfn "%O" (x); x)
Http.get "/"
|> loggingClient
|> Future.onComplete (fun x -> printfn "%O" x) (fun x -> printfn "Failure: %O" x)