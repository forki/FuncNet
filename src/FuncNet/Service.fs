namespace FuncNet

open System

/// Base class for all services
[<AbstractClassAttribute>]
type Service<'Request, 'Response>() =
    
    /// Make a request
    abstract member Request : 'Request -> Future<'Response>
    
    /// Close the current service
    abstract member Close : unit -> unit
    default __.Close() = ()
    
    interface IDisposable with
        member self.Dispose() = self.Close()
