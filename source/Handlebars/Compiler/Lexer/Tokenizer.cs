﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Handlebars.Compiler.Lexer
{
    internal class Tokenizer
    {
        private readonly HandlebarsConfiguration _configuration;

        private static Parser _wordParser = new WordParser();
        private static Parser _literalParser = new LiteralParser();
        private static Parser _commentParser = new CommentParser();
        private static Parser _partialParser = new PartialParser();
        //TODO: structure parser

        public Tokenizer(HandlebarsConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<Token> Tokenize(TextReader source)
        {
            try
            {
                return Parse(source);
            }
            catch (Exception ex)
            {
                throw new HandlebarsParserException("An unhandled exception occurred while trying to compile the template", ex);
            }
        }

        private IEnumerable<Token> Parse(TextReader source)
        {
            bool inExpression = false;
            var buffer = new StringBuilder();
            var node = source.Read();
            while (true)
            {
                if (node == -1)
                {
                    if (buffer.Length > 0)
                    {
                        if (inExpression)
                        {
                            throw new InvalidOperationException("Reached end of template before expression was closed");
                        }
                        else
                        {
                            yield return Token.Static(buffer.ToString());
                        }
                    }
                    break;
                }
                if (inExpression)
                {
                    Token token = null;
                    token = token ?? _wordParser.Parse(source);
                    token = token ?? _literalParser.Parse(source);
                    token = token ?? _commentParser.Parse(source);
                    token = token ?? _partialParser.Parse(source);

                    if (token != null)
                    {
                        yield return token;
                    }

                    if ((char)node == '}' && (char)source.Read() == '}')
                    {
                        bool escaped = true;
                        if ((char)source.Peek() == '}')
                        {
                            node = source.Read();
                            escaped = false;
                        }
                        node = source.Read();
                        yield return Token.EndExpression(escaped);
                        inExpression = false;
                    }
                    else if (char.IsWhiteSpace((char)node) || char.IsWhiteSpace((char)source.Peek()))
                    {
                        node = source.Read();
                        continue;
                    }
                    else
                    {
                        if (token == null)
                        {
                            throw new HandlebarsParserException("Reached unparseable token in expression");
                        }
                        node = source.Read();
                    }
                }
                else
                {
                    if ((char)node == '{' && (char)source.Peek() == '{')
                    {
                        bool escaped = true;
                        node = source.Read();
                        if ((char)source.Peek() == '{')
                        {
                            node = source.Read();
                            escaped = false;
                        }
                        yield return Token.Static(buffer.ToString());
                        yield return Token.StartExpression(escaped);
                        buffer = new StringBuilder();
                        inExpression = true;
                    }
                    else
                    {
                        buffer.Append((char)node);
                        node = source.Read();
                    }
                }
            }
        }
    }
}

