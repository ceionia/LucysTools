LucysTools
----------
Uses GDWeave.

Client Features:
- Makes the client (tunably) read all packets. This fixes chat messages dropping.
- Optionally knocks people back when they punch you.
- Lets you clear gamechat
- Sends messages on P2P channel 2 (This should make your messages more reliable for users who don't have LucysTools)

Host Features:
- Lets you set a custom server name and message that will be sent when someone joins.
- Lets you spawn rainclouds & meteors.
- Lets you do 'raw' messages & BBCode in messages. If enabled, other players on the server can use BBCode too. (Not secure or anything, implemented client side)

More coming soon!
Probably certainly full of bugs.

Packet options:
- 'Per Frame Packets' is the number of net packets your client will attempt to read per frame. 
- 'Bulk Read Packets' is the number of net packets your client will attempt to read per 'Bulk Read Interval' (in seconds)
- 'Full Read Interval' is how often your client will attempt to read *all* net packets (in seconds).

Compatibility:
- Works *only* with WEBFISHING 1.08
- I haven't tested any other mods with this, but I'm happy to try to make things compatible, submit a bug report with the incompatible mod! (Only mods that have source available)

Bugs:
- Make sure your version of LucysTools is the latest release before submitting bug reports, please.
