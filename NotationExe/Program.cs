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
		var str = "f(x) = 4.3 \\cdot x^{3A_z} \\cdot \\Gamma - N_A + \\frac{A + D}{B - G} + (A - B_\\gamma)";
		var lexer = new Lexer(str);

		Console.WriteLine(char.IsWhiteSpace('\0'));

		var str0 = "some string111";
		Console.WriteLine(str0[2..str0.Length]);

		foreach(var token in lexer) {
		    if(token.Id == TokenId.Bad || token.Id == TokenId.Whitespace) continue;

		    Console.WriteLine("{0},  {1}", token.Str, token.Id);
		}

		var parser = new Parser(str);
        var hlist = parser.Parse().ToList();
        Console.WriteLine($"target_str: {str}");

		// parser.Print();


		using var ms = new MemoryStream(8 * 1024);
		// var dt0 = Stopwatch.GetTimestamp();
		
		// for(int i = 0; i < 1000; i++) {
		// 	 ms.Position = 0;
		// 	 using var renderer = new TeXRenderer(hlist, 20f);
		//	 renderer.TypesetRootHList(new Vector2(30, 40));
		// 	 // renderer.Print();
		//	 renderer.Render(ms);			
		// }

		// var dt1 = Stopwatch.GetTimestamp();
		// var dt = (float)(dt1 - dt0) / 1000f;
		// Console.WriteLine("elapsed time: {0}ms", dt / 1000f);

		using var renderer = new TeXRenderer(hlist, 20f);
		renderer.TypesetRootHList(new Vector2(30, 30));
		renderer.Print();
		renderer.Render(ms);
		using var fs = File.Create("test image.png");
		ms.Position = 0;
		ms.CopyTo(fs);
  		Console.WriteLine("done!");
	}
} 
