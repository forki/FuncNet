#r "System.Net.Http"

#load "Future.fs"
#load "Service.fs"
#load "Filter.fs"
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