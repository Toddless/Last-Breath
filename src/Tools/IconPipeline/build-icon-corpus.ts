#!/usr/bin/env -S deno run --allow-read --allow-write
//
// Icon prompt corpus builder.
//
// Reads the project's own data — en.po for names/descriptions, the SharedData json for
// structural facts — and emits the prompt corpus the batch generator consumes:
//   [{ id, family, prompt }]
//
// The prompt of every entry is the approved canon constant verbatim plus a subject
// sentence assembled from that entity's meaning. Old icons are never referenced.
//
// Usage:
//   deno run --allow-read --allow-write src/Tools/IconPipeline/build-icon-corpus.ts \
//     --output ./icon-corpus.json [--data-root <dir>] [--quiet]

// ============================================================================
// Roots and paths — every location is a constant, none are spelled inline.
// ============================================================================

/** src/Tools/IconPipeline/ -> src/SharedData/. Resolved from the script, so the cwd is free. */
const DEFAULT_DATA_ROOT = new URL("../../SharedData/", import.meta.url);

const DATA_PATHS = {
  localization: "Localization/en.po",
  abilities: "Abilities",
  equipItems: "EquipItems",
  resources: "Resources",
} as const;

/**
 * Approved generation canon (AssetStyleGuide.md lineage). Inserted verbatim into every prompt.
 *
 * The closing paragraph is a guard against the model answering with bare line art. An object whose
 * enclosed areas are left blank reads as a hollow outline over the game's dark panels, and any part
 * of it painted in the background color is indistinguishable from background wherever it touches
 * the frame.
 */
const CANON_PROMPT =
  "stylized comic book dark fantasy game icon, single object centered and isolated, bold expressive ink linework with dense crosshatching accents, painted color over inked lines, heavily desaturated washed-out faded palette - cold greys and browns, faded olive, dim rust accents, deep shadows, grim gothic mood, in the manner of Battle Chasers and Darksiders concept art, three-quarter view, even lighting, no background scenery, plain flat warm cream background, no frame, no border, no ornament ring, no text, no watermark, 2D game asset, no photorealism. " +
  "Every area enclosed by the linework is finished painted art: it carries color from that palette and is modeled with shading and crosshatching, and is never left as blank white, bare paper or the flat background color. No uncolored outline drawing, no coloring-book look - the cream belongs to the background alone and never appears inside the object";

/**
 * Canon exemption for raw materials. Desaturation makes copper, iron and silver read as the same
 * grey stone in the bag, so the material's own colour is lifted out from under it — and only that.
 */
const CANON_MATERIAL_COLOR_CLAUSE =
  "One exemption from that palette: the material's OWN colour is the single saturated thing in the picture and must read at full true chroma, so the material is identified at a glance and never mistaken for a neighbouring one - copper reads as copper, gold as gold, an emerald as an emerald. Everything that is not the material itself - the ink linework, the shadows, the surrounding host rock, the vial glass, the dowels, frames, cords and wrappings the material is held by, any setting or backing - stays washed out and desaturated exactly as described above.";

/**
 * Canon exemption for smithing consumables. Four dusts differ by grade alone, so here the saturated
 * carrier is the rarity colour of the game's own ladder rather than a material.
 */
const CANON_RARITY_COLOR_CLAUSE =
  "One exemption from that palette: the grade colour named below is the single saturated thing in the picture and must read at full true chroma, so two grades of the same object are told apart at a glance. Everything else - the ink linework, the shadows, the material body, the vessel or wrapper the object is held in - stays washed out and desaturated exactly as described above. The object rests on nothing: there is no cloth, mat, board, table or ground under it anywhere in the picture.";

// ============================================================================
// Types
// ============================================================================

type Family =
  | "ability"
  | "weapon"
  | "armor"
  | "jewellery"
  | "crafting_resource"
  | "upgrade_resource"
  | "stance";

type Severity = "error" | "warn";

interface Issue {
  severity: Severity;
  where: string;
  message: string;
}

/** One entity pulled out of the project data, before its prompt is written. */
interface Entity {
  id: string;
  /** Structural facts of the source record; shape differs per family. */
  facts: Record<string, unknown>;
}

interface CorpusEntry {
  id: string;
  family: Family;
  prompt: string;
}

interface FamilySpec {
  family: Family;
  /** Expected id count, cross-checked against what the data actually yields. */
  expected: number;
  load: (ctx: BuildContext) => Promise<Entity[]>;
  /** Subject sentence: what the icon depicts. Canon is added by the assembler. */
  subject: (entity: Entity, ctx: BuildContext) => string;
  /** Canon exemption this family is generated under, if any. Sits between canon and subject. */
  canonClause?: string;
}

// ============================================================================
// Issue log — nothing is swallowed silently.
// ============================================================================

class IssueLog {
  private readonly items: Issue[] = [];

  add(severity: Severity, where: string, message: string): void {
    this.items.push({ severity, where, message });
  }

  get all(): readonly Issue[] {
    return this.items;
  }

  count(severity: Severity): number {
    return this.items.filter((i) => i.severity === severity).length;
  }
}

// ============================================================================
// .po parsing
// ============================================================================

const PO_ESCAPES: Record<string, string> = { n: "\n", t: "\t", r: "\r", '"': '"', "\\": "\\" };

function unescapePo(raw: string): string {
  return raw.replace(/\\(.)/g, (_, ch: string) => PO_ESCAPES[ch] ?? ch);
}

function quotedPayload(line: string): string | null {
  const first = line.indexOf('"');
  const last = line.lastIndexOf('"');
  if (first < 0 || last <= first) return null;
  return line.slice(first + 1, last);
}

/**
 * Localization lookup over a parsed .po. Absent keys and empty translations are
 * reported by the caller, never defaulted behind its back.
 */
class Localization {
  private constructor(private readonly entries: Map<string, string>) {}

  static parse(text: string, issues: IssueLog): Localization {
    const entries = new Map<string, string>();
    const lines = text.split(/\r?\n/);
    let key: string | null = null;
    let value: string | null = null;

    const flush = () => {
      if (key !== null && value !== null) entries.set(key, unescapePo(value));
      key = null;
      value = null;
    };

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i].trim();
      if (line.startsWith("#") || line.length === 0) {
        flush();
        continue;
      }
      if (line.startsWith("msgid ")) {
        flush();
        const payload = quotedPayload(line);
        if (payload === null) {
          issues.add("error", DATA_PATHS.localization, `line ${i + 1}: msgid without a quoted value`);
          continue;
        }
        key = unescapePo(payload);
        continue;
      }
      if (line.startsWith("msgstr ")) {
        const payload = quotedPayload(line);
        if (payload === null) {
          issues.add("error", DATA_PATHS.localization, `line ${i + 1}: msgstr without a quoted value`);
          continue;
        }
        value = payload;
        continue;
      }
      if (line.startsWith('"') && value !== null) {
        value += quotedPayload(line) ?? "";
        continue;
      }
    }
    flush();

    if (entries.size === 0) issues.add("error", DATA_PATHS.localization, "no translations parsed");
    return new Localization(entries);
  }

  /** Localized name, or null when the key is missing or translated to an empty string. */
  name(id: string, issues: IssueLog): string | null {
    const raw = this.entries.get(id);
    if (raw === undefined) {
      issues.add("warn", id, "no name key in en.po");
      return null;
    }
    if (raw.trim().length === 0) {
      issues.add("warn", id, "name key present but msgstr is empty");
      return null;
    }
    return raw;
  }

  /**
   * Localized description with template placeholders stripped, or null when the key is
   * missing or empty. Equipment descriptions are known-empty by design: callers fall back
   * to structural facts instead of failing.
   */
  description(id: string): string | null {
    const raw = this.entries.get(`${id}_Description`);
    if (raw === undefined || raw.trim().length === 0) return null;
    return stripTemplate(raw);
  }
}

