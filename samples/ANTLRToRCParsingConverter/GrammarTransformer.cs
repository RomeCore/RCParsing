using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANTLRToRCParsingConverter
{
	public static class GrammarTransformer
	{
		public static void Transform(List<RuleDef> rules)
		{
			EliminateLeftRecursion(rules);
		}

		private static void EliminateLeftRecursion(List<RuleDef> rules)
		{
			var newRules = new List<RuleDef>();

			foreach (var rule in rules)
			{
				var (transformedNode, additionalRules) = EliminateNodeLeftRecursion(rule.Child, rule.RuleName);
				rule.Child = transformedNode;
				newRules.Add(rule);
				if (additionalRules != null)
					newRules.AddRange(additionalRules);
			}

			rules.Clear();
			rules.AddRange(newRules);
		}

		private static (ParserNode, List<RuleDef>?) EliminateNodeLeftRecursion(ParserNode node, string ruleName)
		{
			return node switch
			{
				Choice choice => EliminateChoiceLeftRecursion(choice, ruleName),
				_ => (node, null)
			};
		}

		private static (ParserNode, List<RuleDef>?) EliminateChoiceLeftRecursion(Choice choice, string ruleName)
		{
			var leftRecursive = new List<ParserNode>();
			var nonLeftRecursive = new List<ParserNode>();

			foreach (var alternative in choice.Children)
			{
				if (IsLeftRecursive(alternative, ruleName))
					leftRecursive.Add(alternative);
				else
					nonLeftRecursive.Add(alternative);
			}

			if (leftRecursive.Count == 0)
				return (choice, null);

			var tailRuleName = ruleName + "_tail";
			var tailAlternatives = leftRecursive.Select(alt =>
			{
				var tail = ExtractTail(alt, ruleName);
				if (tail is Sequence tailSeq)
				{
					var children = new ParserNode[tailSeq.Children.Length + 1];
					Array.Copy(tailSeq.Children, children, tailSeq.Children.Length);
					children[children.Length - 1] = new RuleRef { Name = tailRuleName };
					return (ParserNode)new Sequence { Children = children };
				}
				else if (tail is Empty)
				{
					return tail;
				}
				else
				{
					return (ParserNode)new Sequence
					{
						Children = new ParserNode[] { tail, new RuleRef { Name = tailRuleName } }
					};
				}
			}).ToList();

			// tail: (α1 | α2 | ... | ε)
			var fullTailChildren = tailAlternatives.Concat(new ParserNode[] { new Empty() }).ToArray();
			var tailRule = new RuleDef
			{
				RuleName = tailRuleName,
				Child = new Choice { Children = fullTailChildren }
			};

			// For each β do β + RuleRef(tailRuleName). If β — Sequence, 
			// merge it with the tail ref.
			var mainAlternatives = nonLeftRecursive.Select(alt =>
			{
				if (alt is Sequence seq)
				{
					var children = new ParserNode[seq.Children.Length + 1];
					Array.Copy(seq.Children, children, seq.Children.Length);
					children[children.Length - 1] = new RuleRef { Name = tailRuleName };
					return (ParserNode)new Sequence { Children = children };
				}
				else
				{
					return (ParserNode)new Sequence
					{
						Children = new ParserNode[] { alt, new RuleRef { Name = tailRuleName } }
					};
				}
			}).ToArray();

			ParserNode newMain;
			if (mainAlternatives.Length == 1)
				newMain = mainAlternatives[0];
			else
				newMain = new Choice { Children = mainAlternatives };

			return (newMain, new List<RuleDef> { tailRule });
		}

		private static bool IsLeftRecursive(ParserNode node, string ruleName)
		{
			return node switch
			{
				Sequence seq => IsSequenceLeftRecursive(seq, ruleName),
				RuleRef ruleRef => ruleRef.Name == ruleName,
				_ => false
			};
		}

		private static bool IsSequenceLeftRecursive(Sequence seq, string ruleName)
		{
			if (seq.Children.Length == 0) return false;

			var firstChild = seq.Children[0];
			return firstChild switch
			{
				RuleRef ruleRef => ruleRef.Name == ruleName,
				Sequence nestedSeq => IsSequenceLeftRecursive(nestedSeq, ruleName),
				_ => false
			};
		}

		private static ParserNode ExtractTail(ParserNode node, string ruleName)
		{
			return node switch
			{
				Sequence seq => ExtractSequenceTail(seq, ruleName),
				RuleRef ruleRef when ruleRef.Name == ruleName => new Empty(),
				_ => node
			};
		}

		private static ParserNode ExtractSequenceTail(Sequence seq, string ruleName)
		{
			if (seq.Children.Length == 0) return new Empty();

			var firstChild = seq.Children[0];
			if (firstChild is RuleRef ruleRef && ruleRef.Name == ruleName)
			{
				return seq.Children.Length > 1
					? new Sequence { Children = seq.Children.Skip(1).ToArray() }
					: new Empty();
			}

			if (firstChild is Sequence nestedSeq && IsSequenceLeftRecursive(nestedSeq, ruleName))
			{
				var tail = ExtractSequenceTail(nestedSeq, ruleName);
				var rest = seq.Children.Skip(1).ToArray();

				if (rest.Length == 0) return tail;

				return new Sequence
				{
					Children = [tail, .. rest]
				};
			}

			return seq;
		}
	}
}