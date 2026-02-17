namespace web.Controllers

open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open core.fs
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type TransactionsController(service: ImportTransactions.Handler) =
    inherit ControllerBase()

    [<HttpPost("import")>]
    member this.Import(file: IFormFile) = task {
        use streamReader = new StreamReader(file.OpenReadStream())
        let! content = streamReader.ReadToEndAsync()
        let cmd : ImportTransactions.Command = {Content = content; UserId = this.User.Identifier()}
        return! this.OkOrError(service.Handle(cmd))
    }
