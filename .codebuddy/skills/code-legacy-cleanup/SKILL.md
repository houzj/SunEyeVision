---
name: code-legacy-cleanup
description: This skill provides guidance for cleaning up legacy code and deprecated concepts from codebases, with specific focus on refactoring from legacy architectures to modern solutions. Use this skill when dealing with code cleanup, removal of deprecated patterns, or migration between architectural paradigms.
---

# Legacy Code Cleanup Skill

## Purpose

This skill guides the systematic cleanup of legacy code, deprecated concepts, and architectural migration artifacts from codebases. It provides a structured approach to safely removing obsolete code while maintaining system integrity.

## When to Use This Skill

Use this skill when:
- Cleaning up deprecated architectural patterns (e.g., Recipe/Project → Solution migration)
- Removing unused or commented-out code blocks
- Renaming incorrectly named commands, methods, and properties
- Deleting isolated or orphaned classes
- Migrating from old APIs to new architectures
- Consolidating duplicate or redundant functionality

## Cleanup Workflow

### Phase 1: Comprehensive Analysis

Before making any changes, perform a thorough impact analysis:

**Search for all legacy patterns:**
```bash
# Search for legacy concept names across codebase
search_content pattern="Recipe|Project" outputMode="files_with_matches"

# Find class definitions
search_content pattern="class.*Recipe|class.*Project" outputMode="files_with_matches"

# Identify usage patterns
search_content pattern="RecipeRef|RecipeGroupName|RecipeId" outputMode="files_with_matches"
```

**Categorize findings:**
- **Core classes** - Primary definitions of legacy concepts
- **Reference classes** - Classes that reference or use legacy concepts
- **UI layer** - ViewModels, Commands, XAML bindings
- **Test code** - Unit tests and integration tests
- **Documentation** - Comments, summaries, and doc files

### Phase 2: Impact Assessment

Analyze dependencies and usage patterns:

**Check for external references:**
```bash
# Verify if a class has external dependencies
search_content pattern=": Recipe|: RecipeGroup|RecipeJsonConverter" outputMode="files_with_matches"

# Check instantiation patterns
search_content pattern="new Recipe\\(|new RecipeGroup\\(" outputMode="files_with_matches"
```

**Safety criteria for deletion:**
- ✅ Zero external references
- ✅ No runtime dependencies
- ✅ Internal migration tools use separate independent classes
- ✅ New architecture completely replaces functionality

**High-risk scenarios requiring caution:**
- ⚠️ Classes with external references
- ⚠️ Runtime dependencies in critical paths
- ⚠️ Public APIs used by external systems
- ⚠️ Configuration or serialization dependencies

### Phase 3: Safe Cleanup Execution

Execute cleanup in phases, from lowest to highest risk:

**Phase 3.1: Delete Core Legacy Classes (Zero Risk)**
```bash
# Delete when confirmed no external dependencies
Delete-Files:
  - src/Workflow/Recipe.cs
  - src/Workflow/RecipeGroup.cs
  - src/Workflow/RecipeJsonConverter.cs
```

**Phase 3.2: Refactor UI Layer (Low Risk)**
- Rename incorrectly named commands and methods
- Update XAML bindings
- Delete disabled functionality
- Update log messages to match current architecture

**Phase 3.3: Clean Commented Code (Low Risk)**
- Delete large commented-out blocks (>20 lines)
- Remove TODOs that reference deleted features
- Update inline comments to reflect current architecture

**Phase 3.4: Handle Orphaned Classes (Medium Risk)**
- Verify no external usage before deletion
- If in doubt, mark with [Obsolete] instead of deleting
- Consider if functionality might be needed in future

### Phase 4: Validation

After cleanup, validate all changes:

**Lint check:**
```bash
read_lints paths="<modified-file>"
```

**Compilation verification:**
```bash
dotnet build <solution-file>
```

**Functional testing:**
- Verify renamed commands work correctly
- Check UI bindings are functional
- Ensure no runtime errors from deleted classes

### Phase 5: Documentation

Update or document the cleanup:

**Update inline documentation:**
- Remove references to deleted classes
- Update architectural summaries
- Document migration history in code comments

**Create migration guides (if needed):**
- Document what was removed and why
- Provide guidance for new developers
- Reference original documentation if available

## Best Practices

### 1. Preserve Functional Code

**DO NOT delete:**
- Working functionality, even if incorrectly named
- Commands that provide useful features
- Code that users depend on

