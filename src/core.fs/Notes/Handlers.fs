namespace core.fs.Notes

    open System
    open System.ComponentModel.DataAnnotations
    open core.Notes
    open core.Shared
    open core.Shared.Adapters.CSV
    open core.Shared.Adapters.Storage
    open core.fs

    type AddNote = 
        {
            [<Required>]
            Note: string
            [<Required>]
            Ticker: Ticker
            UserId: Guid
        }
        
    type UpdateNote = 
        {
            [<Required>]
            NoteId: Guid
            [<Required>]
            Note: string
            UserId: Guid
        }
        
    type GetNote =
        {
            NoteId: Guid
            UserId: Guid
        }
        
    type GetNotes =
        {
            UserId: Guid
        }
        
    type GetNotesForTicker =
        {
            Ticker: Ticker
            UserId: Guid
        }
        
    type Export = 
        {
            UserId: Guid
        }
        
    type Import = 
        {
            UserId: Guid
            Content: string
        }
        
    type ImportRecord = 
        {
            created: DateTimeOffset
            note: string
            ticker: Ticker
        }
        
    type NotesView(notes:Note seq) =
        member _.Notes = notes |> Seq.map (fun x -> x.State) |> Seq.sortByDescending (fun x -> x.Created) |> Seq.toList
        member _.Tickers = notes |> Seq.map (fun x -> x.State.RelatedToTicker) |> Seq.distinct |> Seq.sort |> Seq.toList
        
    type Handler(accounts:IAccountStorage,csvParser:ICSVParser,csvWriter:ICSVWriter,portfolio:IPortfolioStorage) =
        
        interface IApplicationService
        
        member _.Handle (command: AddNote) = task {
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | null -> return "User not found" |> ResponseUtils.failedTyped<Note>
            | _ -> 
                let note = Note(userId=command.UserId,ticker=command.Ticker,note=command.Note,created=DateTime.UtcNow)
                do! portfolio.Save(note, command.UserId)
                return note |> ResponseUtils.success<Note>
        }
        
        member _.Handle (command: GetNote) = task {
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | null -> return "User not found" |> ResponseUtils.failedTyped<NoteState>
            | _ -> 
                let! note = portfolio.GetNote(noteId=command.NoteId, userId=command.UserId)
                
                match note with
                | null -> return "Note not found" |> ResponseUtils.failedTyped<NoteState>
                | _ -> return note.State |> ResponseUtils.success<NoteState>
        }
        
        member _.Handle (command: GetNotes) = task {
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | null -> return "User not found" |> ResponseUtils.failedTyped<NotesView>
            | _ -> 
                let! notes = portfolio.GetNotes(userId=command.UserId)
                return NotesView(notes) |> ResponseUtils.success<NotesView>
        }
        
        member _.Handle (command: GetNotesForTicker) = task {
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | null -> return "User not found" |> ResponseUtils.failedTyped<NotesView>
            | _ -> 
                let! notes = portfolio.GetNotes(userId=command.UserId)
                return NotesView(notes |> Seq.filter(fun n -> n.State.RelatedToTicker = command.Ticker.Value)) |> ResponseUtils.success<NotesView>
        }
        
        member _.Handle (command: UpdateNote) = task {
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | null -> return "User not found" |> ResponseUtils.failedTyped<Note>
            | _ -> 
                let! note = portfolio.GetNote(noteId=command.NoteId, userId=command.UserId)
                
                match note with
                | null -> return "Note not found" |> ResponseUtils.failedTyped<Note>
                | _ -> 
                    note.Update(command.Note)
                    do! portfolio.Save(note, command.UserId)
                    return note |> ResponseUtils.success<Note>
        }
        
        member _.Handle (command: Export) = task {
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | null -> return "User not found" |> ResponseUtils.failedTyped<ExportResponse>
            | _ -> 
                let! notes = portfolio.GetNotes(userId=command.UserId)
                
                let filename = CSVExport.GenerateFilename("notes")
                
                let response = ExportResponse(filename, CSVExport.Generate(csvWriter, notes))
                
                return response |> ResponseUtils.success<ExportResponse>
        }
        
        member this.Handle (command: Import) = task {
            
            let saveNote (addCommand:AddNote) =
                addCommand |> this.Handle |> Async.AwaitTask
            
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | null -> return "User not found" |> ResponseUtils.failed
            | _ -> 
                let records = csvParser.Parse<ImportRecord>(command.Content)
                match records.IsOk with
                | false -> return records.Error.Message |> ResponseUtils.failed
                | true ->
                    
                    let! imports =
                        records.Success
                        |> Seq.map(fun r -> {Note=r.note;Ticker=r.ticker;UserId=command.UserId})
                        |> Seq.map(saveNote)
                        |> Async.Parallel
                        |> Async.StartAsTask
                        
                    let failedImports =
                        imports |> Seq.filter(fun r -> r.IsOk = false)
                        
                    match failedImports |> Seq.isEmpty with
                    | true -> return ServiceResponse()
                    | false -> 
                        let failedImports = failedImports |> Seq.map(fun r -> r.Error.Message) |> String.concat "\n"
                        return failedImports |> ResponseUtils.failed
        }