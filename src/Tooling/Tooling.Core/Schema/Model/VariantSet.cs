namespace Tooling.Schema.Model
{
    /// <summary>
    /// The shapes one record may take and how the file says which is which: a set of shapes with no
    /// way to tell them apart would leave the editor knowing the forms and not which to draw.
    /// <para>Either a field whose VALUE names the shape — <see cref="Discriminator"/> names that field
    /// — or, with no such field, the PRESENCE of a key, each variant's
    /// <see cref="VariantSchema.DiscriminatorValue"/> being the key that must be there for it. Either
    /// way the field belongs to the variants and is listed among their own
    /// <see cref="RecordSchema.Fields"/>; the inspector draws it once, as the picker.</para>
    /// </summary>
    public sealed record VariantSet
    {
        public VariantSet(SchemaList<VariantSchema> variants, string? discriminator = null)
        {
            Discriminator = discriminator;
            Variants = variants;
        }

        public SchemaList<VariantSchema> Variants
        {
            get => field;
            init
            {
                SchemaGuard.Agree(SchemaGuard.NotEmpty(value, nameof(Variants)), Discriminator);
                field = value;
            }
        }

        /// <summary>Json field whose value picks the shape; null when the presence of a key picks it.</summary>
        public string? Discriminator
        {
            get => field;
            init
            {
                SchemaGuard.Agree(Variants, value);
                field = value;
            }
        }
    }
}
