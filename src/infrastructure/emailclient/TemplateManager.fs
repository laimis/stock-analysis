namespace emailclient

module EmailTemplateManager =
    open System.Collections.Concurrent
    open Scriban
    open System.Reflection
    open System.IO

    /// Cache for parsed templates to improve performance
    let private templateCache = ConcurrentDictionary<string, Template>()
    
    /// The assembly containing the embedded templates
    let private assembly = Assembly.GetExecutingAssembly()
    
    /// Base namespace for embedded templates
    let private templateNamespace = "emailclient.Templates"
    
    /// Gets the full resource name for a template
    let private getResourceName (templateName: string) =
        $"{templateNamespace}.{templateName}.html"
    
    /// Lists all available template names
    let listAvailableTemplates() =
        assembly.GetManifestResourceNames()
        |> Array.filter (fun name -> name.StartsWith(templateNamespace) && name.EndsWith(".html"))
        |> Array.map (fun name -> 
            name.Substring(templateNamespace.Length + 1, name.Length - templateNamespace.Length - 6))
        |> Array.toList
    
    /// Loads a template from embedded resources
    let loadTemplateFromResources (templateName: string) =
        async {
            let resourceName = getResourceName templateName
            use stream = assembly.GetManifestResourceStream(resourceName)
            
            if isNull stream then 
                let availableTemplates = listAvailableTemplates() |> String.concat ", "
                return Error $"Template not found: {templateName}. Available templates: {availableTemplates}"
            else
                use reader = new StreamReader(stream)
                let! content = reader.ReadToEndAsync() |> Async.AwaitTask
                return Ok content
        }
    
    /// Get or parse a template, using the cache when available
    let private getOrParseTemplate (templateName: string) (templateContent: string) =
        templateCache.GetOrAdd(templateName, fun _ -> 
            let parseResult = Template.Parse templateContent
            
            if parseResult.HasErrors then
                let errorMsg = System.String.Join(System.Environment.NewLine, parseResult.Messages)
                failwith $"Error parsing template '{templateName}': {errorMsg}"
                
            parseResult)
    
    /// Renders a template with the provided data model
    let renderTemplate (templateContent: string) (templateName: string) (data: obj) =
        async {
            try
                let template = getOrParseTemplate templateName templateContent
                let result = template.Render data
                return Ok result
            with ex ->
                return Error $"Failed to render template '{templateName}': {ex.Message}"
        }
    
    /// Processes a template by name with the provided data
    let processTemplate (templateName: string) (data: obj) =
        async {
            let! templateContentResult = loadTemplateFromResources templateName
            
            match templateContentResult with
            | Ok content -> return! renderTemplate content templateName data
            | Error err -> return Error err
        }