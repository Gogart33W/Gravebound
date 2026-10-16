import bpy
import math
import sys
import os

def parse_args():
    argv = sys.argv
    if "--" in argv:
        args = argv[argv.index("--") + 1:]
        if len(args) > 0 and args[0]:
            return args[0]
    return "assets/models/environment/crypt_arena.glb"

def clean_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for col in list(bpy.data.collections):
        bpy.data.collections.remove(col)
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)

def create_material(name, color, metallic=0.0, roughness=0.85, emission=None, emission_strength=1.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if emission:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission
                if "Emission Strength" in bsdf.inputs:
                    bsdf.inputs["Emission Strength"].default_value = emission_strength
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = emission
    return mat

def build_arena():
    clean_scene()
    col = bpy.data.collections.new("CryptArena")
    bpy.context.scene.collection.children.link(col)

    # Materials
    mat_floor_outer = create_material("Mat_CryptFloorDark", (0.10, 0.11, 0.13, 1.0), roughness=0.9)
    mat_floor_inner = create_material("Mat_CryptFloorPaved", (0.16, 0.17, 0.19, 1.0), roughness=0.85)
    mat_dais = create_material("Mat_CryptDais", (0.22, 0.23, 0.25, 1.0), roughness=0.8)
    mat_pillar = create_material("Mat_CryptPillar", (0.18, 0.19, 0.20, 1.0), roughness=0.82)
    mat_pillar_trim = create_material("Mat_PillarTrim", (0.13, 0.14, 0.15, 1.0), roughness=0.75)
    mat_rune = create_material("Mat_RuneGlow", (0.1, 0.9, 0.6, 1.0), emission=(0.15, 0.85, 0.55, 1.0), emission_strength=2.5)
    mat_wall = create_material("Mat_CryptWall", (0.12, 0.13, 0.14, 1.0), roughness=0.9)
    mat_brazier = create_material("Mat_BrazierMetal", (0.05, 0.05, 0.05, 1.0), metallic=0.8, roughness=0.5)
    mat_fire = create_material("Mat_CryptFlame", (0.95, 0.45, 0.08, 1.0), emission=(1.0, 0.45, 0.05, 1.0), emission_strength=3.0)

    # 1. Main Base Floor (Octagon base - collision enabled: Arena_Floor-col)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=16.0, depth=0.8, location=(0, 0, -0.4))
    floor_base = bpy.context.active_object
    floor_base.name = "Arena_Floor-col"
    floor_base.data.materials.append(mat_floor_outer)

    # 2. Stepped Inner Paved Ring
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=13.5, depth=0.2, location=(0, 0, 0.1))
    floor_paved = bpy.context.active_object
    floor_paved.name = "Floor_InnerPaved"
    floor_paved.data.materials.append(mat_floor_inner)

    # Decorative Stone Paving Tiles on the floor
    tile_count = 12
    for ring_r in [5.5, 9.0]:
        t_count = 8 if ring_r < 7 else 14
        for i in range(t_count):
            angle = (i / t_count) * 2 * math.pi
            x = math.cos(angle) * ring_r
            y = math.sin(angle) * ring_r
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(x, y, 0.18))
            tile = bpy.context.active_object
            tile.scale = (0.9, 0.6, 0.05)
            tile.rotation_euler = (0, 0, angle + 0.2)
            tile.name = f"Floor_Tile_{ring_r}_{i}"
            tile.data.materials.append(mat_floor_outer)

    # 3. Central Necromantic Ritual Dais
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=4.2, depth=0.35, location=(0, 0, 0.25))
    dais_step1 = bpy.context.active_object
    dais_step1.name = "Ritual_Dais_Step1-col"
    dais_step1.data.materials.append(mat_dais)

    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=3.2, depth=0.3, location=(0, 0, 0.5))
    dais_step2 = bpy.context.active_object
    dais_step2.name = "Ritual_Dais_Step2-col"
    dais_step2.data.materials.append(mat_floor_inner)

    # Rune Glow Ring Inlaid in the dais
    bpy.ops.mesh.primitive_torus_add(major_radius=2.4, minor_radius=0.08, location=(0, 0, 0.66))
    rune_ring = bpy.context.active_object
    rune_ring.name = "Ritual_RuneRing"
    rune_ring.data.materials.append(mat_rune)

    # Central Altar / Ancient Sarcophagus
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 1.05))
    altar = bpy.context.active_object
    altar.scale = (1.1, 0.7, 0.45)
    altar.name = "Central_Altar-col"
    altar.data.materials.append(mat_dais)

    # Altar lid / trim
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 1.3))
    altar_lid = bpy.context.active_object
    altar_lid.scale = (1.2, 0.8, 0.1)
    altar_lid.name = "Central_Altar_Lid"
    altar_lid.data.materials.append(mat_pillar_trim)

    # 4. Massive Ancient Crypt Columns (Pillars)
    pillar_radius = 12.0
    num_pillars = 10
    for i in range(num_pillars):
        angle = (i / num_pillars) * 2 * math.pi
        px = math.cos(angle) * pillar_radius
        py = math.sin(angle) * pillar_radius

        is_broken = (i in [2, 7])
        shaft_height = 2.4 if is_broken else 4.6

        # Pillar Base (pedestal)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, py, 0.45))
        base = bpy.context.active_object
        base.scale = (1.4, 1.4, 0.9)
        base.rotation_euler = (0, 0, angle)
        base.name = f"Pillar_{i}_Base-col"
        base.data.materials.append(mat_pillar_trim)

        # Pillar Shaft (faceted cylinder)
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=10, 
            radius=0.52, 
            depth=shaft_height, 
            location=(px, py, 0.9 + shaft_height / 2.0)
        )
        shaft = bpy.context.active_object
        shaft.name = f"Pillar_{i}_Shaft-col"
        shaft.data.materials.append(mat_pillar)

        # Pillar Capital (if not broken)
        if not is_broken:
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, py, 0.9 + shaft_height + 0.35))
            capital = bpy.context.active_object
            capital.scale = (1.45, 1.45, 0.7)
            capital.rotation_euler = (0, 0, angle)
            capital.name = f"Pillar_{i}_Capital"
            capital.data.materials.append(mat_pillar_trim)
        else:
            # Rubble at base of broken pillar
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px + 0.9, py - 0.4, 0.25))
            rubble = bpy.context.active_object
            rubble.scale = (0.5, 0.4, 0.3)
            rubble.rotation_euler = (0.3, 0.2, 0.5)
            rubble.name = f"Pillar_{i}_Rubble-col"
            rubble.data.materials.append(mat_pillar)

    # 5. Perimeter Ruined Walls & Archway (Boundary Obstacles)
    num_wall_segments = 16
    for i in range(num_wall_segments):
        # Leave open entrance at index 0 and 8 (North & South)
        if i in [0, 8]:
            continue
        angle = (i / num_wall_segments) * 2 * math.pi
        wx = math.cos(angle) * 15.2
        wy = math.sin(angle) * 15.2
        wall_rot = angle + math.pi / 2.0

        w_height = 1.4 if i % 2 == 0 else 1.0
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, wy, w_height / 2.0))
        wall = bpy.context.active_object
        wall.scale = (4.8, 0.8, w_height)
        wall.rotation_euler = (0, 0, wall_rot)
        wall.name = f"Perimeter_Wall_{i}-col"
        wall.data.materials.append(mat_wall)

        # Wall capstone
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, wy, w_height + 0.08))
        cap = bpy.context.active_object
        cap.scale = (5.0, 1.0, 0.16)
        cap.rotation_euler = (0, 0, wall_rot)
        cap.name = f"Wall_Cap_{i}"
        cap.data.materials.append(mat_pillar_trim)

    # 6. Ancient Stone Sarcophagi in the arena perimeter alcoves
    sarcophagus_angles = [0.4, 1.9, 3.6, 5.1]
    for idx, s_angle in enumerate(sarcophagus_angles):
        sx = math.cos(s_angle) * 9.5
        sy = math.sin(s_angle) * 9.5
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx, sy, 0.45))
        sarc = bpy.context.active_object
        sarc.scale = (1.6, 0.8, 0.6)
        sarc.rotation_euler = (0, 0, s_angle + math.pi/2)
        sarc.name = f"Crypt_Sarcophagus_{idx}-col"
        sarc.data.materials.append(mat_pillar)

        # Sarcophagus Lid (slightly askew on some for horror/exploration feel)
        lid_offset = 0.15 if idx % 2 == 1 else 0.0
        lid_rot = 0.08 if idx % 2 == 1 else 0.0
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx + lid_offset, sy, 0.82))
        lid = bpy.context.active_object
        lid.scale = (1.7, 0.9, 0.18)
        lid.rotation_euler = (0, lid_rot, s_angle + math.pi/2)
        lid.name = f"Crypt_Sarcophagus_Lid_{idx}"
        lid.data.materials.append(mat_pillar_trim)

    # 7. Four Ornamental Crypt Braziers on Stone Pedestals
    brazier_angles = [math.pi / 4, 3 * math.pi / 4, 5 * math.pi / 4, 7 * math.pi / 4]
    for idx, b_angle in enumerate(brazier_angles):
        bx = math.cos(b_angle) * 6.5
        by = math.sin(b_angle) * 6.5

        # Stone Stand
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.4, depth=1.0, location=(bx, by, 0.5))
        stand = bpy.context.active_object
        stand.name = f"Brazier_Stand_{idx}-col"
        stand.data.materials.append(mat_pillar_trim)

        # Iron Bowl
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.55, depth=0.3, location=(bx, by, 1.15))
        bowl = bpy.context.active_object
        bowl.name = f"Brazier_Bowl_{idx}"
        bowl.data.materials.append(mat_brazier)

        # Crypt Fire Crystal / Ember
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.25, location=(bx, by, 1.35))
        fire = bpy.context.active_object
        fire.name = f"Brazier_Flame_{idx}"
        fire.data.materials.append(mat_fire)

    # 8. South Entrance Stairway
    for step in range(3):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -15.5 - step * 0.9, 0.3 - step * 0.2))
        stair = bpy.context.active_object
        stair.scale = (3.5, 0.9, 0.3)
        stair.name = f"Entrance_Stair_{step}-col"
        stair.data.materials.append(mat_floor_inner)

def main():
    target_path = parse_args()
    print(f"[Blender] Generating Crypt Arena...")
    build_arena()

    # Ensure output dir exists
    abs_target = os.path.abspath(target_path)
    os.makedirs(os.path.dirname(abs_target), exist_ok=True)

    print(f"[Blender] Exporting GLB to: {abs_target}")
    bpy.ops.export_scene.gltf(
        filepath=abs_target,
        export_format='GLB',
        use_selection=False,
        export_apply=True
    )
    print(f"[Blender] Arena export completed successfully! File size: {os.path.getsize(abs_target)} bytes")

if __name__ == "__main__":
    main()
