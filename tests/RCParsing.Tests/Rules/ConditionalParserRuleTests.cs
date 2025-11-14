namespace RCParsing.Tests.Rules
{
	/// <summary>
	/// Tests for conditional parser rules (If and Switch) that work with parser parameters.
	/// </summary>
	public class ConditionalParserRuleTests
	{
		[Fact]
		public void IfRule_WithTrueCondition()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("test")
				.If(
					param => param is bool flag && flag,
					trueBranch => trueBranch.Literal("true"),
					falseBranch => falseBranch.Literal("false"));

			var parser = builder.Build();

			// Test with true condition
			var success = parser.TryParseRule("test", "true", parameter: true, out var result);
			Assert.True(success);
			Assert.Equal("true", result.Text);
		}

		[Fact]
		public void IfRule_WithFalseCondition()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("test")
				.If(
					param => param is bool flag && flag,
					trueBranch => trueBranch.Literal("true"),
					falseBranch => falseBranch.Literal("false"));

			var parser = builder.Build();

			// Test with false condition
			var success = parser.TryParseRule("test", "false", parameter: false, out var result);
			Assert.True(success);
			Assert.Equal("false", result.Text);
		}

		[Fact]
		public void IfRule_WithoutElseBranch_TrueCondition()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("test")
				.If(
					param => param is bool flag && flag,
					trueBranch => trueBranch.Literal("success"));

			var parser = builder.Build();

			// Test with true condition (should succeed)
			var success = parser.TryParseRule("test", "success", parameter: true, out var result);
			Assert.True(success);
			Assert.Equal("success", result.Text);
		}

		[Fact]
		public void IfRule_WithoutElseBranch_FalseCondition()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("test")
				.If<bool>(
					param => param,
					trueBranch => trueBranch.Literal("success"));

			var parser = builder.Build();

			// Test with false condition (should fail)
			var success = parser.TryParseRule("test", "success", parameter: false, out var result);
			Assert.False(success);
		}

		[Fact]
		public void IfRule_WithComplexParameter()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("access")
				.If(
					param => param is User user && user.IsAdmin,
					admin => admin.Literal("admin_only"),
					user => user.Literal("user_access"));

			var parser = builder.Build();

			// Test with admin user
			var success = parser.TryParseRule("access", "admin_only", parameter: new User { IsAdmin = true }, out var result);
			Assert.True(success);
			Assert.Equal("admin_only", result.Text);

			// Test with regular user
			success = parser.TryParseRule("access", "user_access", parameter: new User { IsAdmin = false }, out result);
			Assert.True(success);
			Assert.Equal("user_access", result.Text);
		}

		[Fact]
		public void SwitchRule_WithIndexSelector()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("language")
				.Switch(
					param => param is int langIndex ? langIndex : -1,
					defaultBranch: b => b.Literal("unknown"),
					b => b.Literal("english"),
					b => b.Literal("russian"),
					b => b.Literal("spanish"));

			var parser = builder.Build();

			// Test valid indices
			Assert.Equal("english", parser.ParseRule("language", "english", parameter: 0).Text);
			Assert.Equal("russian", parser.ParseRule("language", "russian", parameter: 1).Text);
			Assert.Equal("spanish", parser.ParseRule("language", "spanish", parameter: 2).Text);

			// Test default branch
			Assert.Equal("unknown", parser.ParseRule("language", "unknown", parameter: 5).Text);
			Assert.Equal("unknown", parser.ParseRule("language", "unknown", parameter: -1).Text);
		}

		[Fact]
		public void SwitchRule_WithConditionSelector()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("user_level")
				.Switch(
					defaultBranch: b => b.Literal("guest"),
					(param => param is User user && user.IsAdmin, b => b.Literal("admin")),
					(param => param is User user && user.IsModerator, b => b.Literal("moderator")),
					(param => param is User user && user.IsPremium, b => b.Literal("premium")));

			var parser = builder.Build();

			// Test different user types
			Assert.Equal("admin", parser.ParseRule("user_level", "admin",
				parameter: new User { IsAdmin = true }).Text);

			Assert.Equal("moderator", parser.ParseRule("user_level", "moderator",
				parameter: new User { IsModerator = true }).Text);

			Assert.Equal("premium", parser.ParseRule("user_level", "premium",
				parameter: new User { IsPremium = true }).Text);

			Assert.Equal("guest", parser.ParseRule("user_level", "guest",
				parameter: new User()).Text); // No special flags
		}

		[Fact]
		public void SwitchRule_WithoutDefaultBranch()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("strict_mode")
				.Switch(
					(param => param is Context ctx && ctx.StrictMode, b => b.Literal("strict")),
					(param => param is Context ctx && !ctx.StrictMode, b => b.Literal("lenient")));

			var parser = builder.Build();

			// Test both conditions
			Assert.Equal("strict", parser.ParseRule("strict_mode", "strict",
				parameter: new Context { StrictMode = true }).Text);

			Assert.Equal("lenient", parser.ParseRule("strict_mode", "lenient",
				parameter: new Context { StrictMode = false }).Text);

			// Test invalid parameter (should fail)
			var result = parser.TryParseRule("strict_mode", "strict", parameter: "invalid");
			Assert.False(result.Success);
		}

		[Fact]
		public void SwitchRule_WithTypedSelector()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("operation")
				.Switch<UserType>(
					defaultBranch: b => b.Literal("unknown"),
					(t => t == UserType.Admin, b => b.Literal("admin_op")),
					(t => t == UserType.Moderator, b => b.Literal("mod_op")),
					(t => t == UserType.User, b => b.Literal("user_op")));

			var parser = builder.Build();

			// Test different user types
			Assert.Equal("admin_op", parser.ParseRule("operation", "admin_op",
				parameter: UserType.Admin).Text);

			Assert.Equal("mod_op", parser.ParseRule("operation", "mod_op",
				parameter: UserType.Moderator).Text);

			Assert.Equal("user_op", parser.ParseRule("operation", "user_op",
				parameter: UserType.User).Text);

			Assert.Equal("unknown", parser.ParseRule("operation", "unknown",
				parameter: UserType.Guest).Text);
		}

		[Fact]
		public void IfRule_InSequence()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(r => r.Whitespaces());

			builder.CreateRule("conditional_expr")
				.Literal("if")
				.If(
					param => param is bool debug && debug,
					debug => debug.Literal("debug").Identifier(),
					normal => normal.Identifier())
				.Literal("then")
				.Identifier();

			var parser = builder.Build();

			// Test with debug mode
			var debugResult = parser.TryParseRule("conditional_expr", "if debug x then y", parameter: true);
			Assert.True(debugResult.Success);
			Assert.Equal("debug x", debugResult.Children[1].Text);
			Assert.Equal("y", debugResult.Children[3].Text);

			// Test without debug mode
			var normalResult = parser.TryParseRule("conditional_expr", "if x then y", parameter: false);
			Assert.True(normalResult.Success);
			Assert.Equal("x", normalResult.Children[1].Text);
		}

		[Fact]
		public void SwitchRule_WithComplexBranches()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(r => r.Whitespaces());

			builder.CreateRule("command")
				.Switch<string>(
					defaultBranch: b => b.Literal("help"),
					(s => s == "admin", b => b.Literal("delete").Identifier()),
					(s => s == "moderator", b => b.Literal("ban").Identifier()),
					(s => s == "user", b => b.Literal("post").Identifier()));

			var parser = builder.Build();

			// Test admin command
			var adminResult = parser.TryParseRule("command", "delete target", parameter: "admin");
			Assert.True(adminResult.Success);
			Assert.Equal("delete", adminResult[0][0].Text);
			Assert.Equal("target", adminResult[0][1].Text);

			// Test user command
			var userResult = parser.TryParseRule("command", "post message", parameter: "user");
			Assert.True(userResult.Success);
			Assert.Equal("post", userResult.Children[0][0].Text);

			// Test guest (default)
			var guestResult = parser.TryParseRule("command", "help", parameter: "guest");
			Assert.True(guestResult.Success);
			Assert.Equal("help", guestResult.Text);
		}

		[Fact]
		public void IfRule_WithTransformation()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("number")
				.If(
					param => param is bool signed && signed,
					signed => signed.Number<int>(),
					unsigned => unsigned.Number<uint>())
				.Transform(v => v.Children[0].Value);

			var parser = builder.Build();

			// Test signed number
			var signedResult = parser.TryParseRule("number", "-42", parameter: true);
			Assert.True(signedResult.Success);
			Assert.Equal(-42, signedResult.Value);

			// Test unsigned number
			var unsignedResult = parser.TryParseRule("number", "42", parameter: false);
			Assert.True(unsignedResult.Success);
			Assert.Equal(42u, unsignedResult.Value);
		}

		[Fact]
		public void NestedConditionalRules()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(r => r.Whitespaces());

			builder.CreateRule("expression")
				.If(
					param => param is Context ctx && ctx.Level == 1,
					level1 => level1.Literal("level1")
						.If(
							param => param is Context ctx && ctx.Debug,
							debug => debug.Literal("debug"),
							normal => normal.Literal("normal")),
					levelOther => levelOther.Literal("other_level"));

			var parser = builder.Build();

			// Test nested conditions
			var result1 = parser.TryParseRule("expression", "level1 debug",
				parameter: new Context { Level = 1, Debug = true });
			Assert.True(result1.Success);

			var result2 = parser.TryParseRule("expression", "level1 normal",
				parameter: new Context { Level = 1, Debug = false });
			Assert.True(result2.Success);

			var result3 = parser.TryParseRule("expression", "other_level",
				parameter: new Context { Level = 2 });
			Assert.True(result3.Success);
		}

		[Fact]
		public void ConditionalRules_WithErrorRecovery()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("config")
				.If(
					param => param is bool strict && strict,
					strict => strict.Literal("strict").Number<int>(),
					lenient => lenient.Number<int>())
				.Transform(v => v.Children.Last().Value);

			var parser = builder.Build();

			// Test strict mode with invalid input
			var strictResult = parser.TryParseRule("config", "strict abc", parameter: true);
			Assert.False(strictResult.Success);

			// Test lenient mode with same input (should also fail for number parsing)
			var lenientResult = parser.TryParseRule("config", "abc", parameter: false);
			Assert.False(lenientResult.Success);
		}

		// Supporting classes for tests
		private class User
		{
			public bool IsAdmin { get; set; }
			public bool IsModerator { get; set; }
			public bool IsPremium { get; set; }
		}

		private class Context
		{
			public bool StrictMode { get; set; }
			public int Level { get; set; }
			public bool Debug { get; set; }
		}

		private enum UserType
		{
			Guest,
			User,
			Moderator,
			Admin
		}
	}
}