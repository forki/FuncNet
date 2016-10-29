﻿namespace FuncNet

/// Timeout filter. Fails the request if not completed within the specified time, this also
/// cancels the operation
[<RequireQualifiedAccessAttribute>]
module TimeoutFilter =
    /// Creates a timeout filter, with the specified timeout
    let create (timeoutMs : int) (service : Service<'a, 'b>) : Service<'a, 'b> =
        service >> Future.withIn timeoutMs

/// Retry filter. Allows implementing retry logic, both for successful requests and for failed requests
[<RequireQualifiedAccessAttribute>]
module RetryFilter =
    // Retry policy
    type RetryPolicy<'a> =
        {
            /// Predicate determning if it should retry
            Predicate : Outcome<'a> -> bool;
            /// Retry backoff in ms
            Backoff : int option;
        }

    /// Create a retry policy
    let createPolicy predicate =
        { Predicate = predicate; Backoff = None }

    /// Create a retry policy with backoff ms
    let createPolicyWithBackoff backoffMs policy =
        { policy with Backoff = backoffMs }

    /// Creats a new retry filter, using the specifed retry policies
    let create (policies : RetryPolicy<'b> seq) (service : Service<'a, 'b>) : Service<'a, 'b> =
        let rec retry (policies : RetryPolicy<'b> seq) request =
            async {
                let! outcome = service request
                if policies |> Seq.isEmpty then
                    return outcome
                else
                    let policy = Seq.head policies
                    if policy.Predicate outcome then
                        match policy.Backoff with
                        | Some x -> do! Async.Sleep x
                        | None -> ()
                        return! retry (policies |> Seq.tail) request
                    else return outcome
            }
        retry policies

    /// Create a retry filter, retrying as long as the predicate is statisfied
    let doWhile predicate service : Service<'a, 'b> =
        let rec policies = seq {
            yield predicate
            yield! policies
        }
        create policies service

    /// Creates a retry filter, retrying as long as the predicate is statisfied, but maximum the number of
    /// times specified
    let triesWhile times predicate service : Service<'a, 'b> =
        let rec policies = seq {
            for _ in 1 .. times do
                yield predicate
        }
        create policies service

/// Retry filter, only retrying on failed requests.
[<RequireQualifiedAccessAttribute>]
module RetryExceptionsFilter =
    // Retry policy
    type RetryPolicy =
        {
            /// Predicate determning if it should retry
            Predicate : exn -> bool;
            /// Retry backoff in ms
            Backoff : int option;
        }

    /// Create a retry policy
    let createPolicy predicate =
        { Predicate = predicate; Backoff = None }

    /// Create a retry policy with backoff ms
    let createPolicyWithBackoff backoffMs policy =
        { policy with Backoff = backoffMs }
    
    /// Creats a new retry filter, using the specifed retry policies
    let create (policies : RetryPolicy seq) (service : Service<'a, 'b>) : Service<'a, 'b> =
        let rec retry (policies : RetryPolicy seq) request =
            async {
                let! outcome = service request
                match outcome with
                | Success x -> return Success x
                | Failure e ->
                    if policies |> Seq.isEmpty then
                        return Failure e
                    else
                        let policy = Seq.head policies
                        if policy.Predicate e then
                            match policy.Backoff with
                            | Some x -> do! Async.Sleep x
                            | None -> ()
                            return! retry (policies |> Seq.tail) request
                        else return Failure e
            }
        retry policies

    /// Create a retry filter, retrying at most the specified number of times
    let tries times service : Service<'a, 'b> =
        let retry _ = true
        let policies = seq {
            for _ in 1 .. times -> createPolicy retry
        }
        create policies service

    /// Create a retry filter, retrying as long as the predicate is statisfied
    let doWhile predicate service : Service<'a, 'b> =
        let rec policies = seq {
            yield predicate
            yield! policies
        }
        create policies service

    /// Creates a retry filter, retrying as long as the predicate is statisfied, but maximum the number of
    /// times specified
    let triesWhile times predicate service : Service<'a, 'b> =
        let rec policies = seq {
            for _ in 1 .. times do
                yield predicate
        }
        create policies service

/// Logging filter. Use the log requests before they are executed by the service
[<RequireQualifiedAccessAttribute>]
module LoggingFilter =
    /// Logger definition
    type Logger<'a> = 'a -> 'a
    
    /// Creates a new logging filter, using the specified logger
    let create (logger : Logger<'a>) (service : Service<'a, 'b>) : Service<'a, 'b> =
        let logger' request = async {
            return! logger request |> service
        }
        logger'