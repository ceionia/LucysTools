using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;

namespace LucysTools;


public class Mod : IMod {
    public static IModInterface ModInterface;
    public Config Config;

    public Mod(IModInterface modInterface) {
        modInterface.Logger.Information("Lucy was here :3");
        ModInterface = modInterface;
        modInterface.RegisterScriptMod(new LucysNetFixes());
        modInterface.RegisterScriptMod(new LucysChatChanges());
    }

    public void Dispose() {
        // Cleanup anything you do here
    }
}

public class LucysChatChanges : IScriptMod
{
    bool IScriptMod.ShouldRun(string path) => path == "res://Scenes/HUD/playerhud.gdc";

    Func<Token,bool>[] start_sendmsg = {
        t => t.Type == TokenType.PrFunction,
        t => t is IdentifierToken {Name: "_send_message"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.ParenthesisClose,
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline,
    };
    Func<Token,bool>[] sendmsg_openbracket = {
        t => t is IdentifierToken {Name: "color"},
        t => t.Type == TokenType.Period,
        t => t is IdentifierToken {Name: "to_html"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t.Type == TokenType.ParenthesisClose,
        t => t.Type == TokenType.Newline,
        t => t.Type == TokenType.Newline,
        t => t.Type == TokenType.Newline,
    };
    Func<Token,bool>[] sendmsg_closebracket = {
        t => t is IdentifierToken {Name: "text"},
        t => t.Type == TokenType.OpAssign,
        t => t is IdentifierToken {Name: "text"},
        t => t.Type == TokenType.Period,
        t => t is IdentifierToken {Name: "replace"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t is ConstantToken {Value:StringVariant{Value: "["}},
        t => t.Type == TokenType.Comma,
        t => t.Type == TokenType.Constant,
        t => t.Type == TokenType.ParenthesisClose,
        t => t.Type == TokenType.Newline,
    };
    Func<Token,bool>[] sendmsg_breakdownbracket = {
        t => t.Type == TokenType.CfElif,
        t => t is IdentifierToken {Name: "line"},
        t => t.Type == TokenType.Period,
        t => t is IdentifierToken {Name: "begins_with"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t is ConstantToken {Value:StringVariant{Value: "["}},
        t => t.Type == TokenType.ParenthesisClose,
    };

    IEnumerable<Token> IScriptMod.Modify(string path, IEnumerable<Token> tokens)
    {
        var start_sendmsg_waiter = new MultiTokenWaiter(start_sendmsg);
        var sendmsg_openbracket_waiter = new MultiTokenWaiter(sendmsg_openbracket);
        var sendmsg_closebracket_waiter = new MultiTokenWaiter(sendmsg_closebracket);
        var sendmsg_breakdownbracket_waiter = new MultiTokenWaiter(sendmsg_breakdownbracket);

        foreach (var token in tokens) {
            if (start_sendmsg_waiter.Check(token)) {
                Mod.ModInterface.Logger.Information("Adding Lucy Chat mod 0...");
                yield return token;

                // if text.beings_with('%'):
                //     text = text.trim_prefix('%')
                //     Network._send_message(text,chat_local)
                //     return
                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("text");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("begins_with");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("%"));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.OpAnd);
                yield return new IdentifierToken("Network");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("LUCY_CHAT_BBCODE");
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.Newline,2);
                yield return new IdentifierToken("text");
                yield return new Token(TokenType.OpAssign);
                yield return new IdentifierToken("text");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("trim_prefix");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("%"));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline,2);
                yield return new IdentifierToken("Network");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("_send_message");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken("text");
                yield return new Token(TokenType.Comma);
                yield return new IdentifierToken("chat_local");
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline,2);
                yield return new Token(TokenType.CfReturn);
                yield return new Token(TokenType.Newline,1);
            } else if (sendmsg_openbracket_waiter.Check(token)) {
                Mod.ModInterface.Logger.Information("Adding Lucy Chat mod 1...");
                yield return token;

                yield return new Token(TokenType.CfIf);
                yield return new Token(TokenType.OpNot);
                yield return new IdentifierToken("Network");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("LUCY_CHAT_BBCODE");
                yield return new Token(TokenType.Colon);
            } else if (sendmsg_closebracket_waiter.Check(token)) {
                Mod.ModInterface.Logger.Information("Adding Lucy Chat mod 2...");
                yield return token;

                yield return new Token(TokenType.CfIf);
                yield return new Token(TokenType.OpNot);
                yield return new IdentifierToken("Network");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("LUCY_CHAT_BBCODE");
                yield return new Token(TokenType.Colon);
            } else if (sendmsg_breakdownbracket_waiter.Check(token)) {
                Mod.ModInterface.Logger.Information("Adding Lucy Chat mod 3...");
                yield return token;

                yield return new Token(TokenType.OpAnd);
                yield return new ConstantToken(new BoolVariant(false));
            } else {
                yield return token;
            }
        }
    }
}

