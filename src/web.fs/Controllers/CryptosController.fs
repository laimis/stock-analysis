namespace web.Controllers

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open core.fs.Cryptos
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type CryptosController(service: Handler) =
    inherit ControllerBase()

    [<HttpGet>]
    member this.Dashboard() =
        this.OkOrError(service.Handle({DashboardQuery.UserId = this.User.Identifier()}))

    [<HttpGet("{token}")>]
    member this.DetailsAsync([<FromRoute>] token: string) =
        this.OkOrError(service.Handle {Details.Token = token |> core.Cryptos.Token})

    [<HttpGet("{token}/ownership")>]
    member this.Ownership([<FromRoute>] token: string) =
        this.OkOrError(service.Handle {OwnershipQuery.Token = token |> core.Cryptos.Token; UserId = this.User.Identifier()})

    [<HttpDelete("{token}/transactions/{transactionId}")>]
    member this.DeleteTransaction([<FromRoute>] token: string, [<FromRoute>] transactionId: Guid) =
        this.OkOrError(service.Handle {DeleteTransaction.Token = token |> core.Cryptos.Token; TransactionId = transactionId; UserId = this.User.Identifier()})

    [<HttpPost("import")>]
    member this.Import(file: IFormFile) = task {
        use streamReader = new StreamReader(file.OpenReadStream())
        let! content = streamReader.ReadToEndAsync()
        let cmd = ImportCryptoCommandFactory.create file.FileName content (this.User.Identifier())
        return! this.OkOrError(service.Handle cmd)
    }

    [<HttpGet("export")>]
    member this.Export() =
        this.GenerateExport(service.Handle {Export.UserId = this.User.Identifier()})
