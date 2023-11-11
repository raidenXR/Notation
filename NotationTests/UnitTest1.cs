using Notation;
using System.Diagnostics;
namespace NotationTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var str = "f(x) = 3.4 * x^3 - 5.6 * x + 7 + N_A";
        var lexer = new Lexer(str);
        var tokens = new List<Token>(10);

        foreach(var token in lexer) {
            if(token.Id == TokenId.Bad || token.Id == TokenId.Whitespace) continue;

            Debug.WriteLine(token.Id);

            
        }

        Debug.Assert(tokens[0].Id == TokenId.Identifier);
        Debug.Assert(tokens[1].Id == TokenId.Symbol);
        Debug.Assert(tokens[2].Id == TokenId.Identifier);
        Debug.Assert(tokens[3].Id == TokenId.Symbol);
        Debug.Assert(tokens[4].Id == TokenId.Number);
    }
}