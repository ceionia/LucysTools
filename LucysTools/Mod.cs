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
        modInterface.RegisterScriptMod(new LucyServerBrowserChanges());
        modInterface.RegisterScriptMod(new LucyMainMenuChanges());
    }

    public void Dispose(){}
}

public record CodeChange {
    public required String name;
    public required Func<Token, bool>[] multitoken_prefix;
    public required Token[] code_to_add;
}

public class LucyServerBrowserChanges: IScriptMod 
{
    bool IScriptMod.ShouldRun(string path) => path == "res://Scenes/Menus/Main Menu/ServerButton/server_button.gdc";

    CodeChange[] changes = {
        new CodeChange {
            name = "server button arg",
            // , age_limit, dated, banned END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t.Type == TokenType.Comma,
                t => t is IdentifierToken {Name: "age_limit"},
                t => t.Type == TokenType.Comma,
                t => t is IdentifierToken {Name: "dated"},
                t => t.Type == TokenType.Comma,
                t => t is IdentifierToken {Name: "banned"},
            },
            // , lucy_display
            code_to_add = new Token[] {
                new Token(TokenType.Comma),
                new IdentifierToken("lucy_display"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new StringVariant("")),
            }
        },
        new CodeChange {
            name = "server button load",
            // display_name.replace(']', '')
            // END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is IdentifierToken {Name: "display_name"},
                t => t.Type == TokenType.Period,
                t => t is IdentifierToken {Name: "replace"},
                t => t.Type == TokenType.ParenthesisOpen,
                t => t is ConstantToken {Value:StringVariant{Value:"]"}},
                t => t.Type == TokenType.Comma,
                t => t is ConstantToken,
                t => t.Type == TokenType.ParenthesisClose,
                t => t.Type == TokenType.Newline,
            },
            // if lucy_display != "": display_name = lucy_display
            code_to_add = new Token[] {
                new Token(TokenType.CfIf),
                new IdentifierToken("lucy_display"),
                new Token(TokenType.OpNotEqual),
                new ConstantToken(new StringVariant("")),
                new Token(TokenType.Colon),
                new IdentifierToken("display_name"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("lucy_display"),
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
                    Mod.ModInterface.Logger.Information($"Adding Lucy server button mod {change.name}");

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

public class LucyMainMenuChanges: IScriptMod 
{
    bool IScriptMod.ShouldRun(string path) => path == "res://Scenes/Menus/Main Menu/main_menu.gdc";

    CodeChange[] changes = {
        new CodeChange {
            name = "server button lucy_display get",
            // if $"%hidenames".pressed: lobby_custom_name = ""
            // END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t.Type == TokenType.CfIf,
                t => t.Type == TokenType.Dollar,
                t => t is ConstantToken {Value:StringVariant{Value:"%hidenames"}},
                t => t.Type == TokenType.Period,
                t => t is IdentifierToken {Name: "pressed"},
                t => t.Type == TokenType.Colon,
                t => t is IdentifierToken {Name: "lobby_custom_name"},
                t => t.Type == TokenType.OpAssign,
                t => t.Type == TokenType.Constant,
                t => t.Type == TokenType.Newline,
            },
            // var lucy_display = ""
            // if not $"%hidenames".pressed:
            //     lucy_display = Steam.getLobbyData(lobby, "bbcode_lobby_name")
            code_to_add = new Token[] {
                new Token(TokenType.PrVar),
                new IdentifierToken("lucy_display"),
                new Token(TokenType.OpAssign),
                new ConstantToken(new StringVariant("")),
                new Token(TokenType.Newline, 2),

                new Token(TokenType.CfIf),
                new Token(TokenType.OpNot),
                new Token(TokenType.Dollar),
                new ConstantToken(new StringVariant("%hidenames")),
                new Token(TokenType.Period),
                new IdentifierToken("pressed"),
                new Token(TokenType.Colon),
                new Token(TokenType.Newline, 3),

                new IdentifierToken("lucy_display"),
                new Token(TokenType.OpAssign),
                new IdentifierToken("Steam"),
                new Token(TokenType.Period),
                new IdentifierToken("getLobbyData"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("lobby"),
                new Token(TokenType.Comma),
                new ConstantToken(new StringVariant("bbcode_lobby_name")),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 2)
            }
        },

        new CodeChange {
            name = "server button lucy_display arg",
            // lobby_cap, lobby_age, dated, banned END
            multitoken_prefix = new Func<Token, bool>[] {
                t => t is IdentifierToken {Name: "lobby_cap"},
                t => t.Type == TokenType.Comma,
                t => t is IdentifierToken {Name: "lobby_age"},
                t => t.Type == TokenType.Comma,
                t => t is IdentifierToken {Name: "dated"},
                t => t.Type == TokenType.Comma,
                t => t is IdentifierToken {Name: "banned"},
            },
            // , lucy_display END
            code_to_add = new Token[] {
                new Token(TokenType.Comma),
                new IdentifierToken("lucy_display"),
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
                    Mod.ModInterface.Logger.Information($"Adding Lucy server button mod {change.name}");

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

public class LucysChatChanges : IScriptMod
{
    bool IScriptMod.ShouldRun(string path) => path == "res://Scenes/HUD/playerhud.gdc";

    CodeChange[] changes = {
        new CodeChange {
            name = "chat process intercept",
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
            // $"/root/LucyLucysTools".process_message(text, chat_local, player, self)
            // return
            code_to_add = new Token[] {
                new Token(TokenType.Dollar),
                new ConstantToken(new StringVariant("/root/LucyLucysTools")),
                new Token(TokenType.Period),
                new IdentifierToken("process_message"),
                new Token(TokenType.ParenthesisOpen),
                new IdentifierToken("text"),
                new Token(TokenType.Comma),
                new IdentifierToken("chat_local"),
                new Token(TokenType.Comma),
                new IdentifierToken("player"),
                new Token(TokenType.Comma),
                new Token(TokenType.Self),
                new Token(TokenType.ParenthesisClose),
                new Token(TokenType.Newline, 1),

                new Token(TokenType.CfReturn),
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

