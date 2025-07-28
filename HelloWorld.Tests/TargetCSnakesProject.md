# Target CSnakes Project Setup

This document describes how to configure a project that references another CSnakes-enabled project.

## Setup Steps

### 1. Add Project Reference
In Solution Explorer, add a reference to the CSnakes-enabled project.

### 2. Link Python Files
Double-click your project file to edit it and add:

```xml
<ItemGroup>
  <!-- Link all files from Python directory -->
  <Content Include="..\OriginalProject\Python\**\*.*" 
           Link="Python\%(RecursiveDir)%(Filename)%(Extension)">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

Replace `OriginalProject` with the actual project name you're referencing.

That's it! This configuration:
- Copies all Python files and resources at build time
- Maintains directory structure
- Automatically includes new files as they're added
- Avoids code generation conflicts by using `Content` instead of `AdditionalFiles`