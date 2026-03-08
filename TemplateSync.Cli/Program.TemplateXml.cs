using System.Xml.Linq;

namespace TemplateSync.Cli;

internal static partial class Program
{
    private static bool IsInputParameterChunk(XElement parameterChunk)
    {
        var chunkName = parameterChunk.Attribute("name")?.Value;
        return chunkName != null && chunkName.StartsWith("param_input", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOutputParameterChunk(XElement parameterChunk)
    {
        var chunkName = parameterChunk.Attribute("name")?.Value;
        return chunkName != null && chunkName.StartsWith("param_output", StringComparison.OrdinalIgnoreCase);
    }

    private static void RefreshParameterSlots(TemplateComponentUsage usage)
    {
        usage.Inputs.Clear();
        usage.Outputs.Clear();

        var paramChunks = usage.ParameterChunksElement;
        if (paramChunks == null)
        {
            return;
        }

        foreach (var paramChunk in paramChunks.Elements("chunk"))
        {
            var isInput = IsInputParameterChunk(paramChunk);
            var isOutput = IsOutputParameterChunk(paramChunk);
            if (!isInput && !isOutput)
            {
                continue;
            }

            var paramItems = paramChunk.Element("items");
            if (paramItems == null)
            {
                continue;
            }

            var slot = new ParameterSlot
            {
                ItemsElement = paramItems,
                Name = GetItemValue(paramItems, "Name") ?? string.Empty,
                NickName = GetItemValue(paramItems, "NickName") ?? string.Empty
            };

            if (isInput)
            {
                usage.Inputs.Add(slot);
            }
            else
            {
                usage.Outputs.Add(slot);
            }
        }
    }

    private static IEnumerable<TemplateComponentUsage> ExtractEddyComponentUsages(XDocument doc, string templatePath)
    {
        var root = doc.Root;
        if (root?.Name.LocalName != "Archive")
        {
            yield break;
        }

        var definitionObjects = root.Descendants("chunk")
            .FirstOrDefault(c => c.Attribute("name")?.Value == "DefinitionObjects");
        if (definitionObjects == null)
        {
            yield break;
        }

        var objectChunks = definitionObjects.Elements("chunks")
            .SelectMany(c => c.Elements("chunk"))
            .Where(c => c.Attribute("name")?.Value == "Object");

        foreach (var objChunk in objectChunks)
        {
            var items = objChunk.Element("items");
            if (items == null)
            {
                continue;
            }

            var lib = GetItemValue(items, "Lib");
            if (!string.Equals(lib, EddyLibraryGuid, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var guidText = GetItemValue(items, "GUID");
            if (!Guid.TryParse(guidText, out var componentGuid))
            {
                continue;
            }

            var usage = new TemplateComponentUsage
            {
                TemplatePath = templatePath,
                ComponentGuid = componentGuid,
                ComponentName = GetItemValue(items, "Name") ?? "Unknown",
            };

            var container = objChunk.Elements("chunks")
                .SelectMany(c => c.Elements("chunk"))
                .FirstOrDefault(c => c.Attribute("name")?.Value == "Container");

            usage.ParameterChunksElement = container?.Element("chunks");
            RefreshParameterSlots(usage);

            if (container == null)
            {
                yield return usage;
                continue;
            }

            yield return usage;
        }
    }

    private static string? GetItemValue(XElement itemsElement, string itemName)
    {
        return itemsElement
            .Elements("item")
            .FirstOrDefault(i => string.Equals(i.Attribute("name")?.Value, itemName, StringComparison.OrdinalIgnoreCase))
            ?.Value
            ?.Trim();
    }

}
