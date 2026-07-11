# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.9.1] - 2026-07-10

### Added

- Add compact trace columns and an optional detailed packet view with 16-byte hex rows, printable ASCII, search highlighting, and collapsed empty payloads

### Changed

- Upgrade Avalonia desktop packages to 12.1.0 and compatible ItemsRepeater and XAML behavior packages for rendering, hit-testing, bitmap, and Windows GPU performance improvements
- Migrate custom windows, popup templates, text formatting, clipboard access, and placeholder text to Avalonia 12 APIs
- Default traces to detailed rows and case-insensitive text search, align 12-character client names, label unauthenticated connections, and use warning colors for search highlights

### Fixed

- Preserve custom title-bar controls, native window dragging, and edge resizing with Avalonia 12 window decorations
- Restore normal font weight throughout the interface after the Avalonia 12 typography changes
- Keep cooldown countdown updates on the Avalonia UI dispatcher under the new multiple-dispatcher behavior
- Align Windows product version metadata with the app assembly and file versions

## [1.9.0] - 2026-07-10

### Added

- Add reusable IO and imaging libraries for reading game data archives, decoding EPF and palette data, resolving item dyes, and building sprite atlases
- Decode MPF creature animation files and raw or RIFF palettes with reusable imaging APIs and lazy preview loading
- Render full-color inventory sprites and grayscale skill and spell sprites from the configured game client's data archives, with numeric fallbacks when sprites cannot be resolved
- Render monster, NPC, and full-color ground-item sprites in entity tooltips, with numeric fallbacks when assets cannot be resolved
- Reserve inventory slot 60 for the player's gold bag and show the current gold count in its tooltip
- Show blue-tinted skill and spell sprites with centered, compact countdowns while cooldowns are active
- Configure default client and server packet filters that apply on startup and can be restored while tracing
- Search traces with an inline query language supporting implicit criteria, boolean operators and grouping, command lists and ranges, regular expressions, case control for text searches while character names remain case-insensitive, validation help, and in-packet match highlighting

### Changed

- Hide the trace client filter when only one client is present
- Keep inventory sprites centered at their natural size instead of scaling them up to fill their slots
- Show item, skill, and spell icons to the left of their names in tooltips
- Render creature and item sprites in dialog portraits with the sprite ID overlaid in the portrait's bottom-right corner
- Show item quantities greater than one as bottom-aligned `(xN)` labels and format gold as a comma-separated yellow tooltip line
- Improve slot readability with translucent slot-number backgrounds and white borders on mouseover
- Use south-facing idle frames for creature previews in entity tooltips and dialog portraits
- Keep entity row containers stable during active-map updates so mouseover tooltips and selection are not reset by movement packets
- Treat every cooldown packet as authoritative, replacing the active deadline and latest reported duration while keeping the tooltip duration separate from the ticking countdown

## [1.8.2] - 2026-07-09

### Added

- Automated release packaging for version tags

### Changed

- Keep entity collections on the UI thread and queue console and trace updates in UI batches
- Prioritize heartbeat and tick sync packets in the network send queue
- Add focus-aware keyboard shortcuts to the trace, inspector, raw packet fields, and packet send views
- Add visual copy feedback to inspector and raw packet fields, with separate states for selected text and full-field copies
- Highlight active trace search and filter tools when their bars are hidden
- Updated Avalonia and supporting packages to their latest compatible patch versions

## [1.8.1] - 2025-11-14

### Changed

- Updated to .NET 10
- Resize from any window edge

## [1.8.0] - 2025-11-05

### Added

- Option to inject `True Look` and `True Look At` skill/spells
- Backend for virtual spells/skills
- Right-click option to block next redirect for a client
- One-shot filter support for network packets

### Changed

- Fixed `ClientSpellCastMessage` packet definition
- Retain last selected settings tab for ease of use
- Formatted HP/MP values
- Fix issue with NPC names changing when they speak
- Fix some more concurrency issues

## [1.7.0] - 2025-10-29

### Added

- Option to show item durability on equipment view
- `Skills` tab for viewing character skills
- `Spells` tab for viewing character spells

### Changed

- More concurrency fixes with `Entity` list and sorting
- Exclude spell chants from entity list

## [1.6.0] - 2025-10-20

### Added

- Option to show item quantity in sell/deposit dialogs
- `Inventory` tab showing selected character inventory
- Sync inventory state for characters
- Observers in network layer
- Network packet source for detecting injected packets

### Changed

- Allow mod menu to show in Dialog view
- Only non-empty slots in Destroy Item mod menu option
- Remove sprite flags in Sprite value for `ServerAddItemMessage` previews
- Placeholder text in `Dialog` view when no selected client
- Placeholder text in `Clients` view when no clients available
- Placeholder background for `Inspector` view to make it less empty
- Refactor several filters to observers for performance

