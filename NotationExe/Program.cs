using Notation;
using System;
using System.Numerics;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace NotationTests;


class Program
{
	public static void Main()
	{

		var functions = new string[] {
		    "g(x) = \\int_a^b \\frac{1}{2} x^2 dx",
			"f(x) = 4.3 \\cdot x^{3A_z} \\cdot \\Gamma - N_A + \\frac{A + D}{B - G} + (A - B_{\\gamma})",
			"f(x) = \\int _a ^b \\frac{(A + B - 94.3)}{e^{-RT} \\cdot 9.43 x} dx",
			"f(x) = \\int _a ^b \\frac{(A + B - 94.3)}{e^{-RT \\cdot \\frac{K_l}{N_t}} \\cdot 9.43 x} dx",
			"\\frac{(A-B)}{(e^{-RT})} + \\frac{1}{2}",		
		    "f(x) = (A_n + B_{n + 1}) - \\frac{1}{2} x^2 \\cdot \\gamma",
		    "g(x) = E^{-RT} + 4.213 T - 6.422 T - \\gamma^{-2}",
		    "z(x) = 3.2343 e^{-1.2} + 8.5",
		    "a(x) = \\frac{Z - 9.2 + A^2}{e^{0.8}} + \\frac{x^2 + 2 * x + 1}{x^3 - 1}",
		};

		using var ms = new MemoryStream(8 * 1024);
		var dt0 = Stopwatch.GetTimestamp();
		int i = 0;
		
		foreach(var str in functions) {
			var lexer = new Lexer(str);

			foreach(var token in lexer) {
			    if(token.Id == TokenId.Bad || token.Id == TokenId.Whitespace) continue;

			    Console.WriteLine("{0},  {1}", token.Str, token.Id);
			}
			Console.WriteLine(str);

			var parser = new Parser(str);
	        var hlist = parser.Parse().ToList();
	        Console.WriteLine($"target_str: {str}");			
			Console.WriteLine("\n");
			
			using var fs = File.Create($"test image{i++}.png");
			using var renderer = new TeXRenderer();
			renderer.TypesetRootHList(hlist, new Vector2(30f, 30f));
			renderer.Print();
			ms.Position = 0;
			renderer.Render(ms);
			
			ms.Position = 0;
			ms.CopyTo(fs);
		}

		// parser.Print();
  		Console.WriteLine("done!");		
		var dt1 = Stopwatch.GetTimestamp();
		Console.WriteLine($"dt: {(float)(dt1 - dt0) / (1000f * functions.Length)}ms");
	}
} 
