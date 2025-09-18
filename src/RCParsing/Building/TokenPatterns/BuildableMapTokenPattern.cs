﻿using System;
using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches a single element,
	/// transforming its intermediate value with a provided function.
	/// </summary>
	public class BuildableMapTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern of the child that should be parsed.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the mapping function that transforms the intermediate value of the child.
		/// </summary>
		public Func<object?, object?> Mapper { get; set; } = null!;

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			Child.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new MapTokenPattern(tokenChildren[0], Mapper);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableMapTokenPattern other &&
				   Child == other.Child &&
				   Equals(Mapper, other.Mapper);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + (Mapper?.GetHashCode() ?? 0);
			return hashCode;
		}
	}
}