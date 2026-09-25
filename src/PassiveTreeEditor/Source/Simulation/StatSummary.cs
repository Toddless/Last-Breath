// The summator moved to Core (Core.PassiveTree.Summary) when the game's wheel needed a panel saying
// what the taken nodes are worth — the tool and the game must agree on what an allocation adds up to.
// The alias keeps the tool's root and panels addressing the types by their bare names.
global using StatSummary = Core.PassiveTree.Summary.PassiveTreeSummary;
global using TreeSummary = Core.PassiveTree.Summary.TreeSummary;
global using ParameterTotal = Core.PassiveTree.Summary.ParameterTotal;
global using ContextTotal = Core.PassiveTree.Summary.ContextTotal;
