using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

namespace Notation;



public enum Alignment
{
	Left,
	Center,
	Right,
	Defautl,
}

public class ArrayLine
{
	public Expr[][]? exprs;
}


#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value 
// when exiting constructor. Consider declaring it as nullable.

public abstract class Expr {
	public Expr parent;
	public float X;
	public float Y;
	public float W;
	public float H;
	public float Scale = 1f;
	public HBox box;
	public Glue glue;

	// public IEnumerable<Expr>? Children {
	// 	get {
	// 		return this switch {
	// 			ENumber  number => null,
	// 			EGrouped group  => group.exprs,
	// 			EIdentifier id  => null,
	// 			EMathOperator m => null,
	// 			ESymbol symbol  => null,
	// 			ESpace  space   => null,
	// 			EBinary binary  => new Expr[]{binary.lhs, binary.rhs},
	// 			ESub    sub     => new Expr[]{sub.e, sub.sub},
	// 			ESuper  super   => new Expr[]{super.e, super.super},
	// 			ESubsup subsup  => new Expr[]{subsup.e, subsup.sub, subsup.up},
	// 			EOver   over    => new Expr[]{over.e, over.over},
	// 			EUnder  under   => new Expr[]{under.e, under.under},
	// 			EUnderover uo   => new Expr[]{uo.e, uo.under, uo.over}, 
	// 			EUp     up      => new Expr[]{up.e, up.up},
	// 			EDown   down    => new Expr[]{down.e, down.down},
	// 			EDownUp downup  => new Expr[]{downup.e, downup.down, downup.up},
	// 			EUnary  unary   => new Expr[]{unary.operand},
	// 			EScaled scaled  => new Expr[]{scaled.operand},
	// 			EStretchy stret => new Expr[]{stret.operand},
	// 			EArray  array   => null,
	// 			EText   text    => null,
	// 			_ => throw new ArgumentException(),
	// 		};
	// 	}
	// }

	public string Str {
		get {
			return this switch {
				ENumber  number => number.str,
				EGrouped group  => "Group expr",
				EIdentifier id  => id.str,
				EMathOperator m => m.str,
				ESymbol symbol  => symbol.str,
				ESpace  space   => space.str,
				EBinary binary  => binary.str,
				ESub    sub     => "Sub expr",
				ESuper  super   => "Super expr",
				ESubsup subsup  => "Subsup expr",
				EOver   over    => "Over expr",
				EUnder  under   => "Under expr",
				EUnderover uo   => "Underover expr", 
				EUp     up      => "Up expr",
				EDown   down    => "Down expr",
				EDownUp downup  => "DownUp expr",
				EUnary  unary   => unary.str,
				EScaled scaled  => scaled.str,
				EStretchy stret => "Stretch expr",
				EArray  array   => "Array expr",
				EText   text    => "Text expr",
				_ => throw new ArgumentException(this.ToString()),					
			};
		}
	}

	/// <summary>must be used only on Leef-node exprs</summary>
	public string TeXStr {
		get {
			Assert(IsLeafNode());
			
			return this switch {
				ENumber  number => number.str,
				// EGrouped group  => "Group expr",
				EIdentifier id  => id.str,
				EMathOperator m => m.str,
				ESymbol symbol  => symbol.str,
				ESpace  space   => space.str,
				// EBinary binary  => binary.str,
				// ESub    sub     => string.Empty,
				// ESuper  super   => string.Empty,
				// ESubsup subsup  => string.Empty,
				// EOver   over    => string.Empty,
				// EUnder  under   => string.Empty,
				// EUnderover uo   => string.Empty, 
				// EUp     up      => up.up.TeXStr,
				// EDown   down    => down.down.TeXStr,
				// EDownUp downup  => string.Empty,
				EUnary  unary   => unary.str,
				// EScaled scaled  => scaled.str,
				// EStretchy stret => string.Empty,
				// EArray  array   => string.Empty,
				EText   text    => text.rhs_str,
				_ => throw new NotImplementedException($"exp_kind: {this.ToString()} not implemented yet"),								
			};
		}
	}

	public bool IsLeafNode() {
		var _type = this.GetType();

		return _type.IsAssignableTo(typeof(EIdentifier)) ||
			_type.IsAssignableTo(typeof(EMathOperator)) ||
			_type.IsAssignableTo(typeof(ENumber)) ||
			_type.IsAssignableTo(typeof(ESymbol)) ||
			_type.IsAssignableTo(typeof(ESpace)) ||
			_type.IsAssignableTo(typeof(EUnary)) ||
			_type.IsAssignableTo(typeof(EText));
	}

