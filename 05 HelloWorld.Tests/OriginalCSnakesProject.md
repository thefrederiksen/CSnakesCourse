# Original CSnakes Project - Python File Setup

This document describes how to configure Python files in a CSnakes project so they are available to consuming projects (tests, other libraries, etc.).

## Python File Configuration

### 1. Directory Structure
Place all Python files in a `Python/` folder at the project root.

### 2. Project File Setup
```xml
<ItemGroup>
  <!-- This enables CSnakes code generation for .py files AND copies ALL files to output -->
  <AdditionalFiles Include="Python\**\*.*">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </AdditionalFiles>
</ItemGroup>
```

This single configuration:
- Generates C# interfaces from all `.py` files
- Copies all Python files to output
- Copies `requirements.txt` if present
- Copies any other files (data, configs, etc.) in the Python directory

## Why This Matters

- `AdditionalFiles` tells CSnakes to generate C# interfaces from your Python files
- `CopyToOutputDirectory="Always"` ensures Python files are available at runtime
- Without this, consuming projects won't have access to the Python files they need to execute