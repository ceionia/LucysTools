extends Node

const LUCYS_MENU_SCENE = preload("res://mods/Lucy.LucysTools/lucys_menu.tscn")

var lucys_menu = null
onready var root = get_tree().root

var INCERCEPT_MSG: bool = false
var INCERCEPT_SEND_MSG: bool = false

var do_punchback: bool = false
var allow_bbcode: bool = false
var custom_server_name: String = "" setget set_server_name
var server_join_message: String = "[color=#5BCEFA]TRAN[/color][color=#F5A9B8]S RIG[/color][color=#ffffff]HTS![/color]" setget set_join_message

var custom_color_enabled: bool = false
var custom_color: Color = Color("009cd0") setget set_custom_color

var custom_name_enabled: bool = false
var real_custom_name: String = ""
var custom_name: String = "" setget set_custom_name

var allow_intrusive_bbcode: bool = false setget set_allow_intrusive_bbcode
var srv_bbcode: bool = false setget set_srv_bbcode

var log_messages: bool = false

var DEBUG: bool = false

var lucys_menu_visible: bool = true

var custom_text_color: Color = Color("00ff00")
var custom_text_color_enabled: bool = false

var bug_bbcode: bool = false

func set_custom_name(val):
	custom_name = val
	var bb = bbcode_process(val, 256)
	real_custom_name = bb.fin

func set_allow_intrusive_bbcode(bbcode):
	allow_intrusive_bbcode = bbcode
	bbcode_changes()

func set_srv_bbcode(bbcode):
	srv_bbcode = bbcode
	if Network.GAME_MASTER: send_lucy_sync()
	bbcode_changes()

func set_server_name(name):
	custom_server_name = name
	Network.LUCY_SRV_NAME = name
	
func set_join_message(msg):
	server_join_message = msg
	
func set_custom_color(val):
	custom_color = Color(val) if Color(val) != Color("d5aa73") else Color("739ed5")
	custom_color.a = 1

var allowed_tags: Array = ["b", "i", "u", "s", "color"]
var escape_invalid: bool = true
var strip_disallowed: bool = true
var check_alpha: bool = true setget set_check_alpha
var bbcode_matcher = null

var junk_checkers: Dictionary = {}

var alpha_lim := 0.5

var alpha_getter: RegEx = null
func do_alpha_check(junk) -> String:
	if not alpha_getter:
		alpha_getter = RegEx.new()
		alpha_getter.compile("\\s*=\\s*(\\S*)")
	
	var color: Color = Color(alpha_getter.search(junk).get_string(1))
	
	if color.a < alpha_lim:
		color.a = alpha_lim
		return "=#" + color.to_html()
	
	return ""

func set_check_alpha(val):
	check_alpha = val
	if val: junk_checkers["color"] = funcref(self, "do_alpha_check")
	else: junk_checkers.erase("color")

var INNER_MAX_LEN: int = 0

