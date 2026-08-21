namespace Core.PassiveTree
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Data;
    using Enums;
    using Modifiers.Context;
    using Newtonsoft.Json;

    /// <summary>Reads and writes the tree file. Writing is canonical (nodes/edges sorted, positions
    /// rounded) so a save-after-load is a no-op in git. Reading skips and reports a broken record; the rest of the file still loads.</summary>
    public static class PassiveTreeSerializer
    {
        /// <summary>Half a step of the grid the spread is stored on: below this two values are the same
        /// stop, so anything nearer the default than this IS the default.</summary>
        private const float SpreadEpsilon = View.CanvasTransform.SpreadStep / 2f;

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
                issues.Add($"file version {dto.Version} is newer than this build understands ({PassiveTreeFormat.Version})");

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
                Budget = document.Budget,
                Spread = SpreadOrNothing(document.Spread)
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
            Modifiers = node.Modifiers.Count == 0 ? null : node.Modifiers.Select(ToDto).ToList(),
            ContextModifiers = node.ContextModifiers.Count == 0 ? null : node.ContextModifiers.Select(ToDto).ToList()
        };

        private static ModifierLineDto ToDto(ModifierLine line) => new()
        {
            Parameter = line.Parameter.ToString(),
            ValueType = line.ValueType.ToString(),
            Value = MathF.Round(line.Value, PassiveTreeFormat.ValueDecimals),
            PerParameter = line.PerParameter?.ToString(),
            Condition = NullIfBlank(line.Condition)
        };

        private static ContextModifierLineDto ToDto(ContextModifierLine line) => new()
        {
            Parameter = line.Parameter.ToString(),
            ValueType = line.ValueType.ToString(),
            Value = line.IsFlag ? null : MathF.Round(line.Value, PassiveTreeFormat.ValueDecimals),
            Condition = NullIfBlank(line.Condition)
        };

        private static PassiveTreeDocument FromDto(PassiveTreeDto dto, List<string> issues)
        {
            var document = new PassiveTreeDocument
            {
                Budget = dto.Budget > 0 ? dto.Budget : PassiveTreeDocument.DefaultBudget,

                // Mirror of the budget rule: missing/invalid falls back to the authored default rather
                // than clamping to a number nobody chose.
                Spread = dto.Spread is > 0f
                    ? View.CanvasTransform.NormalizeSpread(dto.Spread.Value)
                    : View.CanvasTransform.DefaultSpread
            };

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
                    Kind = ParseMember<PassiveNodeKind>(dto.Kind),
                    Stance = string.IsNullOrWhiteSpace(dto.Stance) ? null : ParseMember<Stance>(dto.Stance),
                    HybridStance = string.IsNullOrWhiteSpace(dto.HybridStance) ? null : ParseMember<Stance>(dto.HybridStance),
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

                foreach (ContextModifierLineDto lineDto in dto.ContextModifiers ?? [])
                {
                    ContextModifierLine? line = FromDto(lineDto, dto.Id, issues);
                    if (line is not null) node.ContextModifiers.Add(line);
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
                var valueType = ParseMember<ModifierValueType>(dto.ValueType);
                var parameter = ParseMember<EntityParameter>(dto.Parameter);
                EntityParameter? perParameter = string.IsNullOrWhiteSpace(dto.PerParameter)
                    ? null
                    : ParseMember<EntityParameter>(dto.PerParameter);

                // A switch is meaningless in parameter math, so it's refused here rather than becoming a
                // modifier the resolver silently drops.
                if (valueType == ModifierValueType.Flag)
                    throw new FormatException($"'{dto.Parameter}' is an entity parameter — a flag has no meaning in parameter math");

                if (WhyNotScalable(parameter, perParameter) is { } refusal) throw new FormatException(refusal);

                return new ModifierLine
                {
                    Parameter = parameter,
                    ValueType = valueType,
                    Value = dto.Value,
                    PerParameter = perParameter,
                    Condition = dto.Condition ?? string.Empty
                };
            }
            catch (FormatException exception)
            {
                issues.Add($"node '{nodeId}', modifier line: {exception.Message}, skipped");
                return null;
            }
        }

        private static ContextModifierLine? FromDto(ContextModifierLineDto dto, string nodeId, List<string> issues)
        {
            try
            {
                var parameter = ParseMember<ContextParameter>(dto.Parameter);
                var valueType = ParseMember<ModifierValueType>(dto.ValueType);

                // A knob's binding is its own business: an unbound knob throws far from the file that
                // caused it, and the wrong line kind silently reads a switch as a full-strength bonus.
                // Refused here at load instead, so the failure stays local.
                if (ContextKnobs.WhyRefused(parameter, valueType) is { } refusal) throw new FormatException(refusal);

                return new ContextModifierLine
                {
                    Parameter = parameter,
                    ValueType = valueType,
                    Value = dto.Value ?? 0f,
                    Condition = dto.Condition ?? string.Empty
                };
            }
            catch (FormatException exception)
            {
                issues.Add($"node '{nodeId}', context line: {exception.Message}, skipped");
                return null;
            }
        }

        /// <summary>Why a line cannot be measured per unit of <paramref name="per"/>, or null when it can.
        /// A parameter that resolves its own scale rewrites itself on every resolve, and an aggregate hands
        /// its change to every member of the family, so sharing a family is the same loop one step wider.
        /// An aggregate as the scale is refused outright: nobody ever holds a value for one, so the line
        /// would be worth nothing and say nothing about why.</summary>
        private static string? WhyNotScalable(EntityParameter parameter, EntityParameter? per)
        {
            if (per is not { } scale) return null;
            if (scale == parameter) return $"'{parameter}' cannot be scaled per unit of itself";
            if (AggregateParameters.IsAggregate(scale)) return $"'{scale}' is an aggregate — nothing holds a value for it to scale by";
            if (AggregateParameters.Members(parameter).Contains(scale) || AggregateParameters.Aggregates(parameter).Contains(scale))
                return $"'{parameter}' and '{scale}' are one family — resolving either resolves the other";

            return null;
        }

        /// <summary>Stricter than <see cref="EnumParser"/> alone, which accepts a bare number for any enum
        /// and would let "parameter": "42" parse into a nonexistent member.</summary>
        private static TEnum ParseMember<TEnum>(string value) where TEnum : struct, Enum
        {
            TEnum member = EnumParser.ParseEnum<TEnum>(value);

            return Enum.IsDefined(member)
                ? member
                : throw new FormatException($"'{value}' is not a valid {typeof(TEnum).Name}");
        }

        private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

        /// <summary>Spread on its storage grid, or null at the authored default — writing it there would
        /// re-diff every file whose layout nobody touched.</summary>
        private static float? SpreadOrNothing(float spread)
        {
            float normalized = View.CanvasTransform.NormalizeSpread(spread);

            return MathF.Abs(normalized - View.CanvasTransform.DefaultSpread) < SpreadEpsilon ? null : normalized;
        }
    }
}