## [1.5.1] - 2025-10-18

### Added

- Destroy Item option in the NPC mod menu

### Changed

- Fix rare concurrency issue with `Entity` list
- Change `Delay` and `Rate` dropdowns to shorthand

## [1.5.0] - 2025-10-18

### Added

- Option for global pursuits for NPCs via "mod menu" dialog injection
- Reveal Monster name on interact in the `Entity` list
- `Interact` context menu to Entity list
- Entity filter mode for `All`, (same) `Map`, and `Nearby` (within visible range)
- `Copy Text` context menu to dialog content area

### Changed

- Replace players with same name in `Entity` list (relogged characters)
- Select next available client when selected one disconnects
- Removed client combo box selections, views now use the selected client globally
- Main app title changes based on selected client
- Fix entity map information being incorrect at times
- Entity search now highlights similar to `Trace` view
- Entity ID uses lighter gray color in the list view
- Color coding in dialog filters to show entity/pursuit IDs

## [1.4.0] - 2025-10-17

### Added

- Real-time dialog display
- Dialog interaction via buttons
- `Start Client` button also visible when left panel is collapsed
- Entity sort ordering (persists across sessions)
- Entity tooltip for additional information
- Entity context menu for copy / delete actions
- Support for decimal literals `#value` (ex: `#100`) in the `Send` packet syntax
- Support for string literals `"text"` in the `Send` packet syntax (single, double, and backticks all work)
- Support for entity ID reference `@Entity` (ex: `@Deoch`) in the `Send` packet syntax (must be seen before)

### Changed

- Auto-select the client in the `Send` view when selected in the left-side panel (if no selected client)
- Color-coded entity types
- Handle hidden/ghost players in `Entity` list
- Update entities on user profile click
- Update entities on say/shout (useful for ghosts)
- Border Z-index for various UI controls
- Pursuit + Step ID in debug filter
- Show actual class instead of "Master" when pre-Medenia class
- Ignore final `@wait` when sending if it is the last action in the `Send` queue
- Improved packet reader/write + filtering backend
- Fix concurrency issue with new `Entity` view

## [1.3.0] - 2025-10-15

### Added

- Send Selected button in `Send` view
- Dialog settings section with new option to show pursuit ID in dialog menus
- Strongly-typed network message filtering wrappers (backend)
- Entity list in right-hand panel

### Changed

- More fixes for disconnected clients appearing in Trace dropdown
- Retain order when copying multiple packets in Trace view
- Improve performance when loading thousands of trace packets from a large file
- Change `Repeat` to `Loop` to avoid the mental `-1` logic
- Text selection brush to be more blue
- Show minimized client windows when bringing to foreground
- Renamed `Decrypted`/`Encrypted` toggle to `Data`/`Raw` for clarity (and UI space)

## [1.2.1] - 2025-10-13

### Added

- Support for `@disconnect` / `@dc` in `Send` packet syntax
- Client `DismissParcel` command (only found on older clients)

### Changed

- Allow trailing commas when loading trace JSON files
- More accurate information for the `LightLevel` (time of day) packet
- Fixed filtered packets showing incorrectly in the trace view
- Renamed redundant `ArmorSprite` to better reflect its purpose

## [1.2.0] - 2025-10-06

### Added

- Support for `@wait <milliseconds>` in `Send` packet syntax
- Save filter action / result on trace packet JSON files
- Inspector hint for packets blocked/replaced by filter rules
- Inspector view toggle for original/filtered packets
- Option for max trace packet history (default: `1000`)

### Changed

- Debounce `Send` input validation for performance
- Pass `FilterResult` up through proxy layer events
- Grey out blocked filtered packets in trace view
- Inspector uses filtered packets as default payload (when applicable)
- Raw hex view uses filtered packets as default payload

## [1.1.0] - 2025-10-06

### Added

- Explicit option for `SkipClientVideo` in Setting (default: true)
- Explicit option for `SuppressLoginNotification` in Settings (not implemented yet)
- Message filter system using Regular Expressions

### Changed

- Fix issue with client list dropdown for disconnected clients
- Prioritize `ServerHeartbeat` message to avoid disconnect in heavy traffic

## [1.0.0] - 2025-10-03

### Added

- Dropdown for selecting a single character when tracing

### Changed

- Improved monster-click entity ID debug filtering
- Colorize the `Start`/`Stop` trace buttons for better visibility
- Colorize the `Send` button
- Hide the `Start/Stop` trace buttons depending on the trace state

## [0.9.11] - 2025-10-02

### Added