# ouch oof this sucks
func bbcode_process(text, max_len) -> Dictionary:
	var end: int
	var all: String
	var before: String
	var whole_tag: String
	var tag_open: String
	var junk: String
	var tag_close: String
	var tag: String
	var is_close: bool
	var inner_full: String
	var inner_stripped: String
	var prev_full: String
	var prev_stripped: String
	var checked: String
	var to_add_full: String
	var to_add_stripped: String
	var last_tag
	
	if DEBUG:
		var thing = {"max_len":max_len,"allowed_tags":allowed_tags,"strip_disallowed":strip_disallowed,"escape_invalid":escape_invalid}
		print("[BBCODE NEW] processing '", text, "' params ", thing)
	
	bbcode_matcher = RegEx.new()
	bbcode_matcher.compile("(.*?)(\\[(\\w+)([^\\[\\]]*?)\\]|\\[/(\\w+)\\])")
	
	var linear_matches: Array = bbcode_matcher.search_all(text)
	if linear_matches.empty():
		var processed = {"fin": text.replace('[','[lb]'), "stripped": text}
		if DEBUG: print("[BBCODE NEW] processed ", processed)
		return processed
	
	var tag_stack := []
	var full_text_stack := [""]
	var stripped_text_stack := [""]
	
	var last_end: int = 0
	
	# all this popping and pushing sucks. whatever
	for m in linear_matches:
		if DEBUG: print("[MATCH] ", m.strings)
		if DEBUG: print("[STACKS] ", {
				"tag stack": tag_stack,
				"full text": full_text_stack,
				"stripped text": stripped_text_stack
			})
		end = m.get_end()
		if end != -1: last_end = end
		all = m.get_string(0)
		before = m.get_string(1)
		whole_tag = m.get_string(2)
		tag_open = m.get_string(3)
		junk = m.get_string(4)
		tag_close = m.get_string(5)
		tag = tag_open
		is_close = false
		if tag_open == "":
			tag = tag_close
			is_close = true
		
		if is_close:
			# get the tag on the stack
			last_tag = tag_stack.pop_back()
			# get the full text on the stack
			inner_full = full_text_stack.pop_back()
			# get the stripped text on the stack
			inner_stripped = stripped_text_stack.pop_back()
			if last_tag == null:
				if DEBUG: print("[UNOPENED CLOSE]")
				# no tags on stack
				# add stripped tag to all text
				# and go back on stack
				full_text_stack.push_back(inner_full+before.replace('[','[lb]')+"[lb]/"+tag+"]")
				stripped_text_stack.push_back(inner_stripped+before.replace('[','[lb]')+"[lb]/"+tag+"]")
				continue
			elif last_tag[0] == tag:
				if DEBUG: print("[CLOSED TAG]")
				# we have closure.
				# check junk
				if junk_checkers.has(tag):
					checked = junk_checkers[tag].call_func(last_tag[1])
					junk = last_tag[1] if checked == "" else checked
					if DEBUG: print("[BB NEW JUNK] ", junk)
				else:
					junk = last_tag[1]
				# add tag in full text
				# but not in stripped 
				prev_full = full_text_stack.pop_back()
				prev_stripped = stripped_text_stack.pop_back()
				if tag in allowed_tags:
					to_add_full = "["+tag+junk+"]" + inner_full + before.replace('[','[lb]') + "[/"+tag+"]"
					to_add_stripped = inner_stripped + before.replace('[','[lb]')
				else:
					to_add_full = inner_full + before.replace('[','[lb]')
					to_add_stripped = inner_stripped + before.replace('[','[lb]')
				# check length - this sucks but whatever
				# just use the stripped version if it's too long.
				# whatever
				if prev_full.length() + to_add_full.length() > max_len:
					to_add_full = to_add_stripped
				full_text_stack.push_back(prev_full + to_add_full)
				stripped_text_stack.push_back(prev_stripped + to_add_stripped)
				continue
			else:
				if DEBUG: print("[WRONG CLOSE]")
				# open followed by different close
				# escape and add the text to previous on stack 
				prev_full = full_text_stack.pop_back()
				prev_stripped = stripped_text_stack.pop_back()
				full_text_stack.push_back(prev_full + "[lb]"+last_tag[0]+last_tag[1]+"]" + inner_full + before.replace('[','[lb]') + "[lb]/"+tag+"]")
				stripped_text_stack.push_back(prev_stripped + "[lb]"+last_tag[0]+last_tag[1]+"]" + inner_stripped + before.replace('[','[lb]') + "[lb]/"+tag+"]")
				continue
		else:
			# special case
			if tag == "lb" or tag == "rb":
				if DEBUG: print("[LB/RB]")
				# add directly to current inner
				inner_full = full_text_stack.pop_back()
				inner_stripped = stripped_text_stack.pop_back()
				full_text_stack.push_back(inner_full + before.replace('[','[lb]') + whole_tag)
				stripped_text_stack.push_back(inner_stripped + before.replace('[','[lb]') + whole_tag)
				continue
			if DEBUG: print("[OPEN TAG]")
			# add to stack
			tag_stack.push_back([tag, junk])
			# add before text escaped to prev
			inner_full = full_text_stack.pop_back()
			inner_stripped = stripped_text_stack.pop_back()
			full_text_stack.push_back(inner_full + before.replace('[','[lb]'))
			stripped_text_stack.push_back(inner_stripped + before.replace('[','[lb]'))
			# new inner text
			full_text_stack.push_back("")
			stripped_text_stack.push_back("")
			continue
	
	if DEBUG: print("[FINAL STACKS] ", {
			"tag stack": tag_stack,
			"full text": full_text_stack,
			"stripped text": stripped_text_stack
		})
	
	# unroll opens at end
	# TODO probably should write this in as escaped
	# but im getting tired 
	while not tag_stack.empty():
		tag_stack.pop_back()
		full_text_stack.pop_back()
		stripped_text_stack.pop_back()
	
	if DEBUG: print("[LAST END] ", last_end)
	var processed = {"fin": full_text_stack.pop_back(), "stripped": stripped_text_stack.pop_back()}
	# end stuff isnt caught by the regex
	if last_end != 0:
		var end_str = text.substr(last_end).replace('[','[lb]')
		if DEBUG: print("[END STR] ", end_str)
		processed.fin += end_str
		processed.stripped += end_str
	
	if DEBUG: print("[BBCODE NEW] processed ", processed)
	return processed

