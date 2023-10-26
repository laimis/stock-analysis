namespace storage.shared;

public static class EventInfraAdjustments
{
    // this class is used to change event JSON to match the namespaces properly after refactoring events.
    // JSON serialization is using namespace paths and those can change after the events are moved.
    // We rewrite JSON on the fly, or we can also use this to fix events when we persist in the database during
    // migrations.

    public static string AdjustIfNeeded(string json)
    {
        // handling Routines move from core.Portfolio to core.Routines
        if (json.Contains("\"$type\":\"core.Portfolio.Routine"))
        {
            json = json.Replace("\"$type\":\"core.Portfolio.Routine", "\"$type\":\"core.Routines.Routine");
        }

        return json;
    }
}