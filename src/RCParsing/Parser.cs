using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RCParsing.ParserRules;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents a parser for parsing text data into AST.
	/// </summary>
	public partial class Parser
	{
		private readonly TokenPattern[] _tokenPatterns;
		private readonly ParserRule[] _rules;
		private readonly BarrierTokenizer[] _tokenizers;
		private readonly Dictionary<string, int> _tokenPatternsAliases = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _rulesAliases = new Dictionary<string, int>();
		private readonly int _mainRuleId = -1;

		/// <summary>
		/// Gets the token patterns registered in this parser.
		/// </summary>
		public IReadOnlyList<TokenPattern> TokenPatterns { get; }

		/// <summary>
		/// Gets the rules registered in this parser.
		/// </summary>
		public IReadOnlyList<ParserRule> Rules { get; }

		/// <summary>
		/// Gets the barrier tokenizers used by when tokenizing input strings.
		/// </summary>
		public IReadOnlyList<BarrierTokenizer> Tokenizers { get; }

		/// <summary>
		/// Gets the main settings used by this parser.
		/// </summary>
		public ParserMainSettings MainSettings { get; }

		/// <summary>
		/// Gets the global settings used by this parser for configuring rules.
		/// </summary>
		public ParserSettings GlobalSettings { get; }

		/// <summary>
		/// Gets the main rule ID that used by default, or -1 if not specified,
		/// </summary>
		public int MainRuleId => _mainRuleId;
		
		/// <summary>
		/// Gets the main rule that used by default, if any.
		/// </summary>
		public ParserRule? MainRule => _mainRuleId == -1 ? null : _rules[_mainRuleId];

		/// <summary>
		/// Initializes a new instance of the <see cref="Parser"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to use. </param>
		/// <param name="rules">The rules to use.</param>
		/// <param name="tokenizers">The barrier tokenizers to use.</param>
		/// <param name="mainSettings">The main settings to use for the parser itself.</param>
		/// <param name="globalSettings">The global settings to use.</param>
		/// <param name="mainRuleId">The ID of the main rule to use.</param>
		/// <param name="initFlags">The initialization flags to use. Default is <see cref="ParserInitFlags.None"/>.</param>
		public Parser(IReadOnlyList<TokenPattern> tokenPatterns, IReadOnlyList<ParserRule> rules,
			IReadOnlyList<BarrierTokenizer> tokenizers, ParserMainSettings mainSettings, ParserSettings globalSettings,
			int mainRuleId = -1, ParserInitFlags initFlags = ParserInitFlags.None)

			: this(tokenPatterns, rules, tokenizers, mainSettings, globalSettings, mainRuleId, e => initFlags)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Parser"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to use. </param>
		/// <param name="rules">The rules to use.</param>
		/// <param name="tokenizers">The barrier tokenizers to use.</param>
		/// <param name="mainSettings">The main settings to use for the parser itself.</param>
		/// <param name="globalSettings">The global settings to use.</param>
		/// <param name="mainRuleId">The ID of the main rule to use.</param>
		/// <param name="initFlagsFactory">The initialization flags factory to use.</param>
		public Parser(IReadOnlyList<TokenPattern> tokenPatterns, IReadOnlyList<ParserRule> rules,
			IReadOnlyList<BarrierTokenizer> tokenizers, ParserMainSettings mainSettings, ParserSettings globalSettings,
			int mainRuleId = -1, Func<ParserElement, ParserInitFlags>? initFlagsFactory = null)
		{
			if (mainSettings.tabSize <= 0)
				mainSettings.tabSize = 4;

			_tokenPatterns = tokenPatterns.ToArray();
			TokenPatterns = _tokenPatterns.AsReadOnlyList();
			_rules = rules.ToArray();
			Rules = _rules.AsReadOnlyCollection();
			_tokenizers = tokenizers.ToArray();
			Tokenizers = _tokenizers.AsReadOnlyList();
			MainSettings = mainSettings;
			GlobalSettings = globalSettings;

			for (int i = 0; i < rules.Count; i++)
			{
				var rule = rules[i];
				if (rule.Parser != null)
					throw new InvalidOperationException("Parser already set for a rule.");
				rule.Id = i;
				rule.Parser = this;

				foreach (var alias in rule.Aliases)
				{
					if (_rulesAliases.ContainsKey(alias))
						throw new InvalidOperationException("Alias already used by another rule.");
					_rulesAliases.Add(alias, rule.Id);
				}
			}
			
			for (int i = 0; i < tokenPatterns.Count; i++)
			{
				var token = tokenPatterns[i];
				if (token.Parser != null)
					throw new InvalidOperationException("Parser already set for a token.");
				token.Id = i;
				token.Parser = this;

				foreach (var alias in token.Aliases)
				{
					if (_tokenPatternsAliases.ContainsKey(alias))
						throw new InvalidOperationException("Alias already used by another token pattern.");
					_tokenPatternsAliases.Add(alias, token.Id);
				}
			}

			_mainRuleId = mainRuleId;

			ParserInitFlags TokenFactory(TokenPattern token)
			{
				return initFlagsFactory?.Invoke(token) ?? ParserInitFlags.None;
			}
			ParserInitFlags RuleFactory(ParserRule rule)
			{
				return initFlagsFactory?.Invoke(rule) ?? ParserInitFlags.None;
			}

			var initFlagsTokenMap = tokenPatterns.ToDictionary(t => t.Id, TokenFactory);
			var initFlagsRuleMap = rules.ToDictionary(t => t.Id, RuleFactory);

			foreach (var pattern in tokenPatterns)
				pattern.PreInitializeInternal(initFlagsTokenMap[pattern.Id]);
			foreach (var rule in rules)
				rule.PreInitializeInternal(initFlagsRuleMap[rule.Id]);

			foreach (var pattern in tokenPatterns)
				pattern.InitializeInternal(initFlagsTokenMap[pattern.Id]);
			foreach (var rule in rules)
				rule.InitializeInternal(initFlagsRuleMap[rule.Id]);

			foreach (var pattern in tokenPatterns)
				pattern.PostInitializeInternal(initFlagsTokenMap[pattern.Id]);
			foreach (var rule in rules)
				rule.PostInitializeInternal(initFlagsRuleMap[rule.Id]);
		}

		/// <summary>
		/// Gets a token pattern by its alias.
		/// </summary>
		/// <param name="alias">The alias of the token pattern.</param>
		/// <returns>The token pattern with the specified alias.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no token pattern is found with the specified alias.</exception>
		public TokenPattern GetTokenPattern(string alias)
		{
			if (_tokenPatternsAliases.TryGetValue(alias, out var id))
				return _tokenPatterns[id];
			throw new InvalidOperationException($"Token pattern not found with alias '{alias}'.");
		}

		/// <summary>
		/// Gets a token pattern by its ID.
		/// </summary>
		/// <param name="id">The ID of the token pattern.</param>
		/// <returns>The token pattern with the specified alias.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the provided ID is out of range.</exception>
		public TokenPattern GetTokenPattern(int id)
		{
			if (id >= 0 && id < _tokenPatterns.Length)
				return _tokenPatterns[id];
			throw new ArgumentOutOfRangeException(nameof(id), "Invalid token pattern ID.");
		}

		/// <summary>
		/// Gets a rule by its alias.
		/// </summary>
		/// <param name="alias">The alias of the rule.</param>
		/// <returns>The rule with the specified alias.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no rule is found with the specified alias.</exception>
		public ParserRule GetRule(string alias)
		{
			if (_rulesAliases.TryGetValue(alias, out var id))
				return _rules[id];
			throw new InvalidOperationException($"Rule not found with alias '{alias}'.");
		}

		/// <summary>
		/// Gets a rule by its ID.
		/// </summary>
		/// <param name="id">The ID of the rule.</param>
		/// <returns>The rule with the specified ID.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the provided ID is out of range.</exception>
		public ParserRule GetRule(int id)
		{
			if (id >= 0 && id < _rules.Length)
				return _rules[id];
			throw new ArgumentOutOfRangeException(nameof(id), "Invalid rule ID.");
		}

		/// <summary>
		/// Creates a <see cref="ParsingException"/> from the current parser context.
		/// </summary>
		private static ParsingException ExceptionFromContext(ParserContext context)
		{
			return new ParsingException(context);
		}



		/// <summary>
		/// Creates a parser context for the given input string.
		/// </summary>
		/// <param name="input">The input string to parse.</param>
		/// <param name="parameter">The optional parameter to pass to the token patterns. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A new parser context initialized with the provided input string.</returns>
		public ParserContext CreateContext(string input, object? parameter = null)
		{
			return new ParserContext(this, input, parameter);
		}

		/// <summary>
		/// Creates a parser context for the given input string.
		/// </summary>
		/// <param name="input">The input string to parse.</param>
		/// <param name="startIndex">The starting index in the input string to parse.</param>
		/// <param name="parameter">The optional parameter to pass to the token patterns. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A new parser context initialized with the provided input string.</returns>
		public ParserContext CreateContext(string input, int startIndex, object? parameter = null)
		{
			return new ParserContext(this, input, parameter)
			{
				position = startIndex
			};
		}

		/// <summary>
		/// Creates a parser context for the given input string.
		/// </summary>
		/// <param name="input">The input string to parse.</param>
		/// <param name="startIndex">The starting index in the input string to parse.</param>
		/// <param name="length">The number of characters to parse from the input string.</param>
		/// <param name="parameter">The optional parameter to pass to the token patterns. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A new parser context initialized with the provided input string.</returns>
		public ParserContext CreateContext(string input, int startIndex, int length, object? parameter = null)
		{
			return new ParserContext(this, input, parameter)
			{
				position = startIndex,
				maxPosition = startIndex + length
			};
		}
	}
}