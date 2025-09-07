using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RCParsing.Building
{
	public partial class RuleBuilder
	{
		private static Func<ParsedRuleResultBase, object?> GenerateLambdaForTypes(
			Delegate factory,
			params Type[] types)
		{
			var rParam = Expression.Parameter(typeof(ParsedRuleResultBase), "r");
			var args = new Expression[types.Length];

			for (int i = 0; i < types.Length; i++)
			{
				if (types[i] == typeof(Ignored))
				{
					args[i] = Expression.Default(types[i]);
				}
				else
				{
					args[i] = Expression.Call(
						rParam,
						nameof(ParsedRuleResultBase.GetValue),
						new[] { types[i] },
						Expression.Constant(i)
					);
				}
			}

			var body = Expression.Invoke(Expression.Constant(factory), args);

			var lambda = Expression.Lambda<Func<ParsedRuleResultBase, object?>>(body, rParam);

			return lambda.Compile();
		}

		/// <summary>
		/// Transforms the parsed result using a factory function with child index selection.
		/// </summary>
		/// <param name="index">The index of the value to select from the children rules.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder TransformSelect(int index)
		{
			return Transform(v => v.GetValue(index));
		}

		/// <summary>
		/// Transforms the parsed result using a factory function with child index selection.
		/// </summary>
		/// <param name="index">The index of the value to select from the children rules.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder TransformLastSelect(int index)
		{
			return TransformLast(v => v.GetValue(index));
		}


		/// <summary>
		/// Sets the transformation function to the current sequence rule with up to 16 arguments.
		/// </summary>
		/// <remarks>
		/// <see cref="Ignored"/> type can be used to ignore arguments.
		/// </remarks>
		/// <typeparam name="T1">Type of the first argument.</typeparam>
		/// <typeparam name="T2">Type of the second argument.</typeparam>
		/// <typeparam name="T3">Type of the third argument.</typeparam>
		/// <typeparam name="T4">Type of the fourth argument.</typeparam>
		/// <typeparam name="T5">Type of the fifth argument.</typeparam>
		/// <typeparam name="T6">Type of the sixth argument.</typeparam>
		/// <typeparam name="T7">Type of the seventh argument.</typeparam>
		/// <typeparam name="T8">Type of the eighth argument.</typeparam>
		/// <typeparam name="T9">Type of the ninth argument.</typeparam>
		/// <typeparam name="T10">Type of the tenth argument.</typeparam>
		/// <typeparam name="T11">Type of the eleventh argument.</typeparam>
		/// <typeparam name="T12">Type of the twelfth argument.</typeparam>
		/// <typeparam name="T13">Type of the thirteenth argument.</typeparam>
		/// <typeparam name="T14">Type of the fourteenth argument.</typeparam>
		/// <typeparam name="T15">Type of the fifteenth argument.</typeparam>
		/// <typeparam name="T16">Type of the sixteenth argument.</typeparam>
		/// <param name="factory">The transformation function (parsed value factory) to set.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">
		/// Thrown if the parser rule is not set or it is a direct reference to a named rule.
		/// </exception>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, object?> factory)
		{
			return Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12),
				typeof(T13), typeof(T14), typeof(T15), typeof(T16)
			));
		}

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2>(Func<T1, T2, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3>(Func<T1, T2, T3, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4>(Func<T1, T2, T3, T4, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5>(
			Func<T1, T2, T3, T4, T5, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6>(
			Func<T1, T2, T3, T4, T5, T6, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7>(
			Func<T1, T2, T3, T4, T5, T6, T7, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12),
				typeof(T13)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12),
				typeof(T13), typeof(T14)
			));

		/// <inheritdoc cref="Transform{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder Transform<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, object?> factory) =>
			Transform(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12),
				typeof(T13), typeof(T14), typeof(T15)
			));

		/// <summary>
		/// Sets the transformation function to the current sequence rule with up to 16 arguments.
		/// </summary>
		/// <remarks>
		/// <see cref="Ignored"/> type can be used to ignore arguments.
		/// </remarks>
		/// <typeparam name="T1">Type of the first argument.</typeparam>
		/// <typeparam name="T2">Type of the second argument.</typeparam>
		/// <typeparam name="T3">Type of the third argument.</typeparam>
		/// <typeparam name="T4">Type of the fourth argument.</typeparam>
		/// <typeparam name="T5">Type of the fifth argument.</typeparam>
		/// <typeparam name="T6">Type of the sixth argument.</typeparam>
		/// <typeparam name="T7">Type of the seventh argument.</typeparam>
		/// <typeparam name="T8">Type of the eighth argument.</typeparam>
		/// <typeparam name="T9">Type of the ninth argument.</typeparam>
		/// <typeparam name="T10">Type of the tenth argument.</typeparam>
		/// <typeparam name="T11">Type of the eleventh argument.</typeparam>
		/// <typeparam name="T12">Type of the twelfth argument.</typeparam>
		/// <typeparam name="T13">Type of the thirteenth argument.</typeparam>
		/// <typeparam name="T14">Type of the fourteenth argument.</typeparam>
		/// <typeparam name="T15">Type of the fifteenth argument.</typeparam>
		/// <typeparam name="T16">Type of the sixteenth argument.</typeparam>
		/// <param name="factory">The transformation function (parsed value factory) to set.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">
		/// Thrown if the parser rule is not set or it is a direct reference to a named rule.
		/// </exception>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, object?> factory)
		{
			return TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12),
				typeof(T13), typeof(T14), typeof(T15), typeof(T16)
			));
		}

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2>(Func<T1, T2, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3>(Func<T1, T2, T3, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4>(Func<T1, T2, T3, T4, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5>(
			Func<T1, T2, T3, T4, T5, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6>(
			Func<T1, T2, T3, T4, T5, T6, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7>(
			Func<T1, T2, T3, T4, T5, T6, T7, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12),
				typeof(T13)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12),
				typeof(T13), typeof(T14)
			));

		/// <inheritdoc cref="TransformLast{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16}(Func{T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14,T15,T16,object})"/>
		public RuleBuilder TransformLast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
			Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, object?> factory) =>
			TransformLast(GenerateLambdaForTypes(factory,
				typeof(T1), typeof(T2), typeof(T3), typeof(T4),
				typeof(T5), typeof(T6), typeof(T7), typeof(T8),
				typeof(T9), typeof(T10), typeof(T11), typeof(T12),
				typeof(T13), typeof(T14), typeof(T15)
			));

		/// <summary>
		/// Applies a left-associative fold transformation on the rule's children.
		/// For sequences: [a, b, c] -> fold(fold(a, b), c)
		/// </summary>
		/// <typeparam name="TAcc">Accumulator and return type</typeparam>
		/// <typeparam name="T1">Type of subsequent values</typeparam>
		/// <param name="foldFunction">Fold function: (accumulator, nextValue) -> newAccumulator</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder TransformFoldLeft<TAcc, T1>(Func<TAcc, T1, TAcc> foldFunction)
		{
			return Transform(v =>
			{
				var accumulator = v.GetValue<TAcc>(0);
				for (int i = 1; i < v.Count; i++)
				{
					var value1 = v.GetValue<T1>(i);
					accumulator = foldFunction(accumulator, value1);
				}
				return accumulator;
			});
		}

		/// <summary>
		/// Applies a left-associative fold transformation on interleaved operator-value pairs.
		/// For sequences: [a, op1, b, op2, c] -> fold(fold(a, op1, b), op2, c)
		/// </summary>
		/// <typeparam name="TAcc">Accumulator and return type</typeparam>
		/// <typeparam name="T1">Operator type</typeparam>
		/// <typeparam name="T2">Operand type</typeparam>
		/// <param name="foldFunction">Fold function: (accumulator, operator, operand) -> newAccumulator</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder TransformFoldLeft<TAcc, T1, T2>(Func<TAcc, T1, T2, TAcc> foldFunction)
		{
			return Transform(v =>
			{
				var accumulator = v.GetValue<TAcc>(0);
				for (int i = 1; i < v.Count - 1; i += 2)
				{
					var value1 = v.GetValue<T1>(i);
					var value2 = v.GetValue<T2>(i + 1);
					accumulator = foldFunction(accumulator, value1, value2);
				}
				return accumulator;
			});
		}

		/// <summary>
		/// Applies a right-associative fold transformation on the rule's children.
		/// For sequences: [a, b, c] -> fold(a, fold(c, b))
		/// </summary>
		/// <typeparam name="TAcc">Accumulator and return type</typeparam>
		/// <typeparam name="T1">Type of previous values</typeparam>
		/// <param name="foldFunction">Fold function: (accumulator, previousValue) -> newAccumulator</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder TransformFoldRight<TAcc, T1>(Func<TAcc, T1, TAcc> foldFunction)
		{
			return Transform(v =>
			{
				var last = v.Count - 1;
				var accumulator = v.GetValue<TAcc>(last);
				for (int i = last - 1; i >= 0; i--)
				{
					var value1 = v.GetValue<T1>(i);
					accumulator = foldFunction(accumulator, value1);
				}
				return accumulator;
			});
		}

		/// <summary>
		/// Applies a right-associative fold transformation on interleaved value-operator pairs.
		/// For sequences: [a, op1, b, op2, c] -> fold(a, op1, fold(c, op2, b))
		/// </summary>
		/// <typeparam name="TAcc">Accumulator and return type</typeparam>
		/// <typeparam name="T1">Operator type</typeparam>
		/// <typeparam name="T2">Operand type</typeparam>
		/// <param name="foldFunction">Fold function: (accumulator, operator, operand) -> newAccumulator</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder TransformFoldRight<TAcc, T1, T2>(Func<TAcc, T1, T2, TAcc> foldFunction)
		{
			return Transform(v =>
			{
				var last = v.Count - 1;
				var accumulator = v.GetValue<TAcc>(last);
				for (int i = last - 1; i > 0; i -= 2)
				{
					var value1 = v.GetValue<T1>(i);
					var value2 = v.GetValue<T2>(i - 1);
					accumulator = foldFunction(accumulator, value1, value2);
				}
				return accumulator;
			});
		}
	}
}