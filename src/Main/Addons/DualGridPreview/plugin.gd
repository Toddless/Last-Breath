@tool
extends EditorPlugin

const Preview = preload("res://Addons/DualGridPreview/preview.gd")
const REFRESH_SECONDS = 0.1

var _preview = Preview.new()
var _toggle: Button
var _elapsed = 0.0


func _enter_tree() -> void:
	_toggle = Button.new()
	_toggle.text = "Terrain preview"
	_toggle.tooltip_text = "Live terrain textures. Turn off to see the painted debug tiles."
	_toggle.toggle_mode = true
	_toggle.button_pressed = true
	_toggle.toggled.connect(_set_enabled)
	add_control_to_container(CONTAINER_CANVAS_EDITOR_MENU, _toggle)
	set_process(true)


func _exit_tree() -> void:
	set_process(false)
	_preview.clear()
	remove_control_from_container(CONTAINER_CANVAS_EDITOR_MENU, _toggle)
	_toggle.queue_free()


func _process(delta: float) -> void:
	_elapsed += delta
	if _elapsed < REFRESH_SECONDS or not _toggle.button_pressed:
		return
	_elapsed = 0.0
	_preview.sync_scene(EditorInterface.get_edited_scene_root())


func _set_enabled(enabled: bool) -> void:
	if enabled:
		_elapsed = REFRESH_SECONDS
	else:
		_preview.clear()
