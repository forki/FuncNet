namespace FuncNet

open System

type Outcome<'a> =
    | Success of 'a
    | Failure of Exception

type Future<'a> = Async<Outcome<'a>>

[<RequireQualifiedAccessAttribute>]
module Future =
    let fromAsync (computation : Async<'a>) : Future<'a> =
        async {
            let! choice = (Async.Catch computation)
            let outcome =
                match choice with
                | Choice1Of2 x -> Success x
                | Choice2Of2 e -> Failure e
            return outcome
        }

    let value x =
        fromAsync (async { return x })

    let flatMap (f : 'a -> Future<'b>) (future : Future<'a>) : Future<'b> =
        async {
            let! outcome = future
            match outcome with
            | Success value -> return! f value
            | Failure e -> return Failure e
        }

    let map (f : 'a -> 'b) : Future<'a> -> Future<'b> =
        let f' value = fromAsync (async { return f value })
        flatMap f'

    let rescue (f : exn -> Future<'a>) (future : Future<'a>) : Future<'a> =
        async {
            let! outcome = future
            match outcome with
            | Success x -> return Success x
            | Failure e -> return! f e
        }

    let bind (f : 'a -> Future<'b>) (g : 'b -> Future<'c>) : 'a -> Future<'c> =
        f >> (flatMap g)

    let chain (futures : ('a -> Future<'a>) list) : 'a -> Future<'a> =
        futures
        |> List.fold bind value

    let onComplete (onSuccess : 'a -> unit) (onFailure : exn -> unit) (future : Future<'a>) : unit =
        async {
            let! outcome = future
            match outcome with
            | Success x -> onSuccess x
            | Failure e -> onFailure e
        } |> Async.Start

    let onSuccess (f : 'a -> unit) (future : Future<'a>) : unit =
        async {
            let! outcome = future
            match outcome with
            | Success x -> f x
            | Failure _ -> ()
        } |> Async.Start

    let onFailure (f : exn -> unit) (future : Future<'a>) : unit =
        async {
            let! outcome = future
            match outcome with
            | Success _ -> ()
            | Failure e -> f e
        } |> Async.Start

    let await (future : Future<'a>) : 'a =
        async {
            let! outcome = future
            match outcome with
            | Success x -> return x
            | Failure e -> return raise e
        } |> Async.RunSynchronously

module Operators =
    let (->>) = Future.bind
    let (-->) f g = f >> (Future.map g)
    let (--|) f g = f >> (Future.rescue g)