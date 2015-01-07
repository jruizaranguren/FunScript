﻿[<NUnit.Framework.TestFixture>] 
module FunScript.Tests.PrimitiveTypes

open NUnit.Framework

[<Test>]
let ``Unit is generated as null``() =
   checkAreEqual null <@@ () @@>

[<Test>]
let ``Ints are generated as numbers``() =
   checkAreEqual 1. <@@ 1 @@>

[<Test>]
let ``Chars are generated as strings``() =
   checkAreEqual "a" <@@ 'a' @@>

[<Test>]
let ``Floats are generated as numbers``() =
   checkAreEqual 1. <@@ 1. @@>

[<Test>]
let ``Strings are generated as strings``() =
   check <@@ "test" @@>

[<Test>]
let ``Bools are generated as bools``() =
   check <@@ true @@>

[<Test>]
let ``Decimals are generated as numbers`` () =
    checkAreEqual 1.345 <@@ 1.345M @@>