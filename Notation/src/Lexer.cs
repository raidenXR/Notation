using System;
using System.Collections;
using System.Collections.Generic;

namespace Notation;


public enum TokenId
{
	None,
	Whitespace,

	Ord,
	Op, 
	Bin, 
	Rel, 
	Open,
	Close,
	Punct,
	Inner,
	// Over,
	// Under,
	Acc,
	Rad,
	Vcent,
	
	// Operator,
	// Accent,
	// Underscore,
	// LeftBrace,
	// RightBrace,
	// LeftParen,
	// RightParen,
	// Number,	
	// Identifier,
	// Pipe,

	// Diacritical,
	// Scaler,
	// BinaryOp,
	// Enclosure,
	// Symbol,

	// Over,
	// Under,
	GroupedOpen,
	GroupedClose,

	Number,
	Grouped,
	Identifier,
	MathOperator,
	Symbol,
	Space,
	Binary,
	Sub,
	Super,
	Subsup,
	Over,
	Under,
	Underover,
	Up,
	Down,
	DownUp,
	Unary,
	Scaled,
	Stretchy,
	Array,
	Text,

	Bad,
	Eof,
}


public readonly struct Token
{
	public readonly int Pos;
	public readonly TokenId Id;
	public readonly string Str;
	public readonly ArraySegment<char> StrSeg;
	public readonly Expr? Expr;

	public Token(int pos, string str, TokenId id)
	{
		Pos = pos;
		Str = str;
		Id = id;
		Expr = null;
	}

	public Token(int pos, string str, Expr expr)
	{
		Pos = pos;
		Str = str;
		Id = TokenId.None;
		Expr = expr;
	}
}

///<summary>TeXSymbolType</summary>
public enum TeXSymbolType
{
	Ord   = 0,
	Op    = 1,
	Bin   = 2,
	Rel   = 3,
	Open  = 4,
	Close = 5,
	Pun   = 6,
	Accent = 7,
}

public class Lexer
{
	string source;
	int pos;

	const int Ord = 0;
	const int Op  = 1;
	const int Bin = 2;
	const int Rel = 3;
	const int Open = 4;
	const int Close = 5;
	const int Pun = 6;
	const int Accent = 7;

	public Lexer(string src)
	{
		source = src;
		pos = 0;
	}
	
	public ref struct Enumerator
	{
		readonly Lexer lexer;
		Token current;

		public Enumerator(Lexer lexer)
		{
			this.lexer = lexer;
		}

		public Token Current => current;

		public bool MoveNext()
		{			
			current = lexer.Lex();
			return (current.Id != TokenId.Eof) ? true : false;			
		}

		public void Dispose() {}		
	}
	
	public Enumerator GetEnumerator() => new Enumerator(this);

	char Peek(int offset) {
		var index = pos + offset;
		return index >= source.Length ? '\0' : source[index];
	}

	char Current => Peek(0);

	char LookAhead() => source[pos + 1];

	void Advance() {
		pos += 1;
	}

	string NextKeyword() {
		var start = pos;
		Advance(); // skip \
		while(char.IsLetter(Current)) Advance();

		return source[start..pos];
	}

	///<summary>Returns the substring upto the next keyword</summary>
	string NextWord()
	{
		var len = source.Length - pos;
		var slice = source.AsSpan(pos, len);
		
		for(int i = 0; i < slice.Length; i++) {
			switch(slice[i]) {
				case ' ':
				case '_':
				case '^':
				case '{': {
					pos += i;
					return slice[0..i].ToString();
				}				
				default: continue;
			}			
		}
		pos += len;
		return slice[0..len].ToString();
	}

	bool MatchSequence(IEnumerable<string> seq, out string key) {
		foreach(var str in seq) {
			var a = pos;
			var b = pos + str.Length;

			if(a >= source.Length || b >= source.Length) {
				key = string.Empty;
				return false;
			}
			
			var src = source.AsSpan();
			if(src[a..b].Equals(str, StringComparison.Ordinal)) {
				key = str;
				pos += str.Length;
				return true;
			}			
		}			
		
		key = string.Empty;
		return false;
	}