var ingame = false

func get_user_color() -> Color:
	var base_color = Color(Globals.cosmetic_data[PlayerData.cosmetics_equipped["primary_color"]]["file"].main_color) * Color(0.95, 0.9, 0.9)
	var color = custom_color if custom_color_enabled else base_color
	return color

func safe_message(user_id, color, boring_msg, local, lucy_user, lucy_msg, require_name):
	var msg: String = boring_msg
	if lucy_msg != "": msg = lucy_msg
	var net_name: String = Network._get_username_from_id(user_id).replace('[','').replace(']','')
	var name: String = net_name if lucy_user == "" else lucy_user
	
	if OptionsMenu.chat_filter:
		msg = SwearFilter._filter_string(msg)
	
	msg = msg.replace("%u", "[color=#" + str(color) + "]" + name + "[/color]")
	if DEBUG: print("[MSG B4 PROC] ", msg)
	
	# process message
	var bb_msg = bbcode_process(msg, 512)
	if require_name and not net_name in bb_msg.stripped:
		msg = net_name + ": " + msg
		bb_msg = bbcode_process(msg, 512)
	
	if log_messages:
		var thing = {"user_id":user_id, "steam name":Network._get_username_from_id(user_id),
			"username":name, "color":color,
			"final": bb_msg.fin, "message": boring_msg,
			"bb_user":lucy_user,"bb_msg":lucy_msg}
		print("[MESSAGE] ", thing)
	Network._update_chat(bb_msg.fin, local)