- Classic visual effect debug filter
- Rogue "zoomed out" tab map debug filter
- Monster-click entity ID debug filter

### Changed

- Add `ProxyConnection` to filter handler callback
- Remove `0x4000` (monster) and `0x8000` (item) bitflags from `Sprite` field values
- Show decimal by default for `Sprite` fields
- Better disconnect handling for client crashes
- Rename `Animation Speed` to `Animation Duration` for consistency

## [0.9.10] - 2025-10-02

### Added

- Many more debug options for entities, players, maps, and messages
- `Serialize` implementations for most client/server packets
- `FilterException` event for logging 

### Changed

- Fix missing sprite in `Learn Spell`/`Learn Skill` dialogs when debug mode is enabled
- Fix blank entity names when debug mode is enabled
- Re-organize debug options in `Settings` by category

## [0.9.9] - 2025-10-01

### Added

- Application icon
- `Debug` options for displaying entity IDs in-game
- `NetworkPacketBuilder` implementation
- Additional expand button for collapsible panels
- Packet filtering backend

### Changed

- Rename `Weather` to `Flags` for `MapInfo` packet 
- CRC calculator styling and spacing
- Better handling of client sudden disconnect (crash)

## [0.9.8] - 2025-09-29

### Added

- Right-click context menu for `Client` view items
- CRC calculator view in the right panel

### Changed

- Tooltip styling
- Use folder icon for `Browse` button in `Settings` view
- Reduce corner radius for segment radio button control
- Swap endianness of `MapInfo` packet checksum to match generated CRC-16

## [0.9.7b] - 2025-09-25

### Changed

- Fix issue with repeat loop hanging/crashing program

## [0.9.7] - 2025-09-25

### Added

- Collapsible `Send/Console` panel
- Collapsible `Clients` panel
- `Repeat Count` support for sending

### Changed

- Save layout state when closing application
- Ignore sending packets starting with `#` or `//` as comments

## [0.9.6] - 2025-09-25

### Added

- `Find Selected` context menu to add selected packet command to search
- `Exclude Selected` context menu to add selected packets command to filter exclusions (uncheck)
- Quick clear button next to search dropdown
- Collapsible `Inspector`/`Hex` panel

### Changed

- Fixed issue when switching tabs in `Trace` view
- Shadow effect on popup for readability
- Shadow effect on tooltip for readability
- Outline effect for combox/dropdown borders for readability
- Graphical issue when search results show incorrectly momentarily
- Fix for some performance issues with large trace files
- Fix issue when clicking checkbox versus clicking item in `Filter` dropdown
- Allow keyboard typing of search text in dropdowns
- Increase font size for level + class in `Client` view
- Remove `Filters` and `Triggers` tab
- Client filtering is now a multi-select dropdown
- Allow wrapping of `Command` and `Sequence` in `Hex` view when resizing panel
- `Send` toolbar layout

## [0.9.5] - 2025-09-22

### Added

- Additional sub-second rates for sending packets
- `Shift+Enter` hotkey for sending packets in the send window
- `InputDescription` for `ServerShowDialogMessage` packet
- Redesigned settings window
- `CanMove` property for `ServerUserIdMessage` packet
- Autosave for traces on application exit

### Changed

- Rename `Aisling` to `Reactor` for dialog entity type
- Rename `TextInput` to `Arguments` for `ClientDialogMenuChoiceMessage` message
- Increase command filter box width slightly
- Fixed `InterfacePane` enum values
- Display `Name` for entities in Inspector
- Remove most transition animations in the UI (need to re-standardize them)
- Selected tab background color
- Fix spacing in search and filter bars
- Fix disable style for `ComboBox` dropdown button
- Reduce corner radius on input controls
- Fixed local port not working if other than default
- Swap order of scroll to end and clear in `Console` view for consistency
- Collapse `Flags` section in `ServerUpdateStatsMessage` message by default
- Multiselect dropdown for filtering by command instead of user textbox input
- Dropdown for searching by command instead of user textbox input

## [0.9.4b] - 2025-09-20

### Changed

- `ClientExitRequestMessage` now has optional `Reason` field (it is not always provided)
- Clear text field button size
- Copying a decrypted packet now includes `<` or `>` direction for `Send` window convenience
- Send syntax allows spacing between `>`/`<` and command code

## [0.9.4] - 2025-09-20

### Added

- CRC16 and CRC32 checksum algorithms
- Encrypt algorithm for server packets
- Encrypt algorithm for client packets (auto-insert `0x00` byte)
- Dialog encryption algorithm for client packets
- Unit tests for network and encryption algorithms
- `PacketException` event invoked when the client tries to send `0x42` exception packet
- Sending of raw packets to client/server
- Transition animations to various UI components
- `PacketQueued` event to `ProxyConnection` and `ProxyServer` classes

