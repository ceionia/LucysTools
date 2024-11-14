extends Node

const LucysLib_t = preload("res://mods/LucysLib/main.gd")
var LucysLib: LucysLib_t
const BBCode_t = preload("res://mods/LucysLib/bbcode.gd")
const NetManager_t := preload("res://mods/LucysLib/net.gd")

const LUCYS_MENU_SCENE = preload("res://mods/Lucy.LucysTools/lucys_menu.tscn")

var lucys_menu = null
onready var root = get_tree().root

var custom_name_enabled: bool = false
var ingame = false

# config options
var do_punchback: bool = false
var custom_server_name: String = ""
var server_join_message: String = "[color=#5BCEFA]TRAN[/color][color=#F5A9B8]S RIG[/color][color=#ffffff]HTS![/color]"
var custom_color_enabled: bool = false
var custom_color: Color = Color("009cd0") setget set_custom_color
var log_messages: bool = false setget set_log_messages
var custom_name: String = ""
var DEBUG: bool = false setget set_DEBUG
var custom_text_color: Color = Color("00ff00")
var custom_text_color_enabled: bool = false
var lucys_menu_visible: bool = true
var allowed_bb: Array = BBCode_t.DEFAULT_ALLOWED_TYPES setget set_allowed_bb
var custom_lobbycode: String = ""

const SAVE_KEYS = [
	"do_punchback", "allowed_bb",
	"custom_server_name", "server_join_message",
	"custom_color_enabled", "custom_color",
	"log_messages", "custom_name",
	"DEBUG", "custom_text_color",
	"custom_text_color_enabled",
	"lucys_menu_visible", "custom_lobbycode"
]

func bbcode_changes():
	if lucys_menu != null: lucys_menu.update()
func set_allowed_bb(val):
	var f = []
	for v in val:
		if v == BBCode_t.TAG_TYPE.NULL or v == BBCode_t.TAG_TYPE.ROOT:
			continue
		if v in BBCode_t.TAG_TYPE.values():
			f.append(v)
	allowed_bb = f
	LucysLib.ALLOWED_TAG_TYPES = f
	bbcode_changes()
func set_custom_color(val):
	custom_color = Color(val) if Color(val) != Color("d5aa73") else Color("739ed5")
	custom_color.a = 1
func set_log_messages(val):
	log_messages = val
	LucysLib.LOG_MESSAGES = val
func set_DEBUG(val):
	DEBUG = val
	LucysLib.DEBUG = val
	LucysLib.NetManager.DEBUG = val
	LucysLib.BBCode.DEBUG = val

func get_user_color() -> Color:
	var base_color = Color(Globals.cosmetic_data[PlayerData.cosmetics_equipped["primary_color"]]["file"].main_color) * Color(0.95, 0.9, 0.9)
	var color = custom_color if custom_color_enabled else base_color
	return color

# intercept player message send
# we just take over - replicate as 
# much as i can be bothered to
func process_message(text: String, local: bool, player, playerhud):
	if DEBUG:
		var thing = {"text":text,"local":local,"player":player,"playerhud":playerhud,"custom_name":custom_name}
		print("[LUCYSTOOLS process_message] ", thing)
	# is this a host message? (no username)
	if text.begins_with("%") and (Network.GAME_MASTER or Network.PLAYING_OFFLINE):
		text = text.trim_prefix("%")
		var msg := LucysLib.BBCode.parse_bbcode_text(text)
		LucysLib.send_message(msg, Color.aqua, false, null, "peers")
		return
	
	# i don't know why the wag stuff toggles multiple times
	# and applies anywhere in string
	# i'm doing it once.
	if "/wag" in text:
		PlayerData.emit_signal("_wag_toggle")
		text.replace("/wag","")
	# /me has to be at beginning because i say so
	var colon: bool = true
	if text.begins_with("/me "):
		colon = false
		text = text.trim_prefix("/me ")
	
	# process message into bbcode nodes
	var msg := LucysLib.BBCode.parse_bbcode_text(text)
	
	# clamp transparency
	if not (Network.GAME_MASTER or Network.PLAYING_OFFLINE):
		LucysLib.BBCode.clamp_alpha(msg, 0.7)
	
	# get drunk params
	var drunk_chance := 0.0
	var drunk_max := 0
	if is_instance_valid(player):
		drunk_chance = 0.13 * player.drunk_tier
		drunk_max = player.drunk_tier
	
	# spoken text is gonna have different drunk text
	# i don't want to think about this more
	# get bbcode tag so it can get sent to the 
	# same function at least
	# maybe i'll just add a toggle for hicc
	var spoken_msg := LucysLib.BBCode.parse_bbcode_text(msg.get_stripped())
	drunk_text_add(msg, drunk_chance, drunk_max, false)
	drunk_text_add(spoken_msg, drunk_chance, drunk_max, true)
	
	# add text color if it exists
	if custom_text_color_enabled:
		var col_tag: BBCode_t.BBCodeColorTag = BBCode_t.tag_creator(BBCode_t.TAG_TYPE.color, "")
		col_tag.color = custom_text_color
		col_tag.inner = [msg]
		msg = col_tag
	
	# prefix endcap suffix stuff
	if colon:
		msg.inner.push_front("%u: ")
	else:
		msg.inner.push_front("(%u ")
		msg.inner.push_back(")")
	
	var name := LucysLib.BBCode.parse_bbcode_text(custom_name) if custom_name_enabled else null
	if DEBUG:
		print("[LUCYSTOOLS process_message] ", {"name":name,"msg":msg})
	
	LucysLib.send_message(msg, get_user_color(), local, name, "peers")
	var spoken_text := spoken_msg.get_stripped()
	if colon and spoken_text != "": playerhud.emit_signal("_message_sent", spoken_text)

