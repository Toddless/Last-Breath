namespace LastBreath.Npc
{
    using System.Collections.Generic;
    using Core;
    using Core.Entity.Silhouette;
    using Godot;

    /// <summary>
    /// Fits the scene's humanoid-authored collision shapes to the art ApplyVisual just put on the
    /// sprite: the used rect of the first Idle_Down frame drives the proportions (the math and the
    /// pinned humanoid reference live in <see cref="NpcSilhouetteCollisionCalculator"/>). Every
    /// touched CollisionShape2D gets a FRESH shape instance — the scene's sub_resource shapes are
    /// shared by all instances of the scene, and resizing one in place would resize every NPC.
    /// A node this expects but cannot find is reported and skipped, never a crash.
    /// V1 limitation: one profile per NPC, measured on the down facing — side frames are wider.
    ///
    /// Known limitations beyond that:
    /// - The fitter (and the calculator's authored numbers) assume the TestNpc.tscn layout. A scene
    ///   built differently — Necro.tscn, say: circle interaction r100, capsule body r25/h90, no
    ///   DialogueActor — would, should its NPC ever gain a visual config, get its body refitted
    ///   with TestNpc's proportions while the unmatched nodes spam the Tracker. Today that path is
    ///   dead (Necro is never instantiated; spawns go through TestNpc), but reviving such a scene
    ///   needs its own calculator preset.
    /// - The shapes' X offset is not mirrored when the sprite flips horizontally (Idle_Right via
    ///   FlipH) — a V1 limitation; today's authored X offsets are 0, so nothing visibly drifts.
    /// </summary>
    internal static class NpcCollisionFitter
    {
        private const string ProfileClip = "Idle_Down";

        /// <summary>Texture2D.GetImage is a full texture decode — measured once per SpriteFrames
        /// resource (the visual library shares one resource per NPC kind).</summary>
        private static readonly Dictionary<ulong, SilhouetteRect> s_silhouettes = new();

        public static void Fit(CharacterBody2D npc, Area2D? interactionArea)
        {
            if (FindChildOfType<AnimatedSprite2D>(npc) is not { } sprite)
            {
                ReportMissing("AnimatedSprite2D of npc", npc);
                return;
            }

            if (!TryMeasure(sprite.SpriteFrames, out var silhouette) || !silhouette.IsUsable)
            {
                ReportMissing($"Measurable '{ProfileClip}' silhouette of npc visual", npc);
                return; // scene shapes stay as authored
            }

            var profile = NpcSilhouetteCollisionCalculator.Main.Calculate(
                silhouette, Mathf.Abs(sprite.Scale.X), Mathf.Abs(sprite.Scale.Y));
            FitBody(npc, profile);
            FitInteraction(npc, interactionArea, profile);
            FitDialogue(npc, profile);
        }

        private static void FitBody(CharacterBody2D npc, in NpcCollisionProfile profile)
        {
            if (profile.BodyCapsule is not { } capsule) return;
            if (FindShapeNode<CapsuleShape2D>(npc) is not { } node)
            {
                ReportMissing("Body CollisionShape2D (capsule) of npc", npc);
                return;
            }

            Place(node, npc, capsule.CenterX, capsule.CenterY);
            node.Shape = new CapsuleShape2D { Radius = capsule.Radius, Height = capsule.Height };
        }

        private static void FitInteraction(CharacterBody2D npc, Area2D? interactionArea, in NpcCollisionProfile profile)
        {
            if (interactionArea == null || FindShapeNode<CapsuleShape2D>(interactionArea) is not { } node)
            {
                ReportMissing("Interaction area CollisionShape2D (capsule) of npc", npc);
                return;
            }

            Place(node, npc, profile.Interaction.CenterX, profile.Interaction.CenterY);
            node.Shape = new CapsuleShape2D { Radius = profile.Interaction.Radius, Height = profile.Interaction.Height };
        }

        private static void FitDialogue(CharacterBody2D npc, in NpcCollisionProfile profile)
        {
            if (profile.Dialogue is not { } circle) return;
            if (FindChildOfType<DialogueActor>(npc) is not { } actor || FindShapeNode<CircleShape2D>(actor) is not { } node)
            {
                ReportMissing("DialogueActor CollisionShape2D (circle) of npc", npc);
                return;
            }

            Place(node, npc, circle.CenterX, circle.CenterY);
            node.Shape = new CircleShape2D { Radius = circle.Radius };
        }

        /// <summary>The profile speaks NPC-local coordinates; a shape node one level down (inside
        /// an Area2D) compensates for its parent's offset so the shape lands where computed.</summary>
        private static void Place(CollisionShape2D node, CharacterBody2D npc, float centerX, float centerY)
        {
            var parentOffset = node.GetParent() is Node2D parent && parent != npc ? parent.Position : Vector2.Zero;
            node.Position = new Vector2(centerX, centerY) - parentOffset;
        }

        private static bool TryMeasure(SpriteFrames? frames, out SilhouetteRect silhouette)
        {
            silhouette = default;
            if (frames == null) return false;

            ulong key = frames.GetInstanceId();
            if (!s_silhouettes.TryGetValue(key, out silhouette))
            {
                silhouette = Measure(frames);
                s_silhouettes[key] = silhouette; // failures cached too — the texture will not decode better next time
            }

            return true;
        }

        private static SilhouetteRect Measure(SpriteFrames frames)
        {
            if (SelectClip(frames) is not { } clip) return default;
            if (frames.GetFrameTexture(clip, 0) is not { } texture) return default;
            if (ExtractFrameImage(texture) is not { } image) return default;

            var used = image.GetUsedRect();
            return new SilhouetteRect(used.Position.X, used.Position.Y, used.Size.X, used.Size.Y);
        }

        /// <summary>The profile is taken from Idle_Down (V1); art without it degrades to any other
        /// Idle_* facing first — a standing silhouette — and only then to whatever clip exists
        /// (a set whose first clip is "Dead" must not fit a lying silhouette onto a standing NPC).</summary>
        private static string? SelectClip(SpriteFrames frames)
        {
            if (frames.HasAnimation(ProfileClip) && frames.GetFrameCount(ProfileClip) > 0) return ProfileClip;

            string? fallback = null;
            foreach (string name in frames.GetAnimationNames())
            {
                if (frames.GetFrameCount(name) == 0) continue;
                if (name.StartsWith("Idle_", System.StringComparison.Ordinal)) return name;
                fallback ??= name;
            }

            return fallback;
        }

        /// <summary>The frame's pixels as an Image: an atlas frame is cut out of its sheet (the
        /// used rect must describe THIS frame, not the whole sheet's bounding box).</summary>
        private static Image? ExtractFrameImage(Texture2D texture)
        {
            if (texture is AtlasTexture { Atlas: { } atlas } atlasTexture)
            {
                var sheet = atlas.GetImage();
                if (sheet == null) return null;
                if (sheet.IsCompressed() && sheet.Decompress() != Error.Ok) return null;
                var region = new Rect2I((Vector2I)atlasTexture.Region.Position, (Vector2I)atlasTexture.Region.Size);
                return sheet.GetRegion(region);
            }

            var image = texture.GetImage();
            if (image == null) return null;
            if (image.IsCompressed() && image.Decompress() != Error.Ok) return null;
            return image;
        }

        private static CollisionShape2D? FindShapeNode<TShape>(Node parent) where TShape : Shape2D
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is CollisionShape2D { Shape: TShape } node) return node;
            }

            return null;
        }

        private static T? FindChildOfType<T>(Node parent) where T : Node
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is T match) return match;
            }

            return null;
        }

        private static void ReportMissing(string what, Node npc)
        {
            Tracker.TrackNotFound($"{what} '{npc.Name}' (collision left as authored)", npc);
            GD.Print($"[NpcCollisionFitter] {what} '{npc.Name}' not found - collision left as authored");
        }
    }
}
