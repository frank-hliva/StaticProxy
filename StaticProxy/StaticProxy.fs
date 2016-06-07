[<RequireQualifiedAccess>]
module Proxies.StaticProxy

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

let rec private typeName (fullName : bool) (t : Type) =
    (match if fullName then t.FullName else t.Name with
    | "Void" | "System.Void" -> "unit"
    | name -> name
    |> fun name -> 
        match name.IndexOf("`") with
        | -1 -> name
        | i -> name.[..i - 1]) +
    if t.IsConstructedGenericType then
        t.GenericTypeArguments
        |> Seq.map(fun t -> t |> typeName fullName)
        |> fun args -> sprintf "<%s>" (String.Join(", ", args))
    else ""

let private generateParameters (settings : ProxySettings) (withType : bool) (mi : MethodInfo) =
    let template = if withType then "{0} : {1}" else "{0}"
    mi.GetParameters()
    |> Seq.map(fun p -> String.Format(template, p.Name, p.ParameterType |> typeName settings.FullNames))
    |> fun p -> String.Join(", ", p)

let private generateMethod (settings : ProxySettings) (mi : MethodInfo) =
    let typedParameters = mi |> generateParameters settings true
    let parameters = mi |> generateParameters settings false
    let returnType = mi.ReturnType |> typeName settings.FullNames
    String.Format("    member this.{0}({1}) : {2} = {4}.{0}({3})",
        mi.Name, typedParameters, returnType, parameters, settings.ObjectName)

let private generateProperty (settings : ProxySettings) (pi : PropertyInfo) =
    let propertyType = pi.PropertyType |> typeName settings.FullNames
    let gs =
        match pi with
        | p when p.CanRead && p.CanWrite -> "with get() : {1} = {2}.{0} and set(value : {1}) = {2}.{0} <- value"
        | p when p.CanRead -> "with get() : {1} = {2}.{0}"
        | p when p.CanWrite -> "with set(value : {1}) = {2}.{0} <- value"
        | _ -> failwith "invalid property"
    String.Format(sprintf "    member this.{0} %s" gs,
        pi.Name, propertyType, settings.ObjectName)

let private generateProxy<'t> (settings : ProxySettings) (lines : string seq) =
    lines
    |> Seq.distinct
    |> fun lines ->
        let lines = seq {
            yield sprintf "type %s(%s : %s) ="
                settings.ProxyClassName
                settings.ObjectName
                (typedefof<'t>.Name)
            yield! lines }
        String.Join(Environment.NewLine, lines)

let private lcFirst = function
| "" -> ""
| str -> str.[0].ToString().ToLower() + str.[1..]

let private initProxySettings<'t> (settings : ProxySettings) =
    { settings with
        ObjectName =
            if String.IsNullOrEmpty(settings.ObjectName)
            then typedefof<'t>.Name |> lcFirst
            else settings.ObjectName }

let fromType<'t> (settings : ProxySettings) =
    let settings = settings |> initProxySettings<'t>
    let methods, props = Interface.getMethods<'t>, Interface.getProperties<'t>
    seq [
        methods
        |> Seq.filter(fun mi -> not (ignoredMethods.Contains mi.Name))
        |> Seq.map(fun mi -> mi |> generateMethod settings)
        props |> Seq.map(fun pi -> pi |> generateProperty settings)
    ]
    |> Seq.concat
    |> generateProxy<'t> settings