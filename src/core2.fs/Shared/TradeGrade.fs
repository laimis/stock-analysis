namespace core.Shared

open System

[<CustomComparison; StructuralEquality>]
[<Struct>]
type TradeGrade =
    val Value: string
    
    new(grade: string) =
        if String.IsNullOrWhiteSpace(grade) then
            invalidArg "grade" "Grade cannot be blank"
        
        // right now we allow only A, B, C
        // this feels like should be configurable by trader?
        let upper = grade.ToUpper()
        if upper <> "A" && upper <> "B" && upper <> "C" then
            invalidArg "grade" "Grade must be A, B, or C"
        
        { Value = upper }
    
    interface IComparable with
        member this.CompareTo(obj: obj) =
            match obj with
            | :? TradeGrade as other -> String.Compare(this.Value, other.Value, StringComparison.Ordinal)
            | _ -> invalidArg "obj" "Object is not a TradeGrade"
