namespace FuncNet

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
    /// Retry policy definition
    type RetryPolicy<'a> = Outcome<'a> -> bool
    
    /// Creats a new retry filter, using the specifed retry policies
    let create (policies : RetryPolicy<'b> seq) (service : Service<'a, 'b>) : Service<'a, 'b> =
        let rec retry (policy : RetryPolicy<'b> seq) request =
            async {
                let! outcome = service request
                if policy |> Seq.isEmpty then
                    return outcome
                elif Seq.head policy outcome then
                    return! retry (policy |> Seq.tail) request
                else return outcome
            }
        retry policies

    /// Create a retry filter, retrying as long as the predicate is statisfied
    let doWhile predicate =
        let rec policies = seq {
            yield predicate
            yield! policies
        }
        create policies

    /// Creates a retry filter, retrying as long as the predicate is statisfied, but maximum the number of
    /// times specified
    let triesWhile times predicate =
        let rec policies = seq {
            for _ in 1 .. times do
                yield predicate
        }
        create policies

/// Retry filter, only retrying on failed requests.
[<RequireQualifiedAccessAttribute>]
module RetryExceptionsFilter =
    /// Retry policy definition
    type RetryPolicy = exn -> bool
    
    /// Creats a new retry filter, using the specifed retry policies
    let create (policies : RetryPolicy seq) (service : Service<'a, 'b>) : Service<'a, 'b> =
        let rec retry (policy : RetryPolicy seq) request =
            async {
                let! outcome = service request
                match outcome with
                | Success x -> return Success x
                | Failure e ->
                    if policy |> Seq.isEmpty then
                        return Failure e
                    elif Seq.head policy e then
                        return! retry (policy |> Seq.tail) request
                    else return Failure e
            }
        retry policies

    /// Create a retry filter, retrying at most the specified number of times
    let tries times =
        let retry _ = true
        let policies = seq {
            for _ in 1 .. times -> retry
        }
        create policies

    /// Create a retry filter, retrying as long as the predicate is statisfied
    let doWhile predicate =
        let rec policies = seq {
            yield predicate
            yield! policies
        }
        create policies

    /// Creates a retry filter, retrying as long as the predicate is statisfied, but maximum the number of
    /// times specified
    let triesWhile times predicate =
        let rec policies = seq {
            for _ in 1 .. times do
                yield predicate
        }
        create policies

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