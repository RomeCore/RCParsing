---
title: Settings and their override modes
icon: gears
---

Parser and every rule themselves can be configured to control some behaviors of parsing. Each rule's setting can be configured with a specific override mode that determines how it propagates through the rule hierarchy. Here is a list of settings that can be configured:

- **Skipping strategy**: Controls how the parser tries to skip the skip-rules, does not work if the skip-rule is not configured.
- **Skip-rule**: The target skip-rule that the parser will try to skip when parsing rules.
- **Error handling**: Defines how rules and tokens should act when encountering an error, record it to context, do not record it (ignore) or throw it (rarely usable).
- **Barriers ignorance**: Whether ignore barrier tokens or not.

Also, settings for each **rule** (not parser) can have *override modes*, which control inheritance behavior.  
Here is short example how you can confige the parser and each rule:

```csharp
// Configure the parser for skipping whitespaces
builder.Settings.Skip(r => r.Whitespaces());

builder.CreateRule("string")
    .Literal('"')
    .TextUntil('"')
    .Literal('"')
    // Call the Configure to apply settings for rule
	.Configure(c => c.NoSkipping()); // Remove skipping when parsing this rule
```

But if you do it like this, the parser won't skip any rules when parsing the `string` rule. Do this:

```csharp
builder.CreateRule("string")
    .Literal('"')
    .TextUntil('"')
    .Literal('"')
	.Configure(c => c.NoSkipping(), ParserSettingMode.LocalForChildrenOnly); // Apply the setting for the child rules, not the target rule (this rule is sequence currently)
```

Now parser will try to skip whitespaces before parsing the `string` rule, but it won't try to skip it when, for example, parsing the `Literal` or `TextUntil` tokens itself.

There is list of possible override/inheritance modes:

- **InheritForSelfAndChildren**, default: Applies the parent's setting for this element and all its children, ignoring any local or global (parser) settings.
- **LocalForSelfAndChildren**, default when configuration applied to rule: Applies the local setting for this element and all its children. This is the default when explicitly providing a local setting.
- **LocalForSelfOnly**: Applies the local setting for this element only, while propagating the parent's setting to all child elements.
- **LocalForChildrenOnly**: Applies the parent's setting for this element only, while propagating the local setting to all child elements.
- **GlobalForSelfAndChildren**: Applies the global setting for this element and all its children, ignoring any inheritance hierarchy.
- **GlobalForSelfOnly**: Applies the global setting for this element only, while propagating the parent's setting to all child elements.
- **GlobalForChildrenOnly**: Applies the parent's setting for this element only, while propagating the global setting to all child elements.

These modes provide fine-grained control over how settings propagate through your parser's rule hierarchy, allowing you to customize behavior at different levels of parsing.

Also, token patterns does not have their own configuration, but you can apply *default* configuration that will be applied to the rule that brings the token, just like `Transform` functions:

```csharp
// Attach the default configuration for token
builder.CreateToken("identifier")
    .Identifier()
    .Configure(c => c.IgnoreErrors());

// And it will be applied to the rule:
builder.CreateRule("id_rule")
    .Token("identifier");
```