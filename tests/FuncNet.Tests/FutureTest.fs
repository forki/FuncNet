namespace FuncNet.Tests

open Xunit
open FuncNet
open Swensen.Unquote

type FutureTests() =
    [<Fact>]
    member __.``Future converted from async returns success value when no exception occurs`` () =
        let expectedValue = 1
        let a =
            async {
                return expectedValue
            }

        let result = a |> Future.fromAsync |> Async.RunSynchronously

        test
            <@
                match result with
                | Success x -> x = expectedValue
                | _ -> false
            @>

    [<Fact>]
    member __.``Future converted from async returns failure value when exception occurs`` () =
        let expectedValue = 1
        let a =
            async {
                failwith "Test"
                return expectedValue
            }

        let result = a |> Future.fromAsync |> Async.RunSynchronously

        test
            <@
                match result with
                | Failure e -> true
                | _ -> false
            @>

    [<Fact>]
    member __.``Future created from value returns the value``() =
        let expectedValue = 2

        let result = expectedValue |> Future.value |> Async.RunSynchronously

        test
            <@
                match result with
                | Success x -> x = expectedValue
                | _ -> false
            @>
    

    [<Fact>]
    member __.``Future created from exception returns a failure``() =
        let expected = (System.Exception("Test"))
        let result = expected |> Future.exn |> Async.RunSynchronously

        test
            <@
                match result with
                | Failure x -> x = expected
                | _ -> false
            @>

    [<Fact>]
    member __.``Future.flatMap does not map a failure``() =
        let failedFuture = async { return Failure (System.Exception("Test")) }
        let nextFuture = async { return Success true }

        let result =
            async {
                return!
                    failedFuture
                    |> Future.flatMap (fun _ -> nextFuture)
            }
            |> Async.RunSynchronously


        test
            <@
                match result with
                | Failure _ -> true
                | _ -> false
            @>

    [<Fact>]
    member __.``Future.flatMap does maps success``() =
        let failedFuture = async { return Success 1 }
        let nextFuture = async { return Success true }

        let result =
            async {
                return!
                    failedFuture
                    |> Future.flatMap (fun _ -> nextFuture)
            }
            |> Async.RunSynchronously


        test
            <@
                match result with
                | Success _ -> true
                | _ -> false
            @>

    [<Fact>]
    member __.``Future.map does not map a failure``() =
        let failedFuture = async { return Failure (System.Exception("Test")) }

        let result =
            async {
                return!
                    failedFuture
                    |> Future.map (fun _ -> true)
            }
            |> Async.RunSynchronously


        test
            <@
                match result with
                | Failure _ -> true
                | _ -> false
            @>

    [<Fact>]
    member __.``Future.map does maps success``() =
        let failedFuture = async { return Success 1 }

        let result =
            async {
                return!
                    failedFuture
                    |> Future.map (fun _ -> true)
            }
            |> Async.RunSynchronously


        test
            <@
                match result with
                | Success _ -> true
                | _ -> false
            @>

    [<Fact>]
    member __.``Future.rescue does map a failure``() =
        let failedFuture = async { return Failure (System.Exception("Test")) }
        let nextFuture = async { return Success 1 }

        let result =
            async {
                return!
                    failedFuture
                    |> Future.rescue (fun _ -> nextFuture)
            }
            |> Async.RunSynchronously


        test
            <@
                match result with
                | Success _ -> true
                | _ -> false
            @>

    [<Fact>]
    member __.``Future.rescue passes on the success value``() =
        let failedFuture = async { return Success 1 }
        let nextFuture = async { return Success 2 }

        let result =
            async {
                return!
                    failedFuture
                    |> Future.rescue (fun _ -> nextFuture)
            }
            |> Async.RunSynchronously


        test
            <@
                match result with
                | Success x when x = 1 -> true
                | _ -> false
            @>