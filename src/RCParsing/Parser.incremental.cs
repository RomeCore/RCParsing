using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	public partial class Parser
	{
		internal ParsedRule ParseIncrementally(ParsedRule root,
			ParserContext context, TextChange change)
		{
			if (change.oldLength == 0 && change.newLength == 0)
				return root;
			if (change.startIndex >= context.maxPosition)
				return root;
			if (change.startIndex + change.oldLength <= context.position)
				return root;

			EmitBarriers(ref context);

			var settings = GlobalSettings;
			var rule = GetRule(root.ruleId);
			rule.AdvanceContext(ref context, ref settings, out var childSettings);

			return ParseIncrementally(context, settings, childSettings, root, change, root.version + 1);
		}

		internal ParsedRule ParseIncrementally(ParserContext context, ParserSettings settings,
			ParserSettings childSettings, ParsedRule node, TextChange change, int newVersion)
		{
			// First, we will find a child that entirely contains the change,
			// but if we have multiple or zero target children, we invalidate the entire node

			int targetChildIndex = 0;
			int entireChilds = 0; // Children that entirely contain the change. Must be equal to 1 to be valid.
			int partialChilds = 0; // Children that partially contain the change. Must be equal to 0 to be valid.

			if (node.children != null)
				for (int i = 0; i < node.children.Count; i++)
				{
					var child = node.children[i];
					if (child.startIndex <= change.startIndex && change.startIndex + change.oldLength <= child.startIndex + child.length)
					{
						targetChildIndex = i;
						entireChilds++;

						if (entireChilds > 1)
							break;
					}
					else if (child.startIndex < change.startIndex + change.oldLength && change.startIndex < child.startIndex + child.length)
					{
						partialChilds++;
						break;
					}
				}

			// We successfully found a target child that entirely contains the change.
			// Propagate the change to this target child.
			if (entireChilds == 1 && partialChilds == 0)
			{
				var targetChild = node.children[targetChildIndex];
				var childRule = _rules[targetChild.ruleId];
				var _settings = childSettings;
				var childContext = context;

				childContext.position = targetChild.startIndex;
				childRule.AdvanceContext(ref childContext, ref _settings, out var _childSettings);

				var newChild = ParseIncrementally(childContext, _settings, _childSettings,
					targetChild, change, newVersion);

				bool startIndexIsOk = targetChild.startIndex == newChild.startIndex;
				bool lengthIsOk = targetChild.length - newChild.length == change.oldLength - change.newLength;

				// If success, update current node and pass it up.
				if (newChild.success && startIndexIsOk && lengthIsOk)
				{
					var newChildren = new ParsedRule[node.children.Count];

					for (int i = 0; i < targetChildIndex; i++)
						newChildren[i] = node.children[i];

					newChild.version = newVersion;
					newChildren[targetChildIndex] = newChild;

					int delta = newChild.length - targetChild.length;
					for (int i = targetChildIndex + 1; i < node.children.Count; i++)
						newChildren[i] = node.children[i].Move(delta); // TODO: Maybe change version for them too?

					node.version = newVersion;
					node.children = newChildren;
					node.length += delta;
					return node;
				}

				// If fail, reparse current node.
			}

			var rule = _rules[node.ruleId];
			var specialReparsed = rule.ParseIncrementallyInternal(context,
				settings, childSettings, node, change, newVersion);
			if (specialReparsed.success)
				return specialReparsed;

			// Otherwise, we invalidate and reparse the current node.

			context.position = node.startIndex;
			var parsedRule = Parse(rule, ref context, ref settings, ref childSettings, canRecover: true);
			parsedRule = parsedRule.ChangeVersion(newVersion);
			return parsedRule;
		}
	}
}