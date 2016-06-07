[<RequireQualifiedAccess>]
module StaticProxy

open System
open System.Reflection

let private ignoredMethods = Set ["ToString"; "Equals"; "GetHashCode"; "GetType"]

module private Interface =

    let private flags = BindingFlags.Public ||| BindingFlags.Instance

    let getMethods<'t> =
        typedefof<'t>.GetMethods(flags)
        |> Seq.filter(fun m -> not m.IsSpecialName)

    let getProperties<'t> =
        typedefof<'t>.GetProperties(flags)

let private typeName = function
| "Void" -> "unit"
| t -> t

let private generateParameters (withType : bool) (mi : MethodInfo) =
    let template = if withType then "{0} : {1}" else "{0}"
    mi.GetParameters()
    |> Seq.map(fun p -> String.Format(template, p.Name, p.ParameterType.Name |> typeName))
    |> fun p -> String.Join(", ", p)

let private generateMethod (objectName : string) (mi : MethodInfo) =
    let typedParameters = mi |> generateParameters true
    let parameters = mi |> generateParameters false
    let returnType = mi.ReturnType.Name |> typeName
    String.Format("    member this.{0}({1}) : {2} = {4}.{0}({3})",
        mi.Name, typedParameters, returnType, parameters, objectName)

let private generateProperty (objectName : string) (pi : PropertyInfo) =
    let propertyType = pi.PropertyType.Name |> typeName
    let gs =
        match pi with
        | p when p.CanRead && p.CanWrite -> "with get() : {1} = {2}.{0} and set(value : {1}) = {2}.{0} <- value"
        | p when p.CanRead -> "with get() : {1} = {2}.{0}"
        | p when p.CanWrite -> "with set(value : {1}) = {2}.{0} <- value"
        | _ -> failwith "invalid property"
    String.Format(sprintf "    member this.{0} %s" gs,
        pi.Name, propertyType, objectName)

let private generateProxy<'t> (className : string) (objectName : string) (lines : string seq) =
    lines
    |> Seq.distinctBy(fun l -> l)
    |> fun lines ->
        let lines = seq {
            yield sprintf "type %s(%s : %s) =" className objectName (typedefof<'t>.Name)
            yield! lines }
        String.Join(Environment.NewLine, lines)

let private lcFirst = function
| "" -> ""
| str -> str.[0].ToString().ToLower() + str.[1..]

let fromType<'t> (className : string) =
    let methods, props = Interface.getMethods<'t>, Interface.getProperties<'t>
    let typeName = typedefof<'t>.Name
    let objectName = typeName |> lcFirst
    seq [
        methods
        |> Seq.filter(fun mi -> not (ignoredMethods.Contains mi.Name))
        |> Seq.map(fun mi -> mi |> generateMethod objectName)
        props |> Seq.map(fun pi -> pi |> generateProperty objectName)
    ]
    |> Seq.concat
    |> generateProxy<'t> className objectName