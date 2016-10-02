#r "System.Net.Http"

#load "Service.fs"
#load "HttpService.fs"
#load "Future.fs"

open FuncNet

// Sample of get without futures
let client = Http.createClient "http://www.google.dk"
async {
    let! response = Http.get "/" |> client
    printfn "%O" response
} |> Async.RunSynchronously
