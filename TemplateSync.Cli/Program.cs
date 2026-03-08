using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Diagnostics;
using EddyLib;

namespace TemplateSync.Cli;

internal static partial class Program
{
    private const string EddyLibraryGuid = "2e38bada-fd78-4b1a-a943-dc06595af3d5";
    private const string DefaultRepoUrl = "https://github.com/Eddy3D-Dev/Eddy3D-OutdoorIndoorTemplates.git";

    public static int Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)))
        {
            PrintUsage();
            return 0;
        }

        if (!TryParseOptions(args, out var options))
        {
            return 2;
        }

        try
        {
            return Run(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 2;
        }
    }

    private static int Run(Options options)
    {
        var solutionRoot = options.SolutionRoot is { Length: > 0 }
            ? Path.GetFullPath(options.SolutionRoot)
            : FindSolutionRoot();

        var repoDir = !string.IsNullOrWhiteSpace(options.TemplateRepoDir)
            ? Path.GetFullPath(options.TemplateRepoDir)
            : GetDefaultTemplateRepoDir(solutionRoot);

        EnsureLocalTemplateRepo(
            repoDir,
            options.RepoUrl,
            options.Branch,
            options.NoGitUpdate);

        var templateRoot = string.IsNullOrWhiteSpace(options.TemplateRoot)
            ? repoDir
            : Path.GetFullPath(options.TemplateRoot);

        if (!Directory.Exists(templateRoot))
        {
            throw new DirectoryNotFoundException($"Template root not found: {templateRoot}");
        }

        var componentsRoot = Path.Combine(solutionRoot, "Eddy", "Components");
        if (!Directory.Exists(componentsRoot))
        {
            throw new DirectoryNotFoundException($"Could not locate Eddy component source folder: {componentsRoot}");
        }

        var definitions = LoadCurrentCommitComponentIoDefinitions(componentsRoot);
        if (definitions.Count == 0)
        {
            throw new InvalidOperationException("No component definitions were parsed from Eddy/Components.");
        }

        var ghxFiles = Directory.GetFiles(templateRoot, "*.ghx", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ghxFiles.Count == 0)
        {
            throw new InvalidOperationException($"No .ghx files found under: {templateRoot}");
        }

        var mode = options.Fix && options.FixCounts
            ? "SYNC (--fix --fix-counts)"
            : options.Fix
                ? "SYNC (--fix)"
                : options.FixCounts
                    ? "SYNC COUNTS (--fix-counts)"
                    : "CHECK";

        var countFixReferences = options.FixCounts
            ? BuildCountFixReferenceMap(ghxFiles, templateRoot, definitions)
            : new Dictionary<Guid, ComponentParameterChunkReference>();

        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine($"Template repo dir: {repoDir}");
        Console.WriteLine($"Template repo branch: {options.Branch}");
        Console.WriteLine($"Template repo source: {options.RepoUrl}");
        Console.WriteLine($"Template repo update: {(options.NoGitUpdate ? "disabled (--no-git-update)" : "enabled")}");
        Console.WriteLine($"Template root: {templateRoot}");
        Console.WriteLine($"Solution root: {solutionRoot}");
        Console.WriteLine($"Template files: {ghxFiles.Count}");
        Console.WriteLine($"Component definitions: {definitions.Count}");
        if (options.FixCounts)
        {
            Console.WriteLine($"Count-fix references found: {countFixReferences.Count}");
        }
        Console.WriteLine();

        var mismatches = new List<string>();
        var scannedInstances = 0;
        var touchedFiles = 0;
        var changedValues = 0;
        var fixedDifferences = 0;
        var countFixOperations = 0;
        var countMismatchesAutoFixed = 0;

        foreach (var ghxFile in ghxFiles)
        {
            var relativePath = Path.GetRelativePath(templateRoot, ghxFile).Replace('\\', '/');
            var doc = XDocument.Load(ghxFile);
            var fileChanged = false;
            var fileInstanceCount = 0;
            var fileChangedValues = 0;
            var fileCountFixOperations = 0;

            foreach (var component in ExtractEddyComponentUsages(doc, relativePath))
            {
                scannedInstances++;
                fileInstanceCount++;

                if (!definitions.TryGetValue(component.ComponentGuid, out var expected))
                {
                    mismatches.Add(
                        $"{relativePath} :: {component.ComponentName} ({component.ComponentGuid}) missing component definition in current codebase.");
                    continue;
                }

                if (component.Inputs.Count != expected.Inputs.Count || component.Outputs.Count != expected.Outputs.Count)
                {
                    var resolved = TryReconcileCountMismatch(
                        component,
                        expected,
                        countFixReferences,
                        options.FixCounts,
                        ref fileChanged,
                        out var operations,
                        out var failureReason);

                    if (operations > 0)
                    {
                        countFixOperations += operations;
                        fileCountFixOperations += operations;
                    }

                    if (resolved)
                    {
                        if (operations > 0)
                        {
                            countMismatchesAutoFixed++;
                        }

                        // Reconciled count mismatch; continue to label checks below.
                    }
                    else
                    {
                        var reasonSuffix = string.IsNullOrWhiteSpace(failureReason)
                            ? string.Empty
                            : $" ({failureReason})";

                        mismatches.Add(
                            $"{relativePath} :: {component.ComponentName} ({component.ComponentGuid}) count mismatch (inputs {component.Inputs.Count}/{expected.Inputs.Count}, outputs {component.Outputs.Count}/{expected.Outputs.Count}){reasonSuffix}.");
                        continue;
                    }
                }

                if (component.Inputs.Count != expected.Inputs.Count || component.Outputs.Count != expected.Outputs.Count)
                {
                    mismatches.Add(
                        $"{relativePath} :: {component.ComponentName} ({component.ComponentGuid}) count mismatch (inputs {component.Inputs.Count}/{expected.Inputs.Count}, outputs {component.Outputs.Count}/{expected.Outputs.Count}).");
                    continue;
                }

                for (var i = 0; i < component.Inputs.Count; i++)
                {
                    fixedDifferences += CompareAndMaybeFixParameter(
                        mismatches,
                        component,
                        "input",
                        i,
                        component.Inputs[i],
                        expected.Inputs[i],
                        options.Fix,
                        ref fileChanged,
                        ref changedValues,
                        ref fileChangedValues);
                }

                for (var i = 0; i < component.Outputs.Count; i++)
                {
                    fixedDifferences += CompareAndMaybeFixParameter(
                        mismatches,
                        component,
                        "output",
                        i,
                        component.Outputs[i],
                        expected.Outputs[i],
                        options.Fix,
                        ref fileChanged,
                        ref changedValues,
                        ref fileChangedValues);
                }
            }

            if (fileChanged && (options.Fix || options.FixCounts))
            {
                doc.Save(ghxFile);
                touchedFiles++;
            }

            if (options.Verbose)
            {
                Console.WriteLine(
                    $"{relativePath}: Eddy components {fileInstanceCount}, changed values {fileChangedValues}, count-fix operations {fileCountFixOperations}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Summary");
        Console.WriteLine($"- Eddy component instances scanned: {scannedInstances}");
        Console.WriteLine($"- Files updated: {touchedFiles}");
        Console.WriteLine($"- Values updated: {changedValues}");
        Console.WriteLine($"- Differences auto-fixed in-memory: {fixedDifferences}");
        if (options.FixCounts)
        {
            Console.WriteLine($"- Count-fix operations applied: {countFixOperations}");
            Console.WriteLine($"- Count mismatches auto-fixed: {countMismatchesAutoFixed}");
        }
        Console.WriteLine($"- Remaining mismatches: {mismatches.Count}");

        if (mismatches.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Mismatches");
            var limit = Math.Min(options.MaxReport, mismatches.Count);
            for (var i = 0; i < limit; i++)
            {
                Console.WriteLine($"- {mismatches[i]}");
            }

            if (mismatches.Count > limit)
            {
                Console.WriteLine($"... {mismatches.Count - limit} more mismatch(es) not shown.");
            }
        }

        return mismatches.Count == 0 ? 0 : 1;
    }

    private static int CompareAndMaybeFixParameter(
        List<string> mismatches,
        TemplateComponentUsage component,
        string ioKind,
        int index,
        ParameterSlot actual,
        ParameterDefinition expected,
        bool fix,
        ref bool fileChanged,
        ref int changedValuesTotal,
        ref int fileChangedValues)
    {
        var fixedHere = 0;

        if (!string.IsNullOrEmpty(expected.Name))
        {
            if (!string.Equals(actual.Name, expected.Name, StringComparison.Ordinal))
            {
                if (fix)
                {
                    if (SetOrAddStringItemValue(actual.ItemsElement, "Name", expected.Name))
                    {
                        actual.Name = expected.Name;
                        fileChanged = true;
                        changedValuesTotal++;
                        fileChangedValues++;
                        fixedHere++;
                    }
                }

                if (!string.Equals(actual.Name, expected.Name, StringComparison.Ordinal))
                {
                    mismatches.Add(
                        $"{component.TemplatePath} :: {component.ComponentName} ({component.ComponentGuid}) {ioKind}[{index}] Name differs (expected '{expected.Name}', template '{actual.Name}').");
                }
            }
        }

        if (!string.IsNullOrEmpty(expected.NickName))
        {
            if (!string.Equals(actual.NickName, expected.NickName, StringComparison.Ordinal))
            {
                if (fix)
                {
                    if (SetOrAddStringItemValue(actual.ItemsElement, "NickName", expected.NickName))
                    {
                        actual.NickName = expected.NickName;
                        fileChanged = true;
                        changedValuesTotal++;
                        fileChangedValues++;
                        fixedHere++;
                    }
                }

                if (!string.Equals(actual.NickName, expected.NickName, StringComparison.Ordinal))
                {
                    mismatches.Add(
                        $"{component.TemplatePath} :: {component.ComponentName} ({component.ComponentGuid}) {ioKind}[{index}] NickName differs (expected '{expected.NickName}', template '{actual.NickName}').");
                }
            }
        }

        return fixedHere;
    }

    private static bool SetOrAddStringItemValue(XElement itemsElement, string itemName, string expectedValue)
    {
        var item = itemsElement
            .Elements("item")
            .FirstOrDefault(i => string.Equals(i.Attribute("name")?.Value, itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            var added = new XElement("item", expectedValue);
            added.SetAttributeValue("name", itemName);
            added.SetAttributeValue("type_name", "gh_string");
            added.SetAttributeValue("type_code", "10");
            itemsElement.Add(added);
            return true;
        }

        if (string.Equals(item.Value ?? string.Empty, expectedValue, StringComparison.Ordinal))
        {
            return false;
        }

        item.Value = expectedValue;
        return true;
    }
}
