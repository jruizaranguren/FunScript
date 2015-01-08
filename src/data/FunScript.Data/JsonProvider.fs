﻿// --------------------------------------------------------------------------------------
// Mappings for the JSON Type Provider runtime
// --------------------------------------------------------------------------------------

module private FunScript.Data.JsonProvider

open System
open System.Globalization
open FunScript
open FSharp.Data
open FSharp.Data.Runtime

// --------------------------------------------------------------------------------------
// Low level JS helpers for getting types & reflection

[<AutoOpen>]
module JsHelpers = 
  [<JS; JSEmit("return typeof({0});")>]
  let jstypeof(o:obj) : string = failwith "never"
  [<JS; JSEmit("return Array.isArray({0});")>]
  let isArray(o:obj) : bool = failwith "never"
  [<JS; JSEmit("return {0}==null;")>]
  let isNull(o:obj) : bool = failwith "never"
  [<JS; JSEmit("return typeof({0}[{1}]) != \"undefined\";")>]
  let hasProperty(o:obj,name:string) : bool = failwith "never"

// --------------------------------------------------------------------------------------
// Reimplementation of the Json Type Provider runtime 

#nowarn "10001"

[<JS>]
type JsDocument = 
     
    private { 
                Json : JsonValue
                Path : string }

    interface IJsonDocument with
        member x.JsonValue = x.Json
        member x.Path() = x.Path
        member x.CreateNew(value,pathInc) = JsDocument.Create(value, x.Path + pathInc)

    static member Create(value,path) = 
        {
            Json = value
            Path = path
        } :> IJsonDocument

