namespace FuncNet

type Filter<'Request, 'Response> = 'Request -> Future<'Response>

/// Timeout filter. Fails the request if not completed within the specified time, this also
/// cancels the operation
[<RequireQualifiedAccessAttribute>]
module TimeoutFilter =
    /// Creates a timeout filter, with the specified timeout
    let create (timeoutMs : int) (service : Service<'a, 'b>) : Filter<'a, 'b> =
        service.Request >> Future.withIn timeoutMs

// Retry policy
type RetryPolicy<'a> =
    {
        /// Predicate determning if it should retry
        Predicate : 'a -> bool;
        /// Retry backoff in ms
        Backoff : int option;
    }

/// Retry filter. Allows implementing retry logic, both for successful requests and for failed requests
[<RequireQualifiedAccessAttribute>]
module RetryFilter =
    /// Create a retry policy
    let createPolicy (predicate : Outcome<'a> -> bool) =
        { Predicate = predicate; Backoff = None }

    /// Create a retry policy with backoff ms
    let createPolicyWithBackoff (predicate : Outcome<'a> -> bool) backoffMs =
        { Predicate = predicate; Backoff = Some backoffMs }

    /// Creats a new retry filter, using the specifed retry policies
    let create (policies : RetryPolicy<_> seq) (service : Service<'a, 'b>) : Filter<'a, 'b> =
        let rec retry (policies : RetryPolicy<_> seq) request =
            async {
                let! outcome = service.Request request
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
    let doWhile predicate (service : Service<'a, 'b>) : Filter<'a, 'b> =
        let rec policies = seq {
            yield predicate
            yield! policies
        }
        create policies service

    /// Creates a retry filter, retrying as long as the predicate is statisfied, but maximum the number of
    /// times specified
    let triesWhile times predicate service : Filter<'a, 'b> =
        let rec policies = seq {
            for _ in 1 .. times do
                yield predicate
        }
        create policies service

/// Retry filter, only retrying on failed requests.
[<RequireQualifiedAccessAttribute>]
module RetryExceptionsFilter =
    /// Create a retry policy
    let createPolicy predicate =
        { Predicate = predicate; Backoff = None }

    /// Create a retry policy with backoff ms
    let createPolicyWithBackoff (predicate : exn -> bool) backoffMs =
        { Predicate = predicate; Backoff = Some backoffMs }
    
    /// Creats a new retry filter, using the specifed retry policies
    let create (policies : RetryPolicy<_> seq) (service : Service<'a, 'b>) : Filter<'a, 'b> =
        let rec retry (policies : RetryPolicy<_> seq) request =
            async {
                let! outcome = service.Request request
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
    let tries times service : Filter<'a, 'b> =
        let retry _ = true
        let policies = seq {
            for _ in 1 .. times -> createPolicy retry
        }
        create policies service

    /// Create a retry filter, retrying as long as the predicate is statisfied
    let doWhile predicate service : Filter<'a, 'b> =
        let rec policies = seq {
            yield predicate
            yield! policies
        }
        create policies service

    /// Creates a retry filter, retrying as long as the predicate is statisfied, but maximum the number of
    /// times specified
    let triesWhile times predicate service : Filter<'a, 'b> =
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
    let create (logger : Logger<'a>) (service : Service<'a, 'b>) : Filter<'a, 'b> =
        let logger' request = async {
            return! logger request |> service.Request
        }
        logger'

/// Filter limiting the number of requests within a timeframe
[<RequireQualifiedAccessAttribute>]
module RateLimitingFilter =
    open System

    /// Exception thrown if a request is refused because of rate limiting
    type RefusedByRateLimiterException() = inherit Exception()

    type private Limiter(windowSize, numberOfRequests) =
        let mutable remainingRequests = numberOfRequests
        let mutable lastTimestamp = DateTime.Now
        let lockObject = new obj()

        let resetRemainingRequestsIfNewWindow (timestamp : DateTime) =
            if timestamp.Subtract(lastTimestamp).TotalMilliseconds > float windowSize then
                remainingRequests <- numberOfRequests

        let shouldBeLimited() =
            remainingRequests <= 0

        let limitingService (service : Service<'a, 'b>) request : Future<'b> =
            async {
                return! lock lockObject (fun () ->
                        let now = DateTime.Now
                        resetRemainingRequestsIfNewWindow now
                        let limited = shouldBeLimited()
                        if limited then
                            async { return Failure <| (RefusedByRateLimiterException() :> Exception) }
                        else
                            remainingRequests <- remainingRequests - 1
                            lastTimestamp <- now
                            async { return! request |> service.Request }
                    )
            }

        member __.apply service =
            limitingService service

    /// Create a new rate limiter filter
    let create (windowSize : int) numberOfRequests (service : Service<'a, 'b>) : Filter<'a, 'b> =
        let limiter = new Limiter(windowSize, numberOfRequests)
        limiter.apply service
