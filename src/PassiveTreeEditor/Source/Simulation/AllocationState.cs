// The allocation rules moved to Core (Core.PassiveTree.Allocation) when the game service took them
// over — the tool and the game must agree on what a legal allocation is. The alias keeps the tool's
// canvas and panels addressing the type by its bare name.
global using AllocationState = Core.PassiveTree.Allocation.AllocationState;
