extends Control

var MANAGER

const LucysLib_t = preload("res://mods/LucysLib/main.gd")
const BBCode_t = preload("res://mods/LucysLib/bbcode.gd")

func setup():
	if MANAGER.DEBUG: print("[LUCYSTOOLS MENU] Setup...")
	get_node("%lucy_punchback").pressed = MANAGER.do_punchback
	get_node("%lucy_servername").text = MANAGER.custom_server_name
	get_node("%lucy_servername_preview").bbcode_text = MANAGER.custom_server_name
	get_node("%lucy_servermsg").text = MANAGER.server_join_message
	var srv_m_bb: BBCode_t.BBCodeTag = MANAGER.LucysLib.BBCode.parse_bbcode_text(MANAGER.server_join_message)
	get_node("%lucy_servermsg_preview").bbcode_text = srv_m_bb.get_full(MANAGER.allowed_bb)
	get_node("%lucy_servermsg_preview2").bbcode_text = srv_m_bb.get_stripped()
	
	get_node("%lucy_chatcolor_bool").pressed = MANAGER.custom_color_enabled
	get_node("%lucy_chatcolor").color = Color(MANAGER.custom_color)
	get_node("%lucy_chatcolor_bool2").pressed = MANAGER.custom_text_color_enabled
	get_node("%lucy_chatcolor2").color = Color(MANAGER.custom_text_color)
	
	get_node("%lucy_lobbycode").text = MANAGER.custom_lobbycode
	
	get_node("%lucy_name").text = MANAGER.custom_name
	
	var tag_container = get_node("%bbcode_tags")
	var new_btn
	for v in BBCode_t.TAG_TYPE.values():
		if v == BBCode_t.TAG_TYPE.ROOT or v == BBCode_t.TAG_TYPE.NULL: continue
		new_btn = CheckBox.new()
		new_btn.text = BBCode_t.TAG_TYPE.keys()[v]
		new_btn.connect("pressed",self,"_tags_changed")
		tag_container.add_child(new_btn)
	
	update()

func update():
	if MANAGER.DEBUG: print("[LUCYSTOOLS MENU] Update...")
	var tag_type
	for tag_btn in get_node("%bbcode_tags").get_children():
		tag_type = BBCode_t.TAG_TYPE[tag_btn.text]
		tag_btn.pressed = tag_type in MANAGER.allowed_bb
		if MANAGER.DEBUG: print("[LUCYSTOOLS M UPDATE] " + tag_btn.text + " " + str(tag_type) + " " + str(tag_btn.pressed))
	_on_lucy_name_text_changed(MANAGER.custom_name)

func _tags_changed():
	if MANAGER.DEBUG: print("[LUCYSTOOLS MENU] Tags changed...")
	var tag_type
	var allowed = []
	for tag_btn in get_node("%bbcode_tags").get_children():
		tag_type = BBCode_t.TAG_TYPE[tag_btn.text]
		if tag_btn.pressed: allowed.append(tag_type)
		if MANAGER.DEBUG: print("[LUCYSTOOLS M _TAGS_CHANGED]" + tag_btn.text + " " + str(tag_type) + " " + str(tag_btn.pressed))
	MANAGER.allowed_bb = allowed

func _on_lucy_name_text_changed(new_text):
	if MANAGER.DEBUG: print("[LUCYSTOOLS MENU] Name text changed...")
	var result: BBCode_t.BBCodeTag = MANAGER.LucysLib.BBCode.parse_bbcode_text(new_text)
	
	var net_name = Network.STEAM_USERNAME.replace("[", "").replace("]", "")
	var good = result.get_stripped() == net_name
	get_node("%lucy_name_preview").bbcode_text = result.get_full(MANAGER.allowed_bb)
	get_node("%lucy_namegood").bbcode_text = "[color=green]Good[/color]" if good else "[color=red]Bad[/color]"
	
	MANAGER.custom_name_enabled = good
	MANAGER.custom_name = new_text if good else ""

func _ready():
	print("[LUCYSTOOLS] Menu Ready")
	
	MANAGER = $"/root/LucyLucysTools"
	
	visible = MANAGER.lucys_menu_visible
	
	var can_spawn = (Network.GAME_MASTER or Network.PLAYING_OFFLINE) and MANAGER.ingame
	
	get_node("%lucy_raincloud").disabled = not can_spawn
	get_node("%lucy_meteor").disabled = not can_spawn
	get_node("%lucy_void").disabled = not can_spawn
	get_node("%lucy_freezerain").disabled = not can_spawn
	get_node("%lucy_clearrain").disabled = not can_spawn
	get_node("%lucy_clearmeteor").disabled = not can_spawn
	get_node("%lucy_lobbyrefresh").disabled = not can_spawn

func _input(event):
	if event is InputEventKey and event.scancode == KEY_F5 && event.pressed:
		visible = !visible
		print("[LUCYSTOOLS] Menu visble: ", visible)
		MANAGER.lucys_menu_visible = visible
	
	if event is InputEventKey and event.scancode == KEY_F6 && event.pressed:
		var name = Steam.getLobbyData(Network.STEAM_LOBBY_ID, "name")
		var lname = Steam.getLobbyData(Network.STEAM_LOBBY_ID, "lobby_name")
		var nm = Steam.getNumLobbyMembers(Network.STEAM_LOBBY_ID)
		var code = Steam.getLobbyData(Network.STEAM_LOBBY_ID, "code")
		var type = Steam.getLobbyData(Network.STEAM_LOBBY_ID, "type")
		var bbname = Steam.getLobbyData(Network.STEAM_LOBBY_ID, "bbcode_lobby_name")
		var lobby_dat = {"name": name, "lobby_name":lname, "bbcode_lobby_name":bbname, "nm": nm, "code": code, "type": type}
		print("[LUCYSTOOLS] LOBBY ", lobby_dat)

