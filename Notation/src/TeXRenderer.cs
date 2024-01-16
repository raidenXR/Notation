using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using System.Numerics;
using System.Linq;
using static System.Diagnostics.Debug;


namespace Notation;

public readonly struct Origin
{
	public readonly float X;
	public readonly float Y;
	public readonly float Scale;
	public readonly float DefaultScale;

	public Origin(float x, float y, float scale, float defaultScale) {
		X = x;
		Y = y;
		Scale = scale;
		DefaultScale = defaultScale;
	}
}

public class TeXRenderer : IDisposable
{	
	readonly float  font_size;
	bool is_typeset;
	bool is_disposed;

	SKTypeface typeface;
	SKPaint paint;

	static readonly List<SKTypeface> typefaces = new(20);

	static readonly string[] names = new string[]{
		"Main-Regular",	
		"AMS-Regular",
		"Caligraphic-Regular",		
		"Fraktur-Regular",		
		"SansSerif-Regular",		
		"Script-Regular",		
		"Math-Italic",		
		"Size1-Regular",		
		"Size2-Regular",		
		"Size3-Regular",		
		"Size4-Regular",		
		"Typewritter-Regular",				
	};
	
	static readonly string[] fontnames = new string[]{
		"fonts/KaTeX_Main-Regular.ttf",	
		"fonts/KaTeX_AMS-Regular.ttf",
		"fonts/KaTeX_Caligraphic-Regular.ttf",		
		"fonts/KaTeX_Fraktur-Regular.ttf",		
		"fonts/KaTeX_SansSerif-Regular.ttf",		
		"fonts/KaTeX_Script-Regular.ttf",		
		"fonts/KaTeX_Math-Italic.ttf",		
		"fonts/KaTeX_Size1-Regular.ttf",		
		"fonts/KaTeX_Size2-Regular.ttf",		
		"fonts/KaTeX_Size3-Regular.ttf",		
		"fonts/KaTeX_Size4-Regular.ttf",		
		"fonts/KaTeX_Typewritter-Regular.ttf",		
	};

	// Expr? ast_root = null;
	// List<Expr>? root_list = null;

	// public List<Expr>? RootList 
	// {
	// 	get => root_list;
	// 	set {
	// 		root_list = value;
	// 	}
	// }

	float default_size;

	static TeXRenderer()
	{
		foreach(var fontname in fontnames) 
		{
			typefaces.Add(SKTypeface.FromFile(fontname));
		}		
	}

	public TeXRenderer(float fontSize)
	{
		default_size = fontSize;
		font_size = default_size;
		typeface = typefaces.First();
	    paint = new SKPaint	{
			Typeface = typeface,
	        IsAntialias = true,
			TextSize = default_size,
			Color = SKColors.Black,
	    };
	}

	~TeXRenderer()
	{
		Dispose();
	}

