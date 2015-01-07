namespace FunScript.Data.Tests

[<FunScript.JS>]
module JsLib =
    open FSharp.Data
    open FunScript
    open System

    type Declaration = JsonProvider<"nested.json", EmbeddedResource="FunScript.Data.Tests, nested.json">
    
    let calculate () =
        let empl = Declaration.Empl (
                    [|
                        Declaration.Monetary("A08099459","CORTEFIEL SA",2500.2M,24000.3M)
                        Declaration.Monetary("A08057895","FCC LOGISTICA SA",200.1M,1300.45M)
                    |],
                    [|
                        Declaration.Monetary("A08057895","FCC LOGISTICA SA",420.1M,1200.35M)
                    |],
                    480.47M,
                    500.2M)

//        let facts = Declaration.Facts(empl)
//
//        let taxPayer = new Declaration.Taxpayer(
//                            facts,
//                            "micorreo@gmail.com",
//                            "34432265T",
//                            "Toxi",
//                            "Cómano",
//                            "Segismundo",
//                            DateTime(1974,8,1),
//                            "NotMuch",
//                            "Pamplona")
        empl
        
    
    let main () =
        calculate() |> ignore

  