/** Drops the description template syntax ({Value}, {x:%}, {@Keyword}, {n|turn|turns}). */
function stripTemplate(text: string): string {
  return text
    .replace(/\{@([A-Za-z_]+)\}/g, (_, key: string) => key.replace(/^Effect_/, "").replace(/_/g, " ").toLowerCase())
    .replace(/\{([A-Za-z]+)\|([^|}]+)\|([^}]+)\}/g, "$3")
    .replace(/\{[^}]*\}/g, "")
    .replace(/\\n/g, " ")
    .replace(/\s{2,}/g, " ")
    .trim();
}

// ============================================================================
// Subject vocabulary — every rule below is a table entry, not a branch.
// ============================================================================

/**
 * Ability motifs: what the icon depicts, read off the ability's mechanic. Keyed by id so a
 * reworked ability is a one-line edit. Abilities missing an entry fall back to their
 * localized description and are reported.
 */
const ABILITY_MOTIFS: Record<string, string> = {
  Ability_Series_Of_Attacks:
    "a fan of three overlapping curved blade slashes, one trailing behind another, their tails breaking into loose ink strokes",
  Ability_Increasing_Pressure:
    "three sword slashes stepped in a rising row, each stroke heavier and longer than the one before, the largest splitting at its tip",
  Ability_Jar_Of_Poison:
    "a thrown clay jar cracking open in mid-air, thick sickly green venom splashing out in heavy droplets",
  Ability_Dark_Shroud:
    "an empty hooded cloak standing upright, its hem dissolving into dark smoke, a single dim ember glowing where the heart would be",
  Ability_Critical_Calculation:
    "a dagger point held under a cracked measuring eye, thin sighting lines converging on the tip",
  Ability_Poison_Explosion:
    "a bursting sphere of venom, curved green shards flung outward from a bright toxic core",
  Ability_Poison_Coating:
    "a dagger blade being coated with venom, thick green drops gathering and falling from its edge",
  Ability_Double_Strike:
    "two sword slashes crossed into an X, the first stroke broad and heavy, the second thin and fast",
  Ability_Ares_Blessing:
    "a battered horned war helm crowned with a laurel wreath, a dull red glow burning behind the visor slit",
  Ability_Porcupine:
    "a round iron shield bristling with long outward-pointing quills, several snapped off at the base",
  Ability_Berserk_Fury:
    "a snarling horned skull wreathed in dim red rage, burning cracks running through the bone",
  Ability_Head_Butt:
    "a horned helmet slammed forward brow-first, impact cracks and small spinning stun-stars bursting around it",
  Ability_Sacrifice:
    "a ritual dagger driven through an open palm, falling drops of blood turning into pale golden sparks",
  Ability_Armageddon:
    "a single burning meteor plummeting downward, wrapped in flame and black smoke, its cracked core glowing molten",
  Ability_Ice_Block:
    "a massive jagged block of ice dropping down, splinters shearing off its underside",
  Ability_Overload:
    "a cracked arcane orb drinking in energy, thin streams of cold blue mana spiralling inward through its fractures",
  Ability_Chain_Lightning:
    "a forked bolt of lightning zigzagging through three branching jumps, each fork thinner than the last, its body a solid painted mass of cold pale blue-grey planes with a dim violet core and shaded undersides",
  Ability_Ice_Aegis:
    "a kite shield carved from cracked blue ice, frost creeping outward from its rim",
  Ability_Ice_Shards:
    "a tight volley of sharp ice shards flying point-first in formation, cold vapour trailing behind them",
  Ability_Deep_Freeze:
    "a figure sealed inside a jagged column of blue ice, frost crystals growing outward across its surface",
  Ability_Discharge:
    "a shattering hexagonal energy barrier releasing one hard bolt of lightning through its broken centre",
  Ability_Static_Armor:
    "a dented breastplate ringed with crackling static arcs, small charge nodes sparking along its edges",
  Ability_Twin_Assist_Attack:
    "two identical curved blades crossed, both edges running with low burning flame",
  Ability_Twin_Assist_Shield:
    "a round ward shield split down the middle into two mirrored halves, a soft pale glow held between them",
  Ability_Summon_Bone_Wolves:
    "a bleached wolf skull with hollow sockets, thin bone shards rising around it like a gathering pack",
};

/**
 * Stance motifs: what the stance's fighting style looks like as one object. A stance is not an item
 * in a bag but a state sign in the combat HUD, so the three are separated by gross outline first -
 * a wide bottom-heavy mass, a round coil, a tall spike - and only then by what they depict.
 */
const STANCE_MOTIFS: Record<string, string> = {
  Stance_Dexterity:
    "a viper wound into one tight coil and reared to strike, its wedge head raised clear above the coil with fangs bared, a single dim green venom drop falling from one fang",
  Stance_Strength:
    "an empty iron gauntlet clenched into a fist and held knuckles forward, one broad blocky mass at its widest across the scarred knuckle plates, its cuff flaring at the wrist, dim ember-red heat caught deep in the joint seams",
  Stance_Intelligence:
    "a compact cluster of pale ice-blue crystal, one broad central shard flanked by two shorter ones grown from the same base into a single blocky wedge, three small sparks caught deep inside the stone itself - one cold blue, one dim ember, one pale violet - and nothing orbiting it on the outside",
};

/** What a stance icon has to survive: the combat HUD shows it at button size, so its outline carries it. */
const STANCE_PRESENTATION_NOTE =
  "one emblematic object, no figure, and no ring, badge or frame around it: a single compact mass with one bold unbroken outline that still reads at button size, no thin floating slivers and no scattered small parts";

/** Weapon body: what shape the blank weapon has before its name is applied. */
const WEAPON_TYPE_FORMS: Record<string, string> = {
  Sword: "a straight double-edged sword standing blade-up",
  Axe: "a bearded battle axe, head turned to the side",
  Dagger:
    "a short narrow-bladed dagger standing point-up, the grip taking up nearly half of its whole length and the blade the other half, a knife and not a small sword",
  Wand: "a slender carved wand laid upright",
};

/**
 * Grip length only. How a grip is dressed belongs to the item's own silhouette: a table that calls
 * every two-hander cloth-wrapped takes that separator away from the axe whose wrapping identifies it.
 */
const HANDEDNESS_FORMS: Record<string, string> = {
  OneHanded: "one-handed, its grip short and sized for a single hand",
  TwoHanded: "two-handed, its grip long and sized for both hands",
};

/** A weapon is an object, not a material: it is shown whole, and nothing holds or carries it. */
const WEAPON_PRESENTATION_NOTE =
  "the whole weapon shown end to end and unbroken, nothing cropped away by the frame, no hand, no sheath, no rack and nothing propping it up";

/** Armor and jewellery body: the blank object of the slot. */
const SLOT_FORMS: Record<string, string> = {
  Body: "a sleeveless chest armor piece shown alone and empty, no wearer, no stand, no pedestal",
  Helmet: "a helmet shown alone and empty, no wearer, no stand, no pedestal",
  Gloves: "a pair of gauntlets laid side by side",
  Boots: "a pair of boots standing side by side",
  Cloak: "a hooded cloak hanging in heavy folds",
  Belt: "a belt with a heavy buckle, coiled and laid flat",
  Amulet: "an amulet on a chain, its pendant hanging centred",
  Ring: "a single finger ring standing upright",
};

