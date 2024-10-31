extends Node

const LUCYS_MENU_SCENE = preload("res://mods/Lucy.LucysTools/lucys_menu.tscn")

var lucys_menu = null
onready var root = get_tree().root

var do_punchback = false setget set_punchback
var allow_bbcode = false setget set_bbcode
var custom_server_name = "" setget set_server_name
var server_join_message = "[color=#5BCEFA]TRAN[/color][color=#F5A9B8]S RIG[/color][color=#ffffff]HTS![/color]" setget set_join_message
var frame_packets = 50 setget set_frame_packets
var bulk_packets = 200 setget set_bulk_packets
var bulk_interval = 1 setget set_bulk_interval
var full_interval = 5 setget set_full_interval

# Patched Network vars
# var LUCY_PACKETS_READ = 0
# var LUCY_BULK_FULL_TIMER = 0
# var LUCY_FRAME_PACKETS = 32
# var LUCY_BULK_PACKETS = 128
# var LUCY_BULK_INTERVAL = 0.8
# var LUCY_BULK_FULL_INTERVAL = 6.4
# var LUCY_CHAT_BBCODE
# var LUCY_SRV_NAME
# var LUCY_PUNCHED_ME

var ingame = false

func set_punchback(punchback):
	do_punchback = punchback
func set_bbcode(bbcode):
	allow_bbcode = bbcode
	Network.LUCY_CHAT_BBCODE = bbcode
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

func _ready():
	print("[LUCY] Loaded LucysTools")
	load_settings()
	root.connect("child_entered_tree", self, "_on_enter")
	Network.connect("_new_player_join", self, "new_player")
	PlayerData.connect("_punched", self, "punched")

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

func _on_enter(node: Node):
	if node.name == "main_menu":
		lucys_menu = LUCYS_MENU_SCENE.instance()
		node.add_child(lucys_menu)
		ingame = false
		lucys_menu.setup(self)
	if node.name == "playerhud":
		lucys_menu = LUCYS_MENU_SCENE.instance()
		node.add_child(lucys_menu)
		ingame = true
		lucys_menu.setup(self)

func load_settings():
	print("[LUCY] Loading settings")
	var file = File.new()
	if file.open(OS.get_executable_path().get_base_dir().plus_file("GDWeave/configs/LucysTools.json"),File.READ) == OK:
		var parse = JSON.parse(file.get_as_text())
		file.close()
		var result = parse.result
		# trigger setters
		self.do_punchback = result.do_punchback
		self.allow_bbcode = result.allow_bbcode
		self.custom_server_name = result.custom_server_name
		self.server_join_message = result.server_join_message
		self.frame_packets = result.frame_packets
		self.bulk_packets = result.bulk_packets
		self.bulk_interval = result.bulk_interval
		self.full_interval = result.full_interval

func save_settings():
	print("[LUCY] Saving settings")
	var settings = {
		"do_punchback": do_punchback,
		"allow_bbcode": allow_bbcode,
		"custom_server_name": custom_server_name,
		"server_join_message": server_join_message,
		"frame_packets": frame_packets,
		"bulk_packets": bulk_packets,
		"bulk_interval": bulk_interval,
		"full_interval": full_interval
	}
	var file = File.new()
	if file.open(OS.get_executable_path().get_base_dir().plus_file("GDWeave/configs/LucysTools.json"),File.WRITE) == OK:
		file.store_string(JSON.print(settings))
		file.close()

func _exit_tree():
	save_settings()
