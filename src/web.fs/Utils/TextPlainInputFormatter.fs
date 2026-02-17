namespace web.Utils

open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Formatters

type TextPlainInputFormatter() =
    inherit InputFormatter()
    
    let contentType = "text/plain"
    
    do
        base.SupportedMediaTypes.Add(contentType)
    
    override this.ReadRequestBodyAsync(context: InputFormatterContext) = task {
        let request = context.HttpContext.Request
        use reader = new StreamReader(request.Body)
        let! content = reader.ReadToEndAsync()
        return InputFormatterResult.SuccessAsync(content) |> Async.AwaitTask |> Async.RunSynchronously
    }
    
    override this.CanRead(context: InputFormatterContext) =
        let contentType = context.HttpContext.Request.ContentType
        let ct = if isNull contentType then "" else contentType
        ct.StartsWith(contentType)
