using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Defines how parser elements should handle specific settings propagation for local and children elements.
	/// </summary>
	public enum ParserSettingMode
	{
		/// <summary>
		/// Applies parent's setting for this element and all of its children. Ignores the local and global settings. The default mode.
		/// </summary>
		InheritForSelfAndChildren = 0,

		/// <summary>
		/// Apllies the local setting (if any) for this element and all of its children. This is default behavior when providing a local setting.
		/// </summary>
		LocalForSelfAndChildren,

		/// <summary>
		/// Applies local setting for this element only. Propagates the parent's setting to all child elements.
		/// </summary>
		LocalForSelfOnly,

		/// <summary>
		/// Applies parent's setting for this element only. Propagates the local setting to all child elements.
		/// </summary>
		LocalForChildrenOnly,

		/// <summary>
		/// Applies global setting for this element and all of its children.
		/// </summary>
		GlobalForSelfAndChildren,

		/// <summary>
		/// Applies global setting for this element only. Propagates the parent's setting to all child elements.
		/// </summary>
		GlobalForSelfOnly,

		/// <summary>
		/// Applies parent's setting for this element only. Propagates the global setting to all child elements.
		/// </summary>
		GlobalForChildrenOnly,
	}

	/// <summary>
	/// Defines how parser should handle skipping of rules.
	/// </summary>
	public enum ParserSkippingStrategy
	{
		/// <summary>
		/// Parser will always ignore the skip-rule and try to parse the target rule. Default behavior.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Parser will try to skip the skip-rule once before parsing the target rule.
		/// </summary>
		SkipBeforeParsing,

		/// <summary>
		/// Parser will repeatedly attempt to skip the skip-rule in an interleaved manner with the target rule,
		/// until the target rule succeeds or both skip and target rules fail. This allows incremental consumption of skip content.
		/// </summary>
		SkipBeforeParsingLazy,

		/// <summary>
		/// Parser will greedily skip the skip-rule as many times as possible before attempting to parse the target rule.
		/// </summary>
		SkipBeforeParsingGreedy,

		/// <summary>
		/// Parser will first attempt to parse the target rule; if parsing fails, it will skip the skip-rule once
		/// and then retry parsing the target rule.
		/// </summary>
		/// <remarks>
		/// Works slower sometimes but allows to use rules that conflict with skip-rules.
		/// </remarks>
		TryParseThenSkip,

		/// <summary>
		/// Parser will attempt to parse the target rule; if parsing fails, it will alternately try to skip the skip-rule
		/// and parse the target rule repeatedly until the target rule succeeds or both fail.
		/// </summary>
		TryParseThenSkipLazy,

		/// <summary>
		/// Parser will attempt to parse the target rule; if parsing fails, it will greedily skip the skip-rule as many times
		/// as possible and then retry parsing the target rule.
		/// </summary>
		TryParseThenSkipGreedy
	}

	/// <summary>
	/// Defines how parser elements should handle errors.
	/// </summary>
	public enum ParserErrorHandlingMode
	{
		/// <summary>
		/// Records errors into parsing context. The default mode.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Ignores any errors when parser elements trying to record them.
		/// </summary>
		NoRecord = 1,

		/// <summary>
		/// Throws any errors when parser elements trying to record them.
		/// </summary>
		Throw = 2
	}

	/// <summary>
	/// Defines settings for a parser. These can be used to control how the parser behaves and what it does when encountering errors or other situations.
	/// </summary>
	public struct ParserSettings : IEquatable<ParserSettings>
	{
		/// <summary>
		/// The skipping strategy to use when parsing rules. Default is to try skip before parsing the target rule.
		/// </summary>
		public ParserSkippingStrategy skippingStrategy;

		/// <summary>
		/// The rule ID to skip before parsing a specific rule. If set to -1, no rules are skipped.
		/// </summary>
		public int skipRule;

		/// <summary>
		/// The error handling mode to use when parsing.
		/// If set to <see cref="ParserErrorHandlingMode.NoRecord"/> any errors are ignored when trying to parse but thrown when just parsing.
		/// If set to <see cref="ParserErrorHandlingMode.Throw"/>, any errors are thrown regardless of whether they are being parsed or just trying, no errors are recorded.
		/// </summary>
		public ParserErrorHandlingMode errorHandling;

		/// <summary>
		/// The value indicates whether to ignore barriers while parsing. Default is false.
		/// </summary>
		public bool ignoreBarriers;



		/// <summary>
		/// Resolves the settings based on the provided local and global settings.
		/// </summary>
		/// <remarks>
		/// This instance is used as parent/inherited settings.
		/// </remarks>
		/// <param name="localSettings">The local settings to use.</param>
		/// <param name="globalSettings">The global settings to use.</param>
		/// <param name="forLocal">The settings to use for the current element.</param>
		/// <param name="forChildren">The settings to use for child elements.</param>
		public readonly void Resolve(ParserLocalSettings localSettings, ParserSettings globalSettings,
			out ParserSettings forLocal, out ParserSettings forChildren)
		{
			forLocal = new ParserSettings();
			forChildren = new ParserSettings();

			// ---- skippingStrategy ----
			ApplySetting(
				this.skippingStrategy, localSettings.skippingStrategy, globalSettings.skippingStrategy,
				localSettings.skippingStrategyUseMode,
				ref forLocal.skippingStrategy, ref forChildren.skippingStrategy
			);

			// ---- skipRule ----
			ApplySetting(
				this.skipRule, localSettings.skipRule, globalSettings.skipRule,
				localSettings.skipRuleUseMode,
				ref forLocal.skipRule, ref forChildren.skipRule
			);

			// ---- errorHandling ----
			ApplySetting(
				this.errorHandling, localSettings.errorHandling, globalSettings.errorHandling,
				localSettings.errorHandlingUseMode,
				ref forLocal.errorHandling, ref forChildren.errorHandling
			);

			// ---- ignoreBarriers ----
			ApplySetting(
				this.ignoreBarriers, localSettings.ignoreBarriers, globalSettings.ignoreBarriers,
				localSettings.ignoreBarriersUseMode,
				ref forLocal.ignoreBarriers, ref forChildren.ignoreBarriers
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ApplySetting<T>(
			T inheritedValue, T localValue, T globalValue,
			ParserSettingMode localMode,
			ref T valueForLocal, ref T valueForChildren)
		{
			switch (localMode)
			{
				case ParserSettingMode.InheritForSelfAndChildren:
					valueForLocal = inheritedValue;
					valueForChildren = inheritedValue;
					break;
				case ParserSettingMode.LocalForSelfAndChildren:
					valueForLocal = localValue;
					valueForChildren = localValue;
					break;
				case ParserSettingMode.LocalForSelfOnly:
					valueForLocal = localValue;
					valueForChildren = inheritedValue;
					break;
				case ParserSettingMode.LocalForChildrenOnly:
					valueForLocal = inheritedValue;
					valueForChildren = localValue;
					break;
				case ParserSettingMode.GlobalForSelfAndChildren:
					valueForLocal = globalValue;
					valueForChildren = globalValue;
					break;
				case ParserSettingMode.GlobalForSelfOnly:
					valueForLocal = globalValue;
					valueForChildren = inheritedValue;
					break;
				case ParserSettingMode.GlobalForChildrenOnly:
					valueForLocal = inheritedValue;
					valueForChildren = globalValue;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(localMode), localMode, "Unknown ParserSettingMode value.");
			}
		}

		public override readonly bool Equals(object? obj)
		{
			return obj is ParserSettings other &&
				   Equals(other);
		}

		public readonly bool Equals(ParserSettings other)
		{
			return skippingStrategy == other.skippingStrategy &&
				   skipRule == other.skipRule &&
				   errorHandling == other.errorHandling &&
				   ignoreBarriers == other.ignoreBarriers;
		}

		public override readonly int GetHashCode()
		{
			int hash = 17;
			hash = hash * 397 + skippingStrategy.GetHashCode();
			hash = hash * 397 + skipRule.GetHashCode();
			hash = hash * 397 + errorHandling.GetHashCode();
			hash = hash * 397 + ignoreBarriers.GetHashCode();
			return hash;
		}

		public static bool operator ==(ParserSettings left, ParserSettings right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ParserSettings left, ParserSettings right)
		{
			return !left.Equals(right);
		}
	}

	/// <summary>
	/// Defines local settings for a parser. These can be used to control how the parser behaves and what it does when encountering errors or other situations.
	/// </summary>
	public struct ParserLocalSettings : IEquatable<ParserLocalSettings>
	{
		/// <summary>
		/// The value indicating that all *UseModes are set to InheritForSelfAndChildren.
		/// </summary>
		public bool isDefault;



		/// <summary>
		/// Defines an override mode for <see cref="skippingStrategy"/> setting.
		/// </summary>
		public ParserSettingMode skippingStrategyUseMode;

		/// <inheritdoc cref="ParserSettings.skippingStrategy"/>
		public ParserSkippingStrategy skippingStrategy;



		/// <summary>
		/// Defines an override mode for <see cref="skipRule"/> setting.
		/// </summary>
		public ParserSettingMode skipRuleUseMode;

		/// <inheritdoc cref="ParserSettings.skipRule"/>
		public int skipRule;



		/// <summary>
		/// Defines an override mode for <see cref="errorHandling"/> setting.
		/// </summary>
		public ParserSettingMode errorHandlingUseMode;

		/// <inheritdoc cref="ParserSettings.errorHandling"/>
		public ParserErrorHandlingMode errorHandling;



		/// <summary>
		/// Defines an override mode for <see cref="ignoreBarriers"/> setting.
		/// </summary>
		public ParserSettingMode ignoreBarriersUseMode;

		/// <inheritdoc cref="ParserSettings.ignoreBarriers"/>
		public bool ignoreBarriers;



		public override bool Equals(object? obj)
		{
			return obj is ParserLocalSettings other &&
				   Equals(other);
		}

		public bool Equals(ParserLocalSettings other)
		{
			return isDefault == other.isDefault &&
				   skippingStrategyUseMode == other.skippingStrategyUseMode &&
				   skippingStrategy == other.skippingStrategy &&
				   skipRuleUseMode == other.skipRuleUseMode &&
				   skipRule == other.skipRule &&
				   errorHandlingUseMode == other.errorHandlingUseMode &&
				   errorHandling == other.errorHandling &&
				   ignoreBarriersUseMode == other.ignoreBarriersUseMode &&
				   ignoreBarriers == other.ignoreBarriers;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 397 + isDefault.GetHashCode();
			hash = hash * 397 + skippingStrategyUseMode.GetHashCode();
			hash = hash * 397 + skippingStrategy.GetHashCode();
			hash = hash * 397 + skipRuleUseMode.GetHashCode();
			hash = hash * 397 + skipRule.GetHashCode();
			hash = hash * 397 + errorHandlingUseMode.GetHashCode();
			hash = hash * 397 + errorHandling.GetHashCode();
			hash = hash * 397 + ignoreBarriersUseMode.GetHashCode();
			hash = hash * 397 + ignoreBarriers.GetHashCode();
			return hash;
		}

		public static bool operator ==(ParserLocalSettings left, ParserLocalSettings right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ParserLocalSettings left, ParserLocalSettings right)
		{
			return !left.Equals(right);
		}
	}
}