/** A name-derived motif and whether it already says everything the base stats would add. */
interface NameMotif {
  readonly match: RegExp;
  readonly motif: string;
  /** Set when the motif already names the piece's material or stones — the stat accent would only repeat it. */
  readonly dropStatAccents?: true;
}

/**
 * Name motifs for equipment: the meaning the item's name carries, matched against the id.
 * Ordered — the first match wins, so specific keys stand above the generic ones.
 */
const EQUIP_NAME_MOTIFS: readonly NameMotif[] = [
  { match: /creators/i, motif: "patched, threadbare and drained of all colour by unimaginable age, humble far beyond its power", dropStatAccents: true },
  { match: /hunters_dream/i, motif: "light hunting leathers strung with bone charms and feather tokens" },
  { match: /stone(heart|_tread)?/i, motif: "rough grey stone slabs bound down with dark iron straps" },
  { match: /steel/i, motif: "plain beaten plate steel, dented and scratched from service" },
  { match: /vital_core/i, motif: "a dull red core stone set at its centre, warm veins running out from it" },
  { match: /feral_instinct/i, motif: "raw untanned fur with claws and fangs bound to it as fetishes" },
  { match: /archmage/i, motif: "dark scholarly cloth embroidered with pale arcane sigils" },
  { match: /mysterious_bastion/i, motif: "blackened iron etched over with faint unreadable sigils" },
  { match: /aegis/i, motif: "layered overlapping warding plates ringed by a thin cold glow" },
  { match: /hunter/i, motif: "worn hunting leather with fur trim and buckled straps" },
  { match: /porcupine/i, motif: "bristling all over with outward-pointing iron quills" },
  { match: /carapace/i, motif: "overlapping ridged chitin plates like a beetle's shell" },
  { match: /strong_spirit/i, motif: "pale translucent spirit-light seeping between its plates" },
  { match: /ice_mage/i, motif: "frost-rimed sheets of pale blue ice laid over dark cloth" },
  { match: /disaster_herald/i, motif: "scorched black plate split by dim ember-lit cracks" },
  { match: /talaria/i, motif: "small feathered wings sprouting at the heels" },
  { match: /rebirth/i, motif: "green shoots pushing up through cracks in the worn material" },
  { match: /direwolf/i, motif: "a shaggy grey wolf pelt with the skull still attached as a hood", dropStatAccents: true },
  { match: /dark_priest/i, motif: "heavy black funeral cloth closed by a tarnished silver clasp" },
  { match: /light_priest/i, motif: "bleached pale cloth closed by a simple sunburst clasp" },
  { match: /eternal_life_shroud/i, motif: "burial-shroud linen wound in one unbroken thread, faintly luminous" },
  { match: /simple/i, motif: "plain, cheap and unadorned, honest workmanship" },
  { match: /fortune_smile/i, motif: "a worn pair of dice and a bent coin sewn over the knuckles" },
  { match: /titan_hand/i, motif: "crude oversized stone-and-iron plating, far too large for a man" },
  { match: /crushing_grip/i, motif: "heavy spiked knuckle plates, fingers built to crush" },
  { match: /flawlessness/i, motif: "seamless flawless craftsmanship without a single scratch" },
  { match: /dwarven/i, motif: "thick dwarven goldwork set with a square-cut stone", dropStatAccents: true },
  { match: /limitless_magic/i, motif: "a woven silver band holding a floating blue stone", dropStatAccents: true },
  { match: /minor_barrier/i, motif: "a small pale crystal ringed by a faint protective glow", dropStatAccents: true },
  { match: /assassin/i, motif: "a blackened band hiding a thin folded blade edge" },
  { match: /trickster/i, motif: "two interlocked bands that read as one, an illusion worked in metal" },
  { match: /knight/i, motif: "polished steel bearing a small heraldic shield motif" },
  { match: /legionnaire/i, motif: "plain military-issue bronze stamped with a legion numeral" },
  { match: /giant/i, motif: "an oversized crude band of unpolished iron" },
  { match: /fire_demon/i, motif: "a blackened band shaped as a clawed finger, dim embers caught in its joints" },
  { match: /hell_servant/i, motif: "a bone-white seal ring carved with a screaming face" },
  { match: /submittion_mark/i, motif: "a cold unadorned iron slave-band stamped with a brand" },
  { match: /bloodthirsty/i, motif: "the blade dark with dried blood, a thirsty groove running its length" },
  { match: /righteous_wrath/i, motif: "the head engraved with a stern sun sigil, dull gold inlaid into the steel" },
  { match: /silent_fury/i, motif: "wrapped in dark cloth to muffle it, only the keen edge left bare" },
  { match: /all_cutting/i, motif: "an impossibly thin blade that seems to part the air along its edge" },
  { match: /lazurite/i, motif: "a deep blue lapis stone in a plain setting", dropStatAccents: true },
  { match: /garnet/i, motif: "a dark blood-red garnet in a plain setting", dropStatAccents: true },
  { match: /malachite/i, motif: "a banded green malachite disc in a plain setting", dropStatAccents: true },
  { match: /trinity/i, motif: "three small stones of different colours set in a row", dropStatAccents: true },
  { match: /pathfinder/i, motif: "a small brass direction needle hung as the pendant", dropStatAccents: true },
  { match: /trapper/i, motif: "knotted cord, small bones and a snare hook hung together", dropStatAccents: true },
  { match: /symbol_of_faith/i, motif: "a simple wooden holy symbol worn smooth by handling", dropStatAccents: true },
  { match: /recovery_source/i, motif: "two small stoppered vials bound together, one dark red and one cold blue", dropStatAccents: true },
  { match: /diamond/i, motif: "a clear cut diamond in a plain setting", dropStatAccents: true },
  { match: /ruby/i, motif: "a deep red ruby in a plain setting", dropStatAccents: true },
  { match: /onyx/i, motif: "a polished black onyx disc in a plain setting", dropStatAccents: true },
  { match: /goliath_seal/i, motif: "a huge heavy seal disc, far larger than a man should wear" },
  { match: /scout_sash/i, motif: "light cloth hung with small pouches and a rolled map" },
  { match: /inquisitor/i, motif: "black leather hung with censer chains and one small key" },
  { match: /life_flow/i, motif: "a glass tube of dark red fluid running along its length", dropStatAccents: true },
  { match: /mana_flow/i, motif: "a glass tube of cold blue fluid running along its length", dropStatAccents: true },
  { match: /ether_flow/i, motif: "a glass tube of colourless shimmering fluid running along its length", dropStatAccents: true },
  { match: /of_life/i, motif: "a dull red heartstone set into the buckle", dropStatAccents: true },
  { match: /leather/i, motif: "plain thick tanned leather with an iron buckle" },
  { match: /cloth/i, motif: "plain woven cloth, soft and unarmored" },
  { match: /dexterity/i, motif: "a slim light band wound with fine wire", dropStatAccents: true },
  { match: /intelligence/i, motif: "a plain band set with one deep blue stone", dropStatAccents: true },
  { match: /medallion/i, motif: "a flat heavy disc pendant" },
];

/**
 * Base stat read as a visual accent rather than a number. Split by family: the same
 * Strength stat is iron studding on a cuirass and a heavy signet stone on a ring.
 */
