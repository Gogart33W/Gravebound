extends SceneTree

# One-off tool: re-bakes crypt_navmesh.tres from the REAL collision geometry of Main.tscn.
# Usage: godot --headless --path . --script res://scripts/tools/bake_navmesh.gd

var main: Node
var region: NavigationRegion3D
var frames := 0

func _initialize() -> void:
	main = load("res://scenes/Main.tscn").instantiate()
	root.add_child(main)
	region = main.get_node("NavigationRegion3D")
	var nm := NavigationMesh.new()
	nm.geometry_parsed_geometry_type = NavigationMesh.PARSED_GEOMETRY_STATIC_COLLIDERS
	nm.geometry_source_geometry_mode = NavigationMesh.SOURCE_GEOMETRY_ROOT_NODE_CHILDREN
	nm.cell_size = 0.25
	nm.cell_height = 0.25
	nm.agent_radius = 0.5
	nm.agent_height = 1.8
	nm.agent_max_climb = 0.5
	nm.agent_max_slope = 50.0
	region.navigation_mesh = nm

func _physics_process(_delta: float) -> bool:
	frames += 1
	if frames == 5:
		region.bake_navigation_mesh(false)
	if frames == 30:
		var nm := region.navigation_mesh
		print("polys=", nm.get_polygon_count(), " verts=", nm.get_vertices().size())
		if nm.get_polygon_count() > 0:
			ResourceSaver.save(nm, "res://assets/models/environment/crypt_navmesh.tres")
			print("SAVED")
		quit()
	return false
