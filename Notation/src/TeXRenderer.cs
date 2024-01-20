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
	float default_size;

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
		
		// tranverse and set x, y, scale
		foreach(var node in nodes) {
			switch(node) {
				case ENumber number: {
					number.X = x;
					number.Y = y;
					number.W = paint.MeasureText(number.str);
					number.H = scale * default_size;
					number.Scale = scale;
					x += number.W;
					// Console.WriteLine($"number {number.str} x: {number.X}, y: {number.Y}");
				}break;
								
				case EIdentifier id: {
					id.X = x;
					id.Y = y;
					id.W = paint.MeasureText(id.str);
					id.H = scale * default_size;	
					id.Scale = scale;

					x += id.W;
				}break;
				
				case EMathOperator op: {
					var w = paint.MeasureText(op.str);
					x += w / 2f;
					op.X = x;
					op.Y = y;
					op.W = w;
					op.H = scale * default_size;
					op.Scale = scale;

					x += op.W + w / 2f;
				}break;
				
				case ESymbol symbol: {
					symbol.X = x;
					symbol.Y = y;
					symbol.W = paint.MeasureText(symbol.str);
					symbol.H = scale * default_size;
					symbol.Scale = scale;

					x += symbol.W;
				}break;
				
				case ESpace space: {
					space.X = x;
					space.Y = y;
					space.W = paint.MeasureText(space.str);
					space.H = scale * default_size;
					space.Scale = scale;

					x += space.W + offset;
				}break;
				
				case EBinary binary: {					
					// a LOT trial and error on this part...
					var upper = (EGrouped)(binary.lhs);
					var lower = (EGrouped)(binary.rhs);

					var upper_size = MeasureSizeAbs(upper.exprs, x, y, scale);
					var lower_size = MeasureSizeAbs(lower.exprs, x, y, scale);

					var upper_w = upper_size.W - upper_size.X;
					var upper_h = upper_size.H - upper_size.Y;
					var lower_w = lower_size.W - lower_size.X;
					var lower_h = lower_size.H - lower_size.Y;
					var width = Math.Max(upper_w, lower_w);
				
					var upper_x = (upper_w < 0.9 * width) ? (x + (width - upper_w) / 2f) : x;
					var lower_x = (lower_w < 0.9 * width) ? (x + (width - lower_w) / 2f) : x;

					var upper_y = y + (y - upper_size.Y) + 4f;   // small offset
					var lower_y = y - (lower_size.H - y) - 4f;  // small offset
					
					TypesetRec(upper.exprs, new Vector2(upper_x, upper_y), scale);
					TypesetRec(lower.exprs, new Vector2(lower_x, lower_y), scale);

					binary.X = x;
					binary.Y = y;
					binary.W = width;
					binary.H = upper_h + lower_h + 8f;  // small offset
					binary.Scale = scale;
					binary.RuleY = y;

					x += binary.W;
				}break;
				
				case EGrouped g: {					
					TypesetRec(g.exprs, new Vector2(x, y), scale);
					// var size = MeasureSize(g.exprs);

					g.X = x;
					g.Y = y;
					// g.W = size.W;
					// g.H = size.H;
					g.Scale = scale;
					// x += g.W;  // offset
				}break;				
				
				case EUp e: {
					if(e.up is EGrouped group) {
						TypesetRec(group.exprs, new Vector2(x, y + offset / 2f), scale * 0.8f);
						var size = MeasureSize(group.exprs);

						x = size.W;
					}
					else {
						var w = paint.MeasureText(e.up.TeXStr) + 3f;
						e.up.X = x;
						e.up.Y = y + offset / 2f;
						e.up.W = w;
						e.up.H = scale * default_size;
						e.up.Scale = scale * 0.8f;

						x += w;
					}
				}break;
				
				case EDown e: {
					if(e.down is EGrouped group) {
						TypesetRec(group.exprs, new Vector2(x, y - offset / 2f), scale * 0.8f);		
						var size = MeasureSize(group.exprs);

						x = size.W;
					}
					else {
						var w = paint.MeasureText(e.down.TeXStr) + 3f;
						e.down.X = x;
						e.down.Y = y - offset / 2f;
						e.down.W = w;
						e.down.H = scale * default_size;
						e.down.Scale = scale * 0.8f;

						x += w;
					}
				}break;
				
				default: {
					throw new NotImplementedException($"{node.GetType().ToString()} not implemented");
				};
			} 
		}	
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

				case EUp e: {
					if(e.up is EGrouped group) MeasureSizeRec(group.exprs, ref size);
					else {
						size.X = Math.Min(size.X, e.up.X);
						size.Y = Math.Min(size.Y, e.up.Y);
						size.W = Math.Max(size.W, e.up.X + e.up.W);
						size.H = Math.Max(size.H, e.up.Y + e.up.H);
					}
				}break;

				case EDown e: {
					if(e.down is EGrouped group) MeasureSizeRec(group.exprs, ref size);
					else {
						size.X = Math.Min(size.X, e.down.X);
						size.Y = Math.Min(size.Y, e.down.Y);
						size.W = Math.Max(size.W, e.down.X + e.down.W);
						size.H = Math.Max(size.H, e.down.Y + e.down.H);
					}break;
				}

				default: {
					size.X = Math.Min(size.X, node.X);
					size.Y = Math.Min(size.Y, node.Y);
					size.W = Math.Max(size.W, node.X + node.W);
					size.H = Math.Max(size.H, node.Y + node.H);
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
		};
	}

	///<summary>returns the the xmin, ymin, xmax, ymax as a HBox</summary>
	public HBox MeasureSizeAbs(List<Expr> hlist, float x, float y, float scale)
	{
		var size = new HBox{X = x, Y = y};

		TypesetRec(hlist, new Vector2(x, y), scale);
		MeasureSizeRec(hlist, ref size);

		return size;
	}

	// Typeset hlist, with no x, y offset, in order to get the W, H
	// public HBox MeasureSizeFromOrigin(List<Expr> hlist, float scale)
	// {
	// 	TypesetRec(hlist, Vector2.Zero, scale);
	// 	var size = MeasureSize(hlist);

	// 	return size;		
	// }

	private void MeasureBinaryLevelsRec(List<Expr> hlist, ref int lv)
	{
		foreach(var node in hlist) {
			if(node is EBinary binary) {
				lv += 2;
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

		var size = MeasureSizeAbs(hlist, 0f, 0f, 1f);
		var width = (int)(size.W - size.X + default_size);
		var height = (int)(size.H - size.Y + default_size);

		//offset +Ymin
		TypesetRec(hlist, new Vector2(0, Math.Abs(size.Y)), 1f);
		
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