const STAT_ACCENTS: Partial<Record<Family, Record<string, string>>> = {
  armor: {
    Armor: "thick riveted steel plating",
    Health: "deep dull red leather panelling",
    Evade: "light supple hide, cut away wherever weight could gather",
    Barrier: "faint blue arcane runes glowing along its edges",
  },
  jewellery: {
    Armor: "a small steel plate set into it",
    Health: "a dull red stone set into it",
    Evade: "worked so slight it has almost no weight",
    Barrier: "a pale crystal ringed by a faint cold glow",
    Strength: "a heavy blunt stone in a massive setting",
    Dexterity: "a slim light band wound with fine wire",
    Intelligence: "a deep blue gemstone set into it",
    Mana: "a hollow socket holding cold blue crystal",
    AllAttribute: "three small stones of different colours set in a row",
    HealthRecovery: "a small stoppered vial of dark red liquid bound to it",
    ManaRecovery: "a small stoppered vial of cold blue liquid bound to it",
    BlockChance: "a small heraldic shield worked into the setting",
    CriticalChance: "a sharp faceted stone with a knife-keen edge",
    Accuracy: "fine engraved sighting marks around the band",
    AdditionalHitChance: "paired mirrored detailing, every element doubled",
    MulticastChance: "concentric arcane rings worked into it",
  },
};

/** Rarity read as workmanship, never as a colour-coded frame. */
const RARITY_TREATMENTS: Record<string, string> = {
  Common: "plainly made, worn and unadorned",
  Uncommon: "simply made and lightly trimmed",
  Rare: "finely made with restrained ornament",
  Epic: "ornate and deeply engraved",
  Legendary: "masterwork, ancient and battle-scarred",
  Unique: "singular and storied, one of a kind",
  Mythic: "otherworldly, a faint dim glow seeping out from within",
};

/** Rarity of a raw material is its grade, not its workmanship — ore is never "finely made". */
const RESOURCE_RARITY_TREATMENTS: Record<string, string> = {
  Common: "a poor common-grade sample, dirty and irregular",
  Uncommon: "an ordinary workaday grade",
  Rare: "a clean high grade, evenly coloured",
  Epic: "a superior grade with a faint sheen to it",
  Legendary: "a legendary grade, almost too pure to be natural",
  Mythic: "a mythic grade carrying a faint inner light",
};

/**
 * Grade of a smithing consumable, carrying the saturated colour of the game's own rarity ladder
 * (TextPalette). Four dusts are the same object at four grades, so the colour has to be the BODY of
 * the object: as an inner glow it reads as a highlight and the grades stay indistinguishable at
 * icon size.
 */
const UPGRADE_RARITY_TREATMENTS: Record<string, string> = {
  Common: "a poor common grade — the whole object is neutral pale grey (#d0d0d0) through and through",
  Uncommon: "an ordinary workaday grade — the whole object is fully saturated vivid leaf green (#7ccb64) through and through, green is the colour of the object itself and not a glow inside it",
  Rare: "a clean high grade — the whole object is fully saturated vivid sky blue (#6ec6ff) through and through, blue is the colour of the object itself and not a glow inside it",
  Epic: "a superior grade — the whole object is fully saturated vivid royal violet (#8845BF) through and through, violet is the colour of the object itself and not a glow inside it",
  Legendary: "a top grade — the whole object is fully saturated vivid hot orange (#E86A3F) through and through, orange is the colour of the object itself and not a glow inside it",
  Unique: "a singular grade — the whole object is fully saturated deep amber (#DE791D) through and through, amber is the colour of the object itself and not a glow inside it",
  Mythic: "a mythic grade — the whole object is fully saturated vivid magenta pink (#F056D6) through and through, magenta is the colour of the object itself and not a glow inside it",
};

/** Crafting resource body: the shape a material of this category is stored in. */
const MATERIAL_CATEGORY_FORMS: Record<string, string> = {
  Category_Metal: "a rough chunk of unrefined ore, broken rock with metal veins running through the fracture",
  Category_Leather: "a rolled and corded bundle of tanned hide",
  Category_Fabric: "a folded bolt of woven cloth with one corner turned back",
  Category_Gem: "a single cut gemstone resting on its facets",
  Category_Bone: "a single cleaned bone trophy shown alone",
  Category_Essence: "a small stoppered glass vial holding a slow swirl of essence",
};

/**
 * What the category's icon must not drift into. Stock material reads as a finished item the moment
 * the model is left to guess: ore becomes an ingot, a gem becomes a ring, an essence becomes an
 * apothecary's shelf.
 */
const MATERIAL_PRESENTATION_NOTES: Record<string, string> = {
  Category_Metal: "raw unrefined stock, not an ingot and not a finished item",
  Category_Fabric: "cloth stock, not a garment and not a finished item",
  Category_Leather: "hide stock, not a garment and not a finished item",
  Category_Gem:
    "the bare loose stone and nothing else - no metal setting, no claws or prongs, no bezel, no ring, no rim, no host rock or matrix around it and nothing under it",
  Category_Bone: "a raw trophy taken from the beast, not a carved or finished item",
  Category_Essence: "one sealed vessel by itself, no shelf, no rack and no second bottle",
};

/**
 * Material tint, keyed by the resource's own last tag. Bone and hide entries are qualified
 * with their category because the same beast yields differently-coloured parts.
 */
const MATERIAL_TINTS: Record<string, string> = {
  Copper: "unmistakably copper - hot orange-red metal with vivid verdigris green tarnish in the crevices",
  Iron: "unmistakably iron - dark blue-grey metal with bright orange rust blooming along the edges",
  Silver: "unmistakably silver - bright white mirror-bright metal gone near-black in the hollows",
  Gold: "unmistakably gold - rich saturated yellow gold, warm and bright",
  Mithril: "unmistakably mithril - luminous pale ice-blue white metal",
  Adamantite: "unmistakably adamantite - black metal with a violet sheen across its glassy fracture faces",
  Wool: "coarse undyed off-white wool with loose fibres standing out",
  Linen: "unbleached greyish linen in a plain weave",
  Broadcloth: "dense fulled brown broadcloth",
  FineLinen: "fine pale linen in a tight even weave",
  Serge: "twilled olive serge with a diagonal rib",
  Velvet: "deep wine-red velvet with a soft nap",
  Silk: "ivory silk with a bright liquid sheen",
  Purple: "imperial purple dyed cloth, the richest dye in the world",
  Malachite: "banded green malachite",
  Lazurite: "deep blue lapis lazuli flecked with gold",
  Garnet: "dark blood-red garnet",
  Ruby: "deep red ruby",
  Sapphire: "cold deep blue sapphire",
  Emerald: "dark green emerald",
  Diamond: "colourless clear diamond",
  Beryl: "pale sea-green beryl",
  Alexandrite: "alexandrite shifting between dull green and dim red",
  Taaffeite: "rare pale violet taaffeite with a faint inner light",
  "Category_Leather:Wolf": "coarse grey wolf fur still on the hide",
  "Category_Leather:Deer": "smooth soft tan deer hide",
  "Category_Leather:WildBoar": "thick bristled dark boar hide",
  "Category_Leather:Bear": "heavy shaggy brown bear hide",
  "Category_Leather:Direwolf": "an enormous scarred dark grey pelt",
  "Category_Bone:Wolf": "a slim yellow-white wolf fang",
  "Category_Bone:Boar": "a heavy curved yellowed boar tusk",
  "Category_Bone:Bear": "a thick blunt bear claw of dark horn",
  "Category_Bone:Direwolf": "a long hooked direwolf claw, grey and scarred",
  "Category_Bone:Ancient": "a huge fossilised fang gone stone-brown and cracked with age",
  Critical: "colourless fluid holding one sharp white-hot spark",
  Accuracy: "a thin bright needle of light suspended in the fluid",
  Damage: "churning ember-orange fluid",
  Health: "thick dark red fluid, nearly opaque",
  Armor: "grey metallic sediment settling through colourless fluid",
  Evade: "pale grey vapour swirling in the fluid",
  Barrier: "cold blue light held inside the fluid",
};