public class LucysNetFixes : IScriptMod {
    bool IScriptMod.ShouldRun(string path) => path == "res://Scenes/Singletons/SteamNetwork.gdc";

    Func<Token,bool>[] original_kick = {
        //t => t.Type == TokenType.Constant && t.AssociatedData == 158, // kick
        t => t is ConstantToken {Value:StringVariant{Value: "kick"}},
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline
    };

    Func<Token,bool>[] original_ban = {
        //t => t.Type == TokenType.Constant && t.AssociatedData == 160, // ban
        t => t is ConstantToken {Value:StringVariant{Value: "ban"}},
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline
    };

    Func<Token,bool>[] original_punch = {
        t => t is ConstantToken {Value:StringVariant{Value: "player_punch"}},
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline
    };

    Func<Token,bool>[] original_msg = {
        //t => t.Type == TokenType.Constant && t.AssociatedData == 139, // message
        t => t is ConstantToken {Value:StringVariant{Value: "message"}},
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline
    };

    Func<Token,bool>[] original_read_all = {
        t => t.Type == TokenType.PrFunction,
        //t => t.Type == TokenType.Identifier && t.AssociatedData == 69, // _read_all_P2P_packets
        t => t is IdentifierToken {Name: "_read_all_P2P_packets"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.OpAssign,
        t => t.Type == TokenType.Constant,
    };

    Func<Token,bool>[] original_process = {
        t => t.Type == TokenType.PrFunction,
        //t => t.Type == TokenType.Identifier && t.AssociatedData == 63, // _process
        t => t is IdentifierToken {Name: "_process"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.ParenthesisClose,
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline,
        t => t.Type == TokenType.CfIf,
        t => t.Type == TokenType.OpNot,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.CfReturn,
        t => t.Type == TokenType.Newline,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.Period,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.ParenthesisOpen,
        t => t.Type == TokenType.ParenthesisClose,
        t => t.Type == TokenType.Newline,
        t => t.Type == TokenType.CfIf,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.OpGreater,
        t => t.Type == TokenType.Constant,
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline,
    };

    Func<Token,bool>[] original_physics_process = {
        t => t.Type == TokenType.PrFunction,
        //t => t.Type == TokenType.Identifier && t.AssociatedData == 68, // _physics_process
        t => t is IdentifierToken {Name: "_physics_process"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.ParenthesisClose,
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline,
        t => t.Type == TokenType.CfIf,
        t => t.Type == TokenType.OpNot,
        t => t.Type == TokenType.Identifier,
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.CfReturn,
        t => t.Type == TokenType.Newline,
    };

    Func<Token,bool>[] original_globals = {
        t => t.Type == TokenType.PrVar,
        //t => t.Type == TokenType.Identifier && t.AssociatedData == 31, // REPLICATIONS_RECIEVED
        t => t is IdentifierToken {Name: "REPLICATIONS_RECIEVED"},
        t => t.Type == TokenType.OpAssign,
        t => t.Type == TokenType.BracketOpen,
        t => t.Type == TokenType.BracketClose,
        t => t.Type == TokenType.Newline,
    };

    Func<Token,bool>[] original_p2p_test = {
        //t => t.Type == TokenType.Identifier && t.AssociatedData == 71, // PACKET
        t => t is IdentifierToken {Name: "PACKET"},
        t => t.Type == TokenType.Period,
        //t => t.Type == TokenType.Identifier && t.AssociatedData == 241, // empty
        t => t is IdentifierToken {Name: "empty"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t.Type == TokenType.ParenthesisClose,
        t => t.Type == TokenType.Colon,
        t => t.Type == TokenType.Newline,
        t => t.Type == TokenType.BuiltInFunc,
        t => t.Type == TokenType.ParenthesisOpen,
        t => t.Type == TokenType.Constant,
        t => t.Type == TokenType.ParenthesisClose,
        t => t.Type == TokenType.Newline,
    };

    Func<Token,bool>[] original_setlobbyname = {
        t => t is IdentifierToken {Name: "setLobbyData"},
        t => t.Type == TokenType.ParenthesisOpen,
        t => t is IdentifierToken {Name: "lobby_id"},
        t => t.Type == TokenType.Comma,
        t => t.Type == TokenType.Constant,
        t => t.Type == TokenType.Comma,
        t => t.Type == TokenType.BuiltInFunc,
        t => t.Type == TokenType.ParenthesisOpen,
        t => t is IdentifierToken {Name: "STEAM_USERNAME"},
        t => t.Type == TokenType.ParenthesisClose,
    };

    IEnumerable<Token> IScriptMod.Modify(string path, IEnumerable<Token> tokens) {
        var kick_waiter = new MultiTokenWaiter(original_kick);
        var ban_waiter = new MultiTokenWaiter(original_ban);
        var msg_waiter = new MultiTokenWaiter(original_msg);
        var read_all_waiter = new MultiTokenWaiter(original_read_all);
        var process_waiter = new MultiTokenWaiter(original_process);
        var physics_process_waiter = new MultiTokenWaiter(original_physics_process);
        var globals_waiter = new MultiTokenWaiter(original_globals);
        var p2p_test_waiter = new MultiTokenWaiter(original_p2p_test);
        var setlobbyname_waiter = new MultiTokenWaiter(original_setlobbyname);
        var punch_waiter = new MultiTokenWaiter(original_punch);

        foreach (var token in tokens) {
            if (globals_waiter.Check(token)) {
                Mod.ModInterface.Logger.Information("Adding Lucy Network globals...");
                yield return token;

                // var LUCY_PACKETS_READ = 0
                // var LUCY_BULK_FULL_TIMER = 0
                // var LUCY_FRAME_PACKETS = 32
                // var LUCY_BULK_PACKETS = 128
                // var LUCY_BULK_INTERVAL = 0.8
                // var LUCY_BULK_FULL_INTERVAL = 6.4
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_PACKETS_READ");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.Newline, 0);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_BULK_FULL_TIMER");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.Newline, 0);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_FRAME_PACKETS");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(32));
                yield return new Token(TokenType.Newline, 0);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_BULK_PACKETS");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(128));
                yield return new Token(TokenType.Newline, 0);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_BULK_INTERVAL");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new RealVariant(0.8));
                yield return new Token(TokenType.Newline, 0);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_BULK_FULL_INTERVAL");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new RealVariant(6.4));
                yield return new Token(TokenType.Newline, 0);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_CHAT_BBCODE");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new BoolVariant(false));
                yield return new Token(TokenType.Newline, 0);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_SRV_NAME");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new StringVariant(""));
                yield return new Token(TokenType.Newline, 0);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("LUCY_PUNCHED_ME");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.Newline, 0);
            } else if (p2p_test_waiter.Check(token)) {
                yield return token;

                // check if packet had sender?
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("packet_sender");
                yield return new Token(TokenType.OpAssign);
                yield return new IdentifierToken("PACKET");
                yield return new Token(TokenType.BracketOpen);
                yield return new ConstantToken(new StringVariant("steam_id_remote"));
                yield return new Token(TokenType.BracketClose);
                yield return new Token(TokenType.Newline, 2);
            } else if (kick_waiter.Check(token)) {
                Mod.ModInterface.Logger.Information("Adding Lucy Network kick fixes...");
                yield return token;

                // print("[KICK]")
                yield return new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint);
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("[KICK]"));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 4);

                // if GAME_MASTER: return
                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("GAME_MASTER");
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.CfReturn);
                yield return new Token(TokenType.Newline, 4);

