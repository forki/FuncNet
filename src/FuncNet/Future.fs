namespace FuncNet

open System

/// Outcome of a future
type Outcome<'a> =
    | Success of 'a
    | Failure of Exception

/// Future definition
type Future<'a> = Async<Outcome<'a>>

/// Future operations
[<RequireQualifiedAccessAttribute>]
module Future =
    open System.Threading
    open System.Threading.Tasks

    /// Converts an async computation to a future
    let fromAsync (computation : Async<'a>) : Future<'a> =
        async {
            let! choice = (Async.Catch computation)
            let outcome =
                match choice with
                | Choice1Of2 x -> Success x
                | Choice2Of2 e -> Failure e
            return outcome
        }

    /// Creates a future from a value
    let value x =
        fromAsync (async { return x })

    /// Creates a future from an exception
    let exn x =
        fromAsync (async { raise x })

    /// Map a success value of a future to another future
    let flatMap (f : 'a -> Future<'b>) (future : Future<'a>) : Future<'b> =
        async {
            let! outcome = future
            match outcome with
            | Success value -> return! f value
            | Failure e -> return Failure e
        }

    /// Map a success value of a future to another future, using a function not returning a future
    let map (f : 'a -> 'b) : Future<'a> -> Future<'b> =
        let f' value = fromAsync (async { return f value })
        flatMap f'

    /// Maps a failure value of a future to another future
    let rescue (f : exn -> Future<'a>) (future : Future<'a>) : Future<'a> =
        async {
            let! outcome = future
            match outcome with
            | Success x -> return Success x
            | Failure e -> return! f e
        }

    /// Combine two functions returning futures
    let bind (f : 'a -> Future<'b>) (g : 'b -> Future<'c>) : 'a -> Future<'c> =
        f >> (flatMap g)

    /// Combines multiple functins returning futures
    let collect (futures : ('a -> Future<'a>) list) : 'a -> Future<'a> =
        futures
        |> List.fold bind value

    /// Define functions to call on completion of the future. One for success, and one for failure
    let onComplete (onSuccess : 'a -> unit) (onFailure : exn -> unit) (future : Future<'a>) : unit =
        let cts = new CancellationTokenSource()
        let a = async {
            let! outcome = future
            match outcome with
            | Success x -> onSuccess x
            | Failure e -> onFailure e
        }
        Async.Start(a, cts.Token)

    /// Define function to be called on successful completion of the future
    let onSuccess (f : 'a -> unit) (future : Future<'a>) : unit =
        let cts = new CancellationTokenSource()
        let a = async {
            let! outcome = future
            match outcome with
            | Success x -> f x
            | Failure _ -> ()
        }
        Async.Start(a, cts.Token)

    /// Define function to be called on failed completion of the future
    let onFailure (f : exn -> unit) (future : Future<'a>) : unit =
        let cts = new CancellationTokenSource()
        let a = async {
            let! outcome = future
            match outcome with
            | Success _ -> ()
            | Failure e -> f e
        }
        Async.Start(a, cts.Token)

    /// Synchronously wait for the future to complete
    let await (future : Future<'a>) : 'a =
        async {
            let! outcome = future
            match outcome with
            | Success x -> return x
            | Failure e -> return raise e
        } |> Async.RunSynchronously

    /// Execute a future with a timeout, if the future is not completed within the timeout
    /// the future fails with a TimeoutException
    let withIn (timeoutMs : int) (future : Future<'a>) : Future<'a> =
        async {
            let mutable outcome = None
            use cts = new CancellationTokenSource(timeoutMs)
            let task = Async.StartAsTask(future, TaskCreationOptions.None, cts.Token).ContinueWith(fun (t : Task<Outcome<'a>>) ->
                if t.IsFaulted then
                    outcome <- Some <| Failure t.Exception
                elif t.IsCanceled then
                    outcome <- Some <| Failure (TimeoutException())
                else
                    outcome <- Some <| t.Result)
            task.Wait()
            match outcome with
            | Some x -> return x
            | None -> return Failure <| ((InvalidOperationException("Outcome not assigned, this should not happen")) :> Exception)
        }

/// Future operators
module Operators =
    /// Future bind operator
    let (->>) = Future.bind
    
    /// Future map operator
    let (-->) f g = f >> (Future.map g)
    
    /// Future rescue operator
    let (--|) f g = f >> (Future.rescue g)