[<JS>]
type JsRuntime = 
  // Document is parsed into JS object using nasty 'eval'
  // Various getters that do conversions become just identities

  [<JSEmit("return null;")>]
  static member GetCulture(name:string) : obj = failwith "never"

  [<JSEmit("var it = {0}; return (typeof(it)==\"string\") ? eval(\"(function(a){return a;})(\" + it + \")\") : it;")>]
  static member Parse(source:string, culture:obj) : JsonValue = failwith "never"

  [<JSEmit("return {0}[{1}];")>]
  static member GetProperty(json:IJsonDocument, name:string) : IJsonDocument = failwith "never"

  static member TryGetProperty(json:IJsonDocument, name:string) = 
    if hasProperty(json, name) then 
        JsRuntime.GetProperty(json, name) |> Some
    else
        None

  static member GetNonOptionalValue(path: string, json:JsonValue option, original:JsonValue option) = 
    match json with
    | Some j -> j
    | None -> JsonValue.Null

  [<JSEmit("""return "";""")>]
  static member EmptyString(arg:obj) : obj = failwith "never"
  
  [<JSEmit("return {0};")>]
  static member Identity(arg:obj) : obj = failwith "never"

  [<JSEmit("return {0};")>]
  static member Identity1Of2(arg:obj, arg2:obj) : obj = failwith "never"
  [<JSEmit("return {1};")>]
  static member Identity2Of2(arg:obj, arg2:obj) : obj = failwith "never"
  
  [<JSEmit("return {0};")>]
  static member Identity1Of3(arg:obj, arg2:obj, arg3:obj) : obj = failwith "never"
  [<JSEmit("return {1};")>]
  static member Identity2Of3(arg:obj, arg2:obj, arg3:obj) : obj = failwith "never"
  [<JSEmit("return {2};")>]
  static member Identity3Of3(arg:obj, arg2:obj, arg3:obj) : obj = failwith "never"

   static member inline OptionToJson f = function None -> JsonValue.Null | Some v -> f v

  static member private ToJsonValue (cultureInfo:CultureInfo) (value:obj) = 
    if value = null then JsonValue.Null
    else
        let tname = value.GetType().FullName
        if tname = typeof<Array>.FullName then
            JsonValue.Array [| for elem in (value :?> Array) -> JsRuntime.ToJsonValue cultureInfo elem |]
        elif tname = typeof<string>.FullName then
            JsonValue.String (value :?> string)
        elif tname = typeof<DateTime>.FullName then
            (value :?> DateTime).ToString(cultureInfo) |> JsonValue.String
        elif tname = typeof<int>.FullName then
            JsonValue.Number(value :?> decimal)
        elif tname = typeof<int64>.FullName then
            JsonValue.Number(value :?> decimal)
        elif tname = typeof<float>.FullName then
            JsonValue.Number(value :?> decimal)
        elif tname = typeof<decimal>.FullName then
            JsonValue.Number(value :?> decimal)
        elif tname = typeof<bool>.FullName then
            JsonValue.Boolean(value :?> bool)
        elif tname = typeof<Guid>.FullName then
            JsonValue.String <| value.ToString()
        elif tname = typeof<IJsonDocument>.FullName then
            (value :?> IJsonDocument).JsonValue
        elif tname = typeof<JsonValue>.FullName then
            (value :?> JsonValue)
        elif tname = typeof<string option>.FullName then
            JsRuntime.OptionToJson JsonValue.String (value :?> string option)
        elif tname = typeof<DateTime option>.FullName then
            JsRuntime.OptionToJson (fun (dt:DateTime) -> dt.ToString(cultureInfo) |> JsonValue.String) (value :?> DateTime option)
        elif tname = typeof<int option>.FullName then
            JsRuntime.OptionToJson (decimal >> JsonValue.Number) (value :?> decimal option)
        elif tname = typeof<int64 option>.FullName then
            JsRuntime.OptionToJson (decimal >> JsonValue.Number) (value :?> decimal option)
        elif tname = typeof<float option>.FullName then
            JsRuntime.OptionToJson (decimal >> JsonValue.Number) (value :?> decimal option)
        elif tname = typeof<decimal option>.FullName then
            JsRuntime.OptionToJson (decimal >> JsonValue.Number) (value :?> decimal option)
        elif tname = typeof<bool option>.FullName then
            JsRuntime.OptionToJson JsonValue.Boolean (value :?> bool option)
        elif tname = typeof<Guid option>.FullName then
            JsRuntime.OptionToJson (fun (g:Guid) -> g.ToString() |> JsonValue.String) (value :?> Guid option)
        elif tname = typeof<IJsonDocument option>.FullName then
            JsRuntime.OptionToJson (fun (v:IJsonDocument) -> v.JsonValue) (value :?> IJsonDocument option)
        elif tname = typeof<JsonValue option>.FullName then
            JsRuntime.OptionToJson id (value :?> JsonValue option)
        else
            failwith <| String.Format("Can't create JsonValue from %A", value) 

  // The `GetArrayChildrenByTypeTag` is a JS reimplementation of the
  // equivalent in the JSON type provider. The following functions
  // are derived (copied from JSON provider code)
  static member GetArrayChildrenByTypeTag<'T>
    (doc:IJsonDocument, cultureStr:string, tagCode:string, mapping:Func<IJsonDocument, 'T>) : 'T[] =
    let matchTag (value:obj) : option<obj> = 
      if isNull value then None
      elif jstypeof(value) = "boolean" && tagCode = "Boolean" then Some(unbox value)
      elif jstypeof(value) = "number" && tagCode = "Number" then Some(unbox value)
      elif jstypeof(value) = "string" && tagCode = "Number" then Some(unbox (1 * unbox value))
      elif jstypeof(value) = "string" && tagCode = "String" then Some(unbox value)
      elif isArray(value) && tagCode = "Array"then Some(unbox value)
      elif jstypeof(value) = "object" && tagCode = "Record" then Some(unbox value)
      else None // ??? maybe sometimes fail
    if isArray doc then
      doc |> unbox 
          |> Array.choose matchTag
          |> Array.map (unbox >> mapping.Invoke)
    else failwith "JSON mismatch: Expected Array node"

  static member GetArrayChildByTypeTag
    (value:IJsonDocument, cultureStr:string, tagCode:string) : IJsonDocument = 
    let arr = JsRuntime.GetArrayChildrenByTypeTag(value, cultureStr, tagCode, Func<_,_>(id)) 
    if arr.Length = 1 then arr.[0] 
    else failwith "JSON mismatch: Expected single value, but found multiple."

  static member TryGetArrayChildByTypeTag<'T>
    (doc:IJsonDocument, cultureStr:string, tagCode:string, mapping:Func<IJsonDocument, 'T>) : 'T option = 
    let arr = JsRuntime.GetArrayChildrenByTypeTag(doc, cultureStr, tagCode, mapping) 
    if arr.Length = 1 then Some arr.[0]
    elif arr.Length = 0 then None
    else failwith "JSON mismatch: Expected Array with single or no elements."

  static member TryGetValueByTypeTag<'T>(value:IJsonDocument, cultureStr:string, tagCode:string, mapping:Func<IJsonDocument,'T>) : 'T option =
    JsonRuntime.TryGetArrayChildByTypeTag(value, cultureStr, tagCode, mapping) 

  static member ConvertOptionalProperty<'T>(doc:IJsonDocument, name:string, mapping:Func<IJsonDocument, 'T>) : 'T option = 
    if hasProperty(doc, name) then Some (mapping.Invoke (JsRuntime.GetProperty(doc, name)))
    else None

  // In the dynamic world, this does not do anything at all :-)  
  [<JSEmit("return {0};")>]
  static member ConvertArray<'T>(value:IJsonDocument, mapping:Func<IJsonDocument, 'T>) : 'T[] = failwith "never"

  static member CreateRecord (properties, cultureStr) : IJsonDocument =  
    let cultureInfo = TextRuntime.GetCulture cultureStr
    let json =
        properties
        |> Array.map (fun (k, v:obj) -> k, JsRuntime.ToJsonValue cultureInfo v)
        |> JsonValue.Record
    JsDocument.Create(json, "")

  static member CreateArray (elements, cultureStr) = 
    let cultureInfo = TextRuntime.GetCulture cultureStr
    let json = 
      elements 
      |> Array.collect (JsRuntime.ToJsonValue cultureInfo
                        >>
                        function JsonValue.Array elements -> elements | JsonValue.Null -> [| |] | element -> [| element |])
      |> JsonValue.Array
    JsDocument.Create(json, "")

  static member CreateValue(value, cultureStr) =
    let cultureInfo = TextRuntime.GetCulture cultureStr
    let json = JsRuntime.ToJsonValue cultureInfo value
    JsDocument.Create(json, "")


