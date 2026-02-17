namespace web.Utils

open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Diagnostics.HealthChecks
open storage.shared

type HealthCheck(storage: IAggregateStorage) =
    interface IHealthCheck with
        member this.CheckHealthAsync(context: HealthCheckContext, cancellationToken: CancellationToken) = task {
            do! storage.DoHealthCheck()
            return HealthCheckResult.Healthy()
        }
