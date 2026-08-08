// The zoom/pan/spread arithmetic moved to Core (Core.PassiveTree.View) when the game's own wheel took
// it over — the tool and the game must agree on where a document coordinate lands on screen. The alias
// keeps the tool's canvas, root and settings addressing the type by its bare name.
global using CanvasTransform = Core.PassiveTree.View.CanvasTransform;
