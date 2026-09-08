@tool
extends RefCounted

const ROOT_SCRIPT = "res://World/TerrainRoot.cs"
const GENERATED = &"dual_grid_editor_preview"
const GROUPED_INDICES = [14, 0, 1, 4, 3, 9, 7, 10, 2, 6, 5, 11, 8, 12, 13, 15]
const CORNERS = [Vector2i(-1, -1), Vector2i(0, -1), Vector2i(-1, 0), Vector2i.ZERO]

# Only native preview nodes and value snapshots survive a refresh. No C# resources,
# objects or managed callables are retained across an assembly reload.
var _layers: Dictionary = {}
var _warnings: Dictionary = {}


func sync_scene(scene: Node) -> void:
	var active: Dictionary = {}
	if is_instance_valid(scene):
		_visit(scene, active)
	for id in _layers.keys():
		if not active.has(id):
			_remove(id)


func clear() -> void:
	for id in _layers.keys():
		_remove(id)
	_warnings.clear()


func _visit(node: Node, active: Dictionary) -> void:
	if node.has_meta(GENERATED):
		return
	var script = node.get_script()
	if script != null and script.resource_path == ROOT_SCRIPT:
		_sync_root(node, active)
	for child in node.get_children():
		_visit(child, active)


func _sync_root(terrain: Node, active: Dictionary) -> void:
	if not _property(terrain, &"PreviewInEditor", true):
		return
	var config = _property(terrain, &"_config")
	if config == null:
		return
	var entries = _property(config, &"Entries", [])
	var by_name: Dictionary = {}
	for entry in entries:
		if entry == null:
			continue
		var key = _property(entry, &"Layer", "")
		if not by_name.has(key):
			by_name[key] = entry
	var order = 0
	for source in terrain.get_children():
		if not source is TileMapLayer or source.has_meta(GENERATED):
			continue
		if not by_name.has(str(source.name)):
			continue
		var entry = by_name[str(source.name)]
		var texture = _property(entry, &"Transitions")
		var layout: int = _property(entry, &"AtlasLayout", 0)
		if not texture is Texture2D or layout < 0 or layout > 1:
			_warn(str(source.get_path()) + ": missing texture or unknown atlas layout")
			continue
		var size: Vector2i = texture.get_size()
		if size.x < 4 or size.y < 4 or size.x % 4 != 0 or size.y % 4 != 0:
			_warn(texture.resource_path + ": expected a 4x4 sheet of equal tiles")
			continue
		_sync_layer(source, texture, _property(entry, &"SurfaceMaterial"), layout, size / 4, order)
		active[source.get_instance_id()] = true
		order += 1


func _sync_layer(source: TileMapLayer, texture: Texture2D, surface: Material,
		layout: int, size: Vector2i, order: int) -> void:
	var id = source.get_instance_id()
	if _layers.has(id) and (not is_instance_valid(_layers[id].display) or _layers[id].display.get_parent() != source.get_parent()):
		_remove(id)
	if not _layers.has(id):
		var display = TileMapLayer.new()
		display.name = str(source.name) + "EditorPreview"
		display.set_meta(GENERATED, true)
		display.collision_enabled = false
		display.navigation_enabled = false
		# Internal and ownerless: not selectable or included in PackedScene saves.
		source.get_parent().add_child(display, false, Node.INTERNAL_MODE_BACK)
		_layers[id] = {"display": display, "cells": {}, "texture": 0, "size": Vector2i.ZERO, "layout": -1}
	var state: Dictionary = _layers[id]
	var display: TileMapLayer = state.display
	var rebuild: bool = state.texture != texture.get_instance_id() or state.size != size or state.layout != layout
	if rebuild:
		display.tile_set = _tileset(texture, size)
		display.clear()
		state.cells = {}
		state.texture = texture.get_instance_id()
		state.size = size
		state.layout = layout
	# Runtime uses the source position and the transition sheet's half-cell offset.
	display.position = source.position - Vector2(size) / 2.0
	display.material = surface
	display.visible = source.visible
	if display.get_index(true) - source.get_parent().get_child_count() != order:
		source.get_parent().move_child(display, order)
	_reconcile(display, state, source.get_used_cells(), layout)
	# Hide only the render RID; the scene's Visible property and undo history stay intact.
	RenderingServer.canvas_item_set_visible(source.get_canvas_item(), false)


func _tileset(texture: Texture2D, size: Vector2i) -> TileSet:
	var atlas = TileSetAtlasSource.new()
	atlas.texture = texture
	atlas.texture_region_size = size
	for row in range(4):
		for column in range(4):
			atlas.create_tile(Vector2i(column, row))
	var tiles = TileSet.new()
	tiles.tile_size = size
	tiles.add_source(atlas, 0)
	return tiles


func _reconcile(display: TileMapLayer, state: Dictionary, cells: Array[Vector2i], layout: int) -> void:
	var present: Dictionary = {}
	var dirty: Dictionary = {}
	for cell in cells:
		present[cell] = true
		if not state.cells.has(cell):
			_invalidate(cell, dirty)
	for cell in state.cells:
		if not present.has(cell):
			_invalidate(cell, dirty)
	state.cells = present
	for cell in dirty:
		var mask = 0
		for corner in range(4):
			if present.has(cell + CORNERS[corner]):
				mask |= 1 << corner
		if mask == 0:
			display.erase_cell(cell)
		else:
			var index: int = GROUPED_INDICES[mask] if layout == 1 else mask
			display.set_cell(cell, 0, Vector2i(index % 4, index / 4))


func _invalidate(cell: Vector2i, dirty: Dictionary) -> void:
	for offset in CORNERS:
		dirty[cell - offset] = true


func _remove(id: int) -> void:
	var source = instance_from_id(id) if is_instance_id_valid(id) else null
	if is_instance_valid(source) and source is CanvasItem:
		RenderingServer.canvas_item_set_visible(source.get_canvas_item(), source.visible)
	var display = _layers[id].display
	if is_instance_valid(display):
		display.get_parent().remove_child(display)
		display.queue_free()
	_layers.erase(id)


func _property(object: Object, key: StringName, fallback = null):
	for property in object.get_property_list():
		if property.name == key:
			var value = object.get(key)
			return fallback if value == null else value
	return fallback


func _warn(message: String) -> void:
	if _warnings.has(message):
		return
	_warnings[message] = true
	push_warning("Dual Grid Preview: " + message)
