using System.Xml.Linq;

namespace TemplateSync.Cli;

internal static partial class Program
{
    private static Dictionary<Guid, ComponentParameterChunkReference> BuildCountFixReferenceMap(
        IEnumerable<string> ghxFiles,
        string templateRoot,
        Dictionary<Guid, ComponentIoDefinition> definitions)
    {
        var references = new Dictionary<Guid, ComponentParameterChunkReference>();

        foreach (var ghxFile in ghxFiles)
        {
            var doc = XDocument.Load(ghxFile);
            var relativePath = Path.GetRelativePath(templateRoot, ghxFile).Replace('\\', '/');

            foreach (var component in ExtractEddyComponentUsages(doc, relativePath))
            {
                if (references.ContainsKey(component.ComponentGuid))
                {
                    continue;
                }

                if (!definitions.TryGetValue(component.ComponentGuid, out var expected))
                {
                    continue;
                }

                if (component.ParameterChunksElement == null)
                {
                    continue;
                }

                if (component.Inputs.Count != expected.Inputs.Count || component.Outputs.Count != expected.Outputs.Count)
                {
                    continue;
                }

                references.Add(component.ComponentGuid, new ComponentParameterChunkReference
                {
                    SourceTemplatePath = component.TemplatePath,
                    InputChunks = component.ParameterChunksElement
                        .Elements("chunk")
                        .Where(IsInputParameterChunk)
                        .Select(c => new XElement(c))
                        .ToList(),
                    OutputChunks = component.ParameterChunksElement
                        .Elements("chunk")
                        .Where(IsOutputParameterChunk)
                        .Select(c => new XElement(c))
                        .ToList()
                });
            }
        }

        return references;
    }

    private static bool TryReconcileCountMismatch(
        TemplateComponentUsage component,
        ComponentIoDefinition expected,
        Dictionary<Guid, ComponentParameterChunkReference> references,
        bool fixCounts,
        ref bool fileChanged,
        out int operationsApplied,
        out string? failureReason)
    {
        operationsApplied = 0;
        failureReason = null;

        if (component.Inputs.Count == expected.Inputs.Count && component.Outputs.Count == expected.Outputs.Count)
        {
            return true;
        }

        if (!fixCounts)
        {
            return false;
        }

        var paramChunks = component.ParameterChunksElement;
        if (paramChunks == null)
        {
            failureReason = "cannot auto-fix counts: missing parameter container";
            return false;
        }

        while (component.Inputs.Count > expected.Inputs.Count)
        {
            var toRemove = paramChunks.Elements("chunk").LastOrDefault(IsInputParameterChunk);
            if (toRemove == null)
            {
                failureReason = "cannot auto-fix counts: expected trailing input parameter chunk not found";
                return false;
            }

            toRemove.Remove();
            operationsApplied++;
            fileChanged = true;
            RefreshParameterSlots(component);
        }

        while (component.Outputs.Count > expected.Outputs.Count)
        {
            var toRemove = paramChunks.Elements("chunk").LastOrDefault(IsOutputParameterChunk);
            if (toRemove == null)
            {
                failureReason = "cannot auto-fix counts: expected trailing output parameter chunk not found";
                return false;
            }

            toRemove.Remove();
            operationsApplied++;
            fileChanged = true;
            RefreshParameterSlots(component);
        }

        while (component.Inputs.Count < expected.Inputs.Count)
        {
            if (!references.TryGetValue(component.ComponentGuid, out var reference))
            {
                failureReason = "cannot auto-fix counts: no reference component with matching port counts found";
                return false;
            }

            var nextIndex = component.Inputs.Count;
            if (nextIndex >= reference.InputChunks.Count)
            {
                failureReason =
                    $"cannot auto-fix counts: reference only has {reference.InputChunks.Count} input chunk(s)";
                return false;
            }

            var clonedInput = new XElement(reference.InputChunks[nextIndex]);
            NormalizeInsertedParameterChunk(clonedInput);

            var firstOutput = paramChunks.Elements("chunk").FirstOrDefault(IsOutputParameterChunk);
            if (firstOutput != null)
            {
                firstOutput.AddBeforeSelf(clonedInput);
            }
            else
            {
                paramChunks.Add(clonedInput);
            }

            operationsApplied++;
            fileChanged = true;
            RefreshParameterSlots(component);
        }

        while (component.Outputs.Count < expected.Outputs.Count)
        {
            if (!references.TryGetValue(component.ComponentGuid, out var reference))
            {
                failureReason = "cannot auto-fix counts: no reference component with matching port counts found";
                return false;
            }

            var nextIndex = component.Outputs.Count;
            if (nextIndex >= reference.OutputChunks.Count)
            {
                failureReason =
                    $"cannot auto-fix counts: reference only has {reference.OutputChunks.Count} output chunk(s)";
                return false;
            }

            var clonedOutput = new XElement(reference.OutputChunks[nextIndex]);
            NormalizeInsertedParameterChunk(clonedOutput);

            var lastOutput = paramChunks.Elements("chunk").LastOrDefault(IsOutputParameterChunk);
            if (lastOutput != null)
            {
                lastOutput.AddAfterSelf(clonedOutput);
            }
            else
            {
                paramChunks.Add(clonedOutput);
            }

            operationsApplied++;
            fileChanged = true;
            RefreshParameterSlots(component);
        }

        if (component.Inputs.Count == expected.Inputs.Count && component.Outputs.Count == expected.Outputs.Count)
        {
            return true;
        }

        failureReason = "count mismatch remains after guarded reconciliation";
        return false;
    }

    private static void NormalizeInsertedParameterChunk(XElement parameterChunk)
    {
        var items = parameterChunk.Element("items");
        if (items == null)
        {
            items = new XElement("items");
            parameterChunk.AddFirst(items);
        }

        RemoveItemsByName(items, "Source");
        SetOrAddItemValue(items, "SourceCount", "0", "gh_int32", "3");
        SetOrAddItemValue(items, "InstanceGuid", Guid.NewGuid().ToString(), "gh_guid", "9");
    }

    private static void SetOrAddItemValue(
        XElement itemsElement,
        string itemName,
        string value,
        string typeName,
        string typeCode)
    {
        var item = itemsElement
            .Elements("item")
            .FirstOrDefault(i => string.Equals(i.Attribute("name")?.Value, itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            var added = new XElement("item", value);
            added.SetAttributeValue("name", itemName);
            added.SetAttributeValue("type_name", typeName);
            added.SetAttributeValue("type_code", typeCode);
            itemsElement.Add(added);
            return;
        }

        item.Value = value;
    }

    private static void RemoveItemsByName(XElement itemsElement, string itemName)
    {
        var removeList = itemsElement
            .Elements("item")
            .Where(i => string.Equals(i.Attribute("name")?.Value, itemName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var item in removeList)
        {
            item.Remove();
        }
    }

}
