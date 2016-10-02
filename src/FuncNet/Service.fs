namespace FuncNet

type Service<'Request, 'Response> = 'Request -> Future<'Response>