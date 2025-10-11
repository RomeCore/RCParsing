using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANTLRToRCParsingConverter
{
	#nullable disable

	public abstract class ParserNode
	{
		public abstract string Format(int depth);
	}

	public class RuleDef : ParserNode
	{
		public string RuleName { get; set; }
		public ParserNode Child { get; set; }

		public override string Format(int depth)
		{
			return
			$"""
			builder.CreateRule("{RuleName}")
			    {Child.Format(depth + 1)};
			""";
		}
	}

	public class Literal : ParserNode
	{
		public string Value { get; set; }

		public bool IsKeyword => Value.All(c => char.IsLetterOrDigit(c) || c == '_');

		public override string Format(int depth)
		{
			if (IsKeyword)
				return ".Keyword(\"" + Value + "\")";
			return ".Literal(\"" + Value + "\")";
		}
	}

	public class Empty : ParserNode
	{
		public override string Format(int depth)
		{
			return ".Empty()";
		}
	}

	public class TokenRef : ParserNode
	{
		public string Name { get; set; }

		public override string Format(int depth)
		{
			if (Name == "EOF")
				return ".EOF()";

			return ".Token(\"" + Name + "\")";
		}
	}

	public class RuleRef : ParserNode
	{
		public string Name { get; set; }

		public override string Format(int depth)
		{
			return ".Rule(\"" + Name + "\")";
		}
	}

	public class Choice : ParserNode
	{
		public ParserNode[] Children { get; set; }

		public override string Format(int depth)
		{
			/*
			
			.Choice(
				b => b.Rule("alt1"),
				b => b.Literal("+"),
				b => b.Token("alt2")
			)

			*/

			if (Children.All(c => c is Literal))
			{
				var literals = Children.Cast<Literal>().ToArray();
				var literalValues = string.Join(", ", literals.Select(l => $"\"{l.Value}\""));
				if (literals.All(c => c.IsKeyword))
					return $".KeywordChoice({literalValues})";
				return $".LiteralChoice({literalValues})";
			}

			var sb = new StringBuilder();
			sb.AppendLine(".Choice(");

			var indent = new string(' ', (depth + 0) * 4);
			var indentp1 = new string(' ', (depth + 1) * 4);
			var indentp2 = new string(' ', (depth + 2) * 4);

			for (int i = 0; i < Children.Length; i++)
			{
				var choice = Children[i];
				var formatted = choice.Format(depth + 2);
				sb.Append(indentp1 + "b => b");

				if (choice is Sequence)
					sb.AppendLine().Append(indentp2);
				sb.Append(formatted);

				if (i < Children.Length - 1)
					sb.Append(',');
				sb.AppendLine();
			}

			sb.Append(indent);
			sb.Append(')');
			
			return sb.ToString();
		}
	}

	public class Sequence : ParserNode
	{
		public ParserNode[] Children { get; set; }

		public override string Format(int depth)
		{
			/*
			
			.Rule("alt1")
				.Literal("+")
				.Token("alt2")

			*/

			var sb = new StringBuilder();
			var indent = new string(' ', depth * 4);

			for (int i = 0; i < Children.Length; i++)
			{
				var choice = Children[i];
				if (i > 0)
					sb.Append(indent);
				sb.Append(choice.Format(depth));
				if (i < Children.Length - 1)
					sb.AppendLine();
			}

			return sb.ToString();
		}
	}

	public class Optional : ParserNode
	{
		public ParserNode Child { get; set; }

		public override string Format(int depth)
		{
			/*
			
			.Optional(b => b.Rule("opt"))

			*/

			return ".Optional(b => b" + Child.Format(depth) + ")";
		}
	}

	public class ZeroOrMore : ParserNode
	{
		public ParserNode Child { get; set; }

		public override string Format(int depth)
		{
			/*
			
			.ZeroOrMore(b => b.Rule("rep"))

			*/

			return ".ZeroOrMore(b => b" + Child.Format(depth) + ")";
		}
	}

	public class OneOrMore : ParserNode
	{
		public ParserNode Child { get; set; }

		public override string Format(int depth)
		{
			/*
			
			.OneOrMore(b => b.Rule("rep"))

			*/

			return ".OneOrMore(b => b" + Child.Format(depth) + ")";
		}
	}
}