                // if packet_sender != KNOWN_GAME_MASTER: return
                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("packet_sender");
                yield return new Token(TokenType.OpNotEqual);
                yield return new IdentifierToken("KNOWN_GAME_MASTER");
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.CfReturn);
                yield return new Token(TokenType.Newline, 4);
            } else if (ban_waiter.Check(token)) { 
                Mod.ModInterface.Logger.Information("Adding Lucy Network ban fixes...");
                yield return token;

                // print("[BAN]")
                yield return new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint);
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("[BAN]"));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 4);

                // if GAME_MASTER: return
                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("GAME_MASTER");
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.CfReturn);
                yield return new Token(TokenType.Newline, 4);

                // if packet_sender != KNOWN_GAME_MASTER: return
                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("packet_sender");
                yield return new Token(TokenType.OpNotEqual);
                yield return new IdentifierToken("KNOWN_GAME_MASTER");
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.CfReturn);
                yield return new Token(TokenType.Newline, 4);
            } else if (msg_waiter.Check(token)) { 
                Mod.ModInterface.Logger.Information("Adding Lucy Network msg fixes...");
                yield return token;

                // print("[msg ", _get_username_from_id(packet_sender), "] ", DATA.message)
                yield return new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint);
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("[msg "));
                yield return new Token(TokenType.Comma);
                yield return new IdentifierToken("_get_username_from_id");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken("packet_sender");
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Comma);
                yield return new ConstantToken(new StringVariant("] "));
                yield return new Token(TokenType.Comma);
                yield return new IdentifierToken("DATA");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("message");
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 4);
            } else if (read_all_waiter.Check(token)) { 
                Mod.ModInterface.Logger.Information("Adding Lucy Network _read_all_P2P_packets fixes...");
                // func _read_all_P2P_packets(channel = 0
                yield return token;

                // Our new function
                //
                // func _read_all_P2P_packets(channel = 0, limit = 64):
                //     var read_count = 0
                //     while Steam.getAvailableP2PPacketSize(channel) > 0 and read_count < limit:
                //         _read_P2P_Packet(channel)
                //         read_count += 1
                //     LUCY_PACKETS_READ += read_count
                //     return
                //     ...old code
                yield return new Token(TokenType.Comma);
                yield return new IdentifierToken("limit");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(64));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.Newline, 1);

                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("read_count");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.Newline, 1);

                yield return new Token(TokenType.CfWhile);
                yield return new IdentifierToken("Steam");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("getAvailableP2PPacketSize");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken("channel");
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.OpGreater);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.OpAnd);
                yield return new IdentifierToken("read_count");
                yield return new Token(TokenType.OpLess);
                yield return new IdentifierToken("limit");
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.Newline, 2);

                yield return new IdentifierToken("_read_P2P_Packet");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken("channel");
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 2);

                yield return new IdentifierToken("read_count");
                yield return new Token(TokenType.OpAssignAdd);
                yield return new ConstantToken(new IntVariant(1));
                yield return new Token(TokenType.Newline, 1);

                yield return new IdentifierToken("LUCY_PACKETS_READ");
                yield return new Token(TokenType.OpAssignAdd);
                yield return new IdentifierToken("read_count");
                yield return new Token(TokenType.Newline, 0);

                // Give old function new signature, it'll come after in token stream
                yield return new Token(TokenType.PrFunction);
                yield return new IdentifierToken("_old_read_all_P2P_packets");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken("channel");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(0));
            } else if (process_waiter.Check(token)) { 
                Mod.ModInterface.Logger.Information("Adding Lucy Network _process fixes...");
                // func _process(delta):
                //     if not STEAM_ENABLED: return
                //     Steam.run_callbacks()
                //     if STEAM_LOBBY_ID > 0:
                //         
                yield return token;

                // better code
                //     if STEAM_LOBBY_ID > 0:
                //         for i in 3: _read_all_P2P_packets(i,LUCY_FRAME_PACKETS)
                //     return
                //     if false:
                yield return new Token(TokenType.CfFor);
                yield return new IdentifierToken("i");
                yield return new Token(TokenType.OpIn);
                yield return new ConstantToken(new IntVariant(3));
                yield return new Token(TokenType.Colon);
                yield return new IdentifierToken("_read_all_P2P_packets");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken("i");
                yield return new Token(TokenType.Comma);
                yield return new IdentifierToken("LUCY_FRAME_PACKETS");
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 1);

                yield return new Token(TokenType.CfReturn);
                yield return new Token(TokenType.Newline, 1);
                yield return new Token(TokenType.CfIf);
                yield return new ConstantToken(new BoolVariant(false));
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.Newline, 2);
            } else if (physics_process_waiter.Check(token)) { 
                Mod.ModInterface.Logger.Information("Adding Lucy Network _physics_process fixes...");
                // func _physics_process(delta):
                //     if not STEAM_ENABLED: return
                //
                yield return token;

                // better code
                //     var do_print = false
                //     BULK_PACKET_READ_TIMER -= delta
	            //     if BULK_PACKET_READ_TIMER <= 0:
		        //         print("Bulk Reading Packets.")
                //         for i in 3: _read_all_P2P_packets(i,LUCY_BULK_PACKETS)
                //         BULK_PACKET_READ_TIMER = LUCY_BULK_INTERVAL
                //         do_print = true
                //     LUCY_BULK_FULL_TIMER -= delta
                //     if LUCY_BULK_FULL_TIMER <= 0:
                //         print("Reading all packets.")
                //         for i in 3: _read_all_P2P_packets(i,1000000)
                //         LUCY_BULK_FULL_TIMER = LUCY_BULK_FULL_INTERVAL
                //         do_print = true
                //     if do_print:
                //         print("PACKETS ", LUCY_PACKETS_READ)
                //         LUCY_PACKETS_READ = 0
                //     return

                //     var do_print = false
                //     BULK_PACKET_READ_TIMER -= delta
	            //     if BULK_PACKET_READ_TIMER <= 0:
		        //         print("Bulk Reading Packets.")
                //         for i in 3: _read_all_P2P_packets(i,LUCY_BULK_PACKETS)
                //         BULK_PACKET_READ_TIMER = LUCY_BULK_INTERVAL
                //         do_print = true
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("do_print");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new BoolVariant(false));
                yield return new Token(TokenType.Newline, 1);

                yield return new IdentifierToken("BULK_PACKET_READ_TIMER");
                yield return new Token(TokenType.OpAssignSub);
                yield return new IdentifierToken("delta");
                yield return new Token(TokenType.Newline, 1);

                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("BULK_PACKET_READ_TIMER");
                yield return new Token(TokenType.OpLessEqual);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.Newline, 2);

                yield return new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint);
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("Bulk Reading Packets."));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 2);

                yield return new Token(TokenType.CfFor);
                yield return new IdentifierToken("i");
                yield return new Token(TokenType.OpIn);
                yield return new ConstantToken(new IntVariant(3));
                yield return new Token(TokenType.Colon);
                yield return new IdentifierToken("_read_all_P2P_packets");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken("i");
                yield return new Token(TokenType.Comma);
                yield return new IdentifierToken("LUCY_BULK_PACKETS");
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 2);

                yield return new IdentifierToken("BULK_PACKET_READ_TIMER");
                yield return new Token(TokenType.OpAssign);
                yield return new IdentifierToken("LUCY_BULK_INTERVAL");
                yield return new Token(TokenType.Newline, 2);

                yield return new IdentifierToken("do_print");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new BoolVariant(true));
                yield return new Token(TokenType.Newline, 1);

                //     LUCY_BULK_FULL_TIMER -= delta
                //     if LUCY_BULK_FULL_TIMER <= 0:
                //         print("Reading all packets.")
                //         for i in 3: _read_all_P2P_packets(i,1000000)
                //         LUCY_BULK_FULL_TIMER = LUCY_BULK_FULL_INTERVAL
                //         do_print = true
                yield return new IdentifierToken("LUCY_BULK_FULL_TIMER");
                yield return new Token(TokenType.OpAssignSub);
                yield return new IdentifierToken("delta");
                yield return new Token(TokenType.Newline, 1);

                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("LUCY_BULK_FULL_TIMER");
                yield return new Token(TokenType.OpLessEqual);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.Newline, 2);

                yield return new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint);
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("Reading all packets."));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 2);

                yield return new Token(TokenType.CfFor);
                yield return new IdentifierToken("i");
                yield return new Token(TokenType.OpIn);
                yield return new ConstantToken(new IntVariant(3));
                yield return new Token(TokenType.Colon);
                yield return new IdentifierToken("_read_all_P2P_packets");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new IdentifierToken("i");
                yield return new Token(TokenType.Comma);
                yield return new ConstantToken(new IntVariant(1000000));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 2);

                yield return new IdentifierToken("LUCY_BULK_FULL_TIMER");
                yield return new Token(TokenType.OpAssign);
                yield return new IdentifierToken("LUCY_BULK_FULL_INTERVAL");
                yield return new Token(TokenType.Newline, 2);

                yield return new IdentifierToken("do_print");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new BoolVariant(true));
                yield return new Token(TokenType.Newline, 1);

                //     if do_print:
                //         print("PACKETS ", LUCY_PACKETS_READ)
                //         LUCY_PACKETS_READ = 0
                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("do_print");
                yield return new Token(TokenType.Colon);
                yield return new Token(TokenType.Newline, 2);

                yield return new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint);
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("PACKETS "));
                yield return new Token(TokenType.Comma);
                yield return new IdentifierToken("LUCY_PACKETS_READ");
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Newline, 2);

                yield return new IdentifierToken("LUCY_PACKETS_READ");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.Newline, 1);

                //     return
                yield return new Token(TokenType.CfReturn);
                yield return new Token(TokenType.Newline, 1);
            } else if (punch_waiter.Check(token)) { 
                Mod.ModInterface.Logger.Information("Adding Lucy Network punch mod...");
                yield return token;

                yield return new Token(TokenType.CfIf);
                yield return new Token(TokenType.OpNot);
                yield return new IdentifierToken("DATA");
                yield return new Token(TokenType.Period);
                yield return new IdentifierToken("has");
                yield return new Token(TokenType.ParenthesisOpen);
                yield return new ConstantToken(new StringVariant("nya"));
                yield return new Token(TokenType.ParenthesisClose);
                yield return new Token(TokenType.Colon);

                yield return new IdentifierToken("LUCY_PUNCHED_ME");
                yield return new Token(TokenType.OpAssign);
                yield return new IdentifierToken("packet_sender");
                yield return new Token(TokenType.Newline,4);
            } else if (setlobbyname_waiter.Check(token)) { 
                Mod.ModInterface.Logger.Information("Adding Lucy Network lobby name mod...");
                // Steam.setLobbyData(lobby_id, "name", str(STEAM_USERNAME)
                yield return token;

                // Steam.setLobbyData(lobby_id, "name", str(STEAM_USERNAME) if LUCY_SRV_NAME == "" else LUCY_SRV_NAME
                yield return new Token(TokenType.CfIf);
                yield return new IdentifierToken("LUCY_SRV_NAME");
                yield return new Token(TokenType.OpEqual);
                yield return new ConstantToken(new StringVariant(""));
                yield return new Token(TokenType.CfElse);
                yield return new IdentifierToken("LUCY_SRV_NAME");
            } else {
                yield return token;
            }
        }
    }

}
