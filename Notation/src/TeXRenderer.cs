using System;
using System.Runtime.InteropServices;
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
	readonly string tex_str;
	readonly float  font_size;
	bool is_typeset;
	bool is_disposed;

	SKTypeface typeface;
	SKPaint paint;
	SKSurface surface;

	static readonly List<SKTypeface> typefaces = new(20);
	
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

	Expr? ast_root = null;
	List<Expr>? root_list = null;

	public List<Expr>? RootList 
	{
		get => root_list;
		set {
			root_list = value;
		}
	}

	float default_size;

	static TeXRenderer()
	{
		foreach(var fontname in fontnames) 
		{
			typefaces.Add(SKTypeface.FromFile(fontname));
		}		
	}

	public TeXRenderer(float fontSize = 20f)
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
		var info = new SKImageInfo(420, 100);
		surface = SKSurface.Create(info);		
	}

	public TeXRenderer(List<Expr> root_list, float default_size) 
	{
		font_size = default_size;
		tex_str = "Not A tex_str";
		this.root_list = root_list;
		this.default_size = default_size;

		// typeface = SKTypeface.FromFile("fonts/KaTeX_Main-Regular.ttf") ?? throw new FileNotFoundException();
		typeface = typefaces.First();
		
	    paint = new SKPaint	{
			Typeface = typeface,
	        IsAntialias = true,
			TextSize = default_size,
			Color = SKColors.Black,
	    };
		var info = new SKImageInfo(420, 100);
		surface = SKSurface.Create(info); 
	}
	
	public TeXRenderer(string tex, float default_size)
	{
		tex_str = tex;
		font_size = default_size;
		this.default_size = default_size;
		// typeface = SKTypeface.FromFile("fonts/KaTeX_Main-Regular.ttf") ?? throw new FileNotFoundException();
		typeface = typefaces.First();
		
	    paint = new SKPaint	{
			Typeface = typeface,
	        IsAntialias = true,
			TextSize = default_size,
			Color = SKColors.Black,
	    };
		var info = new SKImageInfo(420, 100);
		surface = SKSurface.Create(info); 
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
			surface.Dispose();
		}
		is_disposed = true;
	}

	private void print_atom(Expr node, string padding) {
		Assert(node.IsLeafNode());		
		
		var pos_str = $"{node.GetType(), -20},   pos: X= {node.X, -9}, Y= {node.Y, -9}, W= {node.W, - 9} scale: {node.Scale}";
		Console.WriteLine($"{padding + node.Str, -5}" +  pos_str);
	}

	private void print(List<Expr> nodes, string padding) {
		foreach(var node in nodes) {
			switch(node) {
				case EBinary binary: {
					var upper = (EGrouped)(binary.lhs);
					print(upper.exprs, "--" + padding);
				
					var lower = (EGrouped)(binary.rhs);
					print(lower.exprs, "--" + padding);
				}break;
				
				case EGrouped group: {
					print(group.exprs, "--" + padding);
				}break;

				case EUp up: {
					if(up.up is EGrouped g) print(g.exprs, "--" + padding);
					else print_atom(up.up, padding);
				}break;

				case EDown down: {
					if(down.down is EGrouped g) print(g.exprs, "--" + padding);
					else print_atom(down.down, padding);
				}break;
				
				default: {
					print_atom(node, padding);
				}break;						
			}
		}
	}

	public void Print()
	{
		Assert(root_list != null);
	
		print(root_list, "");
	}

	/// <summary> 
	/// assumes the plainest cast whehe TeXRenderer is inialialized with the simple, empty ctor
	/// sets the hlist as the root_list of the Notation
	/// </summary>
	public void TypesetRootHList(List<Expr> hlist, Vector2 pos)
	{
		Assert(hlist != null);
		
		Typeset(hlist, pos, 1f);

		this.root_list = hlist;
		is_typeset = true;		
	}

	/// <summary> assumes that the root_hlist is already set </summary>
	public void TypesetRootHList(Vector2 root_pos)
	{
		Assert(root_list != null);
		
		Typeset(root_list, root_pos, 1f);

		is_typeset = true;
	}

	// public void Typeset(List<Expr> nodes, Vector2 root_pos)
	// {	
	// 	Typeset(nodes, new Origin(root_pos.X, root_pos.Y, 1f, default_size));

	// 	is_typeset = true;
	// }

	private void Typeset(List<Expr> nodes, Vector2 pos, float scale) 
	{
		var x = pos.X;
		var y = pos.Y;
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
					// Console.WriteLine($"identifier {id.str} x: {id.X}, y: {id.Y}");
				}break;
				
				case EMathOperator op: {
					op.X = x;
					op.Y = y;
					op.W = paint.MeasureText(op.str);
					op.Scale = scale;
					x += op.W;
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
					space.W = paint.MeasureText(space.str);
					space.Scale = scale;
					x += space.W;
				}break;
				
				case EBinary binary: {
					binary.X = x;
					binary.Y = y;
					binary.Scale = scale;
					
					var upper = (EGrouped)(binary.lhs);
					Typeset(upper.exprs, new Vector2(x, y - 12f), scale);

					var lower = (EGrouped)(binary.rhs);
					Typeset(lower.exprs, new Vector2(x, y + 12f), scale);					

					var upper_last = upper.exprs.Last();
					var upper_w = upper_last.X + upper_last.W - x;
					
					var lower_last = lower.exprs.Last();
					var lower_w = lower_last.X + lower_last.W - x;					

					binary.W = Math.Max(upper_w, lower_w);
					x += binary.W;
				}break;
				
				case EGrouped g: {
					g.X = x;
					g.Y = y;				
					g.Scale = scale;
					
					Typeset(g.exprs, new Vector2(x, y), scale);

					var last = g.exprs.Last();
					g.W = last.X + last.W - x;
					x += g.W;
				}break;
				
				case ESub sub: {
					sub.X = x - 12f;
					sub.Y = y - default_size;
				}break;
				
				case ESuper super: {
					super.X = x - 12f;					
					super.Y = y + default_size;
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
						Typeset(group.exprs, new Vector2(x, y - (default_size / 4f)), 0.8f * scale);						
						
						var last = group.exprs.Last();
						x += last.X + last.W - x;
					}
					else {
						var atom = up.up;
						atom.X = x;
						atom.Y = y - (default_size / 4f);
						atom.Scale = 0.8f * scale;
						atom.W = paint.MeasureText(atom.TeXStr);
						x += atom.W;						
					
						up.X = atom.X;
						up.Y = atom.Y;
						up.W = atom.W;
						up.Scale = atom.Scale;
					}
				}break;
				
				case EDown down: {
					if(down.down is EGrouped group) {
						Typeset(group.exprs, new Vector2(x, y - (default_size / 4f)), 0.8f * scale);

						var last = group.exprs.Last();
						x += last.X + last.W - x;
					}
					else {
						var atom = down.down;
						atom.X = x;
						atom.Y = y + (default_size / 4f);
						atom.Scale = 0.8f * scale;
						atom.W = paint.MeasureText(atom.TeXStr);					
						x += atom.W;						

						down.X = atom.X;
						down.Y = atom.Y;
						down.W = atom.W;
						down.Scale = atom.Scale;
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

	void TypesetExpr() 
	{
		
	}

	///<summary>must be used only on leaf-nodes expressions</summary>
	private void RenderAtom(SKCanvas canvas, Expr atom)
	{
		Assert(atom.IsLeafNode());
		
		var temp_size = paint.TextSize;
		paint.TextSize *= atom.Scale;
		var rendered = false;

		foreach(var typeface in typefaces) {
			if(typeface.ContainsGlyphs(atom.TeXStr)) {
				paint.Typeface = typeface;
				canvas.DrawText(atom.TeXStr, atom.X, atom.Y, paint);
				rendered = true;
				break;
			}
		}
		if(!rendered) canvas.DrawText(atom.TeXStr, atom.X, atom.Y, paint);
		paint.TextSize = temp_size;		
		paint.Typeface = typeface;
	}

	private void RenderHList(SKCanvas canvas, List<Expr> hlist)
	{
		foreach(var node in hlist) {
			switch(node) {
				case EBinary binary: {
					var upper = (EGrouped)(binary.lhs);
					RenderHList(canvas, upper.exprs);

					var lower = (EGrouped)(binary.rhs);
					RenderHList(canvas, lower.exprs);

					var y = (binary.Scale * font_size) / 4f;
					var p0 = new SKPoint{X = binary.X, Y = binary.Y  - y};
					var p1 = new SKPoint{X = binary.X + binary.W, Y = binary.Y - y};
					canvas.DrawLine(p0, p1, paint);
				}break;
				
				case EGrouped group: {
					RenderHList(canvas, group.exprs);
				}break;
				
				case EUp up: {
					Console.WriteLine("up.up = {0}", up.up.Str);
					if(up.up is EGrouped group)	RenderHList(canvas, group.exprs);
					else RenderAtom(canvas, up.up);					
				}break;
				 				
				case EDown down: {
					Console.WriteLine("down.down = {0}", down.down.Str);
					if(down.down is EGrouped group) RenderHList(canvas, group.exprs);
					else RenderAtom(canvas, down.down);
				}break;			

				default: {
					RenderAtom(canvas, node);
				}break;
			}			
		}
	}


	/// <summary>copy a .png encoded image of the notation to dest_stream</summary>
	public void Render(Stream dest_stream)
	{
		Assert(dest_stream != null);
		Assert(root_list != null);
		Assert(is_typeset);

	    // set up drawing tools		
		var canvas = surface.Canvas;		
		canvas.Clear();
		RenderHList(canvas, root_list);
		
		using (var image = surface.Snapshot())
		using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
		{
		    data.SaveTo(dest_stream);
		}
	}

	///<summary>returns the snapshot of the surface that contains the notation</summary>
	public SKImage SnapshotNotationImg()
	{
		Assert(root_list != null);
		Assert(is_typeset);

		var canvas = surface.Canvas;
		canvas.Clear();
		RenderHList(canvas, root_list);

		return surface.Snapshot();
	}
}