	public void Dispose()
	{
		if(!is_disposed) {
			// typeface.Dispose();
			paint.Dispose();
		}
		is_disposed = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static SKPoint Transform(float x, float y, HBox size)
	{
		var _x = Math.Abs(size.X) + x;
		var _y = (size.H - size.Y) - y;

		return new SKPoint(_x, _y);
	}

	private void print_atom(Expr node, string padding, HBox size) {
		Assert(node.IsLeafNode());		
		
		var pt = Transform(node.X, node.Y, size);
		// var pos_str = $"{node.GetType(), -20},   pos: X= {node.X, -9}, Y= {node.Y, -9}, W= {node.W, - 9} scale: {node.Scale}";
		var pos_str = $"{node.GetType(), -20},   pos: X= {pt.X, -9}, Y= {pt.Y, -9}, W= {node.W, - 9} scale: {node.Scale}";
		Console.WriteLine($"{padding + node.Str, -5}" +  pos_str);
	}

	private void print(List<Expr> nodes, string padding, HBox size) {
		foreach(var node in nodes) {
			switch(node) {
				case EBinary binary: {
					var upper = (EGrouped)(binary.lhs);
					print(upper.exprs, "--" + padding, size);
				
					var lower = (EGrouped)(binary.rhs);
					print(lower.exprs, "--" + padding, size);
				}break;
				
				case EGrouped group: {
					print(group.exprs, "--" + padding, size);
				}break;

				case EUp up: {
					if(up.up is EGrouped g) print(g.exprs, "--" + padding, size);
					else print_atom(up.up, padding, size);
				}break;

				case EDown down: {
					if(down.down is EGrouped g) print(g.exprs, "--" + padding, size);
					else print_atom(down.down, padding, size);
				}break;
				
				default: {
					print_atom(node, padding, size);
				}break;						
			}
		}
	}

	public void Print(List<Expr> root_list)
	{
		Assert(root_list != null);

		var size = MeasureSize(root_list);
	
		print(root_list, "", size);
	}


	// public void Typeset(List<Expr> nodes, Vector2 root_pos)
	// {	
	// 	Typeset(nodes, new Origin(root_pos.X, root_pos.Y, 1f, default_size));

	// 	is_typeset = true;
	// }

	private void TypesetRec(List<Expr> nodes, Vector2 pos, float scale) 
	{
		var x = pos.X;
		var y = pos.Y;
		var offset = scale * default_size; 
		var n = 0;
		
		// tranverse and set x, y, scale
		foreach(var node in nodes) {
			switch(node) {
				case ENumber number: {
					number.X = x;
					number.Y = y;
					number.W = paint.MeasureText(number.str);
					number.Scale = scale;
					x += number.W;
					// Console.WriteLine($"number {number.str} x: {number.X}, y: {number.Y}");
				}break;
								
				case EIdentifier id: {
					id.X = x;
					id.Y = y;
					id.W = paint.MeasureText(id.str);	
					id.Scale = scale;

					x += id.W;
				}break;
				
				case EMathOperator op: {
					var w = paint.MeasureText(op.str);
					x += w / 2f;
					op.X = x;
					op.Y = y;
					op.W = w;
					op.Scale = scale;

					x += op.W + w / 2f;
				}break;
				
				case ESymbol symbol: {
					symbol.X = x;
					symbol.Y = y;
					symbol.W = paint.MeasureText(symbol.str);
					symbol.Scale = scale;

					x += symbol.W;
				}break;
				
				case ESpace space: {
					space.X = x;
					space.Y = y;
					// use same font as symbols - italics ??
					space.W = paint.MeasureText(space.str);
					space.Scale = scale;

					x += space.W + offset;
				}break;
				
				case EBinary binary: {					
					// a LOT trial and error on this part...
					var upper = (EGrouped)(binary.lhs);
					var lower = (EGrouped)(binary.rhs);

					var upper_lvs = MeasureBinaryLevels(upper.exprs);
					var lower_lvs = MeasureBinaryLevels(lower.exprs);
					
					TypesetRec(upper.exprs, new Vector2(x, y + default_size * scale * upper_lvs), scale);
					TypesetRec(lower.exprs, new Vector2(x, y - default_size * scale * lower_lvs), scale);

					var upper_size = MeasureSize(upper.exprs);
					var lower_size = MeasureSize(lower.exprs);

					binary.X = x;
					binary.Y = y;
					binary.W = Math.Max(upper_size.W - x, lower_size.W - x);
					binary.Scale = scale;
					binary.RuleY = y + (scale * default_size / 4f);

					x += binary.W;
				}break;
				
				case EGrouped g: {					
					TypesetRec(g.exprs, new Vector2(x, y), scale);
					var last = g.exprs.Last();

					g.X = x;
					g.Y = y;
					g.W = last.X + last.W - x;
					x += g.W;  // offset
				}break;
				
				case ESub sub: {
					throw new NotImplementedException();
					// sub.X = x - default_size * scale;
					// sub.Y = y - default_size * scale;
					// sub.Scale = scale * 0.8f;
					// sub.X = x - font_size * 0.75f;
					// sub.Y = y - font_size;
				}break;
				
				case ESuper super: {
					throw new NotImplementedException();
					// super.X = x - default_size * scale; 
					// super.Y = y + default_size * scale;
					// super.Scale = scale * 0.8f;
					// super.X = x - font_size * 0.75f;
					// super.Y = y + font_size;
				}break;
				
				case ESubsup su: {
					
				}break;
				
				case EOver over: {
					
				}break;
				
				case EUnder under: {
					
				}break;
				
				case EUnderover uo: {
					
				}break;
				
				case EUp up: {
					if(up.up is EGrouped group) {
						// Typeset(group.exprs, new Vector2(x, y - (default_size / 4f)), 0.8f * scale);		
						var _scale = scale * 0.8f;
						TypesetRec(group.exprs, new Vector2(x, y + (default_size * _scale)), _scale);		
						
						var last = group.exprs.Last();
						x += last.X + last.W - x;
					}
					else {
						up.up.X = x;
						up.up.Y = y + offset / 2f;
						up.up.W = paint.MeasureText(up.up.TeXStr);
						up.up.Scale = scale * 0.8f;

						x += up.up.W;

						up.X = up.up.X;
						up.Y = up.up.Y;
						up.W = up.up.W;
						up.Scale = up.up.Scale;
					}
				}break;
				
				case EDown down: {
					if(down.down is EGrouped group) {
						TypesetRec(group.exprs, new Vector2(x, y - (default_size / 4f)), 0.8f * scale);

						var last = group.exprs.Last();
						x += last.X + last.W - x;
					}
					else {
						down.down.X = x;
						down.down.Y = y - offset / 2f;
						down.down.W = paint.MeasureText(down.down.TeXStr);
						down.down.Scale = scale * 0.8f;

						x += down.down.W;

						down.X = down.down.X;
						down.Y = down.down.Y;
						down.W = down.down.W;
						down.Scale = down.down.Scale;
					}
				}break;
				
				case EDownUp du: {
					
				}break;
				
				case EUnary unary: {
					
				}break;
				
				case EScaled scaled: {
					var next_node = nodes[n + 1];
					next_node.Scale *= scaled.scale;
				}break;
				
				case EStretchy stretchy: {
					
				}break;
				
				case EArray array: {
					
				}break;
				
				case EText text: {
					
				}break;
				
				default: {
					throw new ArgumentException($"not implementing {node.ToString()}");					
				};
			} 
		}	
		n += 1;
	}


	/// <summary> 
	/// assumes the plainest cast whehe TeXRenderer is inialialized with the simple, empty ctor
	/// sets the hlist as the root_list of the Notation
	/// </summary>
	public void Typeset(List<Expr> hlist)
	{
		Assert(hlist != null);

		var pos = new Vector2(default_size, default_size);
		TypesetRec(hlist, pos, 1f);

		is_typeset = true;		
	}


	private void MeasureSizeRec(List<Expr> hlist, ref HBox size)
	{
		foreach(var node in hlist) {
			switch(node) {
				case EBinary binary: {
					var upper = (EGrouped)(binary.lhs);
					MeasureSizeRec(upper.exprs, ref size);

					var lower = (EGrouped)(binary.rhs);
					MeasureSizeRec(lower.exprs, ref size);					
				}break;

				case EGrouped group: {
					MeasureSizeRec(group.exprs, ref size);
				}break;

				case EUp up: {
					if(up.up is EGrouped group) MeasureSizeRec(group.exprs, ref size);
					else {
						size.X = Math.Min(size.X, up.X);
						size.Y = Math.Min(size.Y, up.Y);
						size.W = Math.Max(size.W, up.X + up.W);
						size.H = Math.Max(size.H, up.Y + up.W);
					}
				}break;

				case EDown down: {
					if(down.down is EGrouped group) MeasureSizeRec(group.exprs, ref size);
					else {
						size.X = Math.Min(size.X, down.X);
						size.Y = Math.Min(size.Y, down.Y);
						size.W = Math.Max(size.W, down.X + down.W);
						size.H = Math.Max(size.H, down.Y + down.W);
					}break;
				}

				default: {
					size.X = Math.Min(size.X, node.X);
					size.Y = Math.Min(size.Y, node.Y);
					size.W = Math.Max(size.W, node.X + node.W);
					size.H = Math.Max(size.H, node.Y + node.W);
				}break;
			}
		}	
	}

	public HBox MeasureSize(List<Expr> hlist) 
	{
		var size = new HBox();

		MeasureSizeRec(hlist, ref size);

		return new HBox{
			X = 0f,
			Y = 0f,
			W = Math.Abs(size.W - size.X),
			H = Math.Abs(size.H - size.Y),
			// Depth = 0f,
		};
	}

	private void MeasureBinaryLevelsRec(List<Expr> hlist, ref int lv)
	{
		foreach(var node in hlist) {
			if(node is EBinary binary) {
				lv += 1;
				var upper = (EGrouped)(binary.lhs);
				MeasureBinaryLevelsRec(upper.exprs, ref lv);

				var lower = (EGrouped)(binary.rhs);
				MeasureBinaryLevelsRec(lower.exprs, ref lv);
			}
		}
	}

	public int MeasureBinaryLevels(List<Expr> hlist)
	{
		int lv = 1;
		MeasureBinaryLevelsRec(hlist, ref lv);

		return lv;
	}

	///<summary>must be used only on leaf-nodes expressions</summary>
	private void RenderAtom(SKCanvas canvas, Expr atom, HBox size)
	{
		Assert(atom.IsLeafNode());
		
		var temp_size = paint.TextSize;
		paint.TextSize *= atom.Scale;
		var rendered = false;
		var pt = Transform(atom.X, atom.Y, size);

		foreach(var typeface in typefaces) {
			if(typeface.ContainsGlyphs(atom.TeXStr)) {
				paint.Typeface = typeface;
				canvas.DrawText(atom.TeXStr, pt.X, pt.Y, paint);
				rendered = true;
				break;
			}
		}
		if(!rendered) canvas.DrawText(atom.TeXStr, pt.X, pt.Y, paint);
		paint.TextSize = temp_size;		
		paint.Typeface = typeface;
	}

	private void RenderHList(SKCanvas canvas, List<Expr> hlist, HBox size)
	{
		foreach(var node in hlist) {
			switch(node) {
				case EBinary binary: {
					var upper = (EGrouped)(binary.lhs);
					RenderHList(canvas, upper.exprs, size);

					var lower = (EGrouped)(binary.rhs);
					RenderHList(canvas, lower.exprs, size);

					// var y = binary.Scale * default_size / 4f;
					// var p0 = Transform(binary.X, binary.Y + y, size);
					// var p1 = Transform(binary.X + binary.W, binary.Y + y, size);
					var p0 = Transform(binary.X, binary.RuleY, size);
					var p1 = Transform(binary.X + binary.W, binary.RuleY, size);
					Console.WriteLine($"line: {p0.X}, {p1.X}, {p1.Y}");

					// var y0 = binary.Y + (binary.Scale * font_size) / 4f;
					// var p0 = new SKPoint{X = binary.X, Y = binary.Y  - y};
					// var p1 = new SKPoint{X = binary.X + binary.W, Y = binary.Y - y};
					canvas.DrawLine(p0, p1, paint);
				}break;
				
				case EGrouped group: {
					RenderHList(canvas, group.exprs, size);
				}break;
				
				case EUp up: {
					// Console.WriteLine("up.up = {0}", up.up.Str);
					if(up.up is EGrouped group)	RenderHList(canvas, group.exprs, size);
					else RenderAtom(canvas, up.up, size);	
				}break;
				 				
				case EDown down: {
					// Console.WriteLine("down.down = {0}", down.down.Str);
					if(down.down is EGrouped group) RenderHList(canvas, group.exprs, size);
					else RenderAtom(canvas, down.down, size);
				}break;			

				default: {
					RenderAtom(canvas, node, size);
				}break;
			}			
		}
	}


	/// <summary>copy a .png encoded image of the notation to dest_stream</summary>
	public void Render(Stream dest_stream, List<Expr> hlist)
	{
		Assert(dest_stream != null);
		Assert(hlist != null);
		Assert(is_typeset);

	    // set up drawing tools		

		var size = MeasureSize(hlist);
		var width = (int)(size.W + 2f * default_size);
		var height = (int)(size.H + 2f * default_size);
		
		var info = new SKImageInfo(width, height);
		using var surface = SKSurface.Create(info);		
		var canvas = surface.Canvas;		
		
		RenderHList(canvas, hlist, size);
		using (var image = surface.Snapshot())
		using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
		{
		    data.SaveTo(dest_stream);
		}
	}

	///<summary>returns the snapshot of the surface that contains the notation</summary>
	public SKImage SnapshotNotationImg(List<Expr> hlist)
	{
		Assert(hlist != null);
		Assert(is_typeset);

		var size = MeasureSize(hlist);
		var width = (int)(size.W + 2f * default_size);
		var height = (int)(size.H + 2f * default_size);
		
		var info = new SKImageInfo(width, height);
		using var surface = SKSurface.Create(info);		
		var canvas = surface.Canvas;
				
		RenderHList(canvas, hlist, size);

		return surface.Snapshot();
	}
}

