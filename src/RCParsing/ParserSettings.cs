using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents an immutable settings used directly for parsing.
	/// </summary>
	public class ImmutableParserSettings
	{
		/// <summary>
		/// The skipping strategy to use when parsing rules. Default is to try skip before parsing the target rule.
		/// </summary>
		public readonly ParserSkippingStrategy skippingStrategy;

		/// <summary>
		/// The rule ID to skip before parsing a specific rule. If set to -1, no rules are skipped.
		/// </summary>
		public readonly int skipRule;

		/// <summary>
		/// The error handling mode to use when parsing.
		/// If set to <see cref="ParserErrorHandlingMode.NoRecord"/> any errors are ignored when trying to parse but thrown when just parsing.
		/// If set to <see cref="ParserErrorHandlingMode.Throw"/>, any errors are thrown regardless of whether they are being parsed or just trying, no errors are recorded.
		/// </summary>
		public readonly ParserErrorHandlingMode errorHandling;

		/// <summary>
		/// The value indicates whether to ignore barriers while parsing. Default is false.
		/// </summary>
		public readonly bool ignoreBarriers;

		public ImmutableParserSettings(ParserSettings settings)
		{
			skippingStrategy = settings.skippingStrategy;
			skipRule = settings.skipRule;
			errorHandling = settings.errorHandling;
			ignoreBarriers = settings.ignoreBarriers;
		}
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
}