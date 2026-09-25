extends SceneTree

const Preview = preload("res://Addons/DualGridPreview/preview.gd")
const AUTHORED_MASKS = [1, 2, 8, 4, 3, 10, 9, 6, 12, 5, 7, 11, 13, 14, 0, 15]
const OFFSETS = [Vector2i(-1, -1), Vector2i(0, -1), Vector2i(-1, 0), Vector2i.ZERO]
var _failures = 0
var _checked = 0


func _initialize() -> void:
	call_deferred("_run")


func _run() -> void:
	_check(Engine.is_editor_hint(), "Test must run with --editor to exercise C# placeholders")
	var scene = load("res://World/SourceOfPowerNearVillage.tscn").instantiate()
	root.add_child(scene)
	await process_frame
	var preview = Preview.new()
	var before = PackedScene.new()
	before.pack(scene)
	preview.sync_scene(scene)
	_check(preview._layers.size() == 2, "Reads both terrain entries from non-tool C# resources")
	var terrain = scene.get_node("TerrainRoot")
	for source in terrain.get_children():
		if source is TileMapLayer:
			_verify_layer(preview, source, true)
	var after = PackedScene.new()
	after.pack(scene)
	_check(before.get_state().get_node_count() == after.get_state().get_node_count(), "Preview nodes must not enter PackedScene")
	var square: TileMapLayer = terrain.get_node("Square")
	_check(square.visible, "Preview must not mutate the source Visible property")
	var original_cells = square.tile_map_data.duplicate()
	var config = terrain.get("_config")
	var entry = config.get("Entries")[1]
	for layout in [0, 1]:
		entry.set("AtlasLayout", layout)
		square.clear()
		for mask in range(16):
			var centre = Vector2i(mask * 4 - 40, -30)
			for corner in range(4):
				if mask & (1 << corner):
					square.set_cell(centre + OFFSETS[corner], 0, Vector2i(3, 3))
		preview.sync_scene(scene)
		_verify_layer(preview, square, layout == 1)
		for mask in range(16):
			var centre = Vector2i(mask * 4 - 40, -30)
			var display: TileMapLayer = preview._layers[square.get_instance_id()].display
			var index = AUTHORED_MASKS.find(mask) if layout == 1 else mask
			var expected = Vector2i(index % 4, index / 4) if mask else Vector2i(-1, -1)
			_check(display.get_cell_atlas_coords(centre) == expected, "All 16 mask shapes in both layouts")
		square.clear()
		preview.sync_scene(scene)
		_check(preview._layers[square.get_instance_id()].display.get_used_cells().is_empty(), "Erasing clears every stale preview cell")
	entry.set("AtlasLayout", 1)
	square.tile_map_data = original_cells
	preview.sync_scene(scene)
	var count = preview._layers.size()
	preview.sync_scene(scene)
	_check(preview._layers.size() == count, "Repeated refresh does not duplicate layers")
	terrain.set("PreviewInEditor", false)
	preview.sync_scene(scene)
	_check(preview._layers.is_empty(), "Per-terrain preview switch removes generated layers")
	terrain.set("PreviewInEditor", true)
	preview.sync_scene(scene)
	_check(preview._layers.size() == 2, "Preview can be re-enabled")
	var original_texture = entry.get("Transitions")
	var replacement = ImageTexture.create_from_image(Image.create(512, 512, false, Image.FORMAT_RGBA8))
	entry.set("Transitions", replacement)
	preview.sync_scene(scene)
	var replaced: TileMapLayer = preview._layers[square.get_instance_id()].display
	_check(replaced.tile_set.tile_size == Vector2i(128, 128), "Changing atlas updates tile dimensions")
	_check(replaced.tile_set.get_source(0).texture == replacement, "Changing atlas replaces the displayed texture")
	_check(replaced.position == square.position - Vector2(64, 64), "Changing atlas updates half-cell offset")
	entry.set("Transitions", original_texture)
	preview.sync_scene(scene)
	_verify_layer(preview, square, true)
	preview.sync_scene(null)
	_check(preview._layers.is_empty(), "Closing or switching scene cleans up")
	preview.sync_scene(scene)
	square.free()
	preview.sync_scene(scene)
	_check(preview._layers.size() == 1, "Deleting source cleans up its preview")
	preview.clear()
	preview.clear()

	scene.queue_free()
	print("DUAL_GRID_PREVIEW_TEST: ", _checked, " checks; ", _failures, " failures")
	quit(1 if _failures else 0)


func _verify_layer(preview, source: TileMapLayer, grouped: bool) -> void:
	var id = source.get_instance_id()
	if not preview._layers.has(id):
		_check(false, "Missing preview for " + str(source.name))
		return
	var display: TileMapLayer = preview._layers[id].display
	_check(display.owner == null, "Generated layer must be ownerless")
	_check(display.get_script() == null, "Generated layer must have no C# script")
	_check(display.position == source.position - Vector2(display.tile_set.tile_size) / 2.0, "Runtime half-cell offset")
	var candidates: Dictionary = {}
	for cell in source.get_used_cells():
		for offset in OFFSETS:
			candidates[cell - offset] = true
	_check(display.get_used_cells().size() == candidates.size(), "Exact generated cell count")
	for cell in candidates:
		var mask = 0
		for corner in range(4):
			if source.get_cell_source_id(cell + OFFSETS[corner]) != -1:
				mask |= 1 << corner
		var index = AUTHORED_MASKS.find(mask) if grouped else mask
		_check(display.get_cell_atlas_coords(cell) == Vector2i(index % 4, index / 4), "Preview tile matches world corners")


func _check(condition: bool, message: String) -> void:
	_checked += 1
	if not condition:
		_failures += 1
		push_error(message)