	public Token Lex()	{
		var start = pos;
		var src = source;
		
		if(pos >= source.Length) {
			return new Token(pos, "eof", TokenId.Eof);
		}

		if(char.IsWhiteSpace(Current)) {
			while(char.IsWhiteSpace(Current)) Advance();
			
			return new Token(pos, src[start..pos], TokenId.Whitespace);
		}

		if(char.IsDigit(Current)) {
			while(char.IsDigit(Current) ||
				(Current == '.' && char.IsDigit(LookAhead()))) Advance();

			return new Token(pos, src[start..pos], TokenId.Number);
		}

		if(char.IsLetter(Current)) {
			while(char.IsLetter(Current)) Advance();
			
			return new Token(pos, src[start..pos], TokenId.Identifier);
		}


		switch(Current) {
			case '+':
			case '-':
			case '*':
			case '=':
			case '/': {
				Advance();
				return new Token(pos, src[start..pos], TokenId.MathOperator);
			}
		
			case '{': {
				Advance();
				return new Token(pos, src[start..pos], TokenId.GroupedOpen);
			};
			
			case '}': {
				Advance();
				return new Token(pos, src[start..pos], TokenId.GroupedClose);
			};
			
			case '^': {
				Advance();
				return new Token(pos, src[start..pos], TokenId.Up);	
			};
			
			case '_': {
				Advance();
				return new Token(pos, src[start..pos], TokenId.Down);	
			};

			case '|':
			case '(':
			case '[': {
				Advance();
				return new Token(pos, src[start..pos], TokenId.Open);
			}

			case ')':
			case ']': {
				Advance();
				return new Token(pos, src[start..pos], TokenId.Close);
			}
			case '\\': {
				switch(Peek(1)) {
					case '|': {
						Advance();
						Advance();
						return new Token(pos, "\\|", TokenId.Open);
					}
					case '{': {
						Advance();
						Advance();
						return new Token(pos, "\\{", TokenId.Open);
					}
					case '}': {
						Advance();
						Advance();
						return new Token(pos, "\\}", TokenId.Close);
					}
					default: break;
				}

				var next_word = NextWord();
				// Console.WriteLine($"next keyword: -{next_word}-");

				if(diacriticals.ContainsKey(next_word)) {
					var _diacritical = next_word;
					var expr = diacriticals[_diacritical];
					var expr_t = expr.GetType();

					if(expr_t.IsAssignableTo(typeof(EOver))) {
						return new Token(pos, _diacritical, TokenId.Over);				
					}
					else if(expr_t.IsAssignableTo(typeof(EUnder))) {
						return new Token(pos, _diacritical, TokenId.Under);				
					}
					else {
						throw new InvalidCastException();
					}			
				}

				if(scalers.ContainsKey(next_word)) {
					var _scaler = next_word;
					return new Token(pos, _scaler, TokenId.Scaled);
				}

				if(enclosures.ContainsKey(next_word)) {
					var _enclosure = next_word;
					var expr = (ESymbol)enclosures[_enclosure].Invoke();
					var symbol_type = expr.symbol_type;

					var id = symbol_type switch {
						TeXSymbolType.Open => TokenId.Open,
						TeXSymbolType.Close => TokenId.Close,
						_ => throw new ArgumentException(),
					};			
					return new Token(pos, _enclosure, id);
				}

				if(binaryOps.Contains(next_word)) {
					var _binaryop = next_word;
					return new Token(pos, _binaryop, TokenId.Binary);					
				}

				if(symbols.ContainsKey(next_word)) {
					var _symbol = next_word;
					var expr = symbols[_symbol].Invoke();
					var expr_t = expr.GetType();

					if(expr_t.IsAssignableTo(typeof(ESymbol))) {
						return new Token(pos, _symbol, TokenId.Symbol);
					}
					else if(expr_t.IsAssignableTo(typeof(ESpace))) {
						return new Token(pos, _symbol, TokenId.Space);
					}
					else if(expr_t.IsAssignableTo(typeof(EMathOperator))) {
						return new Token(pos, _symbol, TokenId.MathOperator);
					}
					else {
						throw new InvalidCastException();
					}
				}
			}break;						

			default: {
				Advance();
				return new Token(pos, src[start..pos], TokenId.Bad);				
			}
		}

		Advance();
		return new Token(pos, src[start..pos], TokenId.Bad);			
	}



