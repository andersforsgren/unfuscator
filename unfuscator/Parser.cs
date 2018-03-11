using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Unfuscator.Core
{
    [DataContract]
    [DebuggerDisplay("{ToString()}")]
    public class Signature
    {
        [DataMember]
        public string MethodName { get; set; }

        [DataMember]
        public List<string> Args { get; set; }


        public static Signature ParseDotfuscator(string signature, string methodName)
        {
            return ParseSignature(signature, SignatureFormat.ReturnType | SignatureFormat.GenericArity | SignatureFormat.GenericTypes, methodName);
        }

        public static Signature ParseStackTraceLine(string trace)
        {
            return ParseSignature(trace, SignatureFormat.AtPrefix | SignatureFormat.MethodName | SignatureFormat.GenericArity | SignatureFormat.ParameterNames);
        }

        private static Signature ParseSignature(string input, SignatureFormat signatureFormat, string methodName = null)
        {
            Tokenizer tokenizer = new Tokenizer(input);
            tokenizer.Eat(TokenType.WhiteSpace);
            if (signatureFormat.Has(SignatureFormat.AtPrefix))
            {
                tokenizer.Expect("at");
                tokenizer.Expect(TokenType.WhiteSpace);
            }

            if (signatureFormat.Has(SignatureFormat.ReturnType))
            {
                ParseType(tokenizer, signatureFormat); // Ignore return type.
            }
            if (signatureFormat.Has(SignatureFormat.MethodName))
            {
                if (methodName != null)
                    throw new ArgumentException("Don't pass method name");
                methodName = tokenizer.Expect(TokenType.Identifier).ToString(input);
            }
            tokenizer.Expect(TokenType.LeftParen);
            var args = ParseArgumentList(tokenizer, TokenType.RightParen, signatureFormat);
            tokenizer.Expect(TokenType.RightParen);
            return new Signature(methodName, args.ToList());
        }

        private static IReadOnlyList<string> ParseArgumentList(Tokenizer tokenizer, TokenType endToken, SignatureFormat signatureFormat)
        {
            List<string> args = new List<string>();
            do
            {
                bool hasArg = NextArgType(tokenizer, signatureFormat, endToken, out var argType);
                if (hasArg)
                {
                    args.Add(argType);
                    var t = tokenizer.Peek();

                    if (!t.HasValue)
                        throw new FormatException("Expected ')' found EOF");
                    if (t.Value.Kind == endToken)
                    {
                        return args;
                    }
                    tokenizer.Expect(TokenType.Comma);
                }
                else
                {
                    return args;
                }
            } while (true);
        }

        private static bool NextArgType(Tokenizer tokenizer, SignatureFormat signatureFormat, TokenType endBracket, out string argType)
        {
            var next = tokenizer.Peek();
            argType = null;
            if (next.HasValue && next.Value.Kind == endBracket)
                return false;
            tokenizer.Eat(TokenType.WhiteSpace);
            argType = ParseType(tokenizer, signatureFormat);

            int lastDot = Math.Max(argType.LastIndexOf('.'), argType.LastIndexOf('/'));
            if (lastDot > 0)
                argType = argType.Substring(lastDot + 1);

            if (signatureFormat.Has(SignatureFormat.ParameterNames))
            {
                tokenizer.Expect(TokenType.WhiteSpace);
                tokenizer.Expect(TokenType.Identifier);
            }
            return true;
        }

        private static string ParseType(Tokenizer tokenizer, SignatureFormat signatureFormat)
        {
            var t = tokenizer.Expect(TokenType.Identifier);
            string typeName = t.ToString(tokenizer.Input);//.Replace("/", ".").Replace("+", ".");
            if (typeName == "native")
            {
                throw new NotSupportedException("Native int");
            }
            if (typeName == "unsigned")
            {
                tokenizer.Expect(TokenType.WhiteSpace);
                typeName = tokenizer.Expect(TokenType.Identifier).ToString(tokenizer.Input);
                typeName = ResolveUnsignedPrimitive(typeName);
            }
            else
            {
                typeName = ResolvePrimitive(typeName);
            }

            var arrayOrGenericInfo = tokenizer.Peek();

            if (arrayOrGenericInfo.HasValue && signatureFormat.Has(SignatureFormat.GenericArity) && arrayOrGenericInfo.Value.Kind == TokenType.BackTick)
            {
                tokenizer.Next();
                string arity = tokenizer.Expect(TokenType.Number).ToString(tokenizer.Input);
                typeName = string.Format("{0}`{1}", typeName, arity);
                arrayOrGenericInfo = tokenizer.Peek();
            }

            if (arrayOrGenericInfo.HasValue && signatureFormat.Has(SignatureFormat.GenericTypes) && arrayOrGenericInfo.Value.Kind == TokenType.LeftAngleBracket)
            {
                tokenizer.Next();
                // Comma separated list of types.
                var genericArgs = ParseArgumentList(tokenizer, TokenType.RightAngleBracket, signatureFormat);
                tokenizer.Expect(TokenType.RightAngleBracket);
                if (!signatureFormat.Has(SignatureFormat.GenericArity))
                    typeName = string.Format("{0}`{1}", typeName, genericArgs.Count);
            }

            if (arrayOrGenericInfo.HasValue && arrayOrGenericInfo.Value.Kind == TokenType.Slash)
            {
                // Nested type
                tokenizer.Next();
                string nestedType = ParseType(tokenizer, signatureFormat);
                typeName = string.Format("{0}+{1}", typeName, nestedType);
            }

            typeName = typeName + ParseArraySuffix(tokenizer);

            if (tokenizer.Eat(TokenType.Ampersand))
                typeName += "&";

            return typeName;
        }

        private static string ResolveUnsignedPrimitive(string typeName)
        {
            switch (typeName)
            {
                case "int8": return "Byte";
                case "int16": return "UInt16";
                case "int32": return "UInt32";
                case "int64": return "UInt64";
                default:
                    throw new ArgumentException("Bad unsigned type");
            }
        }

        private static string ResolvePrimitive(string typeName)
        {
            switch (typeName)
            {
                case "float64": return "double";
                case "float32": return "float";
                case "int8": return "SByte";
                case "int16": return "Int16";
                case "int32": return "Int32";
                case "int64": return "Int64";
                case "bool": return "Boolean";
                default:
                    return typeName;
            }
        }

        private static string ParseArraySuffix(Tokenizer tokenizer)
        {
            string s = "";
            do
            {
                Token? token = tokenizer.Peek();
                if (token.HasValue && token.Value.Kind == TokenType.LeftBracket)
                {
                    s += ParseSingleArraySuffix(tokenizer);
                }
                else
                {
                    return s;
                }
            } while (true);
        }

        private static string ParseSingleArraySuffix(Tokenizer tokenizer)
        {
            tokenizer.Eat(TokenType.WhiteSpace);
            tokenizer.Expect(TokenType.LeftBracket);
            tokenizer.Eat(TokenType.WhiteSpace);
            Token? next = tokenizer.Peek();
            if (!next.HasValue)
                throw new FormatException("Expected ','  or ']' found EOF");
            if (next.Value.Kind == TokenType.Comma)
            {
                tokenizer.Next();
                tokenizer.Expect(TokenType.RightBracket);
                return "[,]";
            }
            if (next.Value.Kind == TokenType.RightBracket)
            {
                tokenizer.Next();
                return "[]";
            }
            if (next.Value.Kind == TokenType.Number)
            {
                // [0...,0....]
                tokenizer.Expect(TokenType.Number);
                tokenizer.Expect(TokenType.Dot);
                tokenizer.Expect(TokenType.Dot);
                tokenizer.Expect(TokenType.Dot);
                tokenizer.Expect(TokenType.Comma);
                tokenizer.Expect(TokenType.Number);
                tokenizer.Expect(TokenType.Dot);
                tokenizer.Expect(TokenType.Dot);
                tokenizer.Expect(TokenType.Dot);
                tokenizer.Expect(TokenType.RightBracket);
                return "[,]";
            }
            tokenizer.Fail("Expected array suffix [] or [,] or [N...,N...] found '" + next.Value.ToString(tokenizer.Input) + "' (" + next.Value.Kind + ")" + " after opening '['");
            return null;
        }

        public Signature(string methodName, List<string> args)
        {
            MethodName = methodName;
            Args = args;
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", MethodName, string.Join(", ", Args));
        }

        public Signature WithMethodName(string s)
        {
            return new Signature(s, Args);
        }
    }

    public static class FormatExtensions
    {
        public static bool Has(this SignatureFormat f, SignatureFormat flag)
        {
            return (f & flag) == flag;
        }
    }

    internal struct Token
    {
        public Token(TokenType kind, int from, int to)
        {
            Kind = kind;
            From = from;
            To = to;
        }

        public TokenType Kind { get; }
        public int From { get; }
        public int To { get; }

        public string ToString(string input)
        {
            return input.Substring(From, To - From + 1);
        }
    }

    [Flags]
    public enum SignatureFormat
    {
        None = 0,
        ReturnType = 1,
        MethodName = 2,
        ParameterNames = 4,
        GenericArity = 8,
        GenericTypes = 16,
        AtPrefix = 32,
        ExplicitUnsigned = 64,
    }

    internal enum TokenType
    {
        WhiteSpace,
        Dot,
        Comma,
        LeftAngleBracket,
        RightAngleBracket,
        BackTick,
        LeftParen,
        RightParen,
        Number,
        Identifier,
        LeftBracket,
        RightBracket,
        Slash,
        Plus,
        Ampersand
    }

    internal class Tokenizer
    {
        private readonly string input;
        private int index;
        private Token? currentToken;

        public Tokenizer(string input)
           : this(input, -1)
        {
        }

        private Tokenizer(string input, int index)
        {
            this.input = input;
            this.index = index;
        }

        public string Input => input;

        public Token? CurrentToken => currentToken;

        public int Index => index;

        public Token Expect(TokenType type)
        {
            Next();
            if (!CurrentToken.HasValue)
                throw new FormatException("Expected " + type + " found EOF");
            if (CurrentToken.Value.Kind != type)
                Fail("Expected " + type + " found " + CurrentToken.Value.Kind + " at " + index);
            return CurrentToken.Value;
        }

        internal void Fail(string s)
        {
            throw new FormatException($"Error: {s} in\n{input}\n{new string(' ', index + 1)}^\n");
        }

        public Token Expect(TokenType skip, TokenType expected)
        {
            Next();

            if (!CurrentToken.HasValue)
                throw new FormatException("Expected " + expected + " found EOF");

            if (CurrentToken.Value.Kind == skip)
            {
                Next();
                if (!CurrentToken.HasValue)
                    throw new FormatException("Expected " + expected + " found EOF");
            }
            if (CurrentToken.Value.Kind != expected)
            {
                throw new FormatException("Expected " + expected + " found EOF");
            }
            return CurrentToken.Value;
        }

        public void Expect(string expectedIdentifier)
        {
            string actual = Expect(TokenType.Identifier).ToString(input);
            if (actual != expectedIdentifier)
                Fail($"Expected \'{expectedIdentifier}\' found \'{actual}\'");
        }

        public Token? Peek()
        {
            return new Tokenizer(input, index).Next();
        }

        public Token? Next()
        {
            currentToken = GetNext();
            return currentToken;
        }

        private Token? GetNext()
        {
            if (index + 1 >= input.Length)
            {
                return null;
            }

            index = index + 1;
            char currentChar = input[index];
            switch (currentChar)
            {
                case '(': return new Token(TokenType.LeftParen, index, index);
                case ')': return new Token(TokenType.RightParen, index, index);
                case '<': return new Token(TokenType.LeftAngleBracket, index, index);
                case '>': return new Token(TokenType.RightAngleBracket, index, index);
                case '[': return new Token(TokenType.LeftBracket, index, index);
                case ']': return new Token(TokenType.RightBracket, index, index);
                case '`': return new Token(TokenType.BackTick, index, index);
                case '.': return new Token(TokenType.Dot, index, index);
                case ',': return new Token(TokenType.Comma, index, index);
                case '/': return new Token(TokenType.Slash, index, index);
                case '+': return new Token(TokenType.Plus, index, index);
                case '&': return new Token(TokenType.Ampersand, index, index);
            }

            if (char.IsDigit(currentChar))
            {
                return NumberToken(index);
            }

            if (IsWhitespace(currentChar))
            {
                return WhitespaceToken(index);
            }

            if (IsIdentChcar(currentChar))
            {
                return IdentifierToken(index);
            }
            Fail($"Error, unknown input at position {index} in \'{input}\'");
            return null;
        }

        private bool IsIdentChcar(char currentChar)
        {
            return currentChar == '_' || currentChar == '.' || char.IsLetterOrDigit(currentChar) || currentChar == '/' || currentChar == '+';
        }

        private bool IsWhitespace(char currentChar)
        {
            return currentChar == ' ' || currentChar == '\t';
        }

        private Token NumberToken(int start)
        {
            while (char.IsDigit(PeekChar()))
            {
                NextChar();
            }
            return new Token(TokenType.Number, start, index);
        }

        private Token WhitespaceToken(int start)
        {
            while (Eatc(' ') || Eatc('\t'))
            {
            }
            return new Token(TokenType.WhiteSpace, start, index);
        }

        private Token IdentifierToken(int start)
        {
            while (IsIdentChcar(PeekChar()))
            {
                NextChar();
            }
            return new Token(TokenType.Identifier, start, index);
        }

        private char NextChar()
        {
            return input[index++];
        }

        private char PeekChar()
        {
            return input[index + 1];
        }

        private bool Eatc(char expected)
        {
            if (input[index + 1] == expected)
            {
                index++;
                return true;
            }
            return false;
        }


        public bool Eat(TokenType kind)
        {
            var peeked = Peek();
            if (!peeked.HasValue)
                return false;
            if (peeked.Value.Kind != kind)
                return false;
            Next();
            return true;
        }
    }

}