namespace FuncNet

open System
open System.Threading

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

module Service =
    type private ZeroCountDownEvent() =
        let resetEvent = new ManualResetEventSlim(true)
        let counter = ref 0
        let lockObj = new obj()

        member __.Decrement() =
            lock lockObj (fun() ->
                counter |> decr
                if !counter = 0 then
                    resetEvent.Set())

        member __.Increment() =
            lock lockObj (fun () ->
                counter |> incr
                resetEvent.Reset())

        member __.Wait() =
            resetEvent.Wait()

    type private DrainService<'Request, 'Response>(service : Service<'Request, 'Response>) =
        inherit Service<'Request, 'Response>()

        let counter = new ZeroCountDownEvent()

        override __.Request(request) =
            counter.Increment()
            async {
                use! cancelHandler = Async.OnCancel (fun () -> counter.Decrement())
                let! outcome = request |> service.Request
                counter.Decrement()
                return outcome
            }

        override __.Close() =
            counter.Wait()
            service.Close()

    let withDrain service =
        new DrainService<_, _>(service) :> Service<_, _>