# this is stinky
func process_message(lit_text, final, prefix, suffix, endcap, spoken_text, local, colon, playerhud):
	if log_messages:
		var thing = {
			"lit_text": lit_text, "final": final, "prefix": prefix, "suffix": suffix,
			"endcap": endcap,
			"custom_color_enabled": custom_color_enabled,
			"custom_name_enabled": custom_name_enabled, "allow_bbcode": allow_bbcode,
			"allowed_tags": allowed_tags
		}
		print("process_message ", thing)
	
	if (Network.GAME_MASTER or Network.PLAYING_OFFLINE) and lit_text.begins_with("%"):
		var bb_dat = bbcode_process(lit_text.trim_prefix('%'), 512)
		
		if bug_bbcode:
			print("Using color field...")
			var evil_color = "00ff0000]"\
				+ "[color=#ffeed5]"\
				+ bb_dat.fin\
				+ "[/color]"
			
			if bb_dat.fin != "": 
				lucy_send_message("", "%u", false, evil_color)
			return [true]
		
		lucy_send_message(lit_text.trim_prefix('%'), bb_dat.stripped, false)
		# we sent the message ourself
		return [true]
	
	var msg = final
	var boring_msg = final
	var speak = spoken_text
	if bug_bbcode:
		print("Using color field...")
		var p = bbcode_process(lit_text, 512)
		var name = real_custom_name if custom_name_enabled else Network.STEAM_USERNAME
		var name_color = get_user_color().to_html()
		var text_color = "ffffeed5" if not custom_text_color_enabled else custom_text_color.to_html()
		var evil_color = "00ff0000]"\
			+ "[color=#" + text_color + "]"\
			+ prefix\
			+ "[color=#" + name_color + "]"\
			+ name\
			+ "[/color]"\
			+ endcap + p.fin + suffix\
			+ "[/color]"
		boring_msg = "%u"
		var bb_msg = ""
		var bb_user = ""
		speak = p.stripped
		
		if p.fin != "": 
			lucy_send_message(bb_msg, boring_msg, local, evil_color)
		
		if speak != "" and colon: playerhud.emit_signal("_message_sent", speak)
		return [true]
	elif allow_bbcode:
		var p = bbcode_process(lit_text, 512)
		var txt_col_start = "" if not custom_text_color_enabled else\
			"[color=#"+custom_text_color.to_html()+"]"
		var txt_col_end = "" if not custom_text_color_enabled else\
			"[/color]"
		msg = prefix + "%u" + txt_col_start + endcap + p.fin + suffix + txt_col_end
		boring_msg = prefix + "%u" + endcap + p.stripped + suffix
		speak = p.stripped
		if msg != "": lucy_send_message(msg, boring_msg, local)
		
		if speak != "" and colon: playerhud.emit_signal("_message_sent", speak)
		# we did it ourselves
		return [true]
	
	# return the custom color
	return [false, get_user_color().to_html()]

var LUCYSTOOLS_USERS = []

var i_hate_regex: RegEx = null
func lucy_send_message(message, boring_msg, local = false, evil_color = ""):
	if not Network._message_cap(Network.STEAM_ID):
		Network._update_chat("Sending too many messages too quickly!", false)
		Network._update_chat("Sending too many messages too quickly!", true)
		return
	
	var msg_pos = Network.MESSAGE_ORIGIN.round()
	
	var lucy_user = real_custom_name if custom_name_enabled and not bug_bbcode else ""
	var color = get_user_color().to_html() if not bug_bbcode else evil_color
	
	# idfk
	# the first thing in a color string must be a valid html color,
	# followed optionally by a ] (if people are using bug bbcode)
	if not i_hate_regex:
		i_hate_regex = RegEx.new()
		i_hate_regex.compile("^([a-zA-Z0-9]*)(\\]?)(.*)$")
	var rmatch: RegExMatch = i_hate_regex.search(color)
	var col: Color = rmatch.get_string(1)
	var paren = rmatch.get_string(2)
	var rest = rmatch.get_string(3)
	if paren != "]" and rest != "": paren = "]"
	var idfk = 490 - boring_msg.length() - Network.STEAM_USERNAME.length()
	var ver_rest = bbcode_process(rest, idfk)
	color = col.to_html() + paren + ver_rest.fin.left(idfk)
	
	safe_message(Network.STEAM_ID, color, boring_msg, local, lucy_user, message, false)
	Network._send_P2P_Packet(
		{"type": "message", "message": boring_msg, "color": color, "local": local,
			"position": Network.MESSAGE_ORIGIN, "zone": Network.MESSAGE_ZONE,
			"zone_owner": PlayerData.player_saved_zone_owner,
			"bb_user": lucy_user, "bb_msg": message},
		"peers", 2, Network.CHANNELS.GAME_STATE)
	


