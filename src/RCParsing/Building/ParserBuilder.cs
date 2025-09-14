using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using RCParsing.Building.ParserRules;
using RCParsing.Building.TokenPatterns;
using RCParsing.ParserRules;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/* Concept: (its very old)
	
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
			Dictionary<BuildableParserElement, (List<BuildableParserRule>?,
				List<BuildableTokenPattern>?, List<BuildableParserRule?>)> argMap = new();

			// Queues to process rules and tokens breadth-first
			Queue<BuildableParserElement> elementsToProcess = new();

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
			List<BuildableParserRule?> parserSettingsRuleChildren = new();
			foreach (var ruleChild in _settingsBuilder.RuleChildren)
			{
				if (!ruleChild.HasValue)
				{
					parserSettingsRuleChildren.Add(null);
					continue;
				}

				ruleChild.Value.Switch(name =>
				{
					if (namedRules.TryGetValue(name, out var namedRule))
						parserSettingsRuleChildren.Add(namedRule);
					else
						throw new ParserBuildingException($"Unknown rule reference '{name}' found in settings rule.");
				}, brule =>
				{
					parserSettingsRuleChildren.Add(brule);
					elementsToProcess.Enqueue(brule);
				});
			}

			int mainRuleId = -1;

			// Process all rules and tokens in the queue, assign IDs and collect dependencies
			while (elementsToProcess.Count > 0)
			{
				// Register an ID if it's not already registered
				var element = elementsToProcess.Dequeue();

				// Applying the default factory and configs
				if (element is BuildableTokenParserRule trule)
				{
					var child = trule.Child.Match(name =>
					{
						if (namedTokenPatterns.TryGetValue(name, out var namedPattern))
							return namedPattern;
						else
							throw new ParserBuildingException($"Unknown token pattern reference '{name}'.");
					}, bpattern =>
					{
						return bpattern;
					});

					if (!trule.Settings.HaveBeenChanged)
						child.DefaultConfigurationAction?.Invoke(trule.Settings);
					trule.ParsedValueFactory ??= child.DefaultParsedValueFactory;
				}

				if (elements.ContainsKey(element))
					continue;

				if (element is BuildableParserRule rule)
				{
					if (Equals(element, mainRule))
						mainRuleId = ruleCounter;
					elements.Add(element, ruleCounter++);
				}
				else if (element is BuildableTokenPattern pattern)
					elements.Add(element, tokenCounter++);

				// Queue child rules for processing
				List<BuildableParserRule>? ruleChildrenToArgs = null;
				var children = element.RuleChildren;
				if (children != null)
				{
					ruleChildrenToArgs = new();
					foreach (var ruleChild in children)
					{
						ruleChild.Switch(name =>
						{
							if (namedRules.TryGetValue(name, out var namedRule))
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
				List<BuildableTokenPattern>? tokenChildrenToArgs = null;
				var tokenChildren = element.TokenChildren;
				if (tokenChildren != null)
				{
					tokenChildrenToArgs = new();
					foreach (var tokenChild in tokenChildren)
					{
						tokenChild.Switch(name =>
						{
							if (namedTokenPatterns.TryGetValue(name, out var namedPattern))
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
				List<BuildableParserRule?> settingsRuleChildrenToArgs = new();
				if (element is BuildableParserRule brule)
					foreach (var ruleChild in brule.Settings.RuleChildren)
					{
						if (!ruleChild.HasValue)
						{
							settingsRuleChildrenToArgs.Add(null);
							continue;
						}

						ruleChild.Value.Switch(name =>
						{
							if (namedRules.TryGetValue(name, out var namedRule))
								settingsRuleChildrenToArgs.Add(namedRule);
							else
								throw new ParserBuildingException($"Unknown rule reference '{name}'.");
						}, brule =>
						{
							settingsRuleChildrenToArgs.Add(brule);
							elementsToProcess.Enqueue(brule);
						});
					}

				// Register dependencies for building
				argMap[element] = (ruleChildrenToArgs, tokenChildrenToArgs, settingsRuleChildrenToArgs);
			}

			// Rebuild the dictionaries since the keys were changed during processing
			elements = elements.ToDictionary(k => k.Key, v => v.Value);
			argMap = argMap.ToDictionary(k => k.Key, v => v.Value);

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
				List<int> settingsRuleChildren,
				List<string> aliases)> finalMap = new();

			// Fill the final map
			foreach (var elem in elements)
			{
				var element = elem.Key;
				var id = elem.Value;

				var (_ruleChildren, _tokenChildren, _settingsRuleChildren) = argMap[element];

				var ruleChildren = _ruleChildren?.Select(r => elements[r]).ToList();
				var tokenChildren = _tokenChildren?.Select(p => elements[p]).ToList();
				var settingsRuleChildren = _settingsRuleChildren.Select(r => r == null ? -1 : elements[r]).ToList();
				var aliases = new List<string>();

				if (names.TryGetValue(element, out var _aliases))
					aliases.AddRange(_aliases);

				finalMap.Add((element, id, ruleChildren, tokenChildren, settingsRuleChildren, aliases));
			}

			ParserRule[] resultRules = new ParserRule[elements.Count(e => e.Key is BuildableParserRule)];
			TokenPattern[] resultTokenPatterns = new TokenPattern[elements.Count(e => e.Key is BuildableTokenPattern)];

			// Build the final parser settings with resolved child rules
			var (mainSettings, globalSettings, initFlagsFactory) = _settingsBuilder.Build(parserSettingsRuleChildren
				.Select(r => r == null ? -1 : elements[r]).ToList());

			// Release the final map to the result arrays
			foreach (var elem in finalMap)
			{
				var element = elem.element;
				var id = elem.id;
				var ruleChildren = elem.ruleChildren;
				var tokenChildren = elem.tokenChildren;
				var settingsRuleChildren = elem.settingsRuleChildren;
				var aliases = elem.aliases;

				var builtElement = element.Build(ruleChildren, tokenChildren);

				builtElement.Id = id;
				builtElement.Aliases = aliases.ToImmutableList();

				if (builtElement is ParserRule rule)
				{
					var elementSettings = (element as BuildableParserRule).Settings;
					var builtSettings = elementSettings.Build(settingsRuleChildren);
					rule.Settings = builtSettings;
					resultRules[id] = rule;
				}
				else if (builtElement is TokenPattern pattern)
				{
					resultTokenPatterns[id] = pattern;
				}
			}

			// Return the fully built parser instance with rules and token patterns
			return new Parser(resultTokenPatterns.ToImmutableArray(), resultRules.ToImmutableArray(),
				tokenizers, mainSettings, globalSettings, mainRuleId, initFlagsFactory);
		}
	}
}