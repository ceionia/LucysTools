extends Node

const LUCYS_MENU_SCENE = preload("res://mods/Lucy.LucysTools/lucys_menu.tscn")

var lucys_menu = null
onready var root = get_tree().root

var do_punchback = false
var allow_bbcode = false
var custom_server_name = "" setget set_server_name
var server_join_message = "[color=#5BCEFA]TRAN[/color][color=#F5A9B8]S RIG[/color][color=#ffffff]HTS![/color]" setget set_join_message
var frame_packets = 50 setget set_frame_packets
var bulk_packets = 200 setget set_bulk_packets
var bulk_interval = 1 setget set_bulk_interval
var full_interval = 5 setget set_full_interval

var custom_color_enabled = false
var custom_color = Color("009cd0") setget set_custom_color

var custom_name_enabled = false
var custom_name = ""

var allow_intrusive_bbcode = false setget set_allow_intrusive_bbcode
var srv_allow_bbcode = false setget set_srv_bbcode

var log_messages = false setget set_log_messages


var allowed_tags = ["b", "i", "u", "s", "color"]
var escape_invalid = true
var bbcode_matcher = null

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

# Patched Network vars
# var LUCY_PACKETS_READ = 0
# var LUCY_BULK_FULL_TIMER = 0
# var LUCY_FRAME_PACKETS = 32
# var LUCY_BULK_PACKETS = 128
# var LUCY_BULK_INTERVAL = 0.8
# var LUCY_BULK_FULL_INTERVAL = 6.4
# var LUCY_SRV_NAME
# var LUCY_PUNCHED_ME
# var LUCY_INSTANCE_SENDER
# var LUCY_LOG_MESSAGES

var ingame = false

func process_message(lit_text, final_text, prefix, suffix, endcap, username, final_color, spoken_text):
	var thing = {
		"lit_text": lit_text, "final_text": final_text, "prefix": prefix, "suffix": suffix,
		"endcap": endcap, "username": username, "final_color": final_color,
		"srv_allow_bbcode": srv_allow_bbcode, "custom_color_enabled": custom_color_enabled,
		"custom_name_enabled": custom_name_enabled, "allow_bbcode": allow_bbcode,
		"allowed_tags": allowed_tags
	}
	#print("FUCK ", thing)
	if srv_allow_bbcode and lit_text.begins_with("%"):
		return [lit_text.trim_prefix('%'), spoken_text]
	
	var name = custom_name if custom_name_enabled else username
	var color = custom_color if custom_color_enabled else final_color
	var msg = final_text
	var speak = spoken_text
	if allow_bbcode:
		var p = bbcode_process(lit_text)
		if not p.tags.empty():
			msg = p.fin
			speak = p.stripped
	return [
		prefix + "[color=#" + str(color.to_html()) + "]" + name + endcap + msg + suffix,
		speak
	]

func bbcode_changes():
	if srv_allow_bbcode and allow_intrusive_bbcode:
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
	if Network.GAME_MASTER or not ingame:
		self.srv_allow_bbcode = bbcode
	else: bbcode_changes()
func set_srv_bbcode(bbcode):
	if Network.GAME_MASTER and not Network.PLAYING_OFFLINE: send_server_sync_actor()
	srv_allow_bbcode = bbcode
	bbcode_changes()
func set_server_name(name):
	custom_server_name = name
	Network.LUCY_SRV_NAME = name
func set_join_message(msg):
	server_join_message = msg
func set_frame_packets(val):
	frame_packets = val
	Network.LUCY_FRAME_PACKETS = val
func set_bulk_packets(val):
	bulk_packets = val
	Network.LUCY_BULK_PACKETS = val
func set_bulk_interval(val):
	bulk_interval = val
	Network.LUCY_BULK_INTERVAL = val
	Network.BULK_PACKET_READ_TIMER = 0
func set_full_interval(val):
	full_interval = val
	Network.LUCY_BULK_FULL_INTERVAL = val
	Network.LUCY_BULK_FULL_TIMER = 0
func set_custom_color(val):
	custom_color = Color(val) if Color(val) != Color("d5aa73") else Color("739ed5")
	custom_color.a = 1

func _ready():
	print("[LUCY] Loaded LucysTools")
	load_settings()
	root.connect("child_entered_tree", self, "_on_enter")
	Network.connect("_new_player_join", self, "new_player")
	PlayerData.connect("_punched", self, "punched")
	Network.connect("_instance_actor", self, "_instance_actor")

func send_server_sync_actor(to = "peers"):
	if not Network.GAME_MASTER: return
	var dict = {"actor_type": "lucy_fake_actor", "at": Vector3.ZERO, "zone": "", "actor_id": 0, "creator_id": Network.STEAM_ID, "data": {
		"allow_bbcode": allow_bbcode
	}}
	Network._send_P2P_Packet({"type": "instance_actor", "params": dict}, to, 2)

func _instance_actor(dict):
	if dict["actor_type"] != "lucy_fake_actor": return
	var sender = Network.LUCY_INSTANCE_SENDER
	Network.LUCY_INSTANCE_SENDER = 0
	if sender != Network.KNOWN_GAME_MASTER or Network.GAME_MASTER: return
	var data = dict["data"]
	self.srv_allow_bbcode = data["allow_bbcode"]

func get_player() -> Actor:
	for p in get_tree().get_nodes_in_group("player"):
		if p.controlled and p.owner_id == Network.STEAM_ID:
			return p
	return null

func punched(from, type):
	print("[LUCY] punched!")
	if not do_punchback: return
	if Network.LUCY_PUNCHED_ME == 0 or Network.LUCY_PUNCHED_ME == Network.STEAM_ID: return
	var punched_me = null
	for p in get_tree().get_nodes_in_group("player"):
		if p.owner_id == Network.LUCY_PUNCHED_ME: punched_me = p
	if punched_me == null: return
	if punched_me.controlled: return
	print("[LUCY] punching back...")
	Network.LUCY_PUNCHED_ME = 0
	Network._send_P2P_Packet({"type": "player_punch", "from": get_player().global_transform.origin, "player": Network.STEAM_ID, "punch_type": type, "nya": "nya"}, str(punched_me.owner_id), 2)
	

func new_player(id):
	print("[LUCY] new player!")
	if server_join_message.empty() or not Network.GAME_MASTER: return
	print("[LUCY] sending join message")
	Network._send_message(server_join_message)
	send_server_sync_actor(str(id))

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
		# retrigger setter
		self.srv_allow_bbcode = false
		self.allow_intrusive_bbcode = allow_intrusive_bbcode
		lucys_menu.setup()

const save_keys = [
	"do_punchback", "allow_bbcode",
	"custom_server_name", "server_join_message",
	"frame_packets", "bulk_packets",
	"bulk_interval", "full_interval",
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
