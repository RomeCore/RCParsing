using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// The match function used for matching the <see cref="CustomParserRule"/>.
	/// </summary>
	/// <param name="self">The current custom parser rule.</param>
	/// <param name="context">The current parser context.</param>
	/// <param name="settings">The current parser settings.</param>
	/// <param name="childSettings">Parser settings for child rules.</param>
	/// <param name="children">The child parser rules specified during rule construction.</param>
	/// <returns>The <see cref="ParsedRule"/> result of matching rule.</returns>
	public delegate ParsedRule CustomRuleParseFunction(
		CustomParserRule self,
		ParserContext context,
		ParserSettings settings,
		ParserSettings childSettings,
		int[] children
	);

	/// <summary>
	/// Represents a custom parser rule that can be used for user-defined parsing logic.
	/// </summary>
	public class CustomParserRule : ParserRule
	{
		/// <summary>
		/// The function used for parsing this custom rule.
		/// </summary>
		public CustomRuleParseFunction ParseFunction { get; }

		/// <summary>
		/// The child parser rule IDs.
		/// </summary>
		public IReadOnlyList<int> Children { get; }

		/// <summary>
		/// The string representation of this custom parser rule.
		/// </summary>
		public string StringRepresentation { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomParserRule"/> class.
		/// </summary>
		/// <param name="matchFunction">The function used for matching the parser rule.</param>
		/// <param name="childrenIds">IDs of child parser rules.</param>
		/// <param name="stringRepresentation">The string representation of this custom parser rule.</param>
		public CustomParserRule(CustomRuleParseFunction matchFunction,
			IEnumerable<int> childrenIds, string stringRepresentation = "Custom")
		{
			ParseFunction = matchFunction ?? throw new ArgumentNullException(nameof(matchFunction));
			Children = childrenIds?.ToArray().AsReadOnlyList() ?? throw new ArgumentNullException(nameof(childrenIds));
			StringRepresentation = stringRepresentation ?? throw new ArgumentNullException(nameof(stringRepresentation));
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		private int[] _children;
		private ParseDelegate parseFunction;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			_children = Children.ToArray();

			ParsedRule Parse(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				return ParseFunction(this, context, settings, childSettings, _children);
			}

			parseFunction = WrapParseFunction(Parse, initFlags);
		}

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction(ref context, ref settings, ref childSettings);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (Children.Count == 0)
				return StringRepresentation;

			return $"{StringRepresentation}\n{string.Join(Environment.NewLine, Children.Select(i => GetRule(i).ToString(remainingDepth - 1))).Indent("  ")}";
		}
		
		public override string ToStackTraceString(int remainingDepth, int prevChild)
		{
			if (Children.Count == 0)
				return StringRepresentation;

			return $"{StringRepresentation}\n{string.Join(Environment.NewLine, Children.Select(i => GetRule(i).ToString(remainingDepth - 1) + (prevChild == i ? " <-- here" : ""))).Indent("  ")}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is CustomParserRule other &&
				   ParseFunction == other.ParseFunction &&
				   Children.SequenceEqual(other.Children) &&
				   StringRepresentation == other.StringRepresentation;
		}

		public override int GetHashCode()
		{
			var hc = base.GetHashCode();
			hc = (hc * 397) ^ ParseFunction.GetHashCode();
			hc = (hc * 397) ^ Children.GetSequenceHashCode();
			hc = (hc * 397) ^ StringRepresentation.GetHashCode();
			return hc;
		}
	}
}