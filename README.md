# Arbiter

Network analyzer and packet development tool for Dark Ages.

Arbiter is written in .NET and [Avalonia](https://docs.avaloniaui.net/docs/welcome), using
[MVVM](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) patterns. Its custom interface is inspired by
[Godot's](https://godotengine.org/) look and feel.

---

![Arbiter trace view](docs/src/screenshots/Arbiter.png)

## Features

- Capture, filter, search, save, and load packet traces from one or more connected characters
- Inspect decoded packet fields alongside raw hexadecimal and text representations
- Compose client or server packets with hex bytes, typed values, entity references, delays, and repeat controls
- Track character inventory, skills, spells, dialogs, nearby entities, and cooldowns
- Render item and creature sprites directly from a configured Dark Ages installation
- Apply configurable message filters and calculate CRC-16 or CRC-32 checksums

## Requirements

- Dark Ages Client 7.41
- [.NET 10.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Windows, macOS, or Linux

> [!NOTE]
> Launching a game client is supported only on Windows because it uses Win32 APIs. Arbiter can still analyze saved
> traces or accept redirected clients from other platforms and virtual machines.

## Installation

1. Download the [latest release](https://github.com/ewrogers/Arbiter/releases/).
2. Extract the release into a folder such as `C:\Arbiter`.
3. Run `Arbiter.exe`.
4. Open `Settings` and select your Dark Ages executable if it was not detected automatically.

The game data archives used for sprite previews are read from the directory containing the configured executable.
They are not included with Arbiter.

## Usage

### Launching a Game Client

Use the `+` button in the top-left corner to launch a client. Arbiter starts it with the local proxy endpoint already
configured. You can connect multiple characters and select the active character from the client list.

### Starting a Trace

Open the `Trace` tab and use the Start button to begin capturing packets. A trace can follow all connected clients or
one selected character. Stop the trace before changing that selection.

The default Detailed view aligns the timestamp, character, direction, command name, command byte, payload bytes, and
printable ASCII. Compact view keeps each packet on one line. The view toggle is remembered between sessions.

Use the toolbar to switch between decrypted Data and complete Raw packets, clear the trace, or jump to its end.

### Filtering Traces

Filters limit which packets are visible by direction, command, client, or filter action. Filtering does not modify the
captured packet data. The active filter button remains highlighted when the filter bar is closed.

### Searching Traces

Search finds and highlights matching packets without hiding the rest of the trace. Press `Enter` and `Shift+Enter` to
move between results. Text searches are case-insensitive by default; the `aA` button enables case-sensitive matching.

Each predicate uses `field=value` or `field!=value`:

| Field | Matches | Example |
| --- | --- | --- |
| `client` | Client packet command, as hex or a command name | `client=45-47\|Heartbeat` |
| `server` | Server packet command, as hex or a command name | `server=05,15\|HealthBar` |
| `data` | Contiguous bytes in the decrypted payload | `data=65 01 01` |
| `raw` | Contiguous bytes in the complete raw packet | `raw=AA 00 04 13` |
| `text` | ASCII text or a regular expression in the payload | `text="hello world"` |
| `name` | Character name or a regular expression | `name=/^silo.*$/` |
| `sequence`, `seq` | Packet sequence values | `sequence=02\|04-08` |

Command and sequence values are hexadecimal. Lists can mix values and ascending ranges using commas or `|`. Command
names are case-insensitive and may be mixed with numeric values.

Predicates separated by spaces or commas are combined naturally. Client and server command predicates are alternatives
by default, while payload, text, name, and sequence predicates narrow the result. Use `AND`, `OR`, `NOT`, and
parentheses when you need explicit logic. `AND` is evaluated before `OR`.

```text
server=13 text=error
(server=13 OR client=45-47) AND NOT text=error
data=65 01 01 name=CharacterName
text=/error|warning/
```

Unquoted text and names cannot contain spaces. Quote multi-word values with double quotes. Regular expressions use
`/pattern/` without flags. The `aA` toggle affects text literals and text regular expressions; character names remain
case-insensitive.

The information button beside the search box contains the same quick reference inside the app.

### Saving and Loading Traces

Use Save to write the current trace to JSON. Selected packets can also be saved from the trace context menu.

Use Load to replace the current trace, or hold `Shift` while loading to append packets to it.

> [!CAUTION]
> Traces may contain character names, credentials, chat, or other sensitive data. Review a trace before sharing it,
> especially if capture began during login.

### Sending Raw Packets

The Send editor accepts one packet or control command per line. A packet begins with an optional direction, followed by
the command byte and its data:

```text
[<|>] COMMAND [DATA...]
```

The command and ordinary data tokens are hexadecimal bytes. Direction is relative to the selected character:

- No prefix or `>` sends a client packet to the server.
- `<` sends a server packet to the client.

```text
# Click tile 10, 8 as the selected client
> 43 03 00 0A 00 08 01

# Send command 32 with one data byte to the selected client
< 32 00
```

Packets copied from a trace are already formatted for the editor. Confirm the direction before sending them.

#### Inline Data Types

Data can mix ordinary hex bytes with typed values:

| Syntax | Result |
| --- | --- |
| `#100` | Decimal 100 encoded as `64` |
| `#256` | Decimal 256 encoded as `01 00` |
| `"hello"` | Length-prefixed ASCII encoded as `05 68 65 6C 6C 6F` |
| `@Deoch` | Four-byte big-endian ID of the known entity with that name |

Decimal values use the smallest signed or unsigned one-, two-, or four-byte big-endian representation that can hold
the value. Negative values are supported down to 32-bit signed range.

Strings may use single quotes, double quotes, or backticks. Their ASCII bytes are prefixed with a one-byte length, so a
string cannot exceed 255 characters. The escapes `\n`, `\r`, and `\t` are supported.

Entity names are resolved case-insensitively when the packet is sent. An entity that is unknown at that time is encoded
as ID zero and produces a warning in the Console.

Pasting a complete scalar value also normalizes it for the editor:

| Clipboard value | Inserted text |
| --- | --- |
| `0x121` | `01 21` |
| `13BBFF` | `13 BB FF` |
| `12` | `#12` |

Digit-only values are treated as decimal. A bare value containing `A` through `F` is treated as hexadecimal. Existing
packet text and multiline pastes are left unchanged.

#### Control Commands and Comments

Control lines affect the send sequence but do not create packets:

| Syntax | Behavior |
| --- | --- |
| `@wait 2000` | Wait 2,000 milliseconds before continuing |
| `@disconnect` or `@dc` | Disconnect the selected client |
| `# comment` | Ignore the full line |
| `// comment` | Ignore the full line or the remainder of a packet line |

```text
# Click an entity, wait for its dialog, then disconnect
43 01 00 00 BE EF
@wait 2000
< 32 00 // server response example
@disconnect
```

#### Initial Delay, Rate, and Loop

`Delay` waits once before the first entry. `Rate` controls the normal interval between packet entries. `@wait` adds an
extra delay at its exact location in the sequence.

Enable `Loop` to resend the complete sequence in order. A positive value sets the number of passes; `-1` repeats until
Stop is pressed. If text is selected in the editor, Send operates on the selected lines only.

The information button beside the Send toolbar provides a compact syntax reference inside the app.

### Inspecting Packets

Selecting a trace packet opens its decoded structure in the `Inspector` tab. Sections can be collapsed, individual
values can be copied from their context menus, and Copy JSON writes the complete structured packet to the clipboard.

### Player and Entity State

Inventory, Skills, Spells, Entities, and Dialog views follow the selected character. Sprite previews come from the
configured game installation, with numeric placeholders when an asset cannot be read. Cooldowns use the latest server
update as authoritative.

### Raw Hex View

The `Hex` tab shows the selected packet payload. Selecting bytes exposes text, signed and unsigned numbers, flags, and
other common representations.

### CRC Calculator

The CRC calculator supports CRC-16 and CRC-32 for text, binary, or file input. Input can optionally be compressed before
calculation, which is useful when checking metadata files.

### Message Filtering

Message filters are configured under `Settings > Messages`.

![Arbiter message filters](docs/src/screenshots/Arbiter-Message-Filters.png)

Rules use case-insensitive [regular expressions](https://regex101.com/). The test area previews whether a sample message
would be allowed or filtered. Rules can be enabled, disabled, reordered, edited, or deleted.

## Documentation

The Search and Send sections above describe their supported syntax. The matching information buttons in Arbiter provide
short references without leaving the active workspace.

Protocol structures live in `Arbiter.Net`. Reusable archive and image decoders live in `Arbiter.IO` and
`Arbiter.Imaging` with tests that use generated or mock data rather than bundled game assets.

## Contributing

Contributions are welcome. Open an issue or pull request with a focused explanation of the change and how it was tested.

JetBrains Rider is used for most development, but any .NET editor works with the appropriate Avalonia tooling. Arbiter
targets .NET 10 and follows the conventions in `AGENTS.md`.

## Packaging

Pushing a version tag such as `v1.9.1` builds, tests, packages, and publishes the matching GitHub release. The tag must
match the application version, assembly version, file version, and changelog section.

Release packages contain the `win-x64` single-file executable and its required native libraries. To package locally:

```powershell
cd Arbiter.App
dotnet publish -r win-x64 -c Release --no-self-contained -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
```

Another runtime identifier can be used for local builds, but `win-x64` is the release target.

> [!IMPORTANT]
> Keep the published `.dll` files beside the executable.

## Attribution

Special thanks to [Chaos Server](https://github.com/Sichii/Chaos-Server) and
[Hybrasyl](https://github.com/hybrasyl/server) for many of the packet structures.

### DALib

The MPF decoder in `Arbiter.Imaging` is adapted from the `MpfFile` and `MpfView` implementations in
[eriscorp/dalib](https://github.com/eriscorp/dalib).

MIT License

Copyright (c) 2017 Kyle Speck

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
