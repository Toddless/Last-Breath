namespace LastBreath.Descriptors.Sandbox
{
    using System.Collections.Generic;

    /// <summary>What one dry run has done, in the order it did it: every action the sandbox carried
    /// out, and every one only the running game can carry out, named rather than performed.</summary>
    public sealed class SandboxLog
    {
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines => _lines;

        public void Write(string line) => _lines.Add(line);

        public void Clear() => _lines.Clear();
    }
}