/**
 * Which craft a SHAPED upgrade resource serves — a rune, a flux, a seal all have a body that can
 * carry a motif.
 */
const UPGRADE_ITEM_ACCENTS: Record<string, string> = {
  Weapon: "a bladed motif worked into it",
  Armor: "an armor-plate motif worked into it",
  Jewellery: "a faceted-gem motif worked into it",
};

/**
 * Which craft a POWDER came from. A heap has no body to work a motif into, so provenance may only
 * show in the grain: naming plates or blades here makes the model draw whole items sitting in the
 * heap instead of grinding them.
 */
const UPGRADE_POWDER_ACCENTS: Record<string, string> = {
  Weapon: "milled down from weapon steel, faintly metallic in the grain",
  Armor: "milled down from armor plate, faintly metallic in the grain",
  Jewellery: "milled down from gemstone, faintly glittering in the grain",
};

interface UpgradeRole {
  /** Tag that selects this role. */
  readonly tag: string;
  readonly form: string;
  /** Provenance vocabulary this shape can carry. */
  readonly accents: Record<string, string>;
}

/**
 * Upgrade resource role, resolved from the record's tags. Ordered — a flux and a smith rune
 * both carry the Upgrade tag, so the specific tags are tested first.
 */
const UPGRADE_ROLE_FORMS: readonly UpgradeRole[] = [
  { tag: "Flux", form: "a lump of pale crystalline smithing flux, translucent and crusted with slag", accents: UPGRADE_ITEM_ACCENTS },
  { tag: "Mark", form: "a heavy master's seal stamp turned engraved-face forward, dull wax still clinging to it", accents: UPGRADE_ITEM_ACCENTS },
  { tag: "Rune", form: "a small fired clay rune tablet stamped with a maker's glyph", accents: UPGRADE_ITEM_ACCENTS },
  { tag: "Recraft", form: "a single conical heap of fine milled powder standing alone, floury and evenly ground like flour or ash, no lumps, no grit, no shards and no whole pieces anywhere in it, nothing under it and nothing beside it", accents: UPGRADE_POWDER_ACCENTS },
  { tag: "Upgrade", form: "a flat carved rune stone bearing one deep smith's glyph, iron-bound along its edge", accents: UPGRADE_ITEM_ACCENTS },
  { tag: "Ascend", form: "a heavy master's seal stamp turned engraved-face forward", accents: UPGRADE_ITEM_ACCENTS },
];

/**
 * Presentation form of one entity, standing in for the shared body of its group.
 *
 * A group whose members differ only in tint is one icon in N tints at bag size, and at 64 px hue is
 * the weakest cue there is: eight bolts of cloth, five rolls of hide, seven vials all read as the
 * same object. So every member of such a group is given its OWN form - how the thing is presented,
 * what outline it cuts - and tint is left to separate only what is still close. At most one member
 * may keep the shared body, because by then that body belongs to it alone.
 *
 * Keys are tried most specific first: the entity id, then a subgroup key "<group>:<axis>" for
 * members that also vary along a second axis - four grades of one dust for each of three crafts.
 * Groups where a full-chroma colour already does the separating are named in SHARED_BODY_EXEMPTIONS
 * instead; everything else is expected here, and the build says so when it is not.
 *
 * This is where a family is prepared before it is generated. The armor slots repeat one body
 * fifteen times over, so their entries belong in this table exactly as the cloths do.
 */
const SILHOUETTES: Record<string, string> = {
  // Weapons - separated by the build of the thing itself rather than by how it is laid out: one bit
  // against two, the beard of the head, the length and dressing of the haft, the pommel. Three axes,
  // so all three are authored; the sword and dagger pairs each leave their plain member on the body.
  Weapon_Simple_Axe:
    "a short single-bit hatchet standing haft-down, its one wedge head straight along the top and hooking into a deep beard underneath, set on a stubby bare wooden haft that ends flat with no pommel",
  Weapon_Righteous_Wrath:
    "a great double-bitted axe standing haft-down, two mirrored crescent bits sweeping out either side into one broad symmetrical head, a short spike rising from the crown between them, the long haft ending in a heavy round pommel disc",
  Weapon_Silent_Fury:
    "a long-hafted axe standing haft-down, its single bit a narrow deeply sweeping crescent curving down almost to the haft, haft and the back of the head alike bound over in dark rag strips whose loose tails hang free from the butt",
  Weapon_All_Cutting:
    "a straight sword standing blade-up whose blade is impossibly long and no wider than a finger, rising needle-straight out of a round flat disc guard with no crossbar arms at all, the long grip below it closed by a wide open ring pommel",
  Weapon_Bloodthirsty:
    "a heavy single-edged dagger standing point-up, its blade barely longer than the grip beneath it, its spine straight and thick and its belly swelling toward the point like a cleaver, one hooked quillon curling up from the guard",

  // Fabric - eight bolts of one shape. Velvet keeps the group body, so it now reads as its own.
  Crafting_Resource_Coarse_Wool_Fabric:
    "a skein of coarse spun yarn wound into a figure of eight, two loose ends standing out from it",
  Crafting_Resource_Linen_Fabric:
    "a bolt wound on a plain wooden dowel, its free end unrolled and hanging down the front",
  Crafting_Resource_Broadcloth_Fabric:
    "a thick bolt standing upright on its end, the spiral of its rolled layers showing across the top",
  Crafting_Resource_Fine_Linen_Fabric:
    "a neat stack of five folded squares laid one on another, their edges flush",
  Crafting_Resource_Serge_Fabric:
    "a folded length bound crosswise by cord in two places, the cord biting into the cloth",
  Crafting_Resource_Silk_Fabric:
    "a single loose length hanging free and caught in a soft falling S-curve, not rolled and not folded",
  Crafting_Resource_Purple_Fabric:
    "a small torn swatch with a frayed fringe along its lower edge, no roll and no bolt",

  // Leather - five rolls of one shape. Bear keeps the group body.
  Crafting_Resource_Deer_Leather:
    "a whole hide stretched taut inside a square drying frame and laced to it at the corners",
  Crafting_Resource_Wolf_Leather:
    "a whole pelt laid out flat and open, legs spread and tail still attached",
  Crafting_Resource_Wild_Boar_Leather:
    "a stack of cut rectangular pieces laid one on another, their edges left uneven",
  Crafting_Resource_Direwolf_Leather:
    "an enormous pelt thrown over a beam and hanging down in two heavy halves",

  // Gem - one presentation for the whole group, the loose stone; the cut is what separates them.
  Crafting_Resource_Diamond_Gem:
    "a round brilliant-cut stone, crown up, its facets radiating out from a flat table",
  Crafting_Resource_Ruby_Gem: "a cushion-cut stone, a rounded square under a domed crown",
  Crafting_Resource_Emerald_Gem: "a rectangular step-cut stone with bevelled corners",
  Crafting_Resource_Sapphire_Gem: "a bipyramid crystal, a steep spindle pointed at both ends",
  Crafting_Resource_Beryl_Gem: "a long six-sided prism crystal lying on its side, one end snapped off square",
  Crafting_Resource_Garnet_Gem: "a cabochon, domed smoothly on top and flat underneath",
  Crafting_Resource_Malachite_Gem:
    "a flat disc sawn across the stone's banding, concentric rings running over its face",
  Crafting_Resource_Lazurite_Gem: "a rough tumbled nugget with two flat sawn faces cut into it",
  Crafting_Resource_Alexandrite_Gem: "a pear-cut drop, pointed at the top and rounded below",
  Crafting_Resource_Taaffeite_Gem: "a trillion-cut stone, a flat-topped triangle with three clipped points",

  // Essence - seven vials of one shape. Each gets its own vessel; the fluid keeps carrying the tint.
  Crafting_Resource_Essence_Health:
    "a round-bellied bulb flask with a short neck, its mouth sealed under a blob of dark wax",
  Crafting_Resource_Essence_Critical_Chance:
    "a tall narrow bottle cut in hard facets like a gemstone, its stopper ground from the same glass",
  Crafting_Resource_Essence_Damage:
    "a squat wide-mouthed jar closed by a driven cork standing proud of the rim",
  Crafting_Resource_Essence_Armor:
    "a heavy bottle caged in a woven wicker sleeve, only its neck and shoulder left bare",
  Crafting_Resource_Essence_Accuracy:
    "a long slender tube with a pointed base, hung by a cord looped under its collar",
  Crafting_Resource_Essence_Barrier:
    "a spherical orb-flask with no neck at all, a metal band clamped around its middle",
  Crafting_Resource_Essence_Evade: "a flat curved hip-flask of glass, a small stopper set at one corner",

  // Dust - the grade colour separates the four grades, the vessel separates the three crafts.
  // Armor keeps the free heap.
  "Recraft:Weapon":
    "fine milled powder mounded in an open folded-paper packet, the paper turned down at the top so the heap stands above the fold, floury and evenly ground like flour or ash, no lumps, no grit, no shards and no whole pieces anywhere in it",
  "Recraft:Jewellery":
    "fine milled powder filling a shallow round dish level to its rim, floury and evenly ground like flour or ash, no lumps, no grit, no shards and no whole pieces anywhere in it",

  // Smith runes - three of one grade, so the grade colour cannot separate them. Their bodies do.
  Upgrade_Resource_Weapon_Rune:
    "a slim rune spike, square in section and tapering to a point, one glyph cut into its flattened head",
  Upgrade_Resource_Blacksmith_Rune:
    "a round rune boss, a domed disc with a raised rim and one glyph struck deep into its centre",
  Upgrade_Resource_Jeweler_Rune:
    "a pierced rune bead, a flattened ring with a hole through its middle and one glyph chased around the hole",

  // Master's marks - likewise all of one grade.
  Upgrade_Resource_Weaponsmith_Mark:
    "a heavy stamp seal with a short thick grip behind it, its engraved face turned forward and cut in the shape of a broad blade tip, standing in empty space with nothing under it",
  Upgrade_Resource_Armorsmith_Mark:
    "a heavy square seal block without a handle, engraved face forward, dull wax still clinging along its edges",
  Upgrade_Resource_Jeweler_Mark: "a small round signet worn on a finger ring, the engraved face turned forward",
};