	// public ref struct Enumerator 
	// {
	// 	Expr? prev_node;
	// 	Expr? current_node;
	// 	Expr[] children;
	// 	int child_idx;
		
	// 	public Enumerator(Expr node) 
	// 	{
	// 		prev_node = null;
	// 		current_node = node;
	// 		children = node.Children?.ToArray() ?? throw new NullReferenceException();
	// 		child_idx = 0;
	// 	}

	// 	public Expr Current => current_node!;

	// 	public bool MoveNext() {
	// 		if(children == null) {
	// 			child_idx = 1;
	// 			children = prev_node.Children?.ToArray() ?? throw new NullReferenceException();
	// 			current_node = children[child_idx];
	// 			child_idx += 1;

	// 			return true;				
	// 		}
	// 		else if(child_idx + 1 < children.Length) {
	// 			current_node = children[child_idx];
	// 			child_idx += 1;

	// 			return true;
	// 		}
	// 		else if(child_idx + 1 >= children.Length) {
	// 			child_idx = 1;
	// 			children = prev_node.Children?.ToArray() ?? throw new NullReferenceException();
	// 			current_node = children[child_idx];
	// 			child_idx += 1;

	// 			return true;
	// 		}	
	// 		else if(prev_node == null) {
	// 			return false;        
	// 		}
	// 		else {
	// 			return false;
	// 		}				
	// 	}		
	// }

	// public Enumerator GetEnumerator() {
	// 	return new Enumerator(this);
	// }
}

public class ENumber : Expr {
	public string str;
}

public class EGrouped : Expr {
	public List<Expr> exprs;
}

public class EIdentifier : Expr {
	public string str;
}

public class EMathOperator : Expr {
	public string str;

	public EMathOperator(string msg) {
		str = msg;
	}
}

public class ESymbol : Expr {
	public TeXSymbolType symbol_type;
	public string str;
	
	public ESymbol(int id, string msg) : base() {
		symbol_type = (TeXSymbolType)id;
		str = msg;
	}
}

public class ESpace : Expr {
	public string str;	

	public ESpace(string msg) {
		str = msg;
	}
}

public class EBinary : Expr {
	public string str;
	public Expr lhs;
	public Expr rhs;
}

public class ESub : Expr {
	public Expr e;
	public Expr sub;
}

public class ESuper : Expr {
	public Expr e;
	public Expr super;	
}

public class ESubsup : Expr {
	public Expr e;
	public Expr sub;
	public Expr up;
}

public class EOver : Expr {
	public Expr e;
	public Expr over;

	public EOver(){}

	public EOver(Expr expr) {
		// e = null;
		over = expr;
	}
}

public class EUnder : Expr {
	public Expr e;
	public Expr under;

	public EUnder(){}

	public EUnder(Expr expr) {
		under = expr;
	}
}

public class EUnderover : Expr {
	public Expr e;
	public Expr under;
	public Expr over;
}

public class EUp : Expr {
	public Expr e;
	public Expr up;
}

public class EDown : Expr {
	public Expr e;
	public Expr down;
}

public class EDownUp : Expr {
	public Expr e;
	public Expr down;
	public Expr up;
}

public class EUnary : Expr {
	public string str;
	public Expr operand;
}

public class EScaled : Expr {
	public string str;
	public float scale;
	public Expr operand;
}

public class EStretchy : Expr {
	public Expr operand;
}

public class EArray : Expr {
	public Alignment[] alignments;
	public ArrayLine[] arraylines;
}

public class EText : Expr {
	public string lhs_str;
	public string rhs_str;

	public EText(string str0, string str1) {
		lhs_str = str0;
		rhs_str = str1;
	}
}

public struct HBox {
	public float X;
	public float Y;
	public float W;
	public float H;
	public float Depth;
}

public struct Glue {
	public float Left;
	public float Right;
	public float Top;
	public float Bottom;
}


public struct Atom {
	public string Str;
	public TokenId Id;
}

#pragma warning restore CS8618 // Non-nullable variable must contain a non-null value 
// when exiting constructor. Consider declaring it as nullable.


#pragma warning disable CS8618

abstract class Box {
	public float X;
	public float Y;
	public float W;
	public float H;
	public float Size;

	public Box? parent;
}

class FraqBox : Box {
	public Box upper;
	public Box lower;
}

class OperatorBox : Box {
	
}

class AccentBox : Box {

}

#pragma warning restore CS8618
