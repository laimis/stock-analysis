namespace core.fs.Admin

open System
open core.fs
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage

type AdminCleanUpService(storage: IAccountStorage, logger: ILogger) =

    interface IApplicationService

    member _.Execute() : System.Threading.Tasks.Task<unit> = task {
        try
            logger.LogInformation "Starting admin clean up service"

            // Delete reminders that were sent 7 or more days ago
            let cutoff = DateTimeOffset.UtcNow.AddDays(-7)
            let! deleted = storage.DeleteSentRemindersBefore cutoff
            let cutoffStr = cutoff.ToString "yyyy-MM-dd"
            logger.LogInformation $"Deleted {deleted} sent reminders older than {cutoffStr}"

            logger.LogInformation "Admin clean up service completed"
        with
        | ex ->
            logger.LogError $"Error in admin clean up service: {ex}"
    }