/** How many members of one group may share a body before the build calls the sharing out. */
const SHARED_BODY_LIMIT = 2;

/**
 * Groups allowed to share one body, keyed "<family>/<group>". Ore and the smithing ladders are one
 * object in several materials or at several grades, and the canon hands those a saturated colour of
 * their own to be told apart by; the bone tints already name each trophy's own shape.
 */
const SHARED_BODY_EXEMPTIONS: ReadonlySet<string> = new Set([
  "crafting_resource/Category_Metal",
  "crafting_resource/Category_Bone",
  "upgrade_resource/Recraft",
  "upgrade_resource/Flux",
  "upgrade_resource/Rune",
]);

// ============================================================================
// Subject assembly helpers
// ============================================================================

/** Words that mark a description as a rules sentence — those describe numbers, not pictures. */
const MECHANICAL_DESCRIPTION_MARKERS: readonly RegExp[] = [
  /\bincreases?\b/i,
  /\breduces?\b/i,
  /\bguarantees?\b/i,
  /\bdeals?\b/i,
  /\bgrants?\b/i,
  /\bapplies\b/i,
  /\brestores?\b/i,
  /\bconsumes?\b/i,
  /\bturns?\b/i,
  /%/,
  /\d/,
];

function joinSubject(name: string | null, parts: (string | null | undefined)[]): string {
  const body = parts.filter((p): p is string => typeof p === "string" && p.length > 0).join(", ");
  const lead = name === null ? "The icon shows" : `The icon shows ${name} —`;
  return `${lead} ${body}.`;
}

function firstMotif(id: string, table: readonly NameMotif[]): NameMotif | null {
  return table.find((entry) => entry.match.test(id)) ?? null;
}

function lookup<T>(table: Record<string, T>, key: string | undefined | null): T | null {
  if (key === undefined || key === null) return null;
  return Object.prototype.hasOwnProperty.call(table, key) ? table[key] : null;
}

/** Folds a standalone sentence into the middle of the subject clause. */
function asClause(sentence: string): string {
  const trimmed = sentence.trim().replace(/[.!?]+$/, "");
  return trimmed.charAt(0).toLowerCase() + trimmed.slice(1);
}

/**
 * The description's opening sentence when it paints a picture, used where no authored motif
 * covers the entity. Rules text ("Increases the chance by 3%") is not a picture and is refused.
 */
function visualDescription(id: string, ctx: BuildContext): string | null {
  const description = ctx.localization.description(id);
  if (description === null) return null;
  const sentence = description.split(/(?<=[.!?])\s/)[0]?.trim();
  if (!sentence || sentence.length === 0) return null;
  if (MECHANICAL_DESCRIPTION_MARKERS.some((marker) => marker.test(sentence))) return null;
  return asClause(sentence);
}

function baseStatAccents(family: Family, facts: Record<string, unknown>, issues: IssueLog, id: string): string | null {
  const table = lookup(STAT_ACCENTS as Record<string, Record<string, string>>, family);
  if (table === null) {
    issues.add("warn", id, `family "${family}" has no stat accent table`);
    return null;
  }
  const stats = Array.isArray(facts.baseStats) ? facts.baseStats : [];
  const accents: string[] = [];
  for (const stat of stats) {
    const parameter = (stat as { parameter?: string }).parameter;
    const accent = lookup(table, parameter);
    if (accent === null) {
      issues.add("warn", id, `base stat "${parameter}" has no ${family} accent entry`);
      continue;
    }
    accents.push(accent);
  }
  return accents.length > 0 ? accents.join(" and ") : null;
}

/** What an entity's body is being resolved from. */
interface BodyRequest {
  family: Family;
  id: string;
  /** Key of the group whose members share one form: the material category, the slot, the role. */
  group: string;
  /** That shared form, used when nothing more specific is authored. */
  shared: string | null;
  /** Second-axis key tried after the id, e.g. "Recraft:Weapon". */
  subgroup?: string;
}

/**
 * The entity's own body: its authored silhouette when it has one, its group's shared form
 * otherwise. Every fallback is recorded, so a family generated before its silhouettes were written
 * is named at the end of the build instead of quietly shipping N copies of one shape.
 */
function bodyOf(ctx: BuildContext, request: BodyRequest): string | null {
  const authored = lookup(SILHOUETTES, request.id) ?? lookup(SILHOUETTES, request.subgroup);
  if (authored !== null) return authored;
  const key = `${request.family}/${request.group}`;
  ctx.sharedBodies.set(key, [...(ctx.sharedBodies.get(key) ?? []), request.id]);
  return request.shared;
}

