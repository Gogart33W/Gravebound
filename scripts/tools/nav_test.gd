extends SceneTree

var main: Node
var frames := 0

func _initialize() -> void:
	main = load("res://scenes/Main.tscn").instantiate()
	root.add_child(main)

func _physics_process(_delta: float) -> bool:
	frames += 1
	if frames == 20:
		var map: RID = main.get_world_3d().navigation_map
		var from := Vector3(0, 0, 10.5)
		for to in [Vector3(0, 0, -10.5), Vector3(0, 0.2, -8.0), Vector3(8, 0, 0), Vector3(0, 0, 0)]:
			var snapped: Vector3 = NavigationServer3D.map_get_closest_point(map, to)
			var path := NavigationServer3D.map_get_path(map, from, snapped, true)
			print("to=", to, " snapped=", snapped, " path=", path)
		quit()
	return false
