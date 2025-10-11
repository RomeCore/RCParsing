using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing;

namespace ANTLRToRCParsingConverter
{
	public static class ANTLRParser
	{
		private static readonly Parser parser = CreateParser();

		public static void FillWithRules(ParserBuilder builder)
		{
			builder.Settings.Skip(b => b.Choice(
				b => b.Whitespaces(),
				b => b.Literal("//").TextUntil('\n', '\r')
			).ConfigureForSkip(), ParserSkippingStrategy.SkipBeforeParsingGreedy);

			builder.CreateMainRule()
				.Rule("rule_defs")
				.EOF()

				.Transform(v =>
				{
					var defs = v.SelectValues<RuleDef>(index: 0).ToList();
					GrammarTransformer.Transform(defs);
					return string.Join(Environment.NewLine + Environment.NewLine, defs.Select(d => d.Format(0)));
				});

			builder.CreateToken("Rule_name")
				.Identifier(s => char.IsLower(s), c => char.IsLetterOrDigit(c) || c == '_')
				.Transform(v => v.Text);

			builder.CreateToken("Token_name")
				.Identifier(s => char.IsUpper(s), c => char.IsLetterOrDigit(c) || c == '_')
				.Transform(v => v.Text);

			var strlitEscapes = new Dictionary<string, string>
			{
				// ["\\n"] = "\n",
				// ["\\r"] = "\r",
				// ["\\t"] = "\t",
				["\\'"] = "\\'",
				["\""] = "\\\""
			};

			HashSet<string> strlitForbidden = [ "\n", "\r", "'" ];

			builder.CreateToken("String_literal")
				.Between(
					b => b.Literal('\''),
					b => b.EscapedText(strlitEscapes, strlitForbidden),
					b => b.Literal('\'')
				);

			builder.CreateRule("rule_defs")
				.ZeroOrMore(b => b.Rule("rule_def"));

			builder.CreateRule("rule_def")
				.Token("Rule_name")
				.Literal(':')
				.Rule("rule_expr")
				.Literal(';')

				.Transform(v =>
				{
					var name = v.GetValue<string>(index: 0);
					var expr = v.GetValue<ParserNode>(index: 2);
					return new RuleDef { RuleName =  name, Child = expr };
				});

			builder.CreateRule("rule_choice_expr")
				.OneOrMoreSeparated(b => b.Rule("rule_sequence_expr"), b => b.Literal("|"))

				.Transform(v =>
				{
					var choices = v.SelectArray<ParserNode>();
					if (choices.Length == 1)
						return choices[0];
					return new Choice { Children = choices };
				});

			builder.CreateRule("rule_expr")
				.Rule("rule_choice_expr");

			builder.CreateRule("rule_sequence_expr")
				.OneOrMore(b => b.Rule("rule_quant_expr"))

				.Transform(v =>
				{
					var children = v.SelectArray<ParserNode>();
					if (children.Length == 1)
						return children[0];
					return new Sequence { Children = children };
				});

			builder.CreateRule("rule_quant_expr")
				.Rule("rule_atom_expr")
				.Optional(b => b.LiteralChoice("?", "*", "+"))

				.Transform(v =>
				{
					var atom = v.GetValue<ParserNode>(index: 0);
					var quantifier = v.TryGetValue<string>(index: 1);

					return quantifier switch
					{
						"?" => new Optional { Child = atom },
						"*" => new ZeroOrMore { Child = atom },
						"+" => new OneOrMore { Child = atom },
						_ => atom
					};
				});

			builder.CreateRule("rule_atom_expr")
				.Choice(
					b => b.Literal('(').Rule("rule_expr").Literal(')').TransformSelect(index: 1),
					b => b.Token("Rule_name").Transform(v => new RuleRef { Name = v.Text }),
					b => b.Token("Token_name").Transform(v => new TokenRef { Name = v.Text }),
					b => b.Token("String_literal").Transform(v => new Literal { Value = v.GetIntermediateValue<string>() })
				);
		}
		
		public static Parser CreateParser(Action<ParserBuilder>? buildingAction = null)
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			buildingAction?.Invoke(builder);
			return builder.Build();
		}

		public static string Parse(string input)
		{
			return parser.Parse<string>(input);
		}
	}
}