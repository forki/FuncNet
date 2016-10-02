#r "System.Net.Http"

#load "Service.fs"
#load "HttpService.fs"

open FuncNet
open System.Net.Http

let client = Http.createClient "http://www.google.dk"
async {
    let! response = Http.get "/" |> client
    printfn "%O" response
} |> Async.RunSynchronously