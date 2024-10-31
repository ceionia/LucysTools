extends Control

var MANAGER = null

func setup(manager):
	MANAGER = manager
	get_node("%lucy_bbcode").pressed = manager.allow_bbcode
	get_node("%lucy_punchback").pressed = manager.do_punchback
	get_node("%lucy_servername").text = manager.custom_server_name
	get_node("%lucy_servermsg").text = manager.server_join_message
	get_node("%lucy_fpackets").value = manager.frame_packets
	get_node("%lucy_bpackets").value = manager.bulk_packets
	get_node("%lucy_binterval").value = manager.bulk_interval
	get_node("%lucy_finterval").value = manager.full_interval

func _ready():
	print("[LUCY] Menu Ready")

func _input(event):
	if event is InputEventKey and event.scancode == KEY_F5 && event.pressed:
		visible = !visible
		print("[LUCY] Menu visble: ", visible)
	
	if event is InputEventKey and event.scancode == KEY_F6 && event.pressed:
		var name = Steam.getLobbyData(Network.STEAM_LOBBY_ID, "name")
		var nm = Steam.getNumLobbyMembers(Network.STEAM_LOBBY_ID)
		var code = Steam.getLobbyData(Network.STEAM_LOBBY_ID, "code")
		var type = Steam.getLobbyData(Network.STEAM_LOBBY_ID, "type")
		var lobby_dat = {"name": name, "nm": nm, "code": code, "type": type}
		print("[LUCY] LOBBY ", lobby_dat)

func _on_lucy_bbcode_toggled(button_pressed):
	MANAGER.allow_bbcode =  button_pressed
func _on_lucy_punchback_toggled(button_pressed):
	MANAGER.do_punchback =  button_pressed
func _on_lucy_servername_text_changed(new_text):
	MANAGER.custom_server_name = new_text
func _on_lucy_servermsg_text_changed(new_text):
	MANAGER.server_join_message = new_text
func _on_lucy_fpackets_value_changed(value):
	MANAGER.frame_packets = value
func _on_lucy_bpackets_value_changed(value):
	MANAGER.bulk_packets = value
func _on_lucy_binterval_value_changed(value):
	MANAGER.bulk_interval = value
func _on_lucy_finterval_value_changed(value):
	MANAGER.full_interval = value

func _on_lucy_raincloud_pressed():
	if not MANAGER.ingame: return
	print("[LUCY] Spawning raincloud")
	var player = MANAGER.get_player()
	var pos = Vector3(player.global_transform.origin.x, 42, player.global_transform.origin.z)
	var zone = player.current_zone
	Network._sync_create_actor("raincloud", pos, zone, - 1, Network.STEAM_ID)

func _on_lucy_meteor_pressed():
	if not MANAGER.ingame: return
	print("[LUCY] Spawning meteor")
	var player_pos = MANAGER.get_player().global_transform.origin
	var dist = INF
	var point = null
	for n in get_tree().get_nodes_in_group("fish_spawn"):
		var node_dist = n.global_transform.origin.distance_to(player_pos)
		if node_dist < dist:
			dist = node_dist
			point = n
	var zone = "main_zone"
	var pos = point.global_transform.origin
	Network._sync_create_actor("fish_spawn_alien", pos, zone, - 1, Network.STEAM_ID)

func _on_lucy_freezerain_pressed():
	if not MANAGER.ingame or not Network.GAME_MASTER: return
	print("[LUCY] Freezing rain")
	for cloud in get_tree().get_nodes_in_group("raincloud"):
		if cloud.controlled == true:
			cloud.speed = 0
			cloud.decay = false

func _on_lucy_clearrain_pressed():
	if not MANAGER.ingame or not Network.GAME_MASTER: return
	print("[LUCY] Clearing rain")
	for cloud in get_tree().get_nodes_in_group("raincloud"):
		cloud._deinstantiate(true)

