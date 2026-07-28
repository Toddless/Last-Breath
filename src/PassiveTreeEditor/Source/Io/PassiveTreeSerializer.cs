namespace PassiveTreeEditor.Source.Io
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Core.Data;
    using Core.Enums;
    using Model;
    using Newtonsoft.Json;

    /// <summary>
    /// Reads and writes the tree file. Writing is canonical — nodes sorted by id, edges normalized
    /// and sorted, positions rounded — so that the same tree always produces the same bytes and a
    /// save after a load is a no-op in git. Reading mirrors the game's error policy: a broken record
    /// is reported and skipped, the rest of the file still loads.
    /// </summary>
    public static class PassiveTreeSerializer
    {
        public static string Serialize(PassiveTreeDocument document)
        {
            var builder = new StringBuilder();

            using (var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture))
            using (var jsonWriter = new JsonTextWriter(stringWriter)
                   {
                       Formatting = Formatting.Indented,
                       Indentation = 4,
                       IndentChar = ' ',
                       Culture = CultureInfo.InvariantCulture
                   })
            {
                JsonSerializer.CreateDefault().Serialize(jsonWriter, ToDto(document));
            }

            // LF regardless of host: .gitattributes normalizes the repo to LF, so writing it directly
            // keeps the file byte-identical between a save here and a fresh checkout.
            return builder.Replace("\r\n", "\n").Append('\n').ToString();
        }

        public static PassiveTreeDocument Deserialize(string json, List<string> issues)
        {
            PassiveTreeDto? dto;
            try
            {
                dto = JsonConvert.DeserializeObject<PassiveTreeDto>(json);
            }
            catch (Exception exception)
            {
                issues.Add($"the file is not valid JSON: {exception.Message}");
                return new PassiveTreeDocument();
            }

            if (dto is null)
            {
                issues.Add("the file is empty");
                return new PassiveTreeDocument();
            }

            if (dto.Version > PassiveTreeFormat.Version)
                issues.Add($"file version {dto.Version} is newer than this editor understands ({PassiveTreeFormat.Version})");

            return FromDto(dto, issues);
        }

        public static void Save(PassiveTreeDocument document, string path)
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(path, Serialize(document), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public static PassiveTreeDocument Load(string path, List<string> issues)
        {
            if (!File.Exists(path))
            {
                issues.Add($"no file at {path}");
                return new PassiveTreeDocument();
            }

            return Deserialize(File.ReadAllText(path), issues);
        }

        private static PassiveTreeDto ToDto(PassiveTreeDocument document)
        {
            var dto = new PassiveTreeDto
            {
                Version = PassiveTreeFormat.Version,
                Budget = document.Budget
            };

            foreach (PassiveNode node in document.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal))
                dto.Nodes.Add(ToDto(node));

            dto.Edges = document.Links
                .OrderBy(link => link.A, StringComparer.Ordinal)
                .ThenBy(link => link.B, StringComparer.Ordinal)
                .Select(link => new PassiveEdgeDto { From = link.A, To = link.B })
                .ToList();

            return dto;
        }

        private static PassiveNodeDto ToDto(PassiveNode node) => new()
        {
            Id = node.Id,
            Kind = node.Kind.ToString(),
            Stance = node.Stance?.ToString(),
            HybridStance = node.HybridStance?.ToString(),
            X = MathF.Round(node.X, PassiveTreeFormat.PositionDecimals),
            Y = MathF.Round(node.Y, PassiveTreeFormat.PositionDecimals),
            Title = NullIfBlank(node.Title),
            Description = NullIfBlank(node.Description),
            AbilityId = NullIfBlank(node.AbilityId),
            Modifiers = node.Modifiers.Count == 0 ? null : node.Modifiers.Select(ToDto).ToList()
        };

        private static ModifierLineDto ToDto(ModifierLine line) => new()
        {
            Parameter = line.Parameter.ToString(),
            ValueType = line.ValueType.ToString(),
            Value = MathF.Round(line.Value, PassiveTreeFormat.ValueDecimals),
            Condition = NullIfBlank(line.Condition)
        };

        private static PassiveTreeDocument FromDto(PassiveTreeDto dto, List<string> issues)
        {
            var document = new PassiveTreeDocument { Budget = dto.Budget > 0 ? dto.Budget : PassiveTreeDocument.DefaultBudget };

            foreach (PassiveNodeDto nodeDto in dto.Nodes)
            {
                PassiveNode? node = FromDto(nodeDto, issues);
                if (node is null) continue;

                if (!document.AddNode(node))
                    issues.Add($"node '{nodeDto.Id}': duplicate or empty id, skipped");
            }

            foreach (PassiveEdgeDto edge in dto.Edges)
                if (!document.Link(edge.From, edge.To))
                    issues.Add($"edge '{edge.From}' - '{edge.To}': unknown endpoint or duplicate, skipped");

            return document;
        }

        private static PassiveNode? FromDto(PassiveNodeDto dto, List<string> issues)
        {
            try
            {
                var node = new PassiveNode
                {
                    Id = dto.Id,
                    Kind = EnumParser.ParseEnum<PassiveNodeKind>(dto.Kind),
                    Stance = string.IsNullOrWhiteSpace(dto.Stance) ? null : EnumParser.ParseEnum<Stance>(dto.Stance),
                    HybridStance = string.IsNullOrWhiteSpace(dto.HybridStance) ? null : EnumParser.ParseEnum<Stance>(dto.HybridStance),
                    X = dto.X,
                    Y = dto.Y,
                    Title = dto.Title ?? string.Empty,
                    Description = dto.Description ?? string.Empty,
                    AbilityId = dto.AbilityId ?? string.Empty
                };

                foreach (ModifierLineDto lineDto in dto.Modifiers ?? [])
                {
                    ModifierLine? line = FromDto(lineDto, dto.Id, issues);
                    if (line is not null) node.Modifiers.Add(line);
                }

                return node;
            }
            catch (FormatException exception)
            {
                issues.Add($"node '{dto.Id}': {exception.Message}, skipped");
                return null;
            }
        }

        private static ModifierLine? FromDto(ModifierLineDto dto, string nodeId, List<string> issues)
        {
            try
            {
                return new ModifierLine
                {
                    Parameter = EnumParser.ParseEnum<EntityParameter>(dto.Parameter),
                    ValueType = EnumParser.ParseEnum<ModifierValueType>(dto.ValueType),
                    Value = dto.Value,
                    Condition = dto.Condition ?? string.Empty
                };
            }
            catch (FormatException exception)
            {
                issues.Add($"node '{nodeId}', modifier line: {exception.Message}, skipped");
                return null;
            }
        }

        private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
