extends Node

const LUCYS_MENU_SCENE = preload("res://mods/Lucy.LucysTools/lucys_menu.tscn")

var lucys_menu = null
onready var root = get_tree().root

var INCERCEPT_MSG = false
var INCERCEPT_SEND_MSG = false

var do_punchback = false
var allow_bbcode = false
var custom_server_name = "" setget set_server_name
var server_join_message = "[color=#5BCEFA]TRAN[/color][color=#F5A9B8]S RIG[/color][color=#ffffff]HTS![/color]" setget set_join_message

var custom_color_enabled = false
var custom_color = Color("009cd0") setget set_custom_color

var custom_name_enabled = false
var real_custom_name = ""
var custom_name = "" setget set_custom_name

var allow_intrusive_bbcode = false setget set_allow_intrusive_bbcode

var log_messages = false setget set_log_messages

var lucys_menu_visible = true

var allowed_tags = ["b", "i", "u", "s", "color"]
var escape_invalid = true
var bbcode_matcher = null

func set_custom_name(val):
	custom_name = val
	var bb = bbcode_process(val)
	real_custom_name = bb.fin

# i know this sucks
# but i have things to do
func bbcode_recurse(text, data):
	var m = bbcode_matcher.search(text)
	if m == null:
		var escaped = text.replace('[lb]','[').replace('[','[lb]') if escape_invalid else text
		data.fin += escaped
		data.stripped += escaped
	else:
		#print("Found ", m.strings, " in '", text, "'")
		bbcode_recurse(m.get_string(1), data)
		var tag = m.get_string(2)
		var junk = m.get_string(3)
		var allowed = tag in allowed_tags
		if allowed:
			data.fin += "[" + tag + junk + "]"
		else:
			data.fin += "[lb]" + tag + junk + "]"
			data.stripped += "[lb]" + tag + junk + "]"
		#print("TAG ", m.get_string(2), " JUNK ", m.get_string(3))
		data.tags.append([tag, junk])
		bbcode_recurse(m.get_string(4), data)
		if allowed:
			data.fin += "[/" + tag + "]"
		else:
			data.fin += "[lb]/" + tag + "]"
			data.stripped += "[lb]/" + tag + "]"
		bbcode_recurse(m.get_string(5), data)

func bbcode_process(text):
	bbcode_matcher = RegEx.new()
	bbcode_matcher.compile("^(.*?)\\[(\\w+?)([^\\]]*)\\](.+?)\\[/\\2\\](.*?)$")
	#print("processing '", text, "'")
	var data = {"fin": "", "tags": [], "stripped": ""}
	bbcode_recurse(text, data)
	return data

var ingame = false

func get_user_color() -> Color:
	var base_color = Color(Globals.cosmetic_data[PlayerData.cosmetics_equipped["primary_color"]]["file"].main_color) * Color(0.95, 0.9, 0.9)
	var color = custom_color if custom_color_enabled else base_color
	return color

func safe_message(user_id, color, boring_msg, local, lucy_user, lucy_msg):
	var username = Network._get_username_from_id(user_id) if lucy_user == "" else lucy_user
	var bb_user = bbcode_process(username)
	username = bb_user.fin
	
	var msg = lucy_msg if lucy_msg != "" else boring_msg
	var bb_data = bbcode_process(msg)
	var filter_message = bb_data.fin
	
	if bb_user.stripped != Network._get_username_from_id(user_id):
		filter_message = "(" + Network._get_username_from_id(user_id) + ") " + filter_message
	
	if OptionsMenu.chat_filter:
		filter_message = SwearFilter._filter_string(filter_message)
	
	var final_message = filter_message.replace("%u", "[color=#" + str(color) + "]" + username + "[/color]")
	var thing = {"username":username, "color":color, "filter_message":filter_message,
		"final_message":final_message,"lucy_user":lucy_user,"lucy_msg":lucy_msg}
	#print("FUCK2 ", thing)
	Network._update_chat(final_message, local)

