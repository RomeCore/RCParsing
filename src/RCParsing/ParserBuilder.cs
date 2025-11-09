using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using RCParsing.Building;
using RCParsing.Building.ParserRules;
using RCParsing.Building.TokenPatterns;
using RCParsing.ParserRules;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing
{
	/* Concept (its very old)
	
	var builder = new ParserBuilder();
 
	var identifier_token = builder.CreateToken("identifier").Regex("[a-zA-Z_][a-zA-Z0-9_]*");
	var number_token = builder.CreateToken("number", str => double.Parse(str)).Regex("[0-9]+");
	var esc_text_token = builder.CreateToken("esc_text").EscapedText(charSet: "{}@", EscapingStrategy.DoubleCharacters);
 
	var template_rule = builder.CreateRule("template")
		.Literal("@").Literal("template").Token("identifier").Literal("{").Rule("template_content").Literal("}");

	var template_content_rule = builder.CreateRule("template_content", RuleOptions.KeepWhiteSpaces)
		.Choice(
			b => b.Literal("@").Rule("expression").ForseSkipWhiteSpaces(),
			b => b.Token("esc_text")
		);

	var expression_rule = builder.CreateRule("expression")
		.Choice(
			b => b.Rule("expression").LiteralChoice("+", "-").Rule("expression"),
			b => b.Rule("expression").LiteralChoice("*", "/").Rule("expression"),
			b => b.Literal("(").Rule("expression").Literal(")"),
			b => b.Token("number")
		);

	var parser = builder.Build();
	 */

	/// <summary>
	/// Represents a builder for constructing parsers.
	/// </summary>
	public class ParserBuilder
	{
		private readonly Dictionary<string, TokenBuilder> _tokenPatterns = new();
		private readonly Dictionary<string, RuleBuilder> _rules = new();
		private readonly ParserSettingsBuilder _settingsBuilder = new();
		private readonly ParserTokenizersBuilder _tokenizersBuilder = new();
		private RuleBuilder? _mainRuleBuilder;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserBuilder"/> class.
		/// </summary>
		public ParserBuilder()
		{
		}

		/// <summary>
		/// Creates a token pattern builder and registers it under the given name.
		/// </summary>
		/// <param name="name">The name of the token pattern. Will be bound to token pattern as alias in the built parser.</param>
		/// <returns>A <see cref="TokenBuilder"/> instance for building the token pattern.</returns>
		/// <exception cref="ArgumentException">Thrown if a token pattern with the same name already exists.</exception>
		public TokenBuilder CreateToken(string name)
		{
			if (_tokenPatterns.ContainsKey(name))
				throw new ArgumentException($"Token with name '{name}' already exists.");

			var token = new TokenBuilder();
			_tokenPatterns[name] = token;
			return token;
		}

		/// <summary>
		/// Gets the token pattern builder by its name.
		/// </summary>
		/// <param name="name">The name of the token pattern.</param>
		/// <returns>A <see cref="TokenBuilder"/> instance for building the token pattern.</returns>
		/// <exception cref="ArgumentException">Thrown if no token pattern with the specified name exists.</exception>
		public TokenBuilder GetToken(string name)
		{
			if (_tokenPatterns.TryGetValue(name, out var token))
				return token;
			throw new ArgumentException($"Token with name '{name}' not found.");
		}
		
		/// <summary>
		/// Tries to get the token pattern builder by its name.
		/// </summary>
		/// <param name="name">The name of the token pattern.</param>
		/// <returns>A <see cref="TokenBuilder"/> instance for building the token pattern or <see langword="null"/> if not found.</returns>
		public TokenBuilder? TryGetToken(string name)
		{
			if (_tokenPatterns.TryGetValue(name, out var token))
				return token;
			return null;
		}

		/// <summary>
		/// Creates or gets the existing token pattern builder by its name.
		/// </summary>
		/// <param name="name">The name of the token pattern.</param>
		/// <returns>A <see cref="TokenBuilder"/> instance for building the token pattern.</returns>
		public TokenBuilder GetOrCreateToken(string name)
		{
			if (_tokenPatterns.TryGetValue(name, out var token))
				return token;
			token = new TokenBuilder();
			_tokenPatterns[name] = token;
			return token;
		}

		/// <summary>
		/// Creates a rule builder and registers it under the given name.
		/// </summary>
		/// <param name="name">The name of the rule. Will be bound to rule as alias in the built parser.</param>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the rule.</returns>
		/// <exception cref="ArgumentException">Thrown if a rule with the same name already exists.</exception>
		public RuleBuilder CreateRule(string name)
		{
			if (_rules.ContainsKey(name))
				throw new ArgumentException($"Rule with name '{name}' already exists.");

			var rule = new RuleBuilder();
			_rules[name] = rule;
			return rule;
		}

		/// <summary>
		/// Gets the rule builder by its name.
		/// </summary>
		/// <param name="name">The name of the rule.</param>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the rule.</returns>
		/// <exception cref="ArgumentException">Thrown if no rule with the specified name exists.</exception>
		public RuleBuilder GetRule(string name)
		{
			if (_rules.TryGetValue(name, out var rule))
				return rule;
			throw new ArgumentException($"Rule with name '{name}' not found.");
		}

		/// <summary>
		/// Tries to get the rule builder by its name.
		/// </summary>
		/// <param name="name">The name of the rule.</param>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the rule or <see langword="null"/> if not found.</returns>
		public RuleBuilder? TryGetRule(string name)
		{
			if (_rules.TryGetValue(name, out var rule))
				return rule;
			return null;
		}

		/// <summary>
		/// Creates or gets the existing rule builder by its name.
		/// </summary>
		/// <param name="name">The name of the rule.</param>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the rule.</returns>
		public RuleBuilder GetOrCreateRule(string name)
		{
			if (_rules.TryGetValue(name, out var rule))
				return rule;
			rule = new RuleBuilder();
			_rules[name] = rule;
			return rule;
		}

		/// <summary>
		/// Creates a rule builder and registers it as the main rule.
		/// </summary>
		/// <remarks>
		/// Marks this rule as main rule. The main rule may be used as entry point for parsing.
		/// </remarks>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the rule.</returns>
		/// <exception cref="ArgumentException">Thrown if main rule already exists.</exception>
		public RuleBuilder CreateMainRule()
		{
			if (_mainRuleBuilder != null)
				throw new ArgumentException("Main rule has already been set.");

			_mainRuleBuilder = new RuleBuilder();
			return _mainRuleBuilder;
		}

		/// <summary>
		/// Creates a rule builder and registers it under the given name.
		/// </summary>
		/// <remarks>
		/// Marks this rule as main rule. The main rule may be used as entry point for parsing.
		/// </remarks>
		/// <param name="name">The name of the rule. Will be bound to rule as alias in the built parser.</param>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the rule.</returns>
		/// <exception cref="ArgumentException">Thrown if a rule with the same name already exists.</exception>
		public RuleBuilder CreateMainRule(string name)
		{
			if (_mainRuleBuilder != null)
				throw new ArgumentException("Main rule has already been set.");
			if (_rules.ContainsKey(name))
				throw new ArgumentException($"Rule with name '{name}' already exists.");

			_mainRuleBuilder = new RuleBuilder();
			_rules[name] = _mainRuleBuilder;
			return _mainRuleBuilder;
		}

		/// <summary>
		/// Gets the main rule builder.
		/// </summary>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the main rule.</returns>
		/// <exception cref="ArgumentException">Thrown if no main rule exists.</exception>
		public RuleBuilder GetMainRule()
		{
			if (_mainRuleBuilder != null)
				return _mainRuleBuilder;
			throw new ArgumentException("Main rule has not been set.");
		}

		/// <summary>
		/// Tries to get the main rule builder.
		/// </summary>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the main rule or the <see langword="null"/> if not exists.</returns>
		public RuleBuilder? TryGetMainRule()
		{
			if (_mainRuleBuilder != null)
				return _mainRuleBuilder;
			return null;
		}



		/// <summary>
		/// Gets the current settings builder for configuring additional options.
		/// </summary>
		public ParserSettingsBuilder Settings => _settingsBuilder;

		/// <summary>
		/// Gets the current barrier tokenizers builder.
		/// </summary>
		public ParserTokenizersBuilder BarrierTokenizers => _tokenizersBuilder;



		/// <summary>
		/// Builds the parser from the registered token patterns and rules.
		/// </summary>
		/// <returns>A <see cref="Parser"/> instance representing the built parser.</returns>
		public Parser Build()
		{
			// Counters for assigning unique IDs to rules and tokens
			int ruleCounter = 0;
			int tokenCounter = 0;

			// Maps for named rules and tokens to their buildable representations
			Dictionary<string, BuildableParserRule> namedRules = new();
			Dictionary<string, BuildableTokenPattern> namedTokenPatterns = new();

			// Maps for buildable rules and tokens to their assigned integer IDs
			Dictionary<BuildableParserElement, int> elements = new();

			// Maps for rules and tokens to their children dependencies
			Dictionary<BuildableParserElementBase, (List<BuildableParserRule>?,
				List<BuildableTokenPattern>?, List<BuildableParserElementBase?>)> argMap = new();

			// Queues to process rules and tokens breadth-first
			Queue<BuildableParserElementBase> elementsToProcess = new();

			// Pre-process the tokenizers
			var tokenizers = BarrierTokenizers.Build();
			var allBarriers = new HashSet<string>();
			foreach (var tokenizer in tokenizers)
			{
				var knownTokens = tokenizer.BarrierAliases.ToArray();
				foreach (var tokenName in knownTokens)
				{
					if (!allBarriers.Add(tokenName))
						continue;

					var btoken = new BuildableLeafTokenPattern
					{
						TokenPattern = new BarrierTokenPattern(tokenName)
					};
					namedTokenPatterns[tokenName] = btoken;
					elementsToProcess.Enqueue(btoken);
				}
			}

			// Initialize processing queue and namedRules map with root rules
			foreach (var rule in _rules)
			{
				if (!rule.Value.CanBeBuilt)
					throw new ParserBuildingException($"Rule '{rule.Key}' cannot be built, because it's empty.");

				// Detect circular references and resolve to the base rule
				HashSet<string> checkedNames = new HashSet<string>();
				var currentRule = rule.Value.BuildingRule.Value;

				while (currentRule.VariantIndex != 1)
				{
					if (!checkedNames.Add(currentRule.Value1))
						throw new ParserBuildingException($"Circular reference detected in rule '{rule.Key}': " +
							$"{string.Join(" -> ", checkedNames.Append(currentRule.Value1).Select(n => $"'{n}'"))}");

					if (_rules.TryGetValue(currentRule.Value1, out var nextRule))
					{
						if (!nextRule.CanBeBuilt)
							throw new ParserBuildingException($"Rule '{currentRule.Value1}' cannot be built, because it's empty.");

						currentRule = nextRule.BuildingRule.Value;
					}
					else
					{
						throw new ParserBuildingException($"Rule '{currentRule.Value1}' cannot be found.");
					}
				}

				var brule = currentRule.Value2;
				elementsToProcess.Enqueue(brule);
				namedRules.Add(rule.Key, brule);
			}

			BuildableParserRule? mainRule = null;
			_mainRuleBuilder?.BuildingRule?.Switch(
				name =>
				{
					if (!namedRules.TryGetValue(name, out mainRule))
						throw new ParserBuildingException($"Main rule '{name}' cannot be found.");
				},
				rule =>
				{
					elementsToProcess.Enqueue(mainRule = rule);
				}
			);

			// Initialize processing queue and namedTokenPatterns map with root token patterns
			foreach (var pattern in _tokenPatterns)
			{
				if (!pattern.Value.CanBeBuilt)
					throw new ParserBuildingException($"Token pattern '{pattern.Key}' cannot be built, because it's empty.");

				// Detect circular references and resolve to the base token pattern
				HashSet<string> checkedNames = new HashSet<string>();
				var currentPattern = pattern.Value.BuildingPattern.Value;

				while (currentPattern.VariantIndex != 1)
				{
					if (!checkedNames.Add(currentPattern.Value1))
						throw new ParserBuildingException($"Circular reference detected in token pattern '{pattern.Key}': " +
							$"{string.Join(" -> ", checkedNames.Append(currentPattern.Value1).Select(n => $"'{n}'"))}");

					if (_tokenPatterns.TryGetValue(currentPattern.Value1, out var nextPattern))
					{
						if (!nextPattern.CanBeBuilt)
							throw new ParserBuildingException($"Token pattern '{currentPattern.Value1}' cannot be built, because it's empty.");

						currentPattern = nextPattern.BuildingPattern.Value;
					}
					else
					{
						throw new ParserBuildingException($"Token pattern '{currentPattern.Value1}' cannot be found.");
					}
				}

				var bpattern = currentPattern.Value2;
				elementsToProcess.Enqueue(bpattern);
				namedTokenPatterns.Add(pattern.Key, bpattern);
			}

			// Process global parser settings child rules
			elementsToProcess.Enqueue(_settingsBuilder);
			int mainRuleId = -1;

			// Process all rules and tokens in the queue, assign IDs and collect dependencies
			while (elementsToProcess.Count > 0)
			{
				// Register an ID if it's not already registered
				var element = elementsToProcess.Dequeue();
				if (element is BuildableParserElement _element && elements.ContainsKey(_element))
					continue;

				if (element is BuildableParserRule rule)
				{
					if (Equals(element, mainRule))
						mainRuleId = ruleCounter;
					elements.Add(rule, ruleCounter++);
				}
				else if (element is BuildableTokenPattern pattern)
				{
					elements.Add(pattern, tokenCounter++);
				}

				// Queue child rules for processing
				List<BuildableParserRule?> ruleChildrenToArgs = null;
				var children = element.RuleChildren;
				if (children != null)
				{
					ruleChildrenToArgs = new();
					foreach (var ruleChild in children)
					{
						ruleChild.Switch(name =>
						{
							if (name == null)
								ruleChildrenToArgs.Add(null);
							else if (namedRules.TryGetValue(name, out var namedRule))
								ruleChildrenToArgs.Add(namedRule);
							else
								throw new ParserBuildingException($"Unknown rule reference '{name}'.");
						}, brule =>
						{
							ruleChildrenToArgs.Add(brule);
							elementsToProcess.Enqueue(brule);
						});
					}
				}

				// Queue child token patterns for processing
				List<BuildableTokenPattern?> tokenChildrenToArgs = null;
				var tokenChildren = element.TokenChildren;
				if (tokenChildren != null)
				{
					tokenChildrenToArgs = new();
					foreach (var tokenChild in tokenChildren)
					{
						tokenChild.Switch(name =>
						{
							if (name == null)
								tokenChildrenToArgs.Add(null);
							else if (namedTokenPatterns.TryGetValue(name, out var namedPattern))
								tokenChildrenToArgs.Add(namedPattern);
							else
								throw new ParserBuildingException($"Unknown token pattern reference '{name}'.");
						}, bpattern =>
						{
							tokenChildrenToArgs.Add(bpattern);
							elementsToProcess.Enqueue(bpattern);
						});
					}
				}

				// Queue child settings rules for processing
				List<BuildableParserElementBase?> elementChildrenToArgs = new();
				var elementChildren = element.ElementChildren;
				if (elementChildren != null)
				{
					foreach (var elementChild in elementChildren)
					{
						if (elementChild != null)
							elementsToProcess.Enqueue(elementChild);
						elementChildrenToArgs.Add(elementChild);
					}
				}

				// Register dependencies for building
				argMap[element] = (ruleChildrenToArgs, tokenChildrenToArgs, elementChildrenToArgs);
			}

			// Prepare the names map for quick lookup of elements to names
			var names = namedRules.Select(kvp => new KeyValuePair<string, BuildableParserElement>(kvp.Key, kvp.Value))
				.Concat(namedTokenPatterns.Select(kvp => new KeyValuePair<string, BuildableParserElement>(kvp.Key, kvp.Value)))
				.Flip();

			// Prepare the final map for snapshot of dependencies IDs
			List<(
				BuildableParserElement element,
				int id,
				List<int>? ruleChildren,
				List<int>? tokenChildren,
				List<object?>? elementChildren,
				List<string> aliases)> finalMap = new();

			Dictionary<BuildableParserElementBase, object?> elementBases = new();

			HashSet<IInitializeAfterBuild> toInitialize = new();

			object? BuildElementBase(BuildableParserElementBase? element)
			{
				if (element == null)
					return null;

				var (_ruleChildren, _tokenChildren, _elementChildren) = argMap[element];

				var ruleChildren = _ruleChildren?.Select(r => r == null ? -1 : elements[r]).ToList();
				var tokenChildren = _tokenChildren?.Select(p => p == null ? -1 : elements[p]).ToList();
				var elementChildren = _elementChildren?.Select(e => BuildElementBase(e)).ToList();

				var builtElement = element.Build(ruleChildren, tokenChildren, elementChildren);
				if (builtElement is IInitializeAfterBuild iib)
					toInitialize.Add(iib);
				return builtElement;
			}

			// Fill the final map
			foreach (var elem in elements)
			{
				var element = elem.Key;
				var id = elem.Value;

				var (_ruleChildren, _tokenChildren, _elementChildren) = argMap[element];

				var ruleChildren = _ruleChildren?.Select(r => r == null ? -1 : elements[r]).ToList();
				var tokenChildren = _tokenChildren?.Select(p => p == null ? -1 : elements[p]).ToList();
				var elementChildren = _elementChildren?.Select(e => BuildElementBase(e)).ToList();
				var aliases = new List<string>();

				if (element is BuildableParserElement _element && names.TryGetValue(_element, out var _aliases))
					aliases.AddRange(_aliases);

				finalMap.Add((element, id, ruleChildren, tokenChildren, elementChildren, aliases));
			}

			ParserRule[] resultRules = new ParserRule[elements.Count(e => e.Key is BuildableParserRule)];
			TokenPattern[] resultTokenPatterns = new TokenPattern[elements.Count(e => e.Key is BuildableTokenPattern)];

			// Build the final parser settings with resolved child rules
			(ParserMainSettings mainSettings, ParserSettings globalSettings, Func<ParserElement, ParserInitFlags> initFlagsFactory) =
				((ParserMainSettings, ParserSettings, Func<ParserElement, ParserInitFlags>))BuildElementBase(_settingsBuilder);
			
			// Release the final map to the result arrays
			foreach (var elem in finalMap)
			{
				var element = elem.element;
				var id = elem.id;
				var ruleChildren = elem.ruleChildren;
				var tokenChildren = elem.tokenChildren;
				var elementChildren = elem.elementChildren;
				var aliases = elem.aliases;

				var builtElement = (ParserElement)element.Build(ruleChildren, tokenChildren, elementChildren);
				if (builtElement is IInitializeAfterBuild iib)
					toInitialize.Add(iib);

				builtElement.Id = id;
				builtElement.Aliases = aliases.ToArray().AsReadOnlyList();

				if (builtElement is ParserRule rule)
				{
					resultRules[id] = rule;
				}
				else if (builtElement is TokenPattern pattern)
				{
					resultTokenPatterns[id] = pattern;
				}
			}

			// Return the fully built parser instance with rules and token patterns
			var parser = new Parser(resultTokenPatterns, resultRules,
				tokenizers, mainSettings, globalSettings, mainRuleId, initFlagsFactory);

			foreach (var iib in toInitialize)
				iib.Initialize(parser);

			return parser;
		}
	}
}