// --------------------------------------------------------------------------------------
// Define the mappings

open System.IO
open FunScript.Data.Utils
open Microsoft.FSharp.Quotations

let getComponents() = 
  [ // Document is parsed into JS object
    ExpressionReplacer.createUnsafe <@ JsonValue.Parse @> <@ JsRuntime.Parse @>
    ExpressionReplacer.createUnsafe <@ fun (d:JsonDocument) -> d.JsonValue @> <@ JsRuntime.Identity @>
    ExpressionReplacer.createUnsafe <@ fun (d:JsonValueOptionAndPath) -> d.JsonOpt @> <@ JsRuntime.Identity @>
    ExpressionReplacer.createUnsafe <@ fun (d:JsonValueOptionAndPath) -> d.Path @> <@ JsRuntime.EmptyString @>
    ExpressionReplacer.createUnsafe <@ fun s -> new StringReader(s) @> <@ JsRuntime.Identity @>

    // Turn 'new JsonDocument(...)' to just 'return {0}'
    ExpressionReplacer.createUnsafe <@ JsonDocument.Create:JsonValue*string->IJsonDocument @> <@ JsRuntime.Identity1Of2 @>

    CompilerComponent.create <| fun (|Split|) compiler returnStategy ->
      function
      | Patterns.Call(None, mi, _) when mi.Name = "Some" && mi.DeclaringType.Name = "FSharpOption`1" ->
         [ yield returnStategy.Return <| FunScript.AST.Null ] /// TODO!!!!!
      | _ -> []

    ExpressionReplacer.createUnsafe <@ TextRuntime.GetCulture @> <@ JsRuntime.GetCulture @>

    // Get property using indexer
    ExpressionReplacer.createUnsafe <@ JsonRuntime.GetPropertyPacked @> <@ JsRuntime.GetProperty @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.GetPropertyPackedOrNull @> <@ JsRuntime.GetProperty @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.TryGetPropertyPacked @> <@ JsRuntime.TryGetProperty @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.TryGetPropertyUnpacked @> <@ JsRuntime.TryGetProperty @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.TryGetPropertyUnpackedWithPath @> <@ JsRuntime.TryGetProperty @>

    // Conversion becomes just identity
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertString @> <@ JsRuntime.Identity2Of2 @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertBoolean @> <@ JsRuntime.Identity2Of2 @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertFloat @> <@ JsRuntime.Identity3Of3 @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertDecimal @> <@ JsRuntime.Identity2Of2 @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertInteger @> <@ JsRuntime.Identity2Of2 @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertInteger64 @> <@ JsRuntime.Identity2Of2 @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertDateTime @> <@ JsRuntime.Identity2Of2 @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertGuid @> <@ JsRuntime.Identity @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.GetNonOptionalValue @> <@ JsRuntime.GetNonOptionalValue @>

    // Functions that do something tricky are reimplemented
    ExpressionReplacer.createUnsafe <@ JsonRuntime.GetArrayChildrenByTypeTag @> <@ JsRuntime.GetArrayChildrenByTypeTag @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertArray @> <@ JsRuntime.ConvertArray @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.ConvertOptionalProperty @> <@ JsRuntime.ConvertOptionalProperty @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.TryGetArrayChildByTypeTag @> <@ JsRuntime.TryGetArrayChildByTypeTag @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.GetArrayChildByTypeTag @> <@ JsRuntime.GetArrayChildByTypeTag @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.TryGetValueByTypeTag @> <@ JsRuntime.TryGetValueByTypeTag @>
        
    ExpressionReplacer.createUnsafe <@ JsonRuntime.CreateValue @> <@ JsRuntime.CreateValue @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.CreateArray @> <@ JsRuntime.CreateArray @>
    ExpressionReplacer.createUnsafe <@ JsonRuntime.CreateRecord @> <@ JsRuntime.CreateRecord @>
  ]