# this is stinky
func process_message(lit_text, final, prefix, suffix, endcap, spoken_text, local, colon, playerhud):
	var thing = {
		"lit_text": lit_text, "final": final, "prefix": prefix, "suffix": suffix,
		"endcap": endcap,
		"custom_color_enabled": custom_color_enabled,
		"custom_name_enabled": custom_name_enabled, "allow_bbcode": allow_bbcode,
		"allowed_tags": allowed_tags
	}
	#print("FUCK ", thing)
	if Network.GAME_MASTER and lit_text.begins_with("%"):
		var bb_dat = bbcode_process(lit_text)
		lucy_send_message(lit_text.trim_prefix('%'), bb_dat.stripped, false)
		# we sent the message ourself
		return [true]
	
	var msg = final
	var boring_msg = final
	var speak = spoken_text
	if allow_bbcode:
		var p = bbcode_process(lit_text)
		if not p.tags.empty():
			msg = prefix + "%u" + endcap + p.fin + suffix
			boring_msg = prefix + "%u" + endcap + p.stripped + suffix
			speak = p.stripped
		#print("FUCK3 ", {"msg":msg,"boring_msg":boring_msg,"p":p})
		if msg != "": lucy_send_message(msg, boring_msg, local)
		
		if spoken_text != "" and colon: playerhud.emit_signal("_message_sent", speak)
		# we did it ourselves
		return [true]
	
	# return the custom color
	return [false, get_user_color().to_html()]

var LUCYSTOOLS_USERS = []

func lucy_send_message(message, boring_msg, local = false):
	if not Network._message_cap(Network.STEAM_ID):
		Network._update_chat("Sending too many messages too quickly!", false)
		Network._update_chat("Sending too many messages too quickly!", true)
		return
	
	var msg_pos = Network.MESSAGE_ORIGIN.round()
	
	var lucy_user = real_custom_name if custom_name_enabled else ""
	var color = get_user_color().to_html()
	
	safe_message(Network.STEAM_ID, color, boring_msg, local, lucy_user, message)
	Network._send_P2P_Packet(
		{"type": "message", "message": boring_msg, "color": color, "local": local,
			"position": Network.MESSAGE_ORIGIN, "zone": Network.MESSAGE_ZONE,
			"zone_owner": PlayerData.player_saved_zone_owner,
			"bb_user": lucy_user, "bb_msg": message},
		"peers", 2, Network.CHANNELS.GAME_STATE)
	


func process_read(DATA, PACKET_SENDER, from_host):
	match DATA["type"]:
		"lucy_packet":
			print("[LUCY PACKET]")
			if not PACKET_SENDER in LUCYSTOOLS_USERS: LUCYSTOOLS_USERS.append(PACKET_SENDER)
			return true
		
		"message":
			if DATA.has("message"):
				if typeof(DATA["message"]) == TYPE_STRING:
					if not "%u" in DATA["message"] and not from_host:
						DATA["message"] = "(%u)" + DATA["message"]
			if DATA.has("bb_msg"):
				if typeof(DATA["bb_msg"]) == TYPE_STRING:
					if not "%u" in DATA["bb_msg"] and not from_host:
						DATA["bb_msg"] = "(%u)" + DATA["bb_msg"]
			
			if DATA.has("bb_msg") or DATA.has("bb_user"):
				if not PACKET_SENDER in LUCYSTOOLS_USERS:
					LUCYSTOOLS_USERS.append(PACKET_SENDER)
			else:
				return false
			# yay! this is a lucy user :3
			if PlayerData.players_muted.has(PACKET_SENDER) or PlayerData.players_hidden.has(PACKET_SENDER): return 
			
			if not Network._validate_packet_information(DATA,
				["message", "color", "local", "position", "zone", "zone_owner", "bb_user", "bb_msg"],
				[TYPE_STRING, TYPE_STRING, TYPE_BOOL, TYPE_VECTOR3, TYPE_STRING, TYPE_INT, TYPE_STRING, TYPE_STRING]):
					return
			
			if not Network._message_cap(PACKET_SENDER): return 
			
			var user_id: int = PACKET_SENDER
			var user_color: String = DATA["color"]
			var user_message: String = DATA["message"]
			var lucy_user: String = DATA["bb_user"]
			var lucy_msg: String = DATA["bb_msg"]
			
			
			if not DATA["local"]:
				safe_message(user_id, user_color, user_message, false, lucy_user, lucy_msg)
			else :
				var dist = DATA["position"].distance_to(Network.MESSAGE_ORIGIN)
				if DATA["zone"] == Network.MESSAGE_ZONE and DATA["zone_owner"] == PlayerData.player_saved_zone_owner:
					if dist < 25.0: safe_message(user_id, user_color, user_message, true, lucy_user, lucy_msg)
			
			# don't process it again!
			return true
		
		# lucy punchback :3
		"player_punch":
			if not DATA.has("nya"): punched(PACKET_SENDER, DATA["punch_type"])
			# still get punched!
			return false
		
	# fall through to default code
	return false