/** Names every group that leant on one body for more members than the rule allows. */
function reportSharedBodies(ctx: BuildContext): void {
  for (const [key, ids] of ctx.sharedBodies) {
    if (ids.length <= SHARED_BODY_LIMIT || SHARED_BODY_EXEMPTIONS.has(key)) continue;
    ctx.issues.add("warn", key, `${ids.length} entities share one body with no silhouette entry: ${ids.join(", ")}`);
  }
}

function rarityTreatment(
  table: Record<string, string>,
  facts: Record<string, unknown>,
  issues: IssueLog,
  id: string,
): string | null {
  const rarity = facts.rarity as string | undefined;
  const treatment = lookup(table, rarity);
  if (treatment === null) issues.add("warn", id, `rarity "${rarity}" has no treatment entry`);
  return treatment;
}

// ============================================================================
// Data loading
// ============================================================================

interface BuildContext {
  dataRoot: URL;
  localization: Localization;
  issues: IssueLog;
  /** Ids that fell back to a shared body, keyed "<family>/<group>". */
  sharedBodies: Map<string, string[]>;
}

/** Directory path (absolute, either slash style) as a trailing-slash file URL. */
function toDirectoryUrl(path: string): URL {
  const normalized = `${path.replace(/\\/g, "/").replace(/\/+$/, "")}/`;
  return new URL(normalized.startsWith("/") ? `file://${normalized}` : `file:///${normalized}`);
}

function resolve(dataRoot: URL, relative: string): string {
  return new URL(relative, dataRoot).pathname.replace(/^\/([A-Za-z]:)/, "$1");
}

async function readJsonFilesOf(ctx: BuildContext, relativeDir: string): Promise<{ file: string; data: unknown }[]> {
  const dir = resolve(ctx.dataRoot, `${relativeDir}/`);
  const results: { file: string; data: unknown }[] = [];
  try {
    for await (const entry of Deno.readDir(dir)) {
      if (!entry.isFile || !entry.name.endsWith(".json")) continue;
      const path = `${dir}/${entry.name}`;
      try {
        results.push({ file: entry.name, data: JSON.parse(await Deno.readTextFile(path)) });
      } catch (error) {
        ctx.issues.add("error", `${relativeDir}/${entry.name}`, `unreadable or invalid json: ${describe(error)}`);
      }
    }
  } catch (error) {
    ctx.issues.add("error", relativeDir, `directory not readable: ${describe(error)}`);
  }
  return results.sort((a, b) => a.file.localeCompare(b.file));
}

