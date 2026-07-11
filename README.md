# Arbiter

Network Analyzer Tool for Dark Ages

Written in .NET + [Avalonia](https://docs.avaloniaui.net/docs/welcome), using [MVVM](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) patterns.
Custom UI styling based on [Godot's](https://godotengine.org/) look and feel.

---

<img src="docs/src/screenshots/Arbiter.png"/>

## Requirements ✅

- [Dark Ages](https://www.darkages.com) Client 7.41 (current latest)
- [.NET 10.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Windows, macOS*, Linux*

> [!NOTE]
> Launching a game client is only supported on Windows, as it uses Win32 APIs.  
> You can still analyze network packet dumps or redirect clients from other platforms or VMs.

## Installation 💾

1. Download the [latest release](https://github.com/ewrogers/Arbiter/releases/)
2. Extract all files to `C:\Arbiter` (or your choosing)
3. Open `Arbiter.exe`
4. Configure your DA installation path in `Settings` (if different)

## Usage 📜

### Launching a Game Client

Click on the `+` button in the top-left corner to launch a game client.
It will automatically be redirected to Arbiter running locally on your machine.

### Starting a Trace

Click on the ▶️ button within the `Trace` tab to begin capturing packets.
By default, all packets from all clients will be captured and displayed in the `Trace` tab.
You can change this by selecting the desired client from the dropdown menu.

You can filter the packets by clicking on the `Filter` button in the trace window.

You can search (highlight/navigate) for a specific packet by typing its command in the search box.

### Saving / Loading Traces

Click on the `Save` button to save the current trace to a file.
You can also right-click on selected packets to save them individually.

Click on the `Load` button to load a trace from a file.
You can also hold down `Shift` while clicking `Load` to append the trace to the current one.

> [!CAUTION]
> Be careful when sharing traces with others!  
> They may contain sensitive information such as your character name or even password if you began a trace while logging in.

### Sending Raw Packets

You can send both client and server packets by using the `Send` window.
By default, all packets will be assumed to be client packets (meaning they are sent to the server as if the client sent them).

Each packet should be placed on its own line in the text box area and in the following format:

#### Syntax

```
[DIRECTION] <COMMAND> <DATA...>
```

Packets that you copy from traces using the right-click menu will be in the correct format already.
**However**, you should ensure it is prefixed with the right direction (client or server) before sending it.

You can override this by prefixing the packet with `<` to indicate a server packet or `>` to indicate a client packet:

```
# this is a client packet
> 43 03 00 0A 00 08 01
# this is a server packet
< 32 00
```

For example:

```
# click on tile 10, 8
43 03 00 0A 00 08 01        
```

#### Inline Data Types

In addition to hex values, you can also specify other data types within a packet for ease of use.

Supported data types:

- `#value` - decimal value literal (ex: `#100` -> `64`)
  - The hex will be 8-bit, 16-bit, or 32-bit depending on the value (smallest necessary).
- `"string"` - ascii string literal (ex: `"hello world"` -> `68 65 6C 6C 6F 20 77 6F 72 6C 64`)
  - Single-quote, double-quote, and backtick strings are all supported.
- `@Entity` - entity ID replacement (ex: `@Deoch` -> `00 00 CA 1B`)
  - The entity must have been seen before, otherwise a zero value will be used instead. 

#### Wait Delays

You can specify `@wait <milliseconds>` between certain packets if you want to introduce a variable delay.
This can be useful for certain dialog interactions or other timing-sensitive packet flows.

For example:

```
# click on entity 0xBEEF
43 01 00 00 BE EF
@wait 2000   
// next packets...
```

This will wait 2 seconds before sending the next packets. This is independent of the `Delay` and `Interval` settings.

#### Disconnect

You can specify `@disconnect` (or `@dc`) to force the client to disconnect.
This can be useful when you want to "log out" after sending a packet.

#### Comments

You can use the `#` or `//` prefix to add a comment to the start of the packet line. These will not be sent!

#### Initial Delay, Interval, and Repeat

The initial `Delay` can be set, which determines how much time to wait before sending the first packet.
This is only done **once** when sending is started, regardless of repeat behavior. Think of it like a countdown timer.

The `Interval` determines the time between sending each packet. You can think of it like the tempo or send rate.

The `Repeat` determines how many times to send the packets. All packets in the send window will be sent again, in order.
You can use a specific number of times to repeat, or `-1` for infinite repeats.

### Inspecting Packets

When selecting a packet, the `Inspector` tab will display information about the packet in a structured format.
Sections can be collapsed by clicking on the arrow next to the section title.

You can also right-click on a field to copy its value to the clipboard.
If you wish to copy the entire structured object as JSON, you can click on the `Copy JSON` button on the top-right.

### Raw Hex View

You can view the decrypted packet payload in the `Hex` tab on the right-side panel.
By selecting a range of bytes you can visualize various values in different formats.

### CRC Calccator

You can calculate the CRC-16 or CRC-32 checksum of text, binary, or file contents.
Optionally, you can specify a compression algorithm to use for the input.

This can be useful for verifying against the checksum of metadata files (which are compressed).

### Message Filtering

You can filter out system messages and world shouts in the `Settings->Messages` tab.

<img src="docs/src/screenshots/Arbiter-Message-Filters.png"/>

These accept a case-insensitive [Regular Expression](https://regex101.com/) style syntax.
You can use the bottom text area to test your filter against a sample message,
which will show you if the message would be `Allowed` (shown) or `Filtered` (suppressed).

Individual rules can be enabled/disabled by clicking on the checkbox next to the rule name.
You can move rules up or down, edit them, or delete them using the right-click context menu.

## Documentation 📚

TBD

## Contributing 👨🏻‍💻

Contributions are welcomed for this project! Please open an issue or pull request if you have any suggestions, fixes, or improvements.

I personally use JetBrains Rider for development, but any editor should work as long as you install the appropriate Avalonia plugins.

## Packaging 📦

Pushing a version tag such as `v1.8.2` automatically builds, tests, packages, and publishes the matching GitHub release. The tag version must match the app version, assembly version, file version, and changelog section.

The release package contains the Release `win-x64` single-file executable and its required native libraries.

For local packaging, use the following command:

```powershell
cd Arbiter.App
dotnet publish -r win-x64 -c Release --no-self-contained -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
```

You can use the platform of your choice, but the `win-x64` target is recommended.

> [!IMPORTANT]
> You must include the published `.dll` files with the executable.

## Attribution 🙏🏻

Special thanks to [Chaos Server](https://github.com/Sichii/Chaos-Server) and [Hybrasyl](https://github.com/hybrasyl/server) repositories for many of the packet structures.

### DALib

The MPF format decoder in `Arbiter.Imaging` is adapted from the `MpfFile` and `MpfView` implementations in [eriscorp/dalib](https://github.com/eriscorp/dalib).

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
