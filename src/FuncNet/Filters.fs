namespace FuncNet

[<RequireQualifiedAccessAttribute>]
module TimeoutFilter =
    let create (timeoutMs : int) (service : Service<'a, 'b>) : Service<'a, 'b> =
        service >> Future.withIn timeoutMs

[<RequireQualifiedAccessAttribute>]
module RetryFilter =
    type RetryPolicy<'a> = Outcome<'a> -> bool
    
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

    let doWhile predicate =
        let rec policies = seq {
            yield predicate
            yield! policies
        }
        create policies

    let triesWhile times predicate =
        let rec policies = seq {
            for _ in 1 .. times do
                yield predicate
        }
        create policies

[<RequireQualifiedAccessAttribute>]
module RetryExceptionsFilter =
    type RetryPolicy = exn -> bool
    
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

    let tries times =
        let retry _ = true
        let policies = seq {
            for _ in 1 .. times -> retry
        }
        create policies

    let doWhile predicate =
        let rec policies = seq {
            yield predicate
            yield! policies
        }
        create policies

    let triesWhile times predicate =
        let rec policies = seq {
            for _ in 1 .. times do
                yield predicate
        }
        create policies

[<RequireQualifiedAccessAttribute>]
module LoggingFilter =
    let create (logger : 'a -> 'a) (service : Service<'a, 'b>) : Service<'a, 'b> =
        let logger' request = async {
            return! logger request |> service
        }
        logger'