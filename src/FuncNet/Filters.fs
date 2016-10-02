namespace FuncNet

[<RequireQualifiedAccessAttribute>]
module TimeoutFilter =
    let create (timeoutMs : int) (service : Service<'a, 'b>) : Service<'a, 'b> =
        service >> Future.withIn timeoutMs