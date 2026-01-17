# How to Use the Mediator

This guide explains how to integrate the **Mediator framework** into your solution.


## Prerequisites

- .NET SDK installed
- A solution containing the target project


## Setup Instructions

### 1. Clone the Repository

Clone the repository containing the framework:

```bash
git clone <repository-url>
```

### 2. Copy the projects

Copy the projects `Mediator.Contract` and `Mediator.Generator`
into your solution.

### 3. Update the `.csproj` file of your consuming project

Add the following `ProjectReference` to the implementing project’s `.csproj` file:

```xml
<ItemGroup>
    <ProjectReference Include="..\Mediator.Contract\Mediator.Contract.csproj" />

    <ProjectReference Include="..\Mediator.Generator\Mediator.Generator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 4. Register the Mediator in Dependency Injection container
```bash
services.AddSingletonMediatorServices();
```

### 5. Clean the Solution

```bash
dotnet clean
```

### 6. Restore Dependencies

```bash
dotnet restore
```

### 7. Build the Solution

```bash
dotnet build
```
## Done ✅

The Mediator framework is now integrated and ready to use.

