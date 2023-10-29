using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using System.Numerics;
using System.Linq;
using static System.Diagnostics.Debug;

namespace Notation;

public class Typesetting
{
	const string latex = "f(x) = G_z + K_a^b + (B_3 - 9.03)";

	public static void Main()
	{
		// CreateImage();

		
		// using var stream = File.Create("simple_str.png");
		using var memory = new MemoryStream(8 * 1023);
		// using var writer = new StreamWriter(memory);
		
		var dt0 = System.Diagnostics.Stopwatch.GetTimestamp();
		
		for(int i = 0; i < 100; i++)
			memory.Position = 0;
			NotationSample(latex, memory);
			
		var dt1 = System.Diagnostics.Stopwatch.GetTimestamp();

		NotationSample(latex, memory);
		using var fs = File.Create("copy_stream.png");
		memory.Position = 0;
		memory.CopyTo(fs);

		var dt = (dt1 - dt0) / 1000;
		Console.WriteLine("dt: {0}us", (float)(dt / 100));

		
		using var fs2 = File.Create("img.png");
		NotationSample("f = g_3 + A^2", fs2);
	}

	[UnmanagedCallersOnly(EntryPoint = "parse_to_notation")]
	public static unsafe void parse_to_notation(byte* str, byte* dest_str) 
	{
		var _str = Marshal.PtrToStringUTF8((nint)str) ?? throw new Exception("invalid str");
		var filename = Marshal.PtrToStringUTF8((nint)dest_str) ?? throw new Exception("invalid dest path");

		Console.WriteLine($"to filepath: -{filename}-");
		Console.WriteLine($"to filepath: -{_str}-");
		using var fs = File.Create(filename);
		NotationSample(_str, fs);

	}
	

	public static void NotationSample(string str, Stream dest)
	{	
		using var katex_main = SKTypeface.FromFile("fonts/KaTeX_Main-Regular.ttf")
			?? throw new FileNotFoundException();

	    // set up drawing tools
	    using var paint = new SKPaint() 
		{
			Typeface = katex_main,
	        IsAntialias = true,
			TextSize = 24f,
	    };
		
		var notations = RenderNotation(str, paint);
		
		var info = new SKImageInfo(420, 60);
		using var surface = SKSurface.Create(info); 
		var canvas = surface.Canvas;

		foreach(var notation in notations)
		{
			var temp = paint.TextSize;
			paint.TextSize = notation.Size;
			canvas.DrawText(notation.Str, notation.X, notation.Y, paint);
			paint.TextSize = temp;
		}		
		
		using (var image = surface.Snapshot())
		using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
		{
			var stream = dest ?? throw new NullReferenceException("stream cannot be null");
		    data.SaveTo(stream);
		}
	}
	
	static List<Notation> RenderNotation(string src, SKPaint default_paint)
	{
		// var notations = new List<Notation>(10);
		// var parser = new Parser(src);
		// // parser.Parse();
		// const float Y = 30f;
		// var x = 5f;
		// var y = Y;
		// var prev_level = 0;

		// foreach(var token in parser.Tokens)
		// {
		// 	var substring = src.Substring(token.A, token.B - token.A);			
		// 	var str = string.Empty;
		// 	if(TexSymbols.IsDiacritical(substring, out string diacritical)) 
		// 	{
		// 		str = diacritical;
		// 	}
		// 	else if(TexSymbols.IsSymbol(substring, out string symbol))
		// 	{
		// 		str = symbol;
		// 	}
		// 	else 
		// 	{
		// 		str = substring;
		// 	}
			
		// 	switch(token.Level)
		// 	{
		// 		case -1:
		// 			y = Y + 8;
		// 			break;
					
		// 		case 0:
		// 			y = Y;
		// 			break;
					
		// 		case 1:
		// 			y = Y - 8;
		// 			break;
					
		// 		default:
		// 			y = Y;				
		// 			break;
		// 	}

		// 	var size = token.Level switch
		// 	{
		// 		-1 => 12f,
		// 		 0 => 16f,
		// 		+1 => 12f,
		// 		 _ => 8f,
		// 	};
			
		// 	notations.Add(new Notation(str, x, y, null, size));
		// 	var temp = default_paint.TextSize;
		// 	default_paint.TextSize = size;
		// 	var offset = default_paint.MeasureText(str);
			
		// 	if(Math.Sign(prev_level) == Math.Sign(token.Level))
		// 	{
		// 		x += offset;						
		// 	}
		// 	else if(Math.Sign(prev_level) == 0 && Math.Sign(token.Level) != 0)
		// 	{
		// 		x += offset;
		// 	}
		// 	else if(Math.Sign(token.Level) == 0 && Math.Sign(prev_level) != 0)
		// 	{
		// 		x += offset;
		// 	}
		// 	else if(Math.Abs(prev_level - token.Level) > Math.Abs(prev_level) || 
		// 		Math.Abs(prev_level - token.Level) > Math.Abs(token.Level))
		// 	{
		// 		x += 0;
		// 	}
		// 	default_paint.TextSize = temp;							
			
		// 	prev_level = token.Level;
		// }	

		// return notations;

		return null;
	}
	
	public static void CreateImage()
	{
		var info = new SKImageInfo(640, 480);
		using (var surface = SKSurface.Create(info)) 
		{
		    SKCanvas canvas = surface.Canvas;

			using var katex_main = SKTypeface.FromFile("fonts/KaTeX_Main-Regular.ttf")
				?? throw new Exception("file is not found or improper");
			// using var font = new SKFont(katex_main);

		    canvas.Clear(SKColors.White);

		    // set up drawing tools
		    var paint = new SKPaint {
				Typeface = katex_main,
		        IsAntialias = true,
				TextSize = 12f,
		        Color = new SKColor(0x2c, 0x3e, 0x50),
		        StrokeCap = SKStrokeCap.Round
		    };

		    // create the Xamagon path
		    var path = new SKPath();
		    path.MoveTo(71.4311121f, 56f);
		    path.CubicTo(68.6763107f, 56.0058575f, 65.9796704f, 57.5737917f, 64.5928855f, 59.965729f);
		    path.LineTo(43.0238921f, 97.5342563f);
		    path.CubicTo(41.6587026f, 99.9325978f, 41.6587026f, 103.067402f, 43.0238921f, 105.465744f);
		    path.LineTo(64.5928855f, 143.034271f);
		    path.CubicTo(65.9798162f, 145.426228f, 68.6763107f, 146.994582f, 71.4311121f, 147f);
		    path.LineTo(114.568946f, 147f);
		    path.CubicTo(117.323748f, 146.994143f, 120.020241f, 145.426228f, 121.407172f, 143.034271f);
		    path.LineTo(142.976161f, 105.465744f);
		    path.CubicTo(144.34135f, 103.067402f, 144.341209f, 99.9325978f, 142.976161f, 97.5342563f);
		    path.LineTo(121.407172f, 59.965729f);
		    path.CubicTo(120.020241f, 57.5737917f, 117.323748f, 56.0054182f, 114.568946f, 56f);
		    path.LineTo(71.4311121f, 56f);
		    path.Close();

		    // draw the Xamagon path
		    canvas.DrawPath(path, paint);

			canvas.DrawText("some text", 290f, 120f, paint);
			
			using (var image = surface.Snapshot())
			using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
			using (var stream = File.Create("simple_img.png"))			
			{
			    // save the data to a stream
			    data.SaveTo(stream);
			}
		}
	}
}