### Changed

- Client name filter now matches exactly (unless `*` or `?` are used)
- Renamed a few enum types
- Renamed `MenuChoice` to `DialogMenuChoice` for clarity
- Renamed `ClientMenuChoiceMessage` to `ClientDialogMenuChoiceMessage` for consistency
- Renamed `ShowMenu` to `ShowDialogMenu` for consistency
- Renamed `ServerShowMenuMessage` to `ServerShowDialogMenuMessage` for consistency
- Renamed `MenuChoice` to `Slot` for `ClientDialogMenuChoiceMessage` message
- Decrypting client packets will now remove the trailing padding `0x00` byte
- `NetworkEncryptionParameters` are now read-only for thread safety
- Blocking of outgoing `0x42` client exception packets to the server (still logged/traced locally)
- Refactored a lot of internal systems for clarity and performance
- Improve performance of console log and counting of log entries
- Rename `Console Log` to just `Console` in the tab view
- Fix decimal mode not being set by default in the Hex view when empty
- Fix resize grip hit test area
- Adjust combo box dropdown button size

## [0.9.3] - 2025-09-18

### Added

- `Copy Selected` context menu for trace packets
- `Save Selected` context menu for trace packets
- `Delete Selected` context menu for trace packets
- Double-click on client in list to bring game window to foreground (Windows only)
- Set game client window title to include client name on login (cleared on logout)

### Changed

- Allow multiple selection of trace packets (only first item is inspected)
- Fixed `Shift` modifier getting stuck when loading traces in append mode
- Trace packet list item hit test areas
- Context menu visual design improvements
- Disable `Launch Client` button for non-Windows operating systems

## [0.9.2] - 2025-09-18

### Added

- Severity level labels in console messages
- Console message right-click context menu for copy to clipboard
- Remember last size and position when opening application
- Text view of `ServerLegendMark` listing on `ServerUserProfileMessage` and `ServerSelfProfileMessage` inpsector views
- Text view of `ServerWorldListUser` listing on `ServerWorldListMessage` inspector view
- Copy to JSON representation from inspector
- Tooltips for inspector fields
- Find packet by command with next/prev navigation (`Ctrl+[` and `Ctrl+]` hotkeys)

### Changed

- Console exception messages are now one text run for easy text selection
- Reduced initial window size and layout splits
- Adjusted trace view toolbar layout for resizing
- Hide filter bar by default on trace
- Collapse `Users` on `ServerWorldListMessage` by default in inspector
- Renamed `Type` to `AbilityType` in `ServerCooldownMessage` message
- Renamed `PreviousX` and `PreviousY` to `OriginX` and `OriginY` on `ServerEntityWalkMessage` message
- Renamed `ShowPlayer` to `ShowUser` for consistency across wording
- Renamed `MessageType` to `ResultType` for `ServerLoginResultMessage` message
- Renamed `HasGroupInvite` to `IsRecruiting` for `ServerSelfProfileMessage` message
- Renamed `DialogId` to `StepId` and `SourceId` to `EntityId` for `ServerShowDialogMessage` message
- Renamed `SourceId` to `EntityId` and `Args` to `Prompt` for `ServerShowMenuMessage` message
- Renamed `IsTransparent` to `IsTranslucent` for `ServerShowUserMessage` message
- Renamed `HasUnreadParcels` to `HasAvailableParcels` in `ServerUpdateStatsMessage` message
- Display name overrides for nested types
- Made most nested types collapsed by default in inspector
- Allow bit flags to display on 16-bit and 32-bit values
- Change filter hotkey to `Ctrl+G` (in preparation for `Ctrl+F` for find)
- Adjust hotkey gesture text rendering

## [0.9.1] - 2025-09-17

### Added

- Exception view for inspector view errors
- Improved client view with map location and stats
- `ClientLoggedIn` event for `ProxyServer`, invoked when server sends `0x05` packet
- `ClientLoggedOut` event for `ProxyServer`, invoked when server sends `0x4C` packet

### Changed

- Renamed `DyeColor` to `Color`
- `ClientAuthenticated` event now invoked when client sends `0x10` packet
- Clear login state when client explicitly exits
- Remove `Unknown` from `ServerRequestPortraitMessage` packet/mapping
- Fixed `Name` and `GroupBox` properties for `ServerShowPlayerMessage` message when in monster form

## [0.9.0] - 2025-09-16

### Added

- Initial preview release
- Launch game client support
- Network packet tracing with decryption
- Strong-typed packet inspection
- Raw packet hex view
- Load/save packet traces
- Console logging
- Simple user settings