	// https://hackage.haskell.org/package/texmath-0.3.0.3/docs/src/Text-TeXMath-Parser.html
	public static Dictionary<string, Expr> diacriticals = new(30){
		{"\\acute",  new EOver(new ESymbol(Accent, "\x00B4"))},
		{"\\grave",  new EOver(new ESymbol(Accent, "\x0060"))},
		{"\\breve",  new EOver(new ESymbol(Accent, "\x02D8"))},
		{"\\check",  new EOver(new ESymbol(Accent, "\x02C7"))},
		{"\\dot", new EOver(new ESymbol(Accent , "."))},
		{"\\ddot", new EOver(new ESymbol(Accent , ".."))},
		{"\\mathring", new EOver(new ESymbol(Accent, "\x00B0"))},
		{"\\vec", new EOver(new ESymbol(Accent, "\x20D7"))},
		{"\\overrightarrow", new EOver(new ESymbol(Accent, "\x20D7"))},
		{"\\overleftarrow", new EOver(new ESymbol(Accent, "\x20D6"))},
		{"\\hat", new EOver(new ESymbol(Accent, "\x005E"))},
		{"\\widehat", new EOver(new ESymbol(Accent, "\x0302"))},
		{"\\tilde", new EOver(new ESymbol(Accent, "~"))},
		{"\\widetilde", new EOver(new ESymbol(Accent, "\x02DC"))},
		{"\\bar", new EOver(new ESymbol(Accent, "\x203E"))},
		{"\\overbrace", new EOver(new ESymbol(Accent, "\xFE37"))},
		{"\\overbracket", new EOver(new ESymbol(Accent, "\x23B4"))},
		{"\\overline", new EOver(new ESymbol(Accent, "\x00AF"))},
		{"\\underbrace", new EUnder(new ESymbol(Accent, "\xFE38"))},
		{"\\underbracket", new EUnder(new ESymbol(Accent, "\x23B5"))},
		{"\\underline", new EUnder(new ESymbol(Accent, "\x00AF"))},
	};

	public static HashSet<string> binaryOps = new(20) {
		"\\frac", "\\tfrac", "\\dfrac", "\\stackrel", "\\overset", "\\underset", "\\binom",
	};

	public static Dictionary<string, float> scalers = new(20){
		  {"\\bigg", 2.2f},
		  {"\\Bigg", 2.9f},
		  {"\\big", 1.2f},
		  {"\\Big", 1.6f},
		  {"\\biggr", 2.2f},
		  {"\\Biggr", 2.9f},
		  {"\\bigr", 1.2f},
		  {"\\Bigr", 1.6f},
		  {"\\biggl", 2.2f},
		  {"\\Biggl", 2.9f},
		  {"\\bigl", 1.2f},
		  {"\\Bigl", 1.6f},
	};