func process_read(DATA, PACKET_SENDER, from_host) -> bool:
	match DATA["type"]:
		"lucy_packet":
			print("[LUCY PACKET]")
			if not PACKET_SENDER in LUCYSTOOLS_USERS: LUCYSTOOLS_USERS.append(PACKET_SENDER)
			if Network.GAME_MASTER or not from_host: return true
			if not Network._validate_packet_information(DATA, ["srv_bbcode"], [TYPE_BOOL]): return true
			self.srv_bbcode = DATA["srv_bbcode"]
			return true
		
		"message":
			var has_bb = true
			if not Network._validate_packet_information(DATA,
				["message", "color", "local", "position", "zone", "zone_owner", "bb_user", "bb_msg"],
				[TYPE_STRING, TYPE_STRING, TYPE_BOOL, TYPE_VECTOR3, TYPE_STRING, TYPE_INT, TYPE_STRING, TYPE_STRING]):
				has_bb = false
				if not Network._validate_packet_information(DATA,
					["message", "color", "local", "position", "zone", "zone_owner"],
					[TYPE_STRING, TYPE_STRING, TYPE_BOOL, TYPE_VECTOR3, TYPE_STRING, TYPE_INT]):
					# invalid packet 
					if log_messages:
						print("[MALFORMED MESSAGE] sender: ", Network._get_username_from_id(PACKET_SENDER), "(", PACKET_SENDER + ") " + DATA)
					return true
			
			if has_bb:
				if not PACKET_SENDER in LUCYSTOOLS_USERS:
					LUCYSTOOLS_USERS.append(PACKET_SENDER)
			
			if PlayerData.players_muted.has(PACKET_SENDER) or PlayerData.players_hidden.has(PACKET_SENDER):
				return false
			
			if not Network._message_cap(PACKET_SENDER): return false
			
			var user_id: int = PACKET_SENDER
			var user_color: String = DATA["color"]
			var user_message: String = DATA["message"]
			
			# if host, don't care about visibility
			check_alpha = not from_host
			
			var bb_user: String = ""
			var bb_msg: String = ""
			if has_bb:
				bb_user = DATA["bb_user"]
				bb_msg = DATA["bb_msg"]
						
			if not DATA["local"]:
				safe_message(user_id, user_color, user_message, false, bb_user, bb_msg, not from_host)
			else :
				var dist = DATA["position"].distance_to(Network.MESSAGE_ORIGIN)
				if DATA["zone"] == Network.MESSAGE_ZONE and DATA["zone_owner"] == PlayerData.player_saved_zone_owner:
					if dist < 25.0: safe_message(user_id, user_color, user_message, true, bb_user, bb_msg, not from_host)
			
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
	if allow_intrusive_bbcode and srv_bbcode:
		allowed_tags = [
		"b", "i", "u", "s", "color",
		"wave", "rainbow", "shake", "tornado", "font"]
	else:
		allowed_tags = [
		"b", "i", "u", "s", "color"]
	if lucys_menu != null: lucys_menu.update()

func _ready():
	print("[LUCY] Loaded LucysTools 0.6.1")
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
	Network._send_P2P_Packet({"type": "lucy_packet", "srv_bbcode": srv_bbcode}, to, Network.CHANNELS.GAME_STATE)

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
	var bb_msg = bbcode_process(server_join_message, 512)
	if not bug_bbcode:
		lucy_send_message(bb_msg.fin, bb_msg.stripped, false)
	else:
		var evil_color = "00ff0000]"\
			+ "[color=#ffeed5]"\
			+ bb_msg.fin\
			+ "[/color]"
		
		if bb_msg.fin != "": 
			lucy_send_message("", "%u", false, evil_color)
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
		if not Network.GAME_MASTER and not Network.PLAYING_OFFLINE:
			self.srv_bbcode = false
		lucys_menu.setup()

const save_keys = [
	"do_punchback", "allow_bbcode",
	"custom_server_name", "server_join_message",
	"custom_color_enabled", "custom_color",
	"log_messages", "custom_name",
	"allow_intrusive_bbcode", "DEBUG",
	"bug_bbcode", "custom_text_color",
	"custom_text_color_enabled",
	"lucys_menu_visible"
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
	
	var settings = {}
	for key in save_keys:
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