function describe(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

function recordsOf(data: unknown, key: string, where: string, issues: IssueLog): Record<string, unknown>[] {
  const list = (data as Record<string, unknown> | null)?.[key];
  if (list === undefined) return [];
  if (!Array.isArray(list)) {
    issues.add("error", where, `"${key}" is not an array`);
    return [];
  }
  return list.filter((item): item is Record<string, unknown> => {
    if (item && typeof item === "object" && typeof (item as { id?: unknown }).id === "string") return true;
    issues.add("error", where, `entry in "${key}" has no string id`);
    return false;
  });
}

async function loadAbilities(ctx: BuildContext): Promise<Entity[]> {
  const files = await readJsonFilesOf(ctx, DATA_PATHS.abilities);
  const entities: Entity[] = [];
  for (const { file, data } of files) {
    for (const record of recordsOf(data, "abilities", `${DATA_PATHS.abilities}/${file}`, ctx.issues)) {
      entities.push({ id: record.id as string, facts: record });
    }
  }
  return entities;
}

const EQUIP_CATEGORY_BY_PART: Record<string, Family> = {
  Weapon: "weapon",
  Body: "armor",
  Cloak: "armor",
  Gloves: "armor",
  Boots: "armor",
  Helmet: "armor",
  Amulet: "jewellery",
  Belt: "jewellery",
  Ring: "jewellery",
};

async function loadEquipItems(ctx: BuildContext, family: Family): Promise<Entity[]> {
  const files = await readJsonFilesOf(ctx, DATA_PATHS.equipItems);
  const entities: Entity[] = [];
  for (const { file, data } of files) {
    for (const record of recordsOf(data, "items", `${DATA_PATHS.equipItems}/${file}`, ctx.issues)) {
      const id = record.id as string;
      const part = record.equipmentPart as string | undefined;
      const mapped = lookup(EQUIP_CATEGORY_BY_PART, part);
      if (mapped === null) {
        ctx.issues.add("error", id, `equipmentPart "${part}" maps to no family`);
        continue;
      }
      if (mapped !== family) continue;
      entities.push({ id, facts: record });
    }
  }
  return entities;
}

async function loadResources(ctx: BuildContext, key: string): Promise<Entity[]> {
  const files = await readJsonFilesOf(ctx, DATA_PATHS.resources);
  const entities: Entity[] = [];
  for (const { file, data } of files) {
    for (const record of recordsOf(data, key, `${DATA_PATHS.resources}/${file}`, ctx.issues)) {
      entities.push({ id: record.id as string, facts: record });
    }
  }
  return entities;
}

/**
 * The three stances are a closed set fixed by the game's own stance enum. They are named here
 * rather than read off the icon folder: deriving the roster from art would let a deleted png
 * silently shrink the family to nothing.
 */
const STANCE_IDS: readonly string[] = ["Stance_Dexterity", "Stance_Intelligence", "Stance_Strength"];

function loadStances(_ctx: BuildContext): Promise<Entity[]> {
  return Promise.resolve(STANCE_IDS.map((id) => ({ id, facts: {} })));
}

// ============================================================================
// Family table — a new family is one entry here plus its vocabulary above.
// ============================================================================

const FAMILIES: readonly FamilySpec[] = [
  {
    family: "ability",
    expected: 25,
    load: loadAbilities,
    subject: (entity, ctx) => {
      const name = ctx.localization.name(entity.id, ctx.issues);
      const authored = lookup(ABILITY_MOTIFS, entity.id);
      const motif = authored ?? visualDescription(entity.id, ctx);
      if (authored === null) {
        ctx.issues.add("warn", entity.id, motif === null
          ? "no motif entry and the description is rules text, not a picture"
          : "no motif entry — fell back to the localized description");
      }
      return joinSubject(name, [motif, "no character, no hands, only the effect itself as one object"]);
    },
  },
  {
    family: "weapon",
    expected: 8,
    load: (ctx) => loadEquipItems(ctx, "weapon"),
    subject: (entity, ctx) => {
      const name = ctx.localization.name(entity.id, ctx.issues);
      const type = entity.facts.weaponType as string | undefined;
      const form = lookup(WEAPON_TYPE_FORMS, type);
      if (form === null) ctx.issues.add("warn", entity.id, `weaponType "${type}" has no form entry`);
      const named = firstMotif(entity.id, EQUIP_NAME_MOTIFS);
      const motif = named?.motif ?? visualDescription(entity.id, ctx);
      if (motif === null) ctx.issues.add("warn", entity.id, "no name motif matched and no visual description available");
      return joinSubject(name, [
        bodyOf(ctx, {
          family: "weapon",
          id: entity.id,
          group: type ?? "unknown",
          shared: form ?? "a hand weapon shown alone",
        }),
        lookup(HANDEDNESS_FORMS, entity.facts.handedness as string | undefined),
        motif,
        rarityTreatment(RARITY_TREATMENTS, entity.facts, ctx.issues, entity.id),
        WEAPON_PRESENTATION_NOTE,
      ]);
    },
  },
  ...(["armor", "jewellery"] as const).map<FamilySpec>((family) => ({
    family,
    expected: family === "armor" ? 57 : 40,
    load: (ctx: BuildContext) => loadEquipItems(ctx, family),
    subject: (entity: Entity, ctx: BuildContext) => {
      const name = ctx.localization.name(entity.id, ctx.issues);
      const part = entity.facts.equipmentPart as string | undefined;
      const form = lookup(SLOT_FORMS, part);
      if (form === null) ctx.issues.add("warn", entity.id, `equipmentPart "${part}" has no slot form entry`);
      const named = firstMotif(entity.id, EQUIP_NAME_MOTIFS);
      const motif = named?.motif ?? visualDescription(entity.id, ctx);
      if (motif === null) ctx.issues.add("warn", entity.id, "no name motif matched and no visual description available");
      return joinSubject(name, [
        bodyOf(ctx, {
          family,
          id: entity.id,
          group: part ?? "unknown",
          shared: form ?? "a single piece of gear shown alone",
        }),
        motif,
        named?.dropStatAccents === true ? null : baseStatAccents(family, entity.facts, ctx.issues, entity.id),
        rarityTreatment(RARITY_TREATMENTS, entity.facts, ctx.issues, entity.id),
      ]);
    },
  })),
  {
    family: "crafting_resource",
    expected: 41,
    canonClause: CANON_MATERIAL_COLOR_CLAUSE,
    load: (ctx) => loadResources(ctx, "craftingResources"),
    subject: (entity, ctx) => {
      const name = ctx.localization.name(entity.id, ctx.issues);
      const categoryId = (entity.facts.material as { categoryId?: string } | undefined)?.categoryId;
      const form = lookup(MATERIAL_CATEGORY_FORMS, categoryId);
      if (form === null) ctx.issues.add("warn", entity.id, `material category "${categoryId}" has no form entry`);
      const tags = Array.isArray(entity.facts.tags) ? (entity.facts.tags as string[]) : [];
      const tag = tags.at(-1);
      const tint = lookup(MATERIAL_TINTS, `${categoryId}:${tag}`) ?? lookup(MATERIAL_TINTS, tag);
      if (tint === null) ctx.issues.add("warn", entity.id, `material tag "${tag}" has no tint entry`);
      return joinSubject(name, [
        bodyOf(ctx, {
          family: "crafting_resource",
          id: entity.id,
          group: categoryId ?? "unknown",
          shared: form ?? "a single raw crafting material",
        }),
        tint,
        rarityTreatment(RESOURCE_RARITY_TREATMENTS, entity.facts, ctx.issues, entity.id),
        lookup(MATERIAL_PRESENTATION_NOTES, categoryId) ?? "a plain material, not a finished item",
      ]);
    },
  },
  {
    family: "upgrade_resource",
    expected: 26,
    canonClause: CANON_RARITY_COLOR_CLAUSE,
    load: (ctx) => loadResources(ctx, "upgradeResources"),
    subject: (entity, ctx) => {
      const name = ctx.localization.name(entity.id, ctx.issues);
      const tags = Array.isArray(entity.facts.tags) ? (entity.facts.tags as string[]) : [];
      const role = UPGRADE_ROLE_FORMS.find((entry) => tags.includes(entry.tag)) ?? null;
      if (role === null) ctx.issues.add("warn", entity.id, `tags [${tags.join(",")}] match no role entry`);
      const category = entity.facts.category as string | undefined;
      return joinSubject(name, [
        role === null ? "a single smithing consumable" : bodyOf(ctx, {
          family: "upgrade_resource",
          id: entity.id,
          group: role.tag,
          shared: role.form,
          subgroup: category === undefined ? undefined : `${role.tag}:${category}`,
        }),
        role === null ? null : lookup(role.accents, category),
        visualDescription(entity.id, ctx),
        rarityTreatment(UPGRADE_RARITY_TREATMENTS, entity.facts, ctx.issues, entity.id),
      ]);
    },
  },
  {
    family: "stance",
    expected: 3,
    load: loadStances,
    subject: (entity, ctx) => {
      const name = ctx.localization.name(entity.id, ctx.issues);
      const authored = lookup(STANCE_MOTIFS, entity.id);
      const motif = authored ?? visualDescription(entity.id, ctx);
      if (authored === null) {
        ctx.issues.add("warn", entity.id, "no stance motif entry — fell back to the localized description");
      }
      return joinSubject(name, [motif, STANCE_PRESENTATION_NOTE]);
    },
  },
];

// ============================================================================
// Build
// ============================================================================

interface FamilyCoverage {
  family: Family;
  expected: number;
  actual: number;
}

async function build(dataRoot: URL): Promise<{ corpus: CorpusEntry[]; coverage: FamilyCoverage[]; issues: IssueLog }> {
  const issues = new IssueLog();
  let poText = "";
  try {
    poText = await Deno.readTextFile(resolve(dataRoot, DATA_PATHS.localization));
  } catch (error) {
    issues.add("error", DATA_PATHS.localization, `not readable: ${describe(error)}`);
  }
  const ctx: BuildContext = {
    dataRoot,
    localization: Localization.parse(poText, issues),
    issues,
    sharedBodies: new Map(),
  };

  const corpus: CorpusEntry[] = [];
  const coverage: FamilyCoverage[] = [];
  const seen = new Set<string>();

  for (const spec of FAMILIES) {
    const entities = await spec.load(ctx);
    for (const entity of entities) {
      if (seen.has(entity.id)) {
        issues.add("error", entity.id, `duplicate id, already emitted for another family`);
        continue;
      }
      seen.add(entity.id);
      const prompt = [`${CANON_PROMPT}.`, spec.canonClause, spec.subject(entity, ctx)]
        .filter((part): part is string => typeof part === "string" && part.length > 0)
        .join(" ");
      corpus.push({ id: entity.id, family: spec.family, prompt });
    }
    coverage.push({ family: spec.family, expected: spec.expected, actual: entities.length });
  }

  reportSharedBodies(ctx);
  return { corpus, coverage, issues };
}

// ============================================================================
// CLI
// ============================================================================

function arg(name: string, fallback?: string): string {
  const i = Deno.args.indexOf(`--${name}`);
  if (i >= 0 && i + 1 < Deno.args.length) return Deno.args[i + 1];
  if (fallback !== undefined) return fallback;
  console.error(`Missing --${name}`);
  Deno.exit(1);
}

if (import.meta.main) {
  const output = arg("output");
  const dataRootArg = Deno.args.indexOf("--data-root");
  const dataRoot = dataRootArg >= 0 ? toDirectoryUrl(Deno.args[dataRootArg + 1]) : DEFAULT_DATA_ROOT;
  const quiet = Deno.args.includes("--quiet");

  const { corpus, coverage, issues } = await build(dataRoot);
  await Deno.writeTextFile(output, `${JSON.stringify(corpus, null, 2)}\n`);

  if (!quiet) {
    console.log(`corpus: ${corpus.length} prompts -> ${output}`);
    console.log("family              expected  actual");
    for (const row of coverage) {
      const flag = row.expected === row.actual ? "" : "   <-- MISMATCH";
      console.log(`${row.family.padEnd(20)}${String(row.expected).padStart(8)}${String(row.actual).padStart(8)}${flag}`);
    }
    const total = coverage.reduce((sum, row) => sum + row.actual, 0);
    console.log(`${"TOTAL".padEnd(20)}${String(coverage.reduce((s, r) => s + r.expected, 0)).padStart(8)}${String(total).padStart(8)}`);
  }

  for (const issue of issues.all) console.error(`[${issue.severity}] ${issue.where}: ${issue.message}`);
  console.error(`issues: ${issues.count("error")} error(s), ${issues.count("warn")} warning(s)`);
  if (issues.count("error") > 0) Deno.exit(1);
}
