namespace PassiveTreeEditor.Source.Editing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.PassiveTree;
    using History;

    /// <summary>
    /// The one door every change to the tree goes through, and the place those changes are written down
    /// so they can be taken back. Panels ask it to change the document instead of changing the document
    /// themselves: an edit that found another way in would be an edit the stack never heard of, and the
    /// next undo would then restore a tree the author never had — the single failure this whole layer
    /// exists to avoid.
    /// <para>Plain C# on purpose. Undo is the part of the tool that cannot be checked by looking at the
    /// screen — a wrong step back looks exactly like a right one — so it lives where it can be read,
    /// and later tested, without a running engine.</para>
    /// </summary>
    public sealed class TreeEditor
    {
        private readonly EditHistory _history = new();

        private PassiveTreeDocument _document = new();

        /// <summary>
        /// A step through the history is being applied and the panels are being rebuilt from the
        /// document. Nothing they say back during that is an edit: a control torn down mid-rebuild
        /// hands over whatever it was still holding — a spin box the digits typed into it, a field the
        /// focus it is losing — and those arrive here looking exactly like the author's own work.
        /// Every way in is shut for the length of the step, so a stale control cannot put back what the
        /// step just took away and leave the author watching undo apparently do nothing.
        /// </summary>
        private bool _restoring;

        /// <summary>A step through the history has landed: everything on screen has to be rebuilt from
        /// the document, because every panel holds values copied out of it. The step says what it was
        /// and whether it moved a node to another id — the one change the panels cannot work out for
        /// themselves, since the node they were showing is now under a name they never heard.</summary>
        public event Action<HistoryStep>? Restored;

        public EditHistory History => _history;

        public PassiveTreeDocument Document => _document;

        /// <summary>
        /// Another tree became the one being edited. The stack is dropped rather than carried over:
        /// its commands hold nodes of the previous document, and a step back into a tree that is no
        /// longer open is the one thing undo must never do.
        /// </summary>
        public void SetDocument(PassiveTreeDocument document)
        {
            _document = document;
            _history.Clear();
        }

        public void Seal() => _history.Seal();

        public string? Undo() => Step(_history.Undo, undoing: true);

        public string? Redo() => Step(_history.Redo, undoing: false);

        // ── structure ──────────────────────────────────────────────────────────────────────────

        /// <summary>Adds a node and, when the author was continuing a chain, the edge to the node they
        /// came from. Both together: one click made them, so one step back has to unmake them.</summary>
        public bool CreateNode(PassiveNode node, PassiveNode? linkTo)
        {
            if (_restoring) return false;
            if (!_document.AddNode(node)) return false;

            List<NodeLink> links = [];
            if (linkTo is not null && _document.Link(linkTo.Id, node.Id))
                links.Add(NodeLink.Between(linkTo.Id, node.Id));

            Record(new NodeSetEdit(_document, [node], links, creates: true, $"add {node.Id}"));
            return true;
        }

        /// <summary>
        /// Deletes nodes together with every edge that touched them. The edges are read off the
        /// document before anything is removed — deleting a node drops its edges silently, and edges to
        /// nodes that survive would otherwise have nowhere to come back from.
        /// </summary>
        public int DeleteNodes(IReadOnlyCollection<string> ids)
        {
            if (_restoring) return 0;

            List<PassiveNode> nodes = [];
            HashSet<NodeLink> links = [];

            foreach (string id in ids)
            {
                PassiveNode? node = _document.Find(id);
                if (node is null) continue;

                nodes.Add(node);
                foreach (string neighbour in _document.Neighbours(id)) links.Add(NodeLink.Between(id, neighbour));
            }

            if (nodes.Count == 0) return 0;

            var edit = new NodeSetEdit(_document, nodes, [.. links], creates: false, $"delete {nodes.Count} node(s)");
            edit.Redo();
            Record(edit);
            return nodes.Count;
        }

        public bool SetLink(PassiveNode first, PassiveNode second, bool linked)
        {
            if (_restoring) return false;

            bool changed = linked
                ? _document.Link(first.Id, second.Id)
                : _document.Unlink(first.Id, second.Id);

            if (!changed) return false;

            string verb = linked ? "link" : "unlink";
            Record(new LinkEdit(_document, first, second, linked, $"{verb} {first.Id} — {second.Id}"));
            return true;
        }

        /// <summary>
        /// Moves a node to another id. Renaming a node to the name it already has is not an edit and is
        /// not recorded — the document answers such a call with success, so the guard belongs here, on
        /// the door itself, rather than in whichever field happens to be calling.
        /// </summary>
        public bool Rename(PassiveNode node, string newId)
        {
            if (_restoring || string.Equals(node.Id, newId, StringComparison.Ordinal)) return false;

            string oldId = node.Id;
            if (!_document.Rename(oldId, newId)) return false;

            Record(new RenameEdit(_document, oldId, newId));
            return true;
        }

        /// <summary>Files a drag that already happened. The nodes had to follow the cursor to be a drag
        /// at all, so what the stack is given is where each of them started.</summary>
        public void MoveNodes(IReadOnlyList<NodeMove> moves)
        {
            if (_restoring || moves.Count == 0) return;

            string label = moves.Count == 1 ? $"move {moves[0].Node.Id}" : $"move {moves.Count} node(s)";
            Record(new MoveEdit(_document, moves, label));
        }

        // ── fields ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// One field of one node, set and written down. The caller hands over the value the field held
        /// as well as the value it is taking, so the step back is a write and never a calculation.
        /// A change to the value it already had is not an edit and is not recorded.
        /// </summary>
        public void SetNodeValue<T>(PassiveNode node, string field, T from, T to, Action<T> apply)
        {
            if (_restoring || EqualityComparer<T>.Default.Equals(from, to)) return;

            apply(to);
            Record(new ValueEdit<T>(new EditTarget(node, field), from, to, apply, $"{node.Id}: {field}"));
        }

        /// <summary>The tree's point budget — a field of the document rather than of a node, and the
        /// one such field the file carries.</summary>
        public void SetBudget(int budget)
        {
            if (_restoring) return;

            PassiveTreeDocument document = _document;
            int from = document.Budget;
            if (from == budget) return;

            document.Budget = budget;
            Record(new ValueEdit<int>(new EditTarget(document, EditFields.Budget), from, budget,
                value => document.Budget = value, $"{EditFields.Budget} {budget}"));
        }

        // ── passive ────────────────────────────────────────────────────────────────────────────

        /// <summary>The passive the node grants instead of lines. Refused while the node carries any: the
        /// two channels are alternatives, and a node written both ways is dropped whole when it is read.
        /// False is the door saying no, so the field showing the id can put back what the node still says.</summary>
        public bool SetPassiveId(PassiveNode node, string? id)
        {
            string? wanted = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
            if (_restoring || PassiveNode.WhyChannelsCollide(wanted, node.HasLines) is not null) return false;

            SetNodeValue(node, EditFields.Passive, node.PassiveId, wanted, value => node.PassiveId = value);
            return true;
        }

        /// <summary>
        /// Rewrites the node's named numbers from an ordered list. Whole rather than key by key: the order
        /// the author wrote them in is the order the file keeps, and a dictionary hands a fresh key the
        /// slot a removed one left behind — patching would move a renamed field to wherever that hole is.
        /// <para><paramref name="field"/> is what a run of edits merges on, so a name being typed and a
        /// number being stepped stay separate steps.</para>
        /// </summary>
        public void SetProperties(PassiveNode node, IReadOnlyList<KeyValuePair<string, float>> rows, string field)
        {
            if (_restoring) return;

            List<KeyValuePair<string, float>> before = node.PropertyRows();
            List<KeyValuePair<string, float>> after = [.. rows];
            if (before.SequenceEqual(after)) return;

            node.SetProperties(after);
            Record(new ValueEdit<List<KeyValuePair<string, float>>>(new EditTarget(node, field), before, after,
                node.SetProperties, $"{node.Id}: {field}"));
        }

        // ── modifier lines ─────────────────────────────────────────────────────────────────────

        /// <summary>Refused on a node that grants a passive, for the same reason the passive is refused on
        /// a node carrying lines — one node, one channel.</summary>
        public void AddModifierLine(PassiveNode node)
        {
            if (_restoring || node.IsPassive) return;

            var line = new ModifierLine();
            node.Modifiers.Add(line);
            Record(new LineListEdit<ModifierLine>(node.Modifiers, node.Modifiers.Count - 1, line,
                adds: true, $"{node.Id}: + {EditFields.Line}"));
        }

        public void AddContextLine(PassiveNode node)
        {
            if (_restoring || node.IsPassive) return;

            var line = new ContextModifierLine();
            node.ContextModifiers.Add(line);
            Record(new LineListEdit<ContextModifierLine>(node.ContextModifiers, node.ContextModifiers.Count - 1, line,
                adds: true, $"{node.Id}: + {EditFields.Context}"));
        }

        public void RemoveModifierLine(PassiveNode node, int index)
        {
            if (_restoring) return;

            ModifierLine line = node.Modifiers[index];
            node.Modifiers.RemoveAt(index);
            Record(new LineListEdit<ModifierLine>(node.Modifiers, index, line,
                adds: false, $"{node.Id}: − {EditFields.Line}"));
        }

        public void RemoveContextLine(PassiveNode node, int index)
        {
            if (_restoring) return;

            ContextModifierLine line = node.ContextModifiers[index];
            node.ContextModifiers.RemoveAt(index);
            Record(new LineListEdit<ContextModifierLine>(node.ContextModifiers, index, line,
                adds: false, $"{node.Id}: − {EditFields.Context}"));
        }

        /// <summary>
        /// Runs an edit of a parametric line between two snapshots of it. The caller says what to
        /// change and the whole line is captured either side of it, so a change that turns out to touch
        /// more than one field is still reversed in full. A pick that lands on what the line already
        /// said is not an edit and is not recorded — the same rule the single fields keep, and the one
        /// that stops a picker firing on rebuild from filling the stack with steps that do nothing.
        /// </summary>
        public void EditLine(PassiveNode node, ModifierLine line, string field, Action edit)
        {
            if (_restoring) return;

            ModifierLine before = line.Copy();
            edit();

            ModifierLine after = line.Copy();
            if (LineSnapshots.Same(before, after)) return;

            Record(new LineStateEdit<ModifierLine>(line, before, after, LineSnapshots.Assign,
                new EditTarget(line, field), $"{node.Id}: {EditFields.Line} {field}"));
        }

        /// <summary>The same for a context line, where the whole-line snapshot earns its keep: pointing
        /// a line at a switch knob rewrites its bucket and its number as well.</summary>
        public void EditLine(PassiveNode node, ContextModifierLine line, string field, Action edit)
        {
            if (_restoring) return;

            ContextModifierLine before = line.Copy();
            edit();

            ContextModifierLine after = line.Copy();
            if (LineSnapshots.Same(before, after)) return;

            Record(new LineStateEdit<ContextModifierLine>(line, before, after, LineSnapshots.Assign,
                new EditTarget(line, field), $"{node.Id}: {EditFields.Context} {field}"));
        }

        // ── plumbing ───────────────────────────────────────────────────────────────────────────

        /// <summary>The last line of defence for the rule the entry guards already state: nothing that
        /// happens while a step is being restored is an edit. A future operation that forgets its guard
        /// still cannot file one.</summary>
        private void Record(IEditCommand command)
        {
            if (_restoring) return;

            _history.Record(command);
        }

        /// <summary>Applies one step and lets the tool rebuild itself, with recording shut off for the
        /// whole of it — the rebuild writes the document's values back into controls, and every one of
        /// those write-backs would otherwise be filed as a fresh edit.</summary>
        private string? Step(Func<IEditCommand?> move, bool undoing)
        {
            _restoring = true;

            try
            {
                IEditCommand? command = move();
                if (command is null) return null;

                Restored?.Invoke(new HistoryStep(command.Label, (command as IIdChangingEdit)?.Swap(undoing)));
                return command.Label;
            }
            finally
            {
                _restoring = false;
            }
        }
    }
}
