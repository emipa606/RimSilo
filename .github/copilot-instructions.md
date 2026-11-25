# GitHub Copilot Instructions for RimSilo (Continued)

Welcome to the GitHub Copilot Instructions for RimSilo (Continued)! This document provides a comprehensive guide to understanding the mod, its features, coding patterns, XML integration, and Harmony patching. This will help you contribute effectively to the mod.

## Mod Overview and Purpose

RimSilo (Continued) is an update to the RimSilo mod, enhancing and expanding its functionality in RimWorld. The mod introduces various storage solutions, such as Vaults, Warehouses, and Static Chambers, to manage and store resources, items, and even colonists effectively.

## Key Features and Systems

### Secure Vault
- **Purpose**: Store large amounts of money and valuable items securely.
- **Capacity**: Up to 230,000 units (B18 update).
- **Subscription Fee**: Reduced to 850 units (B18 update).

### Warehouse
- **Purpose**: Prevent goods from deteriorating.
- **Capacity**: Initial capacity of 2000 kg, up to 15000 kg (B18 update).

### Static Chamber
- **Purpose**: Freeze wounds and other medical conditions, and temporarily store prisoners and colonists.
- **Functionality**: Reliable but inexpensive solution for medical emergencies.

### Global DropPod Delivery Service
- **Purpose**: Deliver items or entire caravans using DropPods.
- **Functionality**: Send goods to caravans or raid settlements, and drop entire caravans onto friendly faction bases.

### Transfer System
- **Purpose**: Transfer currency, items, animals, prisoners, and colonists between different maps.
- **Functionality**: Utilizes orbital trade beacons for seamless transfers.

## Coding Patterns and Conventions

### C# Code Structure
- **Mod Structure**: The mod is organized into various C# files, each handling specific functionalities.
- **Namespacing**: Ensure that all classes and methods follow a consistent naming convention and are appropriately namespaced.
- **Comments and Documentation**: Include detailed comments and XML documentation to explain complex logic and usage.

### XML Integration
- **Def Files**: XML files are used to define game objects, settings, and other configurations.
- **Mod Compatibility**: Ensure that XML changes do not break mod compatibility with other mods.

### Harmony Patching
- **Purpose**: Modify vanilla game behavior without altering the original code.
- **Best Practices**: Use Harmony patching sparingly and only when necessary. Ensure patches are well-tested and do not introduce unintended side effects.

## Suggestions for Copilot

### General Guidelines
- **Code Review**: Always review generated code for correctness and adherence to the mod's coding standards.
- **Testing**: Test any new features thoroughly to ensure they work as expected.
- **Documentation**: Update documentation and comments when adding new features or making changes.

### Specific Recommendations
- **C# Files**: Focus on generating methods and classes that adhere to the established naming conventions and structure.
- **XML Files**: Generate XML def files that are compatible with the mod and do not conflict with other mods.
- **Harmony Patches**: When generating Harmony patches, ensure they are targeted and do not disrupt vanilla game behavior.

## Example Code Snippets

### C# Example: Capacity Expansion
csharp
using RimWorld;
using Verse;
using System;

namespace RimSilo
{
    public class CapacityExpansion
    {
        public static void EnsureExpansionInBound()
        {
            // Ensure vault capacity is within the specified bounds
            if (VaultCapacity < MinVaultCapacity || VaultCapacity > MaxVaultCapacity)
            {
                VaultCapacity = Math.Clamp(VaultCapacity, MinVaultCapacity, MaxVaultCapacity);
            }
        }
    }
}


### XML Example: About.xml
xml
<About>
    <ModName>RimSilo (Continued)</ModName>
    <Author>user19990313</Author>
    <Description>
        Update of user19990313's mod. This mod introduces various storage solutions, such as Vaults, Warehouses, and Static Chambers, to manage and store resources, items, and even colonists effectively.
    </Description>
    <Languages>English,繁簡中文,한국어,日本語</Languages>
    <Version>1.0.0</Version>
</About>


### Harmony Patch Example: Modify Vault Capacity
csharp
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimSilo
{
    [HarmonyPatch(typeof(Vault), "SomeMethod")]
    public static class Patch_Vault_SomeMethod
    {
        public static void Prefix(ref int capacity)
        {
            // Modify vault capacity based on mod settings
            capacity = RimSiloMod.Settings.VaultCapacity;
        }
    }
}


By following these guidelines and examples, you can contribute effectively to the RimSilo (Continued) mod, ensuring that your contributions are well-integrated, tested, and adhere to the mod's coding standards.

Thank you for your contributions to RimSilo (Continued)!

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Temporary files

- Do not create temporary files, build output, test artifacts, or Copilot session
  folders inside the mod root.
- Store temporary artifacts under `%USERPROFILE%\CopilotTemp\[modname]`.
- Direct build and test output there as well, without changing project settings.
- Clean up task-specific temporary artifacts when finished.

## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

