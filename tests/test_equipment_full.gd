@tool
extends SceneTree

func _init():
	print("=== RUNNING FULL EQUIPMENT VISUALS INTEGRATION TEST ===")
	var hero_scene = load("res://scenes/actors/Hero.tscn")
	var hero = hero_scene.instantiate()
	root.add_child(hero)
	
	await process_frame
	
	var equipment = hero.get_node("EquipmentComponent")
	var visuals = hero.get_node("EquipmentVisuals")
	
	# Load item resources
	var sword_item = load("res://data/items/sword_rusty.tres")
	var shield_item = load("res://data/items/shield_wooden.tres")
	var helmet_item = load("res://data/items/helmet_knight.tres")
	var armor_item = load("res://data/items/armor_steel.tres")
	var gloves_item = load("res://data/items/gloves_steel.tres")
	var boots_item = load("res://data/items/boots_steel.tres")
	
	print("1. Equipping MainHand...")
	equipment.Equip(sword_item)
	var sword_vis = visuals.GetSpawnedVisual(0) # MainHand
	assert(sword_vis != null, "Sword visual must exist")
	print("   OK: Sword visual attached: ", sword_vis.name, " parent: ", sword_vis.get_parent().name)
	
	print("2. Equipping OffHand...")
	equipment.Equip(shield_item)
	var shield_vis = visuals.GetSpawnedVisual(1) # OffHand
	assert(shield_vis != null, "Shield visual must exist")
	print("   OK: Shield visual attached: ", shield_vis.name, " parent: ", shield_vis.get_parent().name)
	
	print("3. Equipping Helmet...")
	equipment.Equip(helmet_item)
	var helmet_vis = visuals.GetSpawnedVisual(2) # Helmet
	assert(helmet_vis != null, "Helmet visual must exist")
	print("   OK: Helmet visual attached: ", helmet_vis.name, " parent: ", helmet_vis.get_parent().name)
	
	print("4. Equipping BodyArmor...")
	equipment.Equip(armor_item)
	var armor_vis = visuals.GetSpawnedVisual(3) # BodyArmor
	assert(armor_vis != null, "BodyArmor visual must exist")
	print("   OK: BodyArmor visual attached: ", armor_vis.name, " parent: ", armor_vis.get_parent().name)
	
	print("5. Equipping Gloves...")
	equipment.Equip(gloves_item)
	var gloves_vis_list = visuals.GetSpawnedVisuals(4) # Gloves
	assert(gloves_vis_list.size() == 2, "Gloves must attach to both left and right arms")
	print("   OK: Gloves visuals attached: ", gloves_vis_list.size(), " (L: ", gloves_vis_list[0].get_parent().name, ", R: ", gloves_vis_list[1].get_parent().name, ")")
	
	print("6. Equipping Boots...")
	equipment.Equip(boots_item)
	var boots_vis_list = visuals.GetSpawnedVisuals(5) # Boots
	assert(boots_vis_list.size() == 2, "Boots must attach to both left and right legs")
	print("   OK: Boots visuals attached: ", boots_vis_list.size(), " (L: ", boots_vis_list[0].get_parent().name, ", R: ", boots_vis_list[1].get_parent().name, ")")
	
	# Verify Animation and Bone Tracking
	var anim_player = hero.get_node("AnimationPlayer") as AnimationPlayer
	if anim_player.has_animation("Idle_Shield"):
		anim_player.play("Idle_Shield")
		anim_player.advance(0.1)
	
	await process_frame
	await process_frame
	
	print("Visual positions during Idle_Shield:")
	print("  Sword: ", sword_vis.global_position)
	print("  Shield: ", shield_vis.global_position)
	print("  Helmet: ", helmet_vis.global_position)
	print("  Armor: ", armor_vis.global_position)
	print("  Gloves L: ", gloves_vis_list[0].global_position, " R: ", gloves_vis_list[1].global_position)
	print("  Boots L: ", boots_vis_list[0].global_position, " R: ", boots_vis_list[1].global_position)
	
	print("7. Testing Unequip...")
	for slot_idx in range(6):
		equipment.Unequip(slot_idx)
	
	await process_frame
	for slot_idx in range(6):
		var vis = visuals.GetSpawnedVisual(slot_idx)
		assert(vis == null, "Visual for slot " + str(slot_idx) + " must be null after unequip")
	print("   OK: All visuals cleanly removed on unequip!")
	
	print("=== ALL TESTS PASSED SUCCESSFULLY! ===")
	quit(0)