func bbcode_changes():
	if allow_intrusive_bbcode:
		allowed_tags = [
		"b", "i", "u", "s", "color",
		"wave", "rainbow", "shake", "tornado", "font"]
	else:
		allowed_tags = [
		"b", "i", "u", "s", "color"]
	if lucys_menu != null: lucys_menu.update()

func set_log_messages(val):
	log_messages = val
	Network.LUCY_LOG_MESSAGES = val

func set_allow_intrusive_bbcode(bbcode):
	allow_intrusive_bbcode = bbcode
	bbcode_changes()
func set_server_name(name):
	custom_server_name = name
	Network.LUCY_SRV_NAME = name
func set_join_message(msg):
	server_join_message = msg
func set_custom_color(val):
	custom_color = Color(val) if Color(val) != Color("d5aa73") else Color("739ed5")
	custom_color.a = 1

func _ready():
	print("[LUCY] Loaded LucysTools")
	load_settings()
	root.connect("child_entered_tree", self, "_on_enter")
	Network.connect("_new_player_join", self, "new_player")
	Steam.connect("lobby_created", self, "inject_lobby_data")

func inject_lobby_data(connect, lobby_id):
	if connect != 1: return
	
	if custom_server_name != "":
		Steam.setLobbyData(lobby_id, "bbcode_lobby_name", custom_server_name)

func send_lucy_sync(to = "peers"):
	if not Network.GAME_MASTER: return
	Network._send_P2P_Packet({"type": "lucy_packet"}, to, Network.CHANNELS.GAME_STATE)

func get_player() -> Actor:
	for p in get_tree().get_nodes_in_group("player"):
		if p.controlled and p.owner_id == Network.STEAM_ID:
			return p
	return null

func punched(puncher_id, type):
	print("[LUCY] punch from ", Network._get_username_from_id(puncher_id))
	if not do_punchback: return
	if puncher_id == 0 or puncher_id == Network.STEAM_ID: return
	print("[LUCY] punching back...")
	Network._send_P2P_Packet({"type": "player_punch", "from_pos": get_player().global_transform.origin, "punch_type": type, "nya": "nya"}, str(puncher_id), 2, Network.CHANNELS.ACTOR_ACTION)

func new_player(id):
	print("[LUCY] new player!")
	if server_join_message.empty() or not Network.GAME_MASTER: return
	print("[LUCY] sending join message")
	var bb_msg = bbcode_process(server_join_message)
	lucy_send_message(bb_msg.fin, bb_msg.stripped, false)
	send_lucy_sync(str(id))

func _on_enter(node: Node):
	if node.name == "main_menu":
		lucys_menu = LUCYS_MENU_SCENE.instance()
		lucys_menu.MANAGER = self
		node.add_child(lucys_menu)
		ingame = false
		lucys_menu.setup()
	if node.name == "playerhud":
		lucys_menu = LUCYS_MENU_SCENE.instance()
		lucys_menu.MANAGER = self
		node.add_child(lucys_menu)
		ingame = true
		self.allow_intrusive_bbcode = allow_intrusive_bbcode
		lucys_menu.setup()

const save_keys = [
	"do_punchback", "allow_bbcode",
	"custom_server_name", "server_join_message",
	"custom_color_enabled", "custom_color",
	"log_messages", "custom_name",
	"allow_intrusive_bbcode"
]

func load_settings():
	print("[LUCY] Loading settings")
	var file = File.new()
	if file.open(OS.get_executable_path().get_base_dir().plus_file("GDWeave/configs/LucysTools.json"),File.READ) == OK:
		var parse = JSON.parse(file.get_as_text())
		file.close()
		var result = parse.result
		# trigger setters
		for key in result.keys():
			if key in save_keys: self[key] = result[key]

func save_settings():
	print("[LUCY] Saving settings")
	
	custom_color = Color(custom_color).to_html()
	
	var settings = {}
	for key in save_keys:
		settings[key] = self[key]
	
	var file = File.new()
	if file.open(OS.get_executable_path().get_base_dir().plus_file("GDWeave/configs/LucysTools.json"),File.WRITE) == OK:
		file.store_string(JSON.print(settings))
		file.close()

func _exit_tree():
	save_settings()
