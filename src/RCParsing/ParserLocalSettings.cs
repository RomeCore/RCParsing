using System;

namespace RCParsing
{
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