	public static Dictionary<string, Func<Expr>> enclosures = new(30){
		{"(", () => new ESymbol(Open, "(")},
		{")", () => new ESymbol(Close, ")")},
		{"[", () => new ESymbol(Open, "[")},
		{"]", () => new ESymbol(Close, "]")},
		{"\\{", () => new ESymbol(Open, "{")},
		{"\\}", () => new ESymbol(Close, "}")},
		{"\\lbrack", () => new ESymbol(Open, "[")},
		{"\\lbrace", () => new ESymbol(Open, "{")},
		{"\\rbrack", () => new ESymbol(Close, "]")},
		{"\\rbrace", () => new ESymbol(Close, "}")},
		{"\\llbracket", () => new ESymbol(Open, "\x27E6")},
		{"\\rrbracket", () => new ESymbol(Close, "\x230B")},
		{"\\langle", () => new ESymbol(Open, "\x27E8")},
		{"\\rangle", () => new ESymbol(Close, "\x27E9")},
		{"\\lfloor", () => new ESymbol(Open, "\x230A")},
		{"\\rfloor", () => new ESymbol(Close, "\x230B")},
		{"\\lceil", () => new ESymbol(Open, "\x2308")},
		{"\\rceil", () => new ESymbol(Close, "\x2309")},
		{"|", () => new ESymbol(Open, "\x2223")},
		// {"|", () => new ESymbol(Close, "\x2223")},
		{"\\|", () => new ESymbol(Open, "\x2225")},
		// {"\\|", () => new ESymbol(Close, "\x2225")},
		{"\\vert", () => new ESymbol(Open, "\x2223")},
		// {"\\vert", () => new ESymbol(Close, "\x2223")},
		{"\\Vert", () => new ESymbol(Open, "\x2225")},
		// {"\\Vert", () => new ESymbol(Close, "\x2225")},
	};

	
	public static Dictionary<string, Func<Expr>> symbols = new(300){
		{"+", () => new ESymbol(Bin, "+")},
		{"-", () => new ESymbol(Bin, "-")},
		{"*", () => new ESymbol(Bin, "*")},
		{",", () => new ESymbol(Pun, ",")},
		{".", () => new ESymbol(Pun, ".")},
		{";", () => new ESymbol(Pun, ";")},
		{":", () => new ESymbol(Pun, ":")},
		{"?", () => new ESymbol(Pun, "?")},
		{">", () => new ESymbol(Rel, ">")},
		{"<", () => new ESymbol(Rel, "<")},
		{"!", () => new ESymbol(Ord, "!")},
		{"'", () => new ESymbol(Ord, "\x02B9")},
		{"''", () => new ESymbol(Ord, "\x02BA")},
		{"'''", () => new ESymbol(Ord, "\x2034")},
		{"''''", () => new ESymbol(Ord, "\x2057")},
		{"=", () => new ESymbol(Rel, "=")},
		{":=", () => new ESymbol(Rel, ":=")},
		{"\\mid", () => new ESymbol(Bin, "\x2223")},
		{"\\parallel", () => new ESymbol(Rel, "\x2225")},
		{"\\backslash", () => new ESymbol(Bin, "\x2216")},
		{"/", () => new ESymbol(Bin, "/")},
		// {"\\setminus",	() => new ESymbol(Bin, "\\")},
		// {"\\times", () => new ESymbol(Bin, "\x00D7")},
		{"\\alpha", () => new ESymbol(Ord, "\x03B1")},
		{"\\beta", () => new ESymbol(Ord, "\x03B2")},
		{"\\chi", () => new ESymbol(Ord, "\x03C7")},
		{"\\delta", () => new ESymbol(Ord, "\x03B4")},
		{"\\Delta", () => new ESymbol(Op, "\x0394")},
		{"\\epsilon", () => new ESymbol(Ord, "\x03B5")},
		{"\\varepsilon", () => new ESymbol(Ord, "\x025B")},
		{"\\eta", () => new ESymbol(Ord, "\x03B7")},
		{"\\gamma", () => new ESymbol(Ord, "\x03B3")},
		{"\\Gamma", () => new ESymbol(Op, "\x0393") },
		{"\\iota", () => new ESymbol(Ord, "\x03B9")},
		{"\\kappa", () => new ESymbol(Ord, "\x03BA")},
		{"\\lambda", () => new ESymbol(Ord, "\x03BB")},
		{"\\Lambda", () => new ESymbol(Op, "\x039B") },
		{"\\mu", () => new ESymbol(Ord, "\x03BC")},
		{"\\nu", () => new ESymbol(Ord, "\x03BD")},
		{"\\omega", () => new ESymbol(Ord, "\x03C9")},
		{"\\Omega", () => new ESymbol(Op, "\x03A9")},
		{"\\phi", () => new ESymbol(Ord, "\x03C6")},
		{"\\varphi", () => new ESymbol(Ord, "\x03D5")},
		{"\\Phi", () => new ESymbol(Op, "\x03A6") },
		{"\\pi", () => new ESymbol(Ord, "\x03C0")},
		{"\\Pi", () => new ESymbol(Op, "\x03A0") },
		{"\\psi", () => new ESymbol(Ord, "\x03C8")},
		{"\\Psi", () => new ESymbol(Ord, "\x03A8")},
		{"\\rho", () => new ESymbol(Ord, "\x03C1")},
		{"\\sigma", () => new ESymbol(Ord, "\x03C3")},
		{"\\Sigma", () => new ESymbol(Op, "\x03A3") },
		{"\\tau", () => new ESymbol(Ord, "\x03C4")},
		{"\\theta", () => new ESymbol(Ord, "\x03B8")},
		{"\\vartheta", () => new ESymbol(Ord, "\x03D1")},
		{"\\Theta", () => new ESymbol(Op, "\x0398") },
		{"\\upsilon", () => new ESymbol(Ord, "\x03C5")},
		{"\\xi", () => new ESymbol(Ord, "\x03BE")},
		{"\\Xi", () => new ESymbol(Op, "\x039E") },
		{"\\zeta", () => new ESymbol(Ord, "\x03B6")},
		{"\\frac12", () => new ESymbol(Ord, "\x00BD")},
		{"\\frac14", () => new ESymbol(Ord, "\x00BC")},
		{"\\frac34", () => new ESymbol(Ord, "\x00BE")},
		{"\\frac13", () => new ESymbol(Ord, "\x2153")},
		{"\\frac23", () => new ESymbol(Ord, "\x2154")},
		{"\\frac15", () => new ESymbol(Ord, "\x2155")},
		{"\\frac25", () => new ESymbol(Ord, "\x2156")},
		{"\\frac35", () => new ESymbol(Ord, "\x2157")},
		{"\\frac45", () => new ESymbol(Ord, "\x2158")},
		{"\\frac16", () => new ESymbol(Ord, "\x2159")},
		{"\\frac56", () => new ESymbol(Ord, "\x215A")},
		{"\\frac18", () => new ESymbol(Ord, "\x215B")},
		{"\\frac38", () => new ESymbol(Ord, "\x215C")},
		{"\\frac58", () => new ESymbol(Ord, "\x215D")},
		{"\\frac78", () => new ESymbol(Ord, "\x215E")},
		{"\\pm", () => new ESymbol(Bin, "\x00B1")},
		{"\\mp", () => new ESymbol(Bin, "\x2213")},
		{"\\triangleleft", () => new ESymbol(Bin, "\x22B2")},
		{"\\triangleright", () => new ESymbol(Bin, "\x22B3")},
		{"\\cdot", () => new ESymbol(Bin, "\x22C5")},
		{"\\star", () => new ESymbol(Bin, "\x22C6")},
		{"\\ast", () => new ESymbol(Bin, "\x002A")},
		{"\\times", () => new ESymbol(Bin, "\x00D7")},
		{"\\div", () => new ESymbol(Bin, "\x00F7")},
		{"\\circ", () => new ESymbol(Bin, "\x2218")},
		{"\\bullet", () => new ESymbol(Bin, "\x2022")},
		{"\\oplus", () => new ESymbol(Bin, "\x2295")},
		{"\\ominus", () => new ESymbol(Bin, "\x2296")},
		{"\\otimes", () => new ESymbol(Bin, "\x2297")},
		{"\\bigcirc", () => new ESymbol(Bin, "\x25CB")},
		{"\\oslash", () => new ESymbol(Bin, "\x2298")},
		{"\\odot", () => new ESymbol(Bin, "\x2299")},
		{"\\land", () => new ESymbol(Bin, "\x2227")},
		{"\\wedge", () => new ESymbol(Bin, "\x2227")},
		{"\\lor", () => new ESymbol(Bin, "\x2228")},
		{"\\vee", () => new ESymbol(Bin, "\x2228")},
		{"\\cap", () => new ESymbol(Bin, "\x2229")},
		{"\\cup", () => new ESymbol(Bin, "\x222A")},
		{"\\sqcap", () => new ESymbol(Bin, "\x2293")},
		{"\\sqcup", () => new ESymbol(Bin, "\x2294")},
		{"\\uplus", () => new ESymbol(Bin, "\x228E")},
		{"\\amalg", () => new ESymbol(Bin, "\x2210")},
		{"\\bigtriangleup", () => new ESymbol(Bin, "\x25B3")},
		{"\\bigtriangledown", () => new ESymbol(Bin, "\x25BD")},
		{"\\dag", () => new ESymbol(Bin, "\x2020")},
		{"\\dagger", () => new ESymbol(Bin, "\x2020")},
		{"\\ddag", () => new ESymbol(Bin, "\x2021")},
		{"\\ddagger", () => new ESymbol(Bin, "\x2021")},
		{"\\lhd", () => new ESymbol(Bin, "\x22B2")},
		{"\\rhd", () => new ESymbol(Bin, "\x22B3")},
		{"\\unlhd", () => new ESymbol(Bin, "\x22B4")},
		{"\\unrhd", () => new ESymbol(Bin, "\x22B5")},
		{"\\lt", () => new ESymbol(Rel, "<")},
		{"\\gt", () => new ESymbol(Rel, ">")},
		{"\\ne", () => new ESymbol(Rel, "\x2260")},
		{"\\neq", () => new ESymbol(Rel, "\x2260")},
		{"\\le", () => new ESymbol(Rel, "\x2264")},
		{"\\leq", () => new ESymbol(Rel, "\x2264")},
		{"\\leqslant", () => new ESymbol(Rel, "\x2264")},
		{"\\ge", () => new ESymbol(Rel, "\x2265")},
		{"\\geq", () => new ESymbol(Rel, "\x2265")},
		{"\\geqslant", () => new ESymbol(Rel, "\x2265")},
		{"\\equiv", () => new ESymbol(Rel, "\x2261")},
		{"\\ll", () => new ESymbol(Rel, "\x226A")},
		{"\\gg", () => new ESymbol(Rel, "\x226B")},
		{"\\doteq", () => new ESymbol(Rel, "\x2250")},
		{"\\prec", () => new ESymbol(Rel, "\x227A")},
		{"\\succ", () => new ESymbol(Rel, "\x227B")},
		{"\\preceq", () => new ESymbol(Rel, "\x227C")},
		{"\\succeq", () => new ESymbol(Rel, "\x227D")},
		{"\\subset", () => new ESymbol(Rel, "\x2282")},
		{"\\supset", () => new ESymbol(Rel, "\x2283")},
		{"\\subseteq", () => new ESymbol(Rel, "\x2286")},
		{"\\supseteq", () => new ESymbol(Rel, "\x2287")},
		{"\\sqsubset", () => new ESymbol(Rel, "\x228F")},
		{"\\sqsupset", () => new ESymbol(Rel, "\x2290")},
		{"\\sqsubseteq", () => new ESymbol(Rel, "\x2291")},
		{"\\sqsupseteq", () => new ESymbol(Rel, "\x2292")},
		{"\\sim", () => new ESymbol(Rel, "\x223C")},
		{"\\simeq", () => new ESymbol(Rel, "\x2243")},
		{"\\approx", () => new ESymbol(Rel, "\x2248")},
		{"\\cong", () => new ESymbol(Rel, "\x2245")},
		{"\\Join", () => new ESymbol(Rel, "\x22C8")},
		{"\\bowtie", () => new ESymbol(Rel, "\x22C8")},
		{"\\in", () => new ESymbol(Rel, "\x2208")},
		{"\\ni", () => new ESymbol(Rel, "\x220B")},
		{"\\owns", () => new ESymbol(Rel, "\x220B")},
		{"\\propto", () => new ESymbol(Rel, "\x221D")},
		{"\\vdash", () => new ESymbol(Rel, "\x22A2")},
		{"\\dashv", () => new ESymbol(Rel, "\x22A3")},
		{"\\models", () => new ESymbol(Rel, "\x22A8")},
		{"\\perp", () => new ESymbol(Rel, "\x22A5")},
		{"\\smile", () => new ESymbol(Rel, "\x2323")},
		{"\\frown", () => new ESymbol(Rel, "\x2322")},
		{"\\asymp", () => new ESymbol(Rel, "\x224D")},
		{"\\notin", () => new ESymbol(Rel, "\x2209")},
		{"\\gets", () => new ESymbol(Rel, "\x2190")},
		{"\\leftarrow", () => new ESymbol(Rel, "\x2190")},
		{"\\to", () => new ESymbol(Rel, "\x2192")},
		{"\\rightarrow", () => new ESymbol(Rel, "\x2192")},
		{"\\leftrightarrow", () => new ESymbol(Rel, "\x2194")},
		{"\\uparrow", () => new ESymbol(Rel, "\x2191")},
		{"\\downarrow", () => new ESymbol(Rel, "\x2193")},
		{"\\updownarrow", () => new ESymbol(Rel, "\x2195")},
		{"\\Leftarrow", () => new ESymbol(Rel, "\x21D0")},
		{"\\Rightarrow", () => new ESymbol(Rel, "\x21D2")},
		{"\\Leftrightarrow", () => new ESymbol(Rel, "\x21D4")},
		{"\\iff", () => new ESymbol(Rel, "\x21D4")},
		{"\\Uparrow", () => new ESymbol(Rel, "\x21D1")},
		{"\\Downarrow", () => new ESymbol(Rel, "\x21D3")},
		{"\\Updownarrow", () => new ESymbol(Rel, "\x21D5")},
		{"\\mapsto", () => new ESymbol(Rel, "\x21A6")},
		{"\\longleftarrow", () => new ESymbol(Rel, "\x2190")},
		{"\\longrightarrow", () => new ESymbol(Rel, "\x2192")},
		{"\\longleftrightarrow", () => new ESymbol(Rel, "\x2194")},
		{"\\Longleftarrow", () => new ESymbol(Rel, "\x21D0")},
		{"\\Longrightarrow", () => new ESymbol(Rel, "\x21D2")},
		{"\\Longleftrightarrow", () => new ESymbol(Rel, "\x21D4")},
		{"\\longmapsto", () => new ESymbol(Rel, "\x21A6")},
		{"\\sum", () => new ESymbol(Op ,"\x2211")},
		{"\\prod", () => new ESymbol(Op ,"\x220F")},
		{"\\bigcap", () => new ESymbol(Op ,"\x22C2")},
		{"\\bigcup", () => new ESymbol(Op ,"\x22C3")},
		{"\\bigwedge", () => new ESymbol(Op ,"\x22C0")},
		{"\\bigvee", () => new ESymbol(Op ,"\x22C1")},
		{"\\bigsqcap", () => new ESymbol(Op ,"\x2A05")},
		{"\\bigsqcup", () => new ESymbol(Op ,"\x2A06")},
		{"\\coprod", () => new ESymbol(Op ,"\x2210")},
		{"\\bigoplus", () => new ESymbol(Op ,"\x2A01")},
		{"\\bigotimes", () => new ESymbol(Op ,"\x2A02")},
		{"\\bigodot", () => new ESymbol(Op ,"\x2A00")},
		{"\\biguplus", () => new ESymbol(Op ,"\x2A04")},
		{"\\int", () => new ESymbol(Op ,"\x222B")},
		{"\\iint", () => new ESymbol(Op ,"\x222C")},
		{"\\iiint", () => new ESymbol(Op ,"\x222D")},
		{"\\oint", () => new ESymbol(Op ,"\x222E")},
		{"\\prime", () => new ESymbol(Ord, "\x2032")},
		{"\\dots", () => new ESymbol(Ord, "\x2026")},
		{"\\ldots", () => new ESymbol(Ord, "\x2026")},
		{"\\cdots", () => new ESymbol(Ord, "\x22EF")},
		{"\\vdots", () => new ESymbol(Ord, "\x22EE")},
		{"\\ddots", () => new ESymbol(Ord, "\x22F1")},
		{"\\forall", () => new ESymbol(Op ,"\x2200")},
		{"\\exists", () => new ESymbol(Op ,"\x2203")},
		{"\\Re", () => new ESymbol(Ord, "\x211C")},
		{"\\Im", () => new ESymbol(Ord, "\x2111")},
		{"\\aleph", () => new ESymbol(Ord, "\x2135")},
		{"\\hbar", () => new ESymbol(Ord, "\x210F")},
		{"\\ell", () => new ESymbol(Ord, "\x2113")},
		{"\\wp", () => new ESymbol(Ord, "\x2118")},
		{"\\emptyset", () => new ESymbol(Ord, "\x2205")},
		{"\\infty", () => new ESymbol(Ord, "\x221E")},
		{"\\partial", () => new ESymbol(Ord, "\x2202")},
		{"\\nabla", () => new ESymbol(Ord, "\x2207")},
		{"\\triangle", () => new ESymbol(Ord, "\x25B3")},
		{"\\therefore", () => new ESymbol(Pun, "\x2234")},
		{"\\angle", () => new ESymbol(Ord, "\x2220")},
		{"\\diamond", () => new ESymbol(Op, "\x22C4")},
		{"\\Diamond", () => new ESymbol(Op, "\x25C7")},
		{"\\neg", () => new ESymbol(Op, "\x00AC")},
		{"\\lnot", () => new ESymbol(Ord, "\x00AC")},
		{"\\bot", () => new ESymbol(Ord, "\x22A5")},
		{"\\top", () => new ESymbol(Ord, "\x22A4")},
		{"\\square", () => new ESymbol(Ord, "\x25AB")},
		{"\\Box", () => new ESymbol(Op, "\x25A1")},
		{"\\wr", () => new ESymbol(Ord, "\x2240")},
		{"\\!", () => new ESpace("-0.167em")},
		{"\\,", () => new ESpace("0.167em")},
		{"\\>", () => new ESpace("0.222em")},
		{"\\:", () => new ESpace("0.222em")},
		{"\\;", () => new ESpace("0.278em")},
		// {"~", () => new ESpace("0.333em")},
		{"\\quad", () => new ESpace("1em")},
		{"\\qquad", () => new ESpace("2em")},
		{"\\arccos", () => new EMathOperator("arccos")},
		{"\\arcsin", () => new EMathOperator("arcsin")},
		{"\\arctan", () => new EMathOperator("arctan")},
		{"\\arg", () => new EMathOperator("arg")},
		{"\\cos", () => new EMathOperator("cos")},
		{"\\cosh", () => new EMathOperator("cosh")},
		{"\\cot", () => new EMathOperator("cot")},
		{"\\coth", () => new EMathOperator("coth")},
		{"\\csc", () => new EMathOperator("csc")},
		{"\\deg", () => new EMathOperator("deg")},
		{"\\det", () => new EMathOperator("det")},
		{"\\dim", () => new EMathOperator("dim")},
		{"\\exp", () => new EMathOperator("exp")},
		{"\\gcd", () => new EMathOperator("gcd")},
		{"\\hom", () => new EMathOperator("hom")},
		{"\\inf", () => new EMathOperator("inf")},
		{"\\ker", () => new EMathOperator("ker")},
		{"\\lg", () => new EMathOperator("lg")},
		{"\\lim", () => new EMathOperator("lim")},
		{"\\liminf", () => new EMathOperator("liminf")},
		{"\\limsup", () => new EMathOperator("limsup")},
		{"\\ln", () => new EMathOperator("ln")},
		{"\\log", () => new EMathOperator("log")},
		{"\\max", () => new EMathOperator("max")},
		{"\\min", () => new EMathOperator("min")},
		{"\\Pr", () => new EMathOperator("Pr")},
		{"\\sec", () => new EMathOperator("sec")},
		{"\\sin", () => new EMathOperator("sin")},
		{"\\sinh", () => new EMathOperator("sinh")},
		{"\\sup", () => new EMathOperator("sup")},
		{"\\tan", () => new EMathOperator("tan")},
		{"\\tanh", () => new EMathOperator("tanh")},
	};
}

