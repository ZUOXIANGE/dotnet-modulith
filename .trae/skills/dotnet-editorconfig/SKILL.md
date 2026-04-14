---
name: "dotnet-editorconfig"
description: "Creates standardized .editorconfig files for .NET projects following Microsoft and Roslyn team conventions. Invoke when user needs to create or update .editorconfig for a .NET/C# project."
---

# .NET EditorConfig Generator

Creates a standardized `.editorconfig` file for .NET projects following Microsoft's official conventions and the Roslyn compiler team's best practices.

## When to Use

- User asks to create or update an `.editorconfig` file for a .NET project
- User wants to standardize code formatting across a .NET team
- User mentions code style, formatting rules, or editor configuration
- Setting up a new .NET project that needs consistent coding standards

## Workflow

1. **Detect project context**: Check if the project already has an `.editorconfig` file. If it does, ask whether to overwrite or merge.
2. **Determine target framework**: Identify the .NET version (prefer reading `global.json` or `Directory.Build.props`) to apply version-appropriate rules (e.g., `csharp_style_prefer_primary_constructors` requires C# 12+).
3. **Generate the `.editorconfig`**: Create the file with all sections below.
4. **Verify**: Run `dotnet format --verify-no-changes` to validate the configuration works with the project.

## Standard .editorconfig Template

Generate the following content for the `.editorconfig` file at the project root:

```editorconfig
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
# File headers
dotnet_diagnostic.CS1591.severity = none

# var preferences
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

# Expression-bodied members
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_constructors = false:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_indexers = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion
csharp_style_expression_bodied_lambdas = true:suggestion
csharp_style_expression_bodied_local_functions = when_on_single_line:suggestion

# Pattern matching
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion

# Null checking
csharp_style_throw_expression = true:suggestion
csharp_style_conditional_delegate_call = true:suggestion
csharp_style_prefer_null_check_over_type_check = true:suggestion

# Code block preferences
csharp_prefer_braces = true:suggestion
csharp_prefer_simple_using_statement = true:suggestion

# Method group conversion
csharp_style_prefer_method_group_conversion = true:suggestion

# Primary constructors (C# 12+)
csharp_style_prefer_primary_constructors = true:suggestion

# Switch expression
csharp_style_prefer_switch_expression = true:suggestion

# Namespace declarations
csharp_style_namespace_declarations = file_scoped:suggestion

# Readonly struct
csharp_style_prefer_readonly_struct = true:suggestion
csharp_style_prefer_readonly_struct_member = true:suggestion

# New line preferences
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_between_query_expression_clauses = true

# Indentation preferences
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_indent_labels = one_less_than_current
csharp_indent_block_contents = true
csharp_indent_braces = false

# Space preferences
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_parentheses = false
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_around_binary_operators = before_and_after
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_declaration_name_and_open_parenthesis = false
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_call_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_after_comma = true
csharp_space_before_comma = false
csharp_space_after_dot = false
csharp_space_before_dot = false
csharp_space_after_semicolon_in_for_statement = true
csharp_space_before_semicolon_in_for_statement = false
csharp_space_around_declaration_statements = false
csharp_space_before_open_square_brackets = false
csharp_space_between_empty_square_brackets = false
csharp_space_between_square_brackets = false

# Wrapping preferences
csharp_preserve_single_line_statements = false
csharp_preserve_single_line_blocks = true

# .NET style rules
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_require_accessibility_modifiers = always:suggestion
dotnet_style_readonly_field = true:suggestion
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion

# Object and collection initializers
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_auto_properties = true:suggestion
dotnet_style_prefer_simplified_boolean_expressions = true:suggestion

# Null-coalescing and null-propagation
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_prefer_is_null_check_over_type_check = true:suggestion

# Conditional expressions
dotnet_style_prefer_conditional_expression_over_assignment = true:suggestion
dotnet_style_prefer_conditional_expression_over_return = true:suggestion

# Compound assignment and interpolation
dotnet_style_prefer_compound_assignment = true:suggestion
dotnet_style_prefer_simplified_interpolation = true:suggestion

# Namespace and using directives
dotnet_style_namespace_match_folder = true:suggestion
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Naming conventions
dotnet_naming_rule.interface_should_begin_with_i.severity = suggestion
dotnet_naming_rule.interface_should_begin_with_i.symbols = interface
dotnet_naming_rule.interface_should_begin_with_i.style = begins_with_i

dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.types_should_be_pascal_case.symbols = types
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.non_private_fields_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.non_private_fields_should_be_pascal_case.symbols = non_private_fields
dotnet_naming_rule.non_private_fields_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.severity = suggestion
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.style = underscore_camel_case

dotnet_naming_rule.parameters_should_be_camel_case.severity = suggestion
dotnet_naming_rule.parameters_should_be_camel_case.symbols = parameters
dotnet_naming_rule.parameters_should_be_camel_case.style = camel_case

dotnet_naming_rule.local_variables_should_be_camel_case.severity = suggestion
dotnet_naming_rule.local_variables_should_be_camel_case.symbols = local_variables
dotnet_naming_rule.local_variables_should_be_camel_case.style = camel_case

# Symbol specifications
dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = *

dotnet_naming_symbols.types.applicable_kinds = class, struct, interface, enum, delegate
dotnet_naming_symbols.types.applicable_accessibilities = *

dotnet_naming_symbols.non_private_fields.applicable_kinds = field
dotnet_naming_symbols.non_private_fields.applicable_accessibilities = public, protected, internal, protected_internal, private_protected

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_symbols.parameters.applicable_kinds = parameter
dotnet_naming_symbols.parameters.applicable_accessibilities = *

dotnet_naming_symbols.local_variables.applicable_kinds = local
dotnet_naming_symbols.local_variables.applicable_accessibilities = *

# Naming styles
dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

dotnet_naming_style.pascal_case.capitalization = pascal_case

dotnet_naming_style.underscore_camel_case.required_prefix = _
dotnet_naming_style.underscore_camel_case.capitalization = camel_case

dotnet_naming_style.camel_case.capitalization = camel_case

[*.{xml,csproj,props,targets}]
indent_size = 2

[*.{json,yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

## Customization Notes

- **`csharp_style_prefer_primary_constructors`**: Only include this rule if the project uses C# 12+ (.NET 8+). Remove for older versions.
- **`csharp_style_namespace_declarations`**: Set to `file_scoped` for modern projects. Use `block_scoped` for legacy compatibility.
- **Severity levels**: Use `suggestion` for team conventions (shown as IDE hints), `warning` for important rules, `error` for mandatory rules.
- **`dotnet_diagnostic.CS1591.severity`**: Set to `none` to disable XML doc warnings. Change to `warning` if the project requires documentation.
- **Private field naming**: The template uses `_camelCase` prefix. Some teams prefer `s_camelCase` for static fields or `m_camelCase` — adjust the naming rule accordingly.

## Verification

After generating the `.editorconfig`, verify it works:

```bash
# Check if dotnet format respects the configuration
dotnet format --verify-no-changes --verbosity diagnostic

# Apply formatting to fix violations
dotnet format --verbosity normal
```
