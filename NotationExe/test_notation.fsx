#r "bin/Debug/net8.0/Notation.dll"

open Notation
open System
open System.Numerics
open System.IO
open System.Linq
open System.Diagnostics

let functions = [
    "g(x) = \\int_a^b \\frac{1}{2} x^2 dx"
    "f(x) = 4.3 \\cdot x^{3A_z} \\cdot \\Gamma - N_A + \\frac{A + D}{B - G} + (A - B_{\\gamma})"
    "f(x) = \\int _a ^b \\frac{(A + B - 94.3)}{e^{-RT} \\cdot 9.43 x} dx"
    "f(x) = \\int _a ^b \\frac{(A + B - 94.3)}{e^{-RT \\cdot \\frac{K_l}{N_t}} \\cdot 9.43 x} dx"
    "\\frac{(A-B)}{(e^{-RT})} + \\frac{1}{2}"
    "f(x) = (A_n + B_{n + 1}) - \\frac{1}{2} x^2 \\cdot \\gamma"
    "g(x) = E^{-RT} + 4.213 T - 6.422 T - \\gamma^{-2}"
    "z(x) = 3.2343 e^{-1.2} + 8.5"
    "a(x) = \\frac{Z - 9.2 + A^2}{e^{0.8}} + \\frac{x^2 + 2 * x + 1}{x^3 - 1}"
    @"f(x) = \frac{R_{\epsilon} + A_1 - -\frac{\frac{C_{\gamma}}{C_B}}{C_C + \frac{(C_{\alpha}) + C_{\alpha}}{C_a}} - R_{\epsilon}}{C_{\beta} - 3.738} / (A_e)"
]

let [<Literal>] dir_name = "notation_images"
Directory.CreateDirectory dir_name |> ignore
use ms = new MemoryStream(8 * 1024)
let dt0 = Stopwatch.GetTimestamp()
let mutable i = 0

for str in functions do
    let lexer = new Lexer(str)
    
    for token in lexer do
        if token.Id = TokenId.Bad || token.Id = TokenId.Whitespace then ignore()
        else Console.WriteLine("{0}, {1}", token.Str, token.Id)

    Console.WriteLine str
    let parser = new Parser(str)
    let hlist = parser.Parse().ToList()
    Console.WriteLine $"target_str: {str}"
    Console.WriteLine "\n"

    let path = Path.Combine(dir_name, $"test image{i + 1}.png")
    use fs = File.Create path
    i <- i + 1
    use renderer = new TeXRenderer()
    renderer.TypesetRootHList(hlist, new Vector2(30f, 30f))
    renderer.Print()
    ms.Position <- 0
    renderer.Render ms
    ms.Position <- 0
    ms.CopyTo fs

Console.WriteLine "done!"
let dt1 = Stopwatch.GetTimestamp()
Console.WriteLine $"dt: {float32 (dt1 - dt0) / float32 (1000 * functions.Length)}ms"    
