namespace core.fs

open System
open core.Shared

type IApplicationService = interface end

module ResponseUtils =
    
    let failedTyped<'a> (message: string) =
        ServiceResponse<'a>(ServiceError(message))
        
    let failed (message: string) =
        ServiceResponse(ServiceError(message))
        
    let success<'a> (data: 'a) =
        ServiceResponse<'a>(data)