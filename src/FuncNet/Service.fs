namespace FuncNet

type Service<'Request, 'Response> = 'Request -> Async<'Response>