using System;
using System.Collections.Generic;

namespace droll
{
    public partial class Roll
    {
        public class Token
        {
            public enum TokenType
            {
                Number,
                Symbol
            }

            public TokenType Type { get; }
            public string Data { get; }
            public int Pos { get; }

            public Token(TokenType type, string data, int pos)
            {
                Data = data;
                Pos = pos;
                Type = type;
            }
        }

        public class LexerException : Exception
        {
            public string Message { get; }

            public LexerException(string message)
            {
                Message = message;
            }
        }

        private class Lexer
        {
            

            private readonly string _input;
            private int _idx;

            private char Current => _input[_idx];
            private bool IsEof => _idx >= _input.Length;

            public Lexer(string input)
            {
                _input = input;
            }

            private Token Number()
            {
                var buf = "";

                while (!IsEof && Char.IsNumber(Current))
                {
                    buf += Current;
                    _idx++;
                }

                return new Token(Token.TokenType.Number, buf, _idx + 1);
            }

            private Token Symbol()
            {
                return new Token(Token.TokenType.Symbol, _input[_idx++].ToString(), _idx + 1);
            }

            private bool IsSymbol(char c) => Char.IsLetter(c) || Char.IsSymbol(c) || Char.IsPunctuation(c);

            public List<Token> GetTokens()
            {
                var retval = new List<Token>();

                var len = _input.Length;
                while (!IsEof)
                {
                    var c = _input[_idx];

                    if (Char.IsWhiteSpace(c))
                        _idx++;

                    else if (Char.IsNumber(c))
                        retval.Add(Number());

                    else if (IsSymbol(c))
                        retval.Add(Symbol());
                    else
                        throw new LexerException($"Unknown character \"{c}\" at {_idx +1 }");
                }

                return retval;
            }
        }

        public abstract class Expr
        {
            protected string GetRepresentation(int times, int sides) => $"{times}d{sides}";

            public abstract IEnumerable<DiceResult> Execute(Random rng);
        }

        public class ParseException : Exception
        {
            public string Message { get; }

            public ParseException()
            {

            }

            public ParseException(string msg)
            {
                Message = msg;
            }
        }

        private class Parser
        {
            

            private readonly List<Token> _tokens;
            private int _idx;
            private bool IsEof => _idx >= _tokens.Count-1;

            public Token Current => _tokens[_idx];

            public Parser(Lexer lexer)
            {
                _tokens = lexer.GetTokens();
            }

            private int ParseNumberToken(Token tkTimes, int min, int max)
            {
                if (!int.TryParse(tkTimes.Data, out int times))
                    throw new ParseException();

                if (min > times || times > max)
                    throw new ParseException($"This number token can only be multiplied by numbers in the range of [{min}, {max}]. Provided: {times}.");

                return times;
            }

            private void VerifySymbolToken(Token symToken, string target)
            {
                if (!symToken.Data.Equals(target, StringComparison.OrdinalIgnoreCase))
                    throw new ParseException();
            }

            private DiceExpr Dice()
            {
                // d20
                // 3d20
                const string dString = "d";
                const int timesMin = 1;
                const int timesMax = 20;
                const int sidesMin = 2;
                const int sidesMax = 100;

                var times = 1;

                if (Current.Type == Token.TokenType.Number)
                {
                    times = ParseNumberToken(Current, timesMin, timesMax);
                    _idx++;
                }

                VerifySymbolToken(Match(Token.TokenType.Symbol), dString);
                var sides = ParseNumberToken(MatchNext(Token.TokenType.Number), sidesMin, sidesMax);
                _idx++;

                return new DiceExpr(times, sides);
            }

            private MulExpr Mul()
            {
                const string mulString = "*";
                const int maxTimes = 10;
                const int minTimes = 1;

                // 6 * 6d4
                // 6 * DiceExpr

                var times = ParseNumberToken(Match(Token.TokenType.Number), minTimes, maxTimes);
                VerifySymbolToken(MatchNext(Token.TokenType.Symbol), mulString);

                _idx++; // advance caret or Dice() will be off by one.
                var dice = Dice();

                return new MulExpr(dice, times);
            }

            private Token Match(Token.TokenType type)
            {
                if (Current.Type != type)
                    throw new ParseException();

                return Current;
            }

            private Token MatchNext(Token.TokenType type)
            {
                _idx++;
                return Match(type);
            }

            public List<Expr> GetExpressions()
            {
                var retval = new List<Expr>();
                bool success;

                void TryParse(Func<Expr> parseFunc)
                {
                    if (success) return;

                    var prevIdx = _idx;

                    try
                    {
                        var val = parseFunc();
                        retval.Add(val);
                        success = true;
                    }
                    catch (ParseException)
                    {
                        _idx = prevIdx;
                    }
                }

                while (!IsEof)
                {
                    success = false;
                    switch (Current.Type)
                    {
                        case Token.TokenType.Number:
                            TryParse(Mul);
                            TryParse(Dice);
                            break;
                        case Token.TokenType.Symbol:
                            TryParse(Dice);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if(!success)
                        throw new ParseException($"Unknown syntax starting at {Current.Pos}, data: {Current.Data}");
                }

                return retval;
            }
        }

        public static List<Expr> Parse(string query) => new Parser(new Lexer(query)).GetExpressions();
    }
}