**DO:**
- Rename incorrectly named features (e.g., "SaveCurrentRecipe" → "SaveCurrentSolution")
- Update internal implementation while preserving public API
- Add deprecation warnings before removing features

### 2. Maintain Architectural Consistency

**Naming conventions:**
- Use consistent terminology throughout codebase
- Prefer modern architectural terms over legacy ones
- Update all references: code, comments, XAML, tests

**Example refactoring:**
```csharp
// Before (legacy naming)
public ICommand SaveCurrentRecipeCommand { get; }
private void ExecuteSaveCurrentRecipe()
{
    LogInfo("保存当前配方");
}

// After (modern naming)
public ICommand SaveCurrentSolutionCommand { get; }
private void ExecuteSaveCurrentSolution()
{
    LogInfo("保存当前解决方案");
}
```

### 3. Progressive Cleanup Strategy

**Order of operations:**
1. Start with zero-risk deletions (orphaned classes)
2. Refactor semantic issues (rename, log messages)
3. Remove disabled functionality
4. Clean up comments and documentation
5. Update tests

**Risk mitigation:**
- Use version control for easy rollback
- Test after each phase
- Keep detailed commit messages
- Maintain backup of deleted code temporarily if needed

### 4. Validation Before Deletion

**Pre-deletion checklist:**
- [ ] No external file references
- [ ] No runtime dependencies
- [ ] Linter reports no errors
- [ ] Tests pass after related changes
- [ ] UI functionality verified
- [ ] Documentation updated

## Common Patterns

### Pattern 1: Complete Concept Migration

**Scenario:** Legacy concept fully replaced by new architecture

**Actions:**
1. Delete all core classes of legacy concept
2. Remove all references in other classes
3. Update UI layer (ViewModels, Commands, XAML)
4. Clean up commented code
5. Delete test code for legacy concept
6. Update documentation

**Example:** Recipe → Solution migration

### Pattern 2: Semantic Refactoring

**Scenario:** Code works but uses incorrect naming

**Actions:**
1. Identify all occurrences of old naming
2. Rename classes, methods, properties
3. Update XAML bindings
4. Update log messages and user-facing text
5. Update comments and documentation

**Example:** "SaveCurrentRecipeCommand" → "SaveCurrentSolutionCommand"

### Pattern 3: Orphaned Class Cleanup

**Scenario:** Isolated class with no external references

**Actions:**
1. Verify no external usage through comprehensive search
2. Check for serialization/deserialization dependencies
3. Verify not used by reflection or dynamic loading
4. Delete class file
5. Verify compilation succeeds

**Example:** DeviceBinding with RecipeRef/RecipeGroupName properties

### Pattern 4: Commented Code Removal

**Scenario:** Large blocks of commented-out legacy code

**Actions:**
1. Identify commented code blocks (>20 lines)
2. Verify functionality is fully removed/replaced
3. Delete entire comment block including markers
4. Verify no TODOs depend on commented code
5. Clean up orphaned TODOs

**Example:** SolutionConfigurationDialogViewModel ~200 lines of commented Recipe code

## Troubleshooting

### Issue: Compilation Errors After Deletion

**Cause:** Undetected external dependencies

**Solution:**
1. Check compiler error messages for missing references
2. Search for usages of deleted classes
3. Restore deleted code if critical
4. Update dependent code before re-deleting

### Issue: Runtime Errors After Refactoring

**Cause:** XAML bindings not updated or renamed incorrectly

**Solution:**
1. Check all XAML Command bindings match new names
2. Verify property paths in bindings
3. Check DataContext assignments
4. Use data binding diagnostics if available

### Issue: Lint Errors in Modified Files

**Cause:** Syntax errors or formatting issues

**Solution:**
1. Read lint diagnostics
2. Fix syntax errors first
3. Address formatting warnings
4. Verify no unintended code was removed

## Metrics

Track cleanup progress with these metrics:

**Code quality metrics:**
- Lines of code removed
- Files deleted
- Renamed entities (classes, methods, properties)
- Comment blocks removed
- Linter errors reduced

**Architecture metrics:**
- Legacy concepts removed
- New architecture adoption percentage
- Semantic consistency improved
- Orphaned code eliminated

## References

**Bundled resources:**

None - This skill provides procedural guidance without requiring additional scripts or references.

**Related documentation:**

- Solution architecture documentation
- Migration history records
- Project coding standards
