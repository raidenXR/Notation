using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SkiaSharp;
using IO = System.IO;

namespace Notation;



readonly struct Notation
{
	public readonly string Str;
	public readonly float X;
	public readonly float Y;
	public readonly SKFont? Font;
	public readonly float Size;

	public Notation(string str, float x, float y, SKFont? font, float size)
	{
		Str = str;
		X = x;
		Y = y;;
		Font = font;
		Size = size;
	}
}

enum Kind
{
	Upper,
	Lower,
	Main,
}

public class Parser
{
	string source;
	List<Token> tokens = new(20);
	List<Expr> expressions = new(20);
	int pos = 0;
	int node_n = 0;
	// Expr parent = null;

	public IEnumerable<Token> Tokens => tokens;

	public IEnumerable<Expr> Expressions => expressions;
	
	public Parser(string src)
	{
		var lexer = new Lexer(src);
		foreach(var token in lexer) {
			if(token.Id == TokenId.Bad || token.Id == TokenId.Whitespace) continue;

			tokens.Add(token);
		}
		
		source = src;
	}

	public IEnumerable<Expr> Parse()	{
		// foreach(var token in tokens) {
		// loops until all tokens are NextToken()
		while(pos < tokens.Count) {

			// WHEN THE LAST NODE IS REACHED, IT WILL RETURN IT REPEATEDLY, TO AVOID -OVERFLOW-
			// WHEN ParseExpr() recursively it moves forward, while the foreach token in tokens enumerators
			// indexes to previous token.
			
			var expr = ParseExpr();
			// special case, if last token is groupClose it cannot be parsed, and iteratation stops
			if(Current.Id == TokenId.Eof) yield break;
			yield return expr;
		}
	}

	// public void Print() {		
	// 	print(expressions, "");
	// }

	// private void print(List<Expr> nodes, string padding) {
	// 	foreach(var node in nodes) {
	// 		if(node is EBinary binary) {
	// 			var upper = (EGrouped)(binary.lhs);
	// 			print(upper.exprs, "--" + padding);
				
	// 			var lower = (EGrouped)(binary.rhs);
	// 			print(lower.exprs, "--" + padding);
	// 		}
	// 		else if(node is EGrouped group) {
	// 			print(group.exprs, "--" + padding);
	// 		}
	// 		else {
	// 			var pos_str = $"    pos: X= {node.X}, Y= {node.Y}";
	// 			Console.WriteLine(padding + node.Str +  pos_str);
	// 		}			
	// 	}
	// }

	Token Peek(int offset) {
		var index = pos + offset;
		var last = tokens.Last();
		// last = last.Id == TokenId.GroupedClose ? new Token(tokens.Count, "", TokenId.Eof) : last;
		return (index >= tokens.Count) ? last : tokens[index];
	}

	Token Current => Peek(0);

	Token NextToken() {
		var _current = Current;
		pos += 1;
		node_n += 1;
		
		return _current;
	}

	public Token MatchToken(TokenId kind) {
		if(Current.Id == kind) return NextToken();
		return new Token(pos, Current.Str, kind);
	}

	///<summary>Traverses the tree and return the root</summary>
	public Expr CreateAST() {
		// var token = NextToken();

		foreach(var token in tokens) {
			expressions.Add(ParseExpr());
		}

		var root = ParseExpr();

		return root;
	}

	Expr ParseExpr() {		
		return Current.Id switch {
			TokenId.Number     => ParseNumber(),
			TokenId.Identifier => ParseIdentifier(),
			TokenId.Over       => ParseOver(),
			TokenId.Under      => ParseUnder(),
			TokenId.Scaled     => ParseScaled(),
			TokenId.Symbol     => ParseSymbol(),
			TokenId.Binary     => ParseBinary(),
			TokenId.Space      => ParseSpace(),
			TokenId.MathOperator => ParseMathOperator(),
			TokenId.GroupedOpen  => ParseGrouped(),
			TokenId.GroupedClose => new EEof(), 
			TokenId.Open       => ParseEnclosure(),
			TokenId.Close      => ParseEnclosure(),
			TokenId.Up         => ParseUp(),
			TokenId.Down       => ParseDown(),
			_  => throw new ArgumentException($"does not implement {Current.Id}: {Current.Str}"), 
		};
	}

