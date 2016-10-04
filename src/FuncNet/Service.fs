namespace FuncNet

/// Service definition
type Service<'Request, 'Response> = 'Request -> Future<'Response>