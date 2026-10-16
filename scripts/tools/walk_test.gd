extends SceneTree

# Usage: godot --headless --path . --script res://scripts/tools/walk_test.gd
var main: Node
var player: Node
var altar: Node
var frames := 0
var inspected := false

func _initialize() -> void:
	main = load("res://scenes/Main.tscn").instantiate()
	root.add_child(main)

func _physics_process(_delta: float) -> bool:
	frames += 1
	if frames == 30:
		player = main.get_node("Player")
		altar = main.get_node("AltarArea")
		altar.connect("AltarInspected", func(): inspected = true; print("ALTAR INSPECTED at t=", frames - 30))
		print("START pos=", player.global_position)
		player.call("MoveTo", altar.global_position, false)
		player.set("_targetAltar", altar)
	if frames > 30 and frames % 60 == 0:
		print("t=", frames - 30, " pos=", player.global_position, " floor=", player.is_on_floor())
	if frames > 420:
		print("RESULT inspected=", inspected)
		quit()
	return false