# drunk processing. ouch this sucks
# not quite the same as vanilla
# if people want drunk text that
# works better. i will but. ugh
var line: String = ""
func drunk_text_add(msg: BBCode_t.BBCodeTag, drunk_chance: float, drunk_max: int, do_hicc: bool):
	for index in msg.inner.size():
		if msg.inner[index] is BBCode_t.BBCodeTag:
			drunk_text_add(msg.inner[index], drunk_chance, drunk_max, do_hicc)
		else:
			var lines = msg.inner[index].split(" ")
			var new: String = ""
			var linei: int = 0
			for line in lines:
				for i in drunk_max:
					if randf() >= drunk_chance or line == "": break
					var d_effect = randi() % 5
					var slot = randi() % line.length()
					match d_effect:
						0, 1: line = line.insert(slot, line[slot])
						2: line = line.insert(slot, "'")
						3: line = line.insert(slot, ",")
						4:
							if do_hicc: line = line.insert(slot, " -*HICC*- ")
							break
				if linei > 0: new += " "
				linei += 1
				new += line
			msg.inner[index] = new

func process_packet_player_punch(DATA, PACKET_SENDER, from_host) -> bool:
	# lucy punchback :3
	if not DATA.has("nya"): punched(PACKET_SENDER, DATA["punch_type"])
	# still get punched!
	return false

func _ready():
	print("[LUCY] Loaded LucysTools 0.7.0")
	LucysLib = $"/root/LucysLib"
	load_settings()
	root.connect("child_entered_tree", self, "_on_enter")
	Network.connect("_new_player_join", self, "new_player")
	Steam.connect("lobby_created", self, "inject_lobby_data")
	
	LucysLib.register_bb_msg_support()
	LucysLib.register_log_msg_support()
	LucysLib.NetManager.add_network_processor("player_punch", funcref(self, "process_packet_player_punch"), 10)

func inject_lobby_data(connect, lobby_id):
	if connect != 1: return
	if custom_server_name != "":
		var bb_name := LucysLib.BBCode.parse_bbcode_text(custom_server_name)
		Steam.setLobbyData(lobby_id, "bbcode_lobby_name", bb_name.get_full(LucysLib.ALLOWED_TAG_TYPES))
		Steam.setLobbyData(lobby_id, "lobby_name", bb_name.get_stripped())
	if custom_lobbycode != "":
		Steam.setLobbyData(lobby_id, "code", custom_lobbycode)
		Network.LOBBY_CODE = custom_lobbycode

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
	Network._send_P2P_Packet(
		{"type": "player_punch", "from_pos": get_player().global_transform.origin, "punch_type": type, "nya": "nya"},
		str(puncher_id), 2, Network.CHANNELS.ACTOR_ACTION)

func new_player(id):
	print("[LUCY] new player!")
	if server_join_message.empty() or not Network.GAME_MASTER: return
	print("[LUCY] sending join message")
	var bb_msg := LucysLib.BBCode.parse_bbcode_text(server_join_message)
	LucysLib.send_message(bb_msg, Color.aqua, false, null, "peers")

func _on_enter(node: Node):
	if DEBUG: print("[LUCY] INSTANCING MENU")
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
		lucys_menu.setup()

func load_settings():
	print("[LUCY] Loading settings")
	var file = File.new()
	if file.open(OS.get_executable_path().get_base_dir().plus_file("GDWeave/configs/LucysTools.json"),File.READ) == OK:
		var parse = JSON.parse(file.get_as_text())
		file.close()
		var result = parse.result
		# trigger setters
		for key in result.keys():
			if key in SAVE_KEYS: self[key] = result[key]

func save_settings():
	print("[LUCY] Saving settings")
	
	var settings = {}
	for key in SAVE_KEYS:
		if key in ["custom_color", "custom_text_color"]:
			settings[key] = self[key].to_html()
		else:
			settings[key] = self[key]
	
	var file = File.new()
	if file.open(OS.get_executable_path().get_base_dir().plus_file("GDWeave/configs/LucysTools.json"),File.WRITE) == OK:
		file.store_string(JSON.print(settings))
		file.close()

func _exit_tree():
	save_settings()
