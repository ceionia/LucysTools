using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;

namespace LucysTools;


public class Mod : IMod {
    public static IModInterface ModInterface;

    public Mod(IModInterface modInterface) {
        modInterface.Logger.Information("Lucy was here :3");
        ModInterface = modInterface;
        modInterface.RegisterScriptMod(new LucysChatChanges());
        modInterface.RegisterScriptMod(new LucysNetFixes());
    }

    public void Dispose(){}
}

public record CodeChange {
    public required String name;
    public required Func<Token, bool>[] multitoken_prefix;
    public required Token[] code_to_add;
}

public class LucysChatChanges : IScriptMod
{
    bool IScriptMod.ShouldRun(string path) => path == "res://Scenes/HUD/playerhud.gdc";

    CodeChange[] changes = {
        new CodeChange {
            name = "save lit text",
            // color.to_html()
            //
            // END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is IdentifierToken {Name: "color"},
                t => t.Type == TokenType.Period,
                t => t is IdentifierToken {Name: "to_html"},
                t => t.Type == TokenType.ParenthesisOpen,
                t => t.Type == TokenType.ParenthesisClose,
                t => t.Type == TokenType.Newline,
                t => t.Type == TokenType.Newline,
            },
            // var lit_text = text
            code_to_add = new Token[] {
                new Token(TokenType.PrVar),
                new IdentifierToken("lit_text"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("text"),
                new Token(TokenType.Newline, 1),
            }
        },

        new CodeChange {
            name = "chat bbcode",
            // endcap + final_text + suffix
            // END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is IdentifierToken {Name: "endcap"},
                t => t.Type == TokenType.OpAdd,
                t => t is IdentifierToken {Name: "final_text"},
                t => t.Type == TokenType.OpAdd,
                t => t is IdentifierToken {Name: "suffix"},
                t => t.Type == TokenType.Newline,
            },
            // if $"/root/LucyLucysTools":
            //     var tmp = $"/root/LucyLucysTools".process_message(lit_text, final_text, prefix, suffix, endcap, username, spoken_text)
            //     final = tmp[0]
            //     spoken_text = tmp[1]
            // END
            code_to_add = new Token[] {
                new Token(TokenType.CfIf),
                new Token(TokenType.Dollar),
                new ConstantToken(new StringVariant("/root/LucyLucysTools")),
                new Token(TokenType.Colon),
                new Token(TokenType.Newline, 2),

                new Token(TokenType.PrVar),
                new IdentifierToken("tmp"),
                new Token(TokenType.OpAssign),
                new Token(TokenType.Dollar),
                new ConstantToken(new StringVariant("/root/LucyLucysTools")),
                new Token(TokenType.Period),
                new IdentifierToken("process_message"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("lit_text"),
                new Token(TokenType.Comma),
                new IdentifierToken("final_text"),
                new Token(TokenType.Comma),
                new IdentifierToken("prefix"),
                new Token(TokenType.Comma),
                new IdentifierToken("suffix"),
                new Token(TokenType.Comma),
                new IdentifierToken("endcap"),
                new Token(TokenType.Comma),
                new IdentifierToken("username"),
                new Token(TokenType.Comma),
                new IdentifierToken("final_color"),
                new Token(TokenType.Comma),
                new IdentifierToken("spoken_text"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("final"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("tmp"),
                new Token(TokenType.BracketOpen),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.BracketClose),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("spoken_text"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("tmp"),
                new Token(TokenType.BracketOpen),
                new ConstantToken(new IntVariant(1)),
                new Token(TokenType.BracketClose),
                new Token(TokenType.Newline, 1),
            }
        },
    };

    IEnumerable<Token> IScriptMod.Modify(string path, IEnumerable<Token> tokens)
    {
        var pending_changes = changes
            .Select(c => (c, new MultiTokenWaiter(c.multitoken_prefix)))
            .ToList();

        // I'm sure there's a better way to do this
        // with list comprehension stuff, but my 
        // C# is too rusty
        foreach (var token in tokens) {
            var had_change = false;
            foreach (var (change, waiter) in pending_changes) {
                if (waiter.Check(token)) {
                    Mod.ModInterface.Logger.Information($"Adding Lucy Chat mod {change.name}");

                    yield return token;
                    foreach (var t in change.code_to_add) yield return t;

                    had_change = true;
                    break;
                }
            }
            if (!had_change) yield return token;
        }
    }
}

public class LucysNetFixes : IScriptMod {
    bool IScriptMod.ShouldRun(string path) => path == "res://Scenes/Singletons/SteamNetwork.gdc";

    CodeChange[] changes = {
        new CodeChange {
            name = "send_message channel 2",
            // MESSAGE_ZONE, "zone_owner": PlayerData.player_saved_zone_owner}, "peers", 2)
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is IdentifierToken {Name: "MESSAGE_ZONE"},
                t => t.Type == TokenType.Comma,
                t => t is ConstantToken {Value:StringVariant{Value: "zone_owner"}},
                t => t.Type == TokenType.Colon,
                t => t is IdentifierToken {Name: "PlayerData"},
                t => t.Type == TokenType.Period,
                t => t is IdentifierToken {Name: "player_saved_zone_owner"},
                t => t.Type == TokenType.CurlyBracketClose,
                t => t.Type == TokenType.Comma,
                t => t is ConstantToken {Value:StringVariant{Value: "peers"}},
                t => t.Type == TokenType.Comma,
                t => t is ConstantToken {Value:IntVariant{Value: 2}},
            },
            // , 2 END
            code_to_add = new Token[] {
                new Token(TokenType.Comma),
                new ConstantToken(new IntVariant(2)),
            }
        },

        new CodeChange {
            name = "instance_actor",
            // "instance_actor":
            //     END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is ConstantToken {Value:StringVariant{Value: "instance_actor"}},
                t => t.Type == TokenType.Colon,
                t => t.Type == TokenType.Newline,
            },
            //     LUCY_INSTANCE_SENDER = packet_sender
            //     END
            code_to_add = new Token[] {
                new IdentifierToken("LUCY_INSTANCE_SENDER"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.Newline, 4),
            }
        },

        new CodeChange {
            name = "kick",
            // "kick":
            //     END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is ConstantToken {Value:StringVariant{Value: "kick"}},
                t => t.Type == TokenType.Colon,
                t => t.Type == TokenType.Newline
            },
            // if LUCY_LOG_MESSAGES: print("[KICK from", _get_username_from_id(packet_sender), " (", packet_sender, ")]")
            // if GAME_MASTER: return
            // if packet_sender != KNOWN_GAME_MASTER: return
            // END
            code_to_add = new Token[] {
                new Token(TokenType.CfIf),
                new IdentifierToken("LUCY_LOG_MESSAGES"),
                new Token(TokenType.Colon),
                new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint),
                new Token(TokenType.ParenthesisOpen),
                new ConstantToken(new StringVariant("[KICK from ")),
                new Token(TokenType.Comma),
                new IdentifierToken("_get_username_from_id"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Comma),
                new ConstantToken(new StringVariant(" (")),
                new Token(TokenType.Comma),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.Comma),
                new ConstantToken(new StringVariant(")]")),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 4),

                new Token(TokenType.CfIf),
                new IdentifierToken("GAME_MASTER"),
                new Token(TokenType.Colon),
                new Token(TokenType.CfReturn),
                new Token(TokenType.Newline, 4),

                new Token(TokenType.CfIf),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.OpNotEqual),
                new IdentifierToken("KNOWN_GAME_MASTER"),
                new Token(TokenType.Colon),
                new Token(TokenType.CfReturn),
                new Token(TokenType.Newline, 4),
            }
        },

        new CodeChange {
            name = "ban",
            // "ban":
            //     END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is ConstantToken {Value:StringVariant{Value: "ban"}},
                t => t.Type == TokenType.Colon,
                t => t.Type == TokenType.Newline
            },
            // if LUCY_LOG_MESSAGES: print("[BAN from", _get_username_from_id(packet_sender), " (", packet_sender, ")]")
            // if GAME_MASTER: return
            // if packet_sender != KNOWN_GAME_MASTER: return
            // END
            code_to_add = new Token[] {
                new Token(TokenType.CfIf),
                new IdentifierToken("LUCY_LOG_MESSAGES"),
                new Token(TokenType.Colon),
                new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint),
                new Token(TokenType.ParenthesisOpen),
                new ConstantToken(new StringVariant("[BAN from ")),
                new Token(TokenType.Comma),
                new IdentifierToken("_get_username_from_id"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Comma),
                new ConstantToken(new StringVariant(" (")),
                new Token(TokenType.Comma),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.Comma),
                new ConstantToken(new StringVariant(")]")),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 4),

                new Token(TokenType.CfIf),
                new IdentifierToken("GAME_MASTER"),
                new Token(TokenType.Colon),
                new Token(TokenType.CfReturn),
                new Token(TokenType.Newline, 4),

                new Token(TokenType.CfIf),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.OpNotEqual),
                new IdentifierToken("KNOWN_GAME_MASTER"),
                new Token(TokenType.Colon),
                new Token(TokenType.CfReturn),
                new Token(TokenType.Newline, 4),
            }
        },

        new CodeChange {
            name = "punch",
            // "player_punch":
            //     END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is ConstantToken {Value:StringVariant{Value: "player_punch"}},
                t => t.Type == TokenType.Colon,
                t => t.Type == TokenType.Newline
            },
            //     if not DATA.has("nya"): LUCY_PUNCHED_ME = packet_sender
            //     END
            code_to_add = new Token[] {
                new Token(TokenType.CfIf),
                new Token(TokenType.OpNot),
                new IdentifierToken("DATA"),
                new Token(TokenType.Period),
                new IdentifierToken("has"),
                new Token(TokenType.ParenthesisOpen),
                new ConstantToken(new StringVariant("nya")),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Colon),

                new IdentifierToken("LUCY_PUNCHED_ME"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.Newline,4),
            }
        },

        new CodeChange {
            name = "message",
            // "message":
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is ConstantToken {Value:StringVariant{Value: "message"}},
                t => t.Type == TokenType.Colon,
                t => t.Type == TokenType.Newline
            },
            // if LUCY_LOG_MESSAGES: print("[msg ", _get_username_from_id(packet_sender), "] ", DATA.message)
            code_to_add = new Token[] {
                new Token(TokenType.CfIf),
                new IdentifierToken("LUCY_LOG_MESSAGES"),
                new Token(TokenType.Colon),
                new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint),
                new Token(TokenType.ParenthesisOpen),
                new ConstantToken(new StringVariant("[msg ")),
                new Token(TokenType.Comma),
                new IdentifierToken("_get_username_from_id"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Comma),
                new ConstantToken(new StringVariant(" (")),
                new Token(TokenType.Comma),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.Comma),
                new ConstantToken(new StringVariant(")] ")),
                new Token(TokenType.Comma),
                new IdentifierToken("DATA"),
                new Token(TokenType.Period),
                new IdentifierToken("message"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 4),
            }
        },

        new CodeChange {
            name = "_read_all_P2P_packets",
            // func _read_all_P2P_packets(channel = 0 END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t.Type == TokenType.PrFunction,
                t => t is IdentifierToken {Name: "_read_all_P2P_packets"},
                t => t.Type == TokenType.ParenthesisOpen,
                t => t.Type == TokenType.Identifier,
                t => t.Type == TokenType.OpAssign,
                t => t.Type == TokenType.Constant,
            },
            // , limit = 64):
            //     var read_count = 0
            //     while Steam.getAvailableP2PPacketSize(channel) > 0 and read_count < limit:
            //         _read_P2P_Packet(channel)
            //         read_count += 1
            //     LUCY_PACKETS_READ += read_count
            // func _old_read_all_P2P_packets(channel = 0 END
            code_to_add = new Token[] {
                new Token(TokenType.Comma),
                new IdentifierToken("limit"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(64)),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Colon),
                new Token(TokenType.Newline, 1),

                new Token(TokenType.PrVar),
                new IdentifierToken("read_count"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.Newline, 1),

                new Token(TokenType.CfWhile),
                new IdentifierToken("Steam"),
                new Token(TokenType.Period),
                new IdentifierToken("getAvailableP2PPacketSize"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("channel"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.OpGreater),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.OpAnd),
                new IdentifierToken("read_count"),
                new Token(TokenType.OpLess),
                new IdentifierToken("limit"),
                new Token(TokenType.Colon),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("_read_P2P_Packet"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("channel"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("read_count"),
                new Token(TokenType.OpAssignAdd),
                new ConstantToken(new IntVariant(1)),
                new Token(TokenType.Newline, 1),

                new IdentifierToken("LUCY_PACKETS_READ"),
                new Token(TokenType.OpAssignAdd),
                new IdentifierToken("read_count"),
                new Token(TokenType.Newline, 0),

                // Give old function new signature, it'll come after in token stream
                new Token(TokenType.PrFunction),
                new IdentifierToken("_old_read_all_P2P_packets"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("channel"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(0)),
            }
        },

        new CodeChange {
            name = "_process",
            // func _process(delta):
            //     if not STEAM_ENABLED: return
            //     Steam.run_callbacks()
            //     if STEAM_LOBBY_ID > 0:
            //         END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t.Type == TokenType.PrFunction,
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
            },
            //         for i in 3: _read_all_P2P_packets(i,LUCY_FRAME_PACKETS)
            //     return
            //     if false:
            code_to_add = new Token[] {
                new Token(TokenType.CfFor),
                new IdentifierToken("i"),
                new Token(TokenType.OpIn),
                new ConstantToken(new IntVariant(3)),
                new Token(TokenType.Colon),
                new IdentifierToken("_read_all_P2P_packets"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("i"),
                new Token(TokenType.Comma),
                new IdentifierToken("LUCY_FRAME_PACKETS"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 1),

                new Token(TokenType.CfReturn),
                new Token(TokenType.Newline, 1),
                new Token(TokenType.CfIf),
                new ConstantToken(new BoolVariant(false)),
                new Token(TokenType.Colon),
                new Token(TokenType.Newline, 2),
            }
        },

        new CodeChange {
            name = "_physics_process",
            // func _physics_process(delta):
            //     if not STEAM_ENABLED: return
            //     END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t.Type == TokenType.PrFunction,
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
            },
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
            code_to_add = new Token[] {
                //     var do_print = false
                //     BULK_PACKET_READ_TIMER -= delta
	            //     if BULK_PACKET_READ_TIMER <= 0:
		        //         print("Bulk Reading Packets.")
                //         for i in 3: _read_all_P2P_packets(i,LUCY_BULK_PACKETS)
                //         BULK_PACKET_READ_TIMER = LUCY_BULK_INTERVAL
                //         do_print = true
                new Token(TokenType.PrVar),
                new IdentifierToken("do_print"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new BoolVariant(false)),
                new Token(TokenType.Newline, 1),

                new IdentifierToken("BULK_PACKET_READ_TIMER"),
                new Token(TokenType.OpAssignSub),
                new IdentifierToken("delta"),
                new Token(TokenType.Newline, 1),

                new Token(TokenType.CfIf),
                new IdentifierToken("BULK_PACKET_READ_TIMER"),
                new Token(TokenType.OpLessEqual),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.Colon),
                new Token(TokenType.Newline, 2),

                new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint),
                new Token(TokenType.ParenthesisOpen),
                new ConstantToken(new StringVariant("Bulk Reading Packets.")),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 2),

                new Token(TokenType.CfFor),
                new IdentifierToken("i"),
                new Token(TokenType.OpIn),
                new ConstantToken(new IntVariant(3)),
                new Token(TokenType.Colon),
                new IdentifierToken("_read_all_P2P_packets"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("i"),
                new Token(TokenType.Comma),
                new IdentifierToken("LUCY_BULK_PACKETS"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("BULK_PACKET_READ_TIMER"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("LUCY_BULK_INTERVAL"),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("do_print"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new BoolVariant(true)),
                new Token(TokenType.Newline, 1),

                //     LUCY_BULK_FULL_TIMER -= delta
                //     if LUCY_BULK_FULL_TIMER <= 0:
                //         print("Reading all packets.")
                //         for i in 3: _read_all_P2P_packets(i,1000000)
                //         LUCY_BULK_FULL_TIMER = LUCY_BULK_FULL_INTERVAL
                //         do_print = true
                new IdentifierToken("LUCY_BULK_FULL_TIMER"),
                new Token(TokenType.OpAssignSub),
                new IdentifierToken("delta"),
                new Token(TokenType.Newline, 1),

                new Token(TokenType.CfIf),
                new IdentifierToken("LUCY_BULK_FULL_TIMER"),
                new Token(TokenType.OpLessEqual),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.Colon),
                new Token(TokenType.Newline, 2),

                new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint),
                new Token(TokenType.ParenthesisOpen),
                new ConstantToken(new StringVariant("Reading all packets.")),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 2),

                new Token(TokenType.CfFor),
                new IdentifierToken("i"),
                new Token(TokenType.OpIn),
                new ConstantToken(new IntVariant(3)),
                new Token(TokenType.Colon),
                new IdentifierToken("_read_all_P2P_packets"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("i"),
                new Token(TokenType.Comma),
                new ConstantToken(new IntVariant(1000000)),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("LUCY_BULK_FULL_TIMER"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("LUCY_BULK_FULL_INTERVAL"),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("do_print"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new BoolVariant(true)),
                new Token(TokenType.Newline, 1),

                //     if do_print:
                //         print("PACKETS ", LUCY_PACKETS_READ)
                //         LUCY_PACKETS_READ = 0
                new Token(TokenType.CfIf),
                new IdentifierToken("do_print"),
                new Token(TokenType.Colon),
                new Token(TokenType.Newline, 2),

                new Token(TokenType.BuiltInFunc, (uint)BuiltinFunction.TextPrint),
                new Token(TokenType.ParenthesisOpen),
                new ConstantToken(new StringVariant("PACKETS ")),
                new Token(TokenType.Comma),
                new IdentifierToken("LUCY_PACKETS_READ"),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 2),

                new IdentifierToken("LUCY_PACKETS_READ"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.Newline, 1),

                //     return
                new Token(TokenType.CfReturn),
                new Token(TokenType.Newline, 1),
            }
        },

        new CodeChange {
            name = "new globals",
            // var REPLICATIONS_RECIEVED = []
            // END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t.Type == TokenType.PrVar,
                t => t is IdentifierToken {Name: "REPLICATIONS_RECIEVED"},
                t => t.Type == TokenType.OpAssign,
                t => t.Type == TokenType.BracketOpen,
                t => t.Type == TokenType.BracketClose,
                t => t.Type == TokenType.Newline,
            },
            // var LUCY_PACKETS_READ = 0
            // var LUCY_BULK_FULL_TIMER = 0
            // var LUCY_FRAME_PACKETS = 32
            // var LUCY_BULK_PACKETS = 128
            // var LUCY_BULK_INTERVAL = 0.8
            // var LUCY_BULK_FULL_INTERVAL = 6.4
            // var LUCY_SRV_NAME = ""
            // var LUCY_PUNCHED_ME = 0
            // var LUCY_INSTANCE_SENDER = 0
            // var LUCY_LOG_MESSAGES = false
            // END
            code_to_add = new Token[] {
                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_PACKETS_READ"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_BULK_FULL_TIMER"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_FRAME_PACKETS"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(32)),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_BULK_PACKETS"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(128)),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_BULK_INTERVAL"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new RealVariant(0.8)),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_BULK_FULL_INTERVAL"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new RealVariant(6.4)),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_SRV_NAME"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new StringVariant("")),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_PUNCHED_ME"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_INSTANCE_SENDER"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new IntVariant(0)),
                new Token(TokenType.Newline, 0),

                new Token(TokenType.PrVar),
                new IdentifierToken("LUCY_LOG_MESSAGES"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new BoolVariant(false)),
                new Token(TokenType.Newline, 0),
            }
        },

        new CodeChange {
            name = "packet sender",
            //         if PACKET.empty():
			//             print("Error! Empty Packet!")
            //         END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is IdentifierToken {Name: "PACKET"},
                t => t.Type == TokenType.Period,
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
            },
            //         var packet_sender = PACKET['steam_id_remote']
            //         END
            code_to_add = new Token[] {
                new Token(TokenType.PrVar),
                new IdentifierToken("packet_sender"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("PACKET"),
                new Token(TokenType.BracketOpen),
                new ConstantToken(new StringVariant("steam_id_remote")),
                new Token(TokenType.BracketClose),
                new Token(TokenType.Newline, 2),
            }
        },

        new CodeChange {
            name = "set lobby name",
            // Steam.setLobbyData(lobby_id, "name", str(STEAM_USERNAME) END
            multitoken_prefix = new Func<Token, bool>[] {
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
            },
            //    if LUCY_SRV_NAME == "" else LUCY_SRV_NAME END
            code_to_add = new Token[] {
                new Token(TokenType.CfIf),
                new IdentifierToken("LUCY_SRV_NAME"),
                new Token(TokenType.OpEqual),
                new ConstantToken(new StringVariant("")),
                new Token(TokenType.CfElse),
                new IdentifierToken("LUCY_SRV_NAME"),
            }
        },
    };

    IEnumerable<Token> IScriptMod.Modify(string path, IEnumerable<Token> tokens)
    {
        var pending_changes = changes
            .Select(c => (c, new MultiTokenWaiter(c.multitoken_prefix)))
            .ToList();

        // I'm sure there's a better way to do this
        // with list comprehension stuff, but my 
        // C# is too rusty
        foreach (var token in tokens) {
            var had_change = false;
            foreach (var (change, waiter) in pending_changes) {
                if (waiter.Check(token)) {
                    Mod.ModInterface.Logger.Information($"Adding Lucy Network mod {change.name}");

                    yield return token;
                    foreach (var t in change.code_to_add) yield return t;

                    had_change = true;
                    break;
                }
            }
            if (!had_change) yield return token;
        }
    }
}
