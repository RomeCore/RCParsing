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

			return ParseIncrementally(context, GlobalSettings, root, change, root.version + 1);
		}

		internal ParsedRule ParseIncrementally(ParserContext context, ParserSettings settings,
			ParsedRule node, TextChange change, int newVersion)
		{
			var rule = _rules[node.ruleId];
			var ruleContext = context;
			var ruleSettings = settings;
			rule.AdvanceContext(ref ruleContext, ref ruleSettings, out var ruleChildSettings);

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
				var newChild = ParseIncrementally(ruleContext, ruleChildSettings,
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

			// Try to use the special strategy to parse this node (if implemented).

			ruleContext.position = node.startIndex;
			var specialReparsed = rule.ParseIncrementallyInternal(ruleContext,
				ruleSettings, ruleChildSettings, node, change, newVersion);
			if (specialReparsed.success)
				return specialReparsed;

			// Otherwise, we invalidate and reparse the current node.

			var skipStrategy = ruleSettings.skippingStrategy ?? SkipStrategy.NoSkipping;
			var result = skipStrategy.ParseWithSkip(context, settings,
				rule, ruleContext, ruleSettings, ruleChildSettings);

			if (result.success)
			{
				result = result.ChangeVersion(newVersion);
				return result;
			}

			var recovery = rule.ErrorRecovery ?? ErrorRecoveryStrategy.NoRecovery;
			result = recovery.TryRecover(context, settings, rule, ruleContext, ruleSettings, ruleChildSettings);
			result = result.ChangeVersion(newVersion);
			return result;
		}
	}
}