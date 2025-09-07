using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RCParsing;
using RCParsing.TokenPatterns;

namespace SyntaxColorizer
{
	public static class ColorProvider
	{
		private static Regex wordRegex = new Regex(@"^\w+$", RegexOptions.Compiled);
		private static Regex numberRegex = new Regex(@"^\d+\.?\d*([eE][-+]?\d+)?$", RegexOptions.Compiled);

		public static Color GetColorForToken(ParsedRuleResultBase token)
		{
			// Get the token pattern and fallback to white.
			var pattern = token.Token?.Token;
			if (pattern == null)
				return Color.White;

			// Check if token returns a specific color.
			if (token.TryGetValue<Color>() is Color color && color != default)
				return color;

			switch (pattern)
			{
				case LiteralTokenPattern:
				case LiteralCharTokenPattern:
				case LiteralChoiceTokenPattern:

					if (wordRegex.IsMatch(token.Span))
						return Color.CornflowerBlue; // Keywords

					return Color.SlateGray; // Control symbols

				case RegexTokenPattern:

					if (numberRegex.IsMatch(token.Span))
						return Color.OrangeRed; // Numbers

					if (wordRegex.IsMatch(token.Span))
						return Color.CornflowerBlue; // Keywords

					return Color.SlateGray; // Control symbols

				case IdentifierTokenPattern:
					return Color.MediumPurple; // Identifiers

				case SequenceTokenPattern:
					return Color.DarkOrange; // Mostly used for strings

				case NumberTokenPattern:
					return Color.OrangeRed; // Numbers

				default:
					return Color.White; // Default color for unknown tokens
			}
		}

		public static Color GetColorForComment() => Color.SeaGreen; // Comments
	}
}