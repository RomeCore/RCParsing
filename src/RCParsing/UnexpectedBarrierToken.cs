using RCParsing.TokenPatterns;

namespace RCParsing
{
	/// <summary>
	/// Represents an unexpected barrier token encountered during parsing.
	/// </summary>
	public class UnexpectedBarrierToken
	{
		/// <summary>
		/// Gets the parser context used for parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets the intermediate barrier token that caused the error.
		/// </summary>
		public IntermediateBarrierToken Barrier { get; }

		/// <summary>
		/// Gets the intermediate barrier token pattern that caused the error.
		/// </summary>
		public BarrierTokenPattern TokenPattern { get; }

		/// <summary>
		/// Gets the ID of the barrier token that was unexpected.
		/// </summary>
		public int Id => TokenPattern.Id;

		/// <summary>
		/// Gets the alias of the barrier token that was unexpected.
		/// </summary>
		public string Alias => TokenPattern.MainAlias;

		/// <summary>
		/// Initializes a new instance of the <see cref="UnexpectedBarrierToken"/> class.
		/// </summary>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="barrier">The intermediate barrier token that caused the error.</param>
		public UnexpectedBarrierToken(ParserContext context, IntermediateBarrierToken barrier)
		{
			Context = context;
			Barrier = barrier;
			TokenPattern = (BarrierTokenPattern)context.parser.GetTokenPattern(barrier.tokenId);
		}
	}
}