	Expr ParseGroupClosed() {
		NextToken();
		return ParseExpr();
	}

	Expr ParseNumber() {
		var number = NextToken();
		return new ENumber{
			str = number.Str,
		};
	}

	Expr ParseIdentifier() {
		var identifier = NextToken();
		return new EIdentifier{
			str = identifier.Str,
		};
	}

	Expr ParseUp() {
		var _current = NextToken(); 	// ignore
		// var prev = expressions.Last();
		var next = ParseExpr();

		return new EUp {
			// e = prev,
			up = next, 
		};
	}

	Expr ParseDown() {
		var _current = NextToken(); 	// ignore
		// var prev = expressions.Last();
		var next = ParseExpr();

		return new EDown {
			// e = prev,
			down = next,
		};
	}

	Expr ParseOver() {
		var _current = NextToken();
		var symbol = Lexer.symbols[_current.Str].Invoke();
		var operand = ParseExpr();

		return new EOver{
			e = operand,
			over = symbol,
		};
	}

	Expr ParseUnder() {
		var _current = NextToken();
		var symbol = Lexer.symbols[_current.Str].Invoke();
		var operand = ParseExpr();

		return new EUnder{
			e = operand,
			under = symbol,
		};	
	}

	Expr ParseScaled() {
		var _current = NextToken();
		var expr = ParseExpr();

		return new EScaled{
			str = _current.Str,
			scale = float.Parse(_current.Str),
			operand = expr,
		};
	}	
	
	Expr ParseSymbol() {
		var _current = NextToken();
		
		return Lexer.symbols[_current.Str].Invoke();	
	}

	Expr ParseEnclosure() {
		var _current = NextToken();

		return Lexer.enclosures[_current.Str].Invoke();
	}

	Expr ParseBinary() {
		var _current = NextToken();
		var lhs = ParseExpr();
		var rhs = ParseExpr();
		
		return new EBinary{
			str = _current.Str,
			lhs = lhs, 
			rhs = rhs
		};		
	}

	Expr ParseSpace() {
		var _current = NextToken();

		return Lexer.symbols[_current.Str].Invoke();
	}

	Expr ParseMathOperator() {
		var _current = NextToken();

		return Lexer.symbols[_current.Str].Invoke();
	}

	IEnumerable<Expr> ParseGroupedExprs() {
		// NextToken();  // ignore first token?
		while(Current.Id != TokenId.GroupedClose && pos < tokens.Count) {			
			// Console.WriteLine($"    --{Current.Id}, {Current.Str}");
			yield return ParseExpr();
		}
		NextToken();    // ignore }
	}

	Expr ParseGrouped() {
		var _open = NextToken(); 	// ignore {
		var exprs = ParseGroupedExprs();

		return new EGrouped{
			exprs = exprs.ToList(),
		};
	}
}


public static class TexSymbols
{
	public static bool IsDiacritical(string str, out string res)
	{
		var contains = diacriticals.ContainsKey(str);
		res = contains ? diacriticals[str] : string.Empty;
		
		return contains;
	}

	public static bool IsSymbol(string str, out string res)
	{
		var contains = symbols.ContainsKey(str);
		res = contains ? symbols[str] : string.Empty;

		return contains;
	}

	public static bool IsAlighment(string str)
	{
		throw new NotImplementedException();
	}
	
	static Dictionary<string, string> diacriticals = new()
	{
		{"\\acute", "\x00B4"},
		{"\\grave", "\x0060"},
		{"\\breve", "\x02D8"},
		{"\\check", "\x02C7"},
		{"\\dot", "."},
		{"\\ddot", ".."},
		{"\\mathring", "\x00B0"},
		{"\\vec", "\x20D7"},
		{"\\overrightarrow", "\x20D7"},
		{"\\overleftarrow", "\x20D6"},
		{"\\hat", "\x005E"},
		{"\\widehat", "\x0302"},
		{"\\tilde", "~"},
		{"\\widetilde", "\x02DC"},
		{"\\bar", "\x203E"},
		{"\\overbrace", "\xFE37"},
		{"\\overbracket", "\x23B4"},
		{"\\overline", "\x00AF"},
		{"\\underbrace", "\xFE38"},
		{"\\underbracket", "\x23B5"},
		{"\\underline", "\x00AF"},
	};

	static Dictionary<string, string> symbols = new()
	{
		{"+", "+"},
		{"-", "-"},
		{"*", "*"},
		{",", ","},
		{".", "."},
		{";", ";"},
		{":", ":"},
		{"?", "?"},
		{">", ">"},
		{"<", "<"},
		{"!", "!"},
		{"'", "\x02B9"},
		{"''", "\x02BA"},
		{"'''", "\x2034"},
		{"''''", "\x2057"},
		{"=", "="},
		{":=", ":="},
		{"\\mid", "\x2223"},
		{"\\parallel", "\x2225"},
		{"\\backslash", "\x2216"},
		{"/", "/"},
		{"\\setminus",	"\\"},
		{"\\times", "\x00D7"},
		{"\\alpha", "\x03B1"},
		{"\\beta", "\x03B2"},
		{"\\chi", "\x03C7"},
		{"\\delta", "\x03B4"},
		{"\\Delta", "\x0394"},
		{"\\epsilon", "\x03B5"},
		{"\\varepsilon", "\x025B"},
		{"\\eta", "\x03B7"},
		{"\\gamma", "\x03B3"},
		{"\\Gamma", "\x0393"},
		{"\\iota", "\x03B9"},
		{"\\kappa", "\x03BA"},
		{"\\lambda", "\x03BB"},
		{"\\Lambda", "\x039B"},
		{"\\mu", "\x03BC"},
		{"\\nu", "\x03BD"},
		{"\\omega", "\x03C9"},
		{"\\Omega", "\x03A9"},
		{"\\phi", "\x03C6"},
		{"\\varphi", "\x03D5"},
		{"\\Phi", "\x03A6"},
		{"\\pi", "\x03C0"},
		{"\\Pi", "\x03A0"},
		{"\\psi", "\x03C8"},
		{"\\Psi", "\x03A8"},
		{"\\rho", "\x03C1"},
		{"\\sigma", "\x03C3"},
		{"\\Sigma", "\x03A3"},
		{"\\tau", "\x03C4"},
		{"\\theta", "\x03B8"},
		{"\\vartheta", "\x03D1"},
		{"\\Theta", "\x0398"},
		{"\\upsilon", "\x03C5"},
		{"\\xi", "\x03BE"},
		{"\\Xi", "\x039E"},
		{"\\zeta", "\x03B6"},
		{"\\frac12", "\x00BD"},
		{"\\frac14", "\x00BC"},
		{"\\frac34", "\x00BE"},
		{"\\frac13", "\x2153"},
		{"\\frac23", "\x2154"},
		{"\\frac15", "\x2155"},
		{"\\frac25", "\x2156"},
		{"\\frac35", "\x2157"},
		{"\\frac45", "\x2158"},
		{"\\frac16", "\x2159"},
		{"\\frac56", "\x215A"},
		{"\\frac18", "\x215B"},
		{"\\frac38", "\x215C"},
		{"\\frac58", "\x215D"},
		{"\\frac78", "\x215E"},
		{"\\pm", "\x00B1"},
		{"\\mp", "\x2213"},
		{"\\triangleleft", "\x22B2"},
		{"\\triangleright", "\x22B3"},
		{"\\cdot", "\x22C5"},
		{"\\star", "\x22C6"},
		{"\\ast", "\x002A"},
		{"\\div", "\x00F7"},
		{"\\circ", "\x2218"},
		{"\\bullet", "\x2022"},
		{"\\oplus", "\x2295"},
		{"\\ominus", "\x2296"},
		{"\\otimes", "\x2297"},
		{"\\bigcirc", "\x25CB"},
		{"\\oslash", "\x2298"},
		{"\\odot", "\x2299"},
		{"\\land", "\x2227"},
		{"\\wedge", "\x2227"},
		{"\\lor", "\x2228"},
		{"\\vee", "\x2228"},
		{"\\cap", "\x2229"},
		{"\\cup", "\x222A"},
		{"\\sqcap", "\x2293"},
		{"\\sqcup", "\x2294"},
		{"\\uplus", "\x228E"},
		{"\\amalg", "\x2210"},
		{"\\bigtriangleup", "\x25B3"},
		{"\\bigtriangledown", "\x25BD"},
		{"\\dag", "\x2020"},
		{"\\dagger", "\x2020"},
		{"\\ddag", "\x2021"},
		{"\\ddagger", "\x2021"},
		{"\\lhd", "\x22B2"},
		{"\\rhd", "\x22B3"},
		{"\\unlhd", "\x22B4"},
		{"\\unrhd", "\x22B5"},
		{"\\lt", "<"},
		{"\\gt", ">"},
		{"\\ne", "\x2260"},
		{"\\neq", "\x2260"},
		{"\\le", "\x2264"},
		{"\\leq", "\x2264"},
		{"\\leqslant", "\x2264"},
		{"\\ge", "\x2265"},
		{"\\geq", "\x2265"},
		{"\\geqslant", "\x2265"},
		{"\\equiv", "\x2261"},
		{"\\ll", "\x226A"},
		{"\\gg", "\x226B"},
		{"\\doteq", "\x2250"},
		{"\\prec", "\x227A"},
		{"\\succ", "\x227B"},
		{"\\preceq", "\x227C"},
		{"\\succeq", "\x227D"},
		{"\\subset", "\x2282"},
		{"\\supset", "\x2283"},
		{"\\subseteq", "\x2286"},
		{"\\supseteq", "\x2287"},
		{"\\sqsubset", "\x228F"},
		{"\\sqsupset", "\x2290"},
		{"\\sqsubseteq", "\x2291"},
		{"\\sqsupseteq", "\x2292"},
		{"\\sim", "\x223C"},
		{"\\simeq", "\x2243"},
		{"\\approx", "\x2248"},
		{"\\cong", "\x2245"},
		{"\\Join", "\x22C8"},
		{"\\bowtie", "\x22C8"},
		{"\\in", "\x2208"},
		{"\\ni", "\x220B"},
		{"\\owns", "\x220B"},
		{"\\propto", "\x221D"},
		{"\\vdash", "\x22A2"},
		{"\\dashv", "\x22A3"},
		{"\\models", "\x22A8"},
		{"\\perp", "\x22A5"},
		{"\\smile", "\x2323"},
		{"\\frown", "\x2322"},
		{"\\asymp", "\x224D"},
		{"\\notin", "\x2209"},
		{"\\gets", "\x2190"},
		{"\\leftarrow", "\x2190"},
		{"\\to", "\x2192"},
		{"\\rightarrow", "\x2192"},
		{"\\leftrightarrow", "\x2194"},
		{"\\uparrow", "\x2191"},
		{"\\downarrow", "\x2193"},
		{"\\updownarrow", "\x2195"},
		{"\\Leftarrow", "\x21D0"},
		{"\\Rightarrow", "\x21D2"},
		{"\\Leftrightarrow", "\x21D4"},
		{"\\iff", "\x21D4"},
		{"\\Uparrow", "\x21D1"},
		{"\\Downarrow", "\x21D3"},
		{"\\Updownarrow", "\x21D5"},
		{"\\mapsto", "\x21A6"},
		{"\\longleftarrow", "\x2190"},
		{"\\longrightarrow", "\x2192"},
		{"\\longleftrightarrow", "\x2194"},
		{"\\Longleftarrow", "\x21D0"},
		{"\\Longrightarrow", "\x21D2"},
		{"\\Longleftrightarrow", "\x21D4"},
		{"\\longmapsto", "\x21A6"},
		{"\\sum", "\x2211"},
		{"\\prod", "\x220F"},
		{"\\bigcap", "\x22C2"},
		{"\\bigcup", "\x22C3"},
		{"\\bigwedge", "\x22C0"},
		{"\\bigvee", "\x22C1"},
		{"\\bigsqcap", "\x2A05"},
		{"\\bigsqcup", "\x2A06"},
		{"\\coprod", "\x2210"},
		{"\\bigoplus", "\x2A01"},
		{"\\bigotimes", "\x2A02"},
		{"\\bigodot", "\x2A00"},
		{"\\biguplus", "\x2A04"},
		{"\\int", "\x222B"},
		{"\\iint", "\x222C"},
		{"\\iiint", "\x222D"},
		{"\\oint", "\x222E"},
		{"\\prime", "\x2032"},
		{"\\dots", "\x2026"},
		{"\\ldots", "\x2026"},
		{"\\cdots", "\x22EF"},
		{"\\vdots", "\x22EE"},
		{"\\ddots", "\x22F1"},
		{"\\forall", "\x2200"},
		{"\\exists", "\x2203"},
		{"\\Re", "\x211C"},
		{"\\Im", "\x2111"},
		{"\\aleph", "\x2135"},
		{"\\hbar", "\x210F"},
		{"\\ell", "\x2113"},
		{"\\wp", "\x2118"},
		{"\\emptyset", "\x2205"},
		{"\\infty", "\x221E"},
		{"\\partial", "\x2202"},
		{"\\nabla", "\x2207"},
		{"\\triangle", "\x25B3"},
		{"\\therefore", "\x2234"},
		{"\\angle", "\x2220"},
		{"\\diamond", "\x22C4"},
		{"\\Diamond", "\x25C7"},
		{"\\neg", "\x00AC"},
		{"\\lnot", "\x00AC"},
		{"\\bot", "\x22A5"},
		{"\\top", "\x22A4"},
		{"\\square", "\x25AB"},
		{"\\Box", "\x25A1"},
		{"\\wr", "\x2240"},
		{"\\!", "-0.167em"},
		{"\\,", "0.167em"},
		{"\\>", "0.222em"},
		{"\\:", "0.222em"},
		{"\\;", "0.278em"},
		{"~", "0.333em"},
		{"\\quad", "1em"},
		{"\\qquad", "2em"},
		{"\\arccos", "arccos"},
		{"\\arcsin", "arcsin"},
		{"\\arctan", "arctan"},
		{"\\arg", "arg"},
		{"\\cos", "cos"},
		{"\\cosh", "cosh"},
		{"\\cot", "cot"},
		{"\\coth", "coth"},
		{"\\csc", "csc"},
		{"\\deg", "deg"},
		{"\\det", "det"},
		{"\\dim", "dim"},
		{"\\exp", "exp"},
		{"\\gcd", "gcd"},
		{"\\hom", "hom"},
		{"\\inf", "inf"},
		{"\\ker", "ker"},
		{"\\lg", "lg"},
		{"\\lim", "lim"},
		{"\\liminf", "liminf"},
		{"\\limsup", "limsup"},
		{"\\ln", "ln"},
		{"\\log", "log"},
		{"\\max", "max"},
		{"\\min", "min"},
		{"\\Pr", "Pr"},
		{"\\sec", "sec"},
		{"\\sin", "sin"},
		{"\\sinh", "sinh"},
		{"\\sup", "sup"},
		{"\\tan", "tan"},
		{"\\tanh", "tanh"},
	};
	
}
