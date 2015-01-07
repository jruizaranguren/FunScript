module FunScript.Data.Tests.JsonProvider

open NUnit.Framework
open JsLib
open FunScript
open FunScript.Data.Components
open System
open System.Reflection
open System.IO

let strDec = """ { "year": 2013, "id": "f1b4d108-5dac-b6ff-0645-781c00000001", "taxpayer": { "facts": { "empl": { "monetary": [ { "vat": "A08099459", "name": "CORTEFIEL SA", "withholding": 2500.2, "remuneration": 24000.3 }, { "vat": "A08057895", "name": "FCC LOGISTICA SA", "withholding": 200.1, "remuneration": 1300.45 } ], "inkind": [ { "vat": "A08057895", "name": "FCC LOGISTICA SA", "withholding": 420.1, "remuneration": 1200.35 } ], "socialSecurity": 480.47, "deduction": 500.2 } }, "mail": "micorreo@gmail.com", "nid": "34432265T", "familyName": "Toxi", "Surname": "Cómano", "name": "Segismundo", "birthDate": "01/08/1974", "sex": "NotMuch", "city": "Pamplona" } } """
let dec = Declaration.Parse(strDec)

let getDir () = 
    Assembly.GetExecutingAssembly().CodeBase
    |> fun dir -> UriBuilder(dir).Path
    |> Path.GetDirectoryName

[<Test>] 
let ``build correct provided type from json string`` () =
    Assert.That("Segismundo", Is.EqualTo dec.Taxpayer.Name)

[<Test>] 
let  ``can compile createRecord`` () = 
    Assert.That(
        (Compiler.Compiler.Compile(<@ JsLib.main() @>,getDataProviders())), 
        Is.Not.Empty)

    
            