func _on_lucy_bbcode_toggled(button_pressed):
	MANAGER.allow_bbcode =  button_pressed
func _on_lucy_punchback_toggled(button_pressed):
	MANAGER.do_punchback =  button_pressed
func _on_lucy_servername_text_changed(new_text):
	get_node("%lucy_servername_preview").bbcode_text = new_text
	MANAGER.custom_server_name = new_text
func _on_lucy_servermsg_text_changed(new_text):
	var srv_m_bb: BBCode_t.BBCodeTag = MANAGER.LucysLib.BBCode.parse_bbcode_text(new_text)
	get_node("%lucy_servermsg_preview").bbcode_text = srv_m_bb.get_full(MANAGER.allowed_bb)
	get_node("%lucy_servermsg_preview2").bbcode_text = srv_m_bb.get_stripped()
	MANAGER.server_join_message = new_text
func _on_lucy_chatcolor_bool_toggled(button_pressed):
	MANAGER.custom_color_enabled = button_pressed
func _on_lucy_chatcolor_color_changed(color):
	MANAGER.custom_color = color
func _on_lucy_chatcolor_bool2_toggled(button_pressed):
	MANAGER.custom_text_color_enabled = button_pressed
func _on_lucy_chatcolor2_color_changed(color):
	MANAGER.custom_text_color = color
func _on_lucy_intbbcode_toggled(button_pressed):
	MANAGER.allow_intrusive_bbcode = button_pressed
func _on_lucy_bug_bb_toggled(button_pressed):
	MANAGER.bug_bbcode = button_pressed
func _on_lucy_srv_bbcode_toggled(button_pressed):
	if (not Network.GAME_MASTER and not Network.PLAYING_OFFLINE): return
	MANAGER.srv_bbcode = button_pressed
func _on_lucy_lobbycode_text_changed(new_text):
	MANAGER.custom_lobbycode = new_text

func _on_lucy_raincloud_pressed():
	if not MANAGER.ingame or (not Network.GAME_MASTER and not Network.PLAYING_OFFLINE): return
	print("[LUCYSTOOLS] Spawning raincloud")
	var player = MANAGER.get_player()
	var pos = Vector3(player.global_transform.origin.x, 42, player.global_transform.origin.z)
	var zone = player.current_zone
	Network._sync_create_actor("raincloud", pos, zone, - 1, Network.STEAM_ID)

func _on_lucy_meteor_pressed():
	if not MANAGER.ingame or (not Network.GAME_MASTER and not Network.PLAYING_OFFLINE): return
	if get_tree().get_nodes_in_group("meteor").size() > 10: return
	print("[LUCYSTOOLS] Spawning meteor")
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
	if not MANAGER.ingame or (not Network.GAME_MASTER and not Network.PLAYING_OFFLINE): return
	print("[LUCYSTOOLS] Freezing rain")
	for cloud in get_tree().get_nodes_in_group("raincloud"):
		if cloud.controlled == true:
			cloud.speed = 0
			cloud.decay = false

func _on_lucy_clearrain_pressed():
	if not MANAGER.ingame or (not Network.GAME_MASTER and not Network.PLAYING_OFFLINE): return
	print("[LUCYSTOOLS] Clearing rain")
	for cloud in get_tree().get_nodes_in_group("raincloud"):
		cloud._deinstantiate(true)

func _on_lucy_clearchat_pressed():
	Network._wipe_chat()
	Network.emit_signal("_chat_update")

func _on_lucy_clearmeteor_pressed():
	if not MANAGER.ingame or (not Network.GAME_MASTER and not Network.PLAYING_OFFLINE): return
	print("[LUCYSTOOLS] Clearing meteor")
	for meteor in get_tree().get_nodes_in_group("meteor"):
		meteor._deinstantiate(true)

func _on_lucy_void_pressed():
	if not MANAGER.ingame or (not Network.GAME_MASTER and not Network.PLAYING_OFFLINE): return
	if get_tree().get_nodes_in_group("void_portal").size() > 10: return
	print("[LUCYSTOOLS] Spawning void")
	var player_pos = MANAGER.get_player().global_transform.origin
	var dist = INF
	var point = null
	for n in get_tree().get_nodes_in_group("hidden_spot"):
		var node_dist = n.global_transform.origin.distance_to(player_pos)
		if node_dist < dist:
			dist = node_dist
			point = n
	var zone = "main_zone"
	var pos = point.global_transform.origin
	Network._sync_create_actor("void_portal", pos, zone, - 1, Network.STEAM_ID)

func _on_lucy_lobbyrefresh_pressed():
	if not MANAGER.ingame or (not Network.GAME_MASTER and not Network.PLAYING_OFFLINE): return
	MANAGER.inject_lobby_data(1,Network.STEAM_LOBBY_ID)
