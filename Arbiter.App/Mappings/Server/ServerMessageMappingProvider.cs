using System.Linq;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.Mappings.Server;

public class ServerMessageMappingProvider : IInspectorMappingProvider
{
    public void RegisterMappings(InspectorMappingRegistry registry)
    {
        RegisterServerAddEntityMapping(registry);
        RegisterServerAddItemMapping(registry);
        RegisterServerAddSkillMapping(registry);
        RegisterServerAddSpellMapping(registry);
        RegisterServerAnimateEntityMapping(registry);
        RegisterServerBoardResultMapping(registry);
        RegisterServerCooldownMapping(registry);
        RegisterServerEntityTurnMapping(registry);
        RegisterServerEntityWalkMapping(registry);
        RegisterServerExchangeMapping(registry);
        RegisterServerExitResponseMapping(registry);
        RegisterServerForcePacketMapping(registry);
        RegisterServerGroupMapping(registry);
        RegisterServerHealthBarMapping(registry);
        RegisterServerHeartbeatMapping(registry);
        RegisterServerHelloMapping(registry);
        RegisterServerLightLevelMapping(registry);
        RegisterServerLoginNoticeMapping(registry);
        RegisterServerLoginResultMapping(registry);
        RegisterServerWeatherChangedMapping(registry);
        RegisterServerMapChangingMapping(registry);
        RegisterServerMapDoorMapping(registry);
        RegisterServerMapInfoMapping(registry);
        RegisterServerMapLocationMapping(registry);
        RegisterServerMapTransferCompleteMapping(registry);
        RegisterServerMapTransferMapping(registry);
        RegisterServerManufactureMapping(registry);
        RegisterServerMetadataMapping(registry);
        RegisterServerPlaySoundMapping(registry);
        RegisterServerPublicMessageMapping(registry);
        RegisterServerRedirectMapping(registry);
        RegisterServerRefreshCompletedMapping(registry);
        RegisterServerRemoveEntityMapping(registry);
        RegisterServerRemoveEquipmentMapping(registry);
        RegisterServerRemoveItemMapping(registry);
        RegisterServerRemoveSkillMapping(registry);
        RegisterServerRemoveSpellMapping(registry);
        RegisterServerRequestUserPortraitMapping(registry);
        RegisterServerSelfProfileMapping(registry);
        RegisterServerServerInfoMapping(registry);
        RegisterServerServerListMapping(registry);
        RegisterServerServerTableMapping(registry);
        RegisterServerSetEquipmentMapping(registry);
        RegisterServerShowDialogMapping(registry);
        RegisterServerShowDialogMenuMapping(registry);
        RegisterServerShowEffectMapping(registry);
        RegisterServerShowMapHelpMapping(registry);
        RegisterServerShowNotepadMapping(registry);
        RegisterServerShowSpinnerMapping(registry);
        RegisterServerShowUserMapping(registry);
        RegisterServerStatPointsMapping(registry);
        RegisterServerStatusEffectMapping(registry);
        RegisterServerSwitchPaneMapping(registry);
        RegisterServerSyncTicksMapping(registry);
        RegisterServerUpdateStatsMapping(registry);
        RegisterServerUserIdMapping(registry);
        RegisterServerUserIdResponseMapping(registry);
        RegisterServerUserProfileMapping(registry);
        RegisterServerWalkResponseMapping(registry);
        RegisterServerWorldListMapping(registry);
        RegisterServerWorldMapMapping(registry);
        RegisterServerWorldMessageMapping(registry);
    }

    private static void RegisterServerAddEntityMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerDrawObjectsMessage>(b =>
        {
            b.Section("EntityManager")
                .Property(m => m.Entities);
        });
    }

    private static void RegisterServerAddItemMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerAddInventoryMessage>(b =>
        {
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot containing the item."))
                .Property(m => m.Sprite, p => p.ToolTip("Sprite used to display the item."))
                .Property(m => m.Color, p => p.ToolTip("Override (dye) color applied to the sprite."))
                .Property(m => m.Name, p => p.ShowMultiline().ToolTip("Display name of the item."));
            b.Section("Quantity")
                .Property(m => m.Quantity, p => p.ToolTip("Quantity held of the item."))
                .Property(m => m.IsStackable,
                    p => p.ToolTip("Whether multiples of the item can be held in a single slot."))
                .IsExpanded(m => m.Quantity > 1);
            b.Section("Durability")
                .Property(m => m.Durability, p => p.ToolTip("Current durability of the item."))
                .Property(m => m.MaxDurability, p => p.ToolTip("Maximum durability of the item."))
                .IsExpanded(m => m.MaxDurability > 0);
        });
    }

    private static void RegisterServerAddSkillMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerAddSkillMessage>(b =>
        {
            b.Section("Skill")
                .Property(m => m.Slot, p => p.ToolTip("Slot containing the skill."))
                .Property(m => m.Icon, p => p.ToolTip("Icon used to display the skill."))
                .Property(m => m.Name, p => p.ShowMultiline().ToolTip("Display name of the skill."));
        });
    }

    private static void RegisterServerAddSpellMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerAddSpellMessage>(b =>
        {
            b.Section("Spell")
                .Property(m => m.Slot, p => p.ToolTip("Slot containing the spell."))
                .Property(m => m.Icon, p => p.ToolTip("Icon used to display the spell."))
                .Property(m => m.Name, p => p.ShowMultiline().ToolTip("Display name of the spell."))
                .Property(m => m.CastLines, p => p.ToolTip("Number of chant lines to cast the spell."));
            b.Section("Target")
                .Property(m => m.TargetType, p => p.ToolTip("Type of target the spell can be cast on."))
                .Property(m => m.Prompt,
                    p => p.ShowMultiline()
                        .ToolTip("Prompt displayed to the user when casting the spell, if applicable."));
        });
    }

    private static void RegisterServerAnimateEntityMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerMotionMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the target entity."));
            b.Section("Animation")
                .Property(m => m.Animation, p => p.ToolTip("Animation to play."))
                .Property(m => m.Duration, p => p.ToolTip("Duration of the animation (lower is faster)."));
            b.Section("Sound")
                .Property(m => m.Sound, p => p.ToolTip("Sound to play along with the animation."))
                .IsExpanded(m => m.Sound != 0xFF);
        });
    }

    private static void RegisterServerBoardResultMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerBulletinMessage>(b =>
        {
            b.Section("Result")
                .Property(m => m.ResultType, p => p.ToolTip("Type of message board result."))
                .Property(m => m.ResultSuccess, p => p.ToolTip("Whether the message board submission was successful."))
                .Property(m => m.ResultMessage, p => p.ShowMultiline().ToolTip("Message to display to the user."));
            b.Section("Board List")
                .Property(m => m.Boards)
                .IsExpanded(m => m.Boards.Count > 0);
            b.Section("Board")
                .Property(m => m.BoardType, p => p.ToolTip("Type of message board interaction."))
                .Property(m => m.BoardId, p => p.ToolTip("ID of the message board."))
                .Property(m => m.BoardName, p => p.ShowMultiline().ToolTip("Display name of the message board."))
                .IsExpanded(m => m.ResultType is MessageBoardResult.Board or MessageBoardResult.Mailbox);
            b.Section("Post List")
                .Property(m => m.Posts)
                .IsExpanded(m => m.Posts.Count > 0);
            b.Section("Post")
                .Property(m => m.Post)
                .IsExpanded(m => m.Post is not null);
            b.Section("Navigation")
                .Property(m => m.CanNavigatePrev, p => p.ToolTip("Whether the previous button should be enabled."))
                .IsExpanded(m => m.ResultType is MessageBoardResult.Post or MessageBoardResult.MailLetter);
        });
    }

    private static void RegisterServerCooldownMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerActionDelayMessage>(b =>
        {
            b.Section("Cooldown")
                .Property(m => m.AbilityType, p => p.ToolTip("Type of ability that is now on cooldown."))
                .Property(m => m.Slot, p => p.ToolTip("Slot containing the ability that is now on cooldown."))
                .Property(m => m.Seconds, p => p.ToolTip("Number of seconds until the ability is available again."));
        });
    }

    private static void RegisterServerEntityTurnMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerChangeDirectionMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the target entity."));
            b.Section("Movement")
                .Property(m => m.Direction, p => p.ToolTip("Direction the entity should face."));
        });
    }

    private static void RegisterServerEntityWalkMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerMoveObjectMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the target entity."));
            b.Section("Position")
                .Property(m => m.OriginX, p => p.ToolTip("Original X-coordinate of the entity."))
                .Property(m => m.OriginY, p => p.ToolTip("Original Y-coordinate of the entity."));
            b.Section("Movement")
                .Property(m => m.Direction, p => p.ToolTip("Direction the entity should walk, from the origin."));
            b.Section("Unknown")
                .Property(m => m.Unknown, p => p.ShowHex());
        });
    }

    private static void RegisterServerExchangeMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerExchangeMessage>(b =>
        {
            b.Section("Event")
                .Property(m => m.Event, p => p.ToolTip("Type of exchange event."))
                .Property(m => m.Party, p => p.ToolTip("Party that is involved in the exchange."))
                .Property(m => m.Message, p => p.ShowMultiline().ToolTip("Message to display to the user."));
            b.Section("Target")
                .Property(m => m.TargetId, p => p.ShowHex().ToolTip("ID of the target user to exchange."))
                .Property(m => m.TargetName, p => p.ShowMultiline().ToolTip("Display name of the user to exchange."))
                .IsExpanded(m => m.Event == ExchangeServerEventType.Started);
            b.Section("Inventory")
                .Property(m => m.Slot, p => p.ToolTip("Slot containing the item."))
                .IsExpanded(m => m.Event == ExchangeServerEventType.QuantityPrompt);
            b.Section("Item")
                .Property(m => m.ItemIndex, p => p.ToolTip("Index of the item in the exchange window."))
                .Property(m => m.ItemSprite, p => p.ToolTip("Sprite of the item in the exchange window."))
                .Property(m => m.ItemColor, p => p.ToolTip("Override (dye) color applied to the sprite."))
                .Property(m => m.ItemName,
                    p => p.ShowMultiline().ToolTip("Display name of the item in the exchange window."))
                .IsExpanded(m => m.Event == ExchangeServerEventType.ItemAdded);
            b.Section("Gold")
                .Property(m => m.GoldAmount, p => p.ToolTip("Amount of gold displayed in the exchange window."))
                .IsExpanded(m => m.Event == ExchangeServerEventType.GoldAdded);
        });
    }

    private static void RegisterServerExitResponseMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerQuitMessage>(b =>
        {
            b.Section("Response")
                .Property(m => m.Result, p => p.ToolTip("Response to the exit request."))
                .Property(m => m.Unknown, p => p.ShowHex());
        });
    }

    private static void RegisterServerForcePacketMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerBounceMessage>(b =>
        {
            b.Section("Command")
                .Property(m => m.ClientCommand, p => p.ShowHex().ToolTip("Client command code for the raw packet."));
            b.Section("Data")
                .Property(m => m.Data, p => p.ShowMultiline().ToolTip("Raw payload to be sent by the client."));
        });
    }

    private static void RegisterServerGroupMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerGroupMessage>(b =>
        {
            b.Section("Action")
                .Property(m => m.Action, p => p.ToolTip("Type of group action."));
            b.Section("User")
                .Property(m => m.Name, p => p.ToolTip("Name of the user."));
            b.Section("Group Box")
                .Property(m => m.GroupBox, p => p.ShowMultiline().ToolTip("Display text for the floating group box."));
        });
    }

    private static void RegisterServerHealthBarMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerDamageEffectMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the target entity."));
            b.Section("Health Bar")
                .Property(m => m.Percent, p => p.ToolTip("Percentage of health remaining."));
            b.Section("Sound")
                .Property(m => m.Sound, p => p.ToolTip("Sound to play while displaying the health bar."))
                .IsExpanded(m => m.Sound != 0xFF);
            b.Section("Unknown")
                .Property(m => m.Unknown, p => p.ShowHex());
        });
    }

    private static void RegisterServerHeartbeatMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerRequestCRCMessage>(b =>
        {
            b.Section("Heartbeat")
                .Property(m => m.Request, p => p.ShowHex().ToolTip("Nonce of the heartbeat."));
        });
    }

    private static void RegisterServerHelloMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerHelloMessage>(b =>
        {
            b.Section("Greeting")
                .Property(m => m.Message, p => p.ShowMultiline().ToolTip("Greeting message from the server."));
        });
    }

    private static void RegisterServerLightLevelMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerChangeHourMessage>(b =>
        {
            b.Section("Time of Day")
                .Property(m => m.TimeOfDay,
                    p => p.ToolTip("Sundial position for time of day.\nThis is used on older client versions."));
            b.Section("Lighting")
                .Property(m => m.Lighting,
                    p => p.ShowHex().ToolTip("Lighting used for lanterns on the map, determined from metadata."));
        });
    }

    private static void RegisterServerLoginNoticeMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerStipulationMessage>(b =>
        {
            b.Section("Notice")
                .Property(m => m.Checksum, p => p.ShowHex().ToolTip("CRC-32 checksum of the notice message."))
                .Property(m => m.Content, p => p.ShowMultiline().ToolTip("Content of the notice message."));
        });
    }

    private static void RegisterServerLoginResultMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerLoginCheckMessage>(b =>
        {
            b.Section("Login")
                .Property(m => m.Result, p => p.ToolTip("Type of login result."))
                .Property(m => m.Message, p => p.ShowMultiline().ToolTip("Message to be displayed to the user."));
        });
    }

    private static void RegisterServerManufactureMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerManufactureMessage>(b =>
        {
            b.Section("Message")
                .Property(m => m.MessageType, p => p.ToolTip("Type of manufacture message."))
                .Property(m => m.ManufactureId, p => p.ShowHex().ToolTip("ID of the manufacture dialog."));
            b.Section("Recipe Count")
                .Property(m => m.RecipeCount, p => p.ToolTip("Number of recipes in the manufacture dialog."))
                .IsExpanded(m => m.RecipeCount.HasValue);
            b.Section("Recipe")
                .Property(m => m.RecipeIndex,
                    p => p.ToolTip("Zero-based index of the recipe in the manufacture dialog."))
                .Property(m => m.Sprite, p => p.ToolTip("Sprite of the recipe in the manufacture dialog."))
                .Property(m => m.RecipeName,
                    p => p.ShowMultiline().ToolTip("Name of the recipe in the manufacture dialog."))
                .Property(m => m.RecipeDescription,
                    p => p.ShowMultiline().ToolTip("Description of the recipe in the manufacture dialog."))
                .Property(m => m.Ingredients,
                    p => p.ShowMultiline().ToolTip("List of ingredients for the recipe in the manufacture dialog."))
                .IsExpanded(m => m.RecipeIndex.HasValue);
        });
    }
    
    private static void RegisterServerWeatherChangedMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerChangeWeatherMessage>(b =>
        {
            b.Section("Weather")
                .Property(m => m.WeatherFlags, p => p.ShowHex().ToolTip("Active weather effect flags."));
        });
    }

    private static void RegisterServerMapChangingMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerMapTransferOKMessage>(b =>
        {
            b.Section("Map")
                .Property(m => m.ChangeType, p => p.ToolTip("Type of map change."));
            b.Section("Unknown")
                .Property(m => m.Unknown, p => p.ShowHex());
        });
    }

    private static void RegisterServerMapDoorMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerStaticObjectStateMessage>(b =>
        {
            b.Section("Doors")
                .Property(m => m.Doors);
        });
    }

    private static void RegisterServerMapInfoMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerMapSizeMessage>(b =>
        {
            b.Section("Map")
                .Property(m => m.MapId, p => p.ToolTip("ID of the map."))
                .Property(m => m.Name, p => p.ToolTip("Display name of the map."))
                .Property(m => m.Checksum, p => p.ShowHex().ToolTip("CRC-16 checksum of the map."));
            b.Section("Dimensions")
                .Property(m => m.Width, p => p.ToolTip("Width of the map, in tiles."))
                .Property(m => m.Height, p => p.ToolTip("Height of the map, in tiles."));
            b.Section("Flags")
                .Property(m => m.Flags, p => p.ToolTip("Current flags for the map."));
        });
    }

    private static void RegisterServerMapLocationMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerUserPositionMessage>(b =>
        {
            b.Section("Position")
                .Property(m => m.X, p => p.ToolTip("Current X-coordinate of the user."))
                .Property(m => m.Y, p => p.ToolTip("Current Y-coordinate of the user."));
            b.Section("Unknown")
                .Property(m => m.UnknownX, p => p.ShowHex())
                .Property(m => m.UnknownY, p => p.ShowHex());
        });
    }

    private static void RegisterServerMapTransferCompleteMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerUserReadyMessage>(b =>
        {
            b.Section("Result")
                .Property(m => m.Result, p => p.ToolTip("Result of the map transfer."));
        });
    }

    private static void RegisterServerMapTransferMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerMapPartMessage>(b =>
        {
            b.Section("Row Index")
                .Property(m => m.RowY, p => p.ToolTip("Y-index of the map tiles transfer."));
            b.Section("Data")
                .Property(m => m.Data, p => p.ShowMultiline().ToolTip("Map tiles for the current row."));
        });
    }

    private static void RegisterServerMetadataMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerMetadataMessage>(b =>
        {
            b.Section("Response")
                .Property(m => m.ResponseType, p => p.ToolTip("Type of metadata response."));
            b.Section("Metadata")
                .Property(m => m.Name, p => p.ToolTip("Name of the metadata file."))
                .Property(m => m.Checksum, p => p.ShowHex().ToolTip("CRC-32 checksum of the metadata file."))
                .Property(m => m.Data, p => p.ShowMultiline().ToolTip("Raw data of the metadata file."))
                .IsExpanded(m => m.ResponseType == MetadataResponseType.Metadata);
            b.Section("Listing")
                .Property(m => m.MetadataFiles)
                .IsExpanded(m => m.ResponseType == MetadataResponseType.Listing);
        });
    }

    private static void RegisterServerPlaySoundMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerSoundEffectMessage>(b =>
        {
            b.Section("Sound")
                .Property(m => m.Sound, p => p.ToolTip("Sound to play."))
                .IsExpanded(m => m.Sound != 0xFF);
            b.Section("Music")
                .Property(m => m.Track, p => p.ToolTip("Music track to play."))
                .Property(m => m.Unknown, p => p.ShowHex())
                .IsExpanded(m => m.Sound == 0xFF);
        });
    }

    private static void RegisterServerPublicMessageMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerSayMessage>(b =>
        {
            b.Section("Message")
                .Property(m => m.MessageType, p => p.ToolTip("Type of public message."))
                .Property(m => m.SenderId, p => p.ShowHex().ToolTip("ID of the entity saying the message."))
                .Property(m => m.Message, p => p.ShowMultiline().ToolTip("Content of the public message."));
        });
    }

    private static void RegisterServerRedirectMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerTransferServerMessage>(b =>
        {
            b.Section("Server")
                .Property(m => m.Address, p => p.ToolTip("Address of the server to redirect to."))
                .Property(m => m.Port, p => p.ToolTip("Port of the server to redirect to."));
            b.Section("Encryption")
                .Property(m => m.Seed, p => p.ToolTip("Seed table used for encryption."))
                .Property(m => m.PrivateKey, p => p.ShowMultiline().ToolTip("Private key used for encryption."));
            b.Section("Connection")
                .Property(m => m.ConnectionId, p => p.ShowHex().ToolTip("Connection ID of the client."))
                .Property(m => m.Name);
        });
    }

    private static void RegisterServerRefreshCompletedMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerRefreshUserOKMessage>(b =>
        {
            // Nothing to map
        });
    }

    private static void RegisterServerRemoveEntityMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerRemoveObjectsMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the target entity."));
        });
    }

    private static void RegisterServerRemoveEquipmentMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerRemoveEquipMessage>(b =>
        {
            b.Section("Equipment")
                .Property(m => m.Slot, p => p.ToolTip("Equipment slot to be removed."));
        });
    }

    private static void RegisterServerRemoveItemMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerRemoveInventoryMessage>(b =>
        {
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot to be removed."));
        });
    }

    private static void RegisterServerRemoveSkillMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerRemoveSkillMessage>(b =>
        {
            b.Section("Skill")
                .Property(m => m.Slot, p => p.ToolTip("Skill slot to be removed."));
        });
    }

    private static void RegisterServerRemoveSpellMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerRemoveSpellMessage>(b =>
        {
            b.Section("Spell")
                .Property(m => m.Slot, p => p.ToolTip("Spell slot to be removed."));
        });
    }

    private static void RegisterServerRequestUserPortraitMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerRequestPortraitMessage>(b =>
        {
            // Nothing to map    
        });
    }

    private static void RegisterServerSelfProfileMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerSelfLookMessage>(b =>
        {
            b.Section("Profile")
                .Property(m => m.Class, p => p.ToolTip("Base class of the user."))
                .Property(m => m.DisplayClass, p => p.ToolTip("Display class of the user."))
                .Property(m => m.Guild, p => p.ToolTip("Guild the user is a member of."))
                .Property(m => m.GuildRank, p => p.ToolTip("Guild rank of the user."))
                .Property(m => m.Title, p => p.ToolTip("Title of the user."));
            b.Section("Citizenship")
                .Property(m => m.Nation, p => p.ToolTip("Nation flag displayed for the user."));
            b.Section("Group")
                .Property(m => m.IsGroupOpen, p => p.ToolTip("Whether the user is accepting group invitations."))
                .Property(m => m.IsRecruiting, p => p.ToolTip("Whether the user is recruiting for group members."))
                .Property(m => m.GroupMembers, p => p.ShowMultiline());
            b.Section("Group Box")
                .Property(m => m.GroupBox, p => p.ShowMultiline().ToolTip("Display text for the floating group box."))
                .IsExpanded(m => m.IsRecruiting);
            b.Section("Metadata")
                .Property(m => m.ShowMasterMetadata, p => p.ToolTip("Whether to show the Master metadata tab."))
                .Property(m => m.ShowAbilityMetadata, p => p.ToolTip("Whether to show the Ability metadata tab."));
            b.Section("Legend")
                .Property(m => m.LegendMarks)
                .IsExpanded(_ => false);
            b.Section("Legend (Text)")
                .Computed("Text", m => string.Join(System.Environment.NewLine, m.LegendMarks.Select(l => l.Text)),
                    p => p.ShowMultiline().ToolTip("List of the user's legend marks."));
        });
    }

    private static void RegisterServerServerInfoMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerBrowserMessage>(b =>
        {
            b.Section("Information")
                .Property(m => m.DataType, p => p.ToolTip("Type of server information."))
                .Property(m => m.Value, p => p.ShowMultiline().ToolTip("Information sent by the server."));
        });
    }

    private static void RegisterServerServerListMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerVersionCheckMessage>(b =>
        {
            b.Section("Server List")
                .Property(m => m.Checksum, p => p.ShowHex().ToolTip("CRC-32 checksum of the server table."));
            b.Section("Encryption")
                .Property(m => m.Seed, p => p.ToolTip("Seed table used for encryption."))
                .Property(m => m.PrivateKey, p => p.ShowMultiline().ToolTip("Private key used for encryption."));
        });
    }

    private static void RegisterServerServerTableMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerMultiServerMessage>(b =>
        {
            b.Section("Servers")
                .Property(m => m.Servers);
        });
    }

    private static void RegisterServerSetEquipmentMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerAddEquipMessage>(b =>
        {
            b.Section("Equipment")
                .Property(m => m.Slot, p => p.ToolTip("Equipment slot to be set."))
                .Property(m => m.Sprite, p => p.ToolTip("Sprite used to display the equipment."))
                .Property(m => m.Color, p => p.ToolTip("Override (dye) color applied to the sprite."))
                .Property(m => m.Name, p => p.ShowMultiline().ToolTip("Display name of the equipment."));
            b.Section("Durability")
                .Property(m => m.Durability, p => p.ToolTip("Durability of the equipment."))
                .Property(m => m.MaxDurability, p => p.ToolTip("Maximum durability of the equipment."))
                .IsExpanded(m => m.MaxDurability > 0);
        });
    }

    private static void RegisterServerShowDialogMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerPursuitMessage>(b =>
        {
            b.Section("Dialog")
                .Property(m => m.DialogType, p => p.ToolTip("Type of dialog."))
                .Property(m => m.StepId, p => p.ToolTip("ID of the current step."))
                .Property(m => m.PursuitId, p => p.ToolTip("ID of the pursuit."));
            b.Section("Source")
                .Property(m => m.EntityType, p => p.ToolTip("Type of entity responsible for the dialog."))
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the entity responsible for the dialog."))
                .Property(m => m.Name, p => p.ToolTip("Display name of the entity in the dialog."))
                .IsExpanded(m => m.DialogType != DialogType.CloseDialog);
            b.Section("Content")
                .Property(m => m.Sprite, p => p.ToolTip("Sprite displayed in the dialog."))
                .Property(m => m.SpriteType, p => p.ToolTip("Type of sprite displayed in the dialog."))
                .Property(m => m.Color, p => p.ToolTip("Override color applied to the sprite."))
                .Property(m => m.Content, p => p.ShowMultiline().ToolTip("Message displayed in the dialog."))
                .Property(m => m.ShowGraphic, p => p.ToolTip("Whether to show the graphic."))
                .IsExpanded(m => m.DialogType != DialogType.CloseDialog);
            b.Section("Menu Choices")
                .Property(m => m.MenuChoices)
                .IsExpanded(m => m.DialogType is DialogType.Menu or DialogType.CreatureMenu);
            b.Section("Text Input")
                .Property(m => m.InputPrompt, p => p.ShowMultiline().ToolTip("Prompt displayed above the text input."))
                .Property(m => m.InputDescription,
                    p => p.ShowMultiline().ToolTip("Description displayed below the text input."))
                .Property(m => m.InputMaxLength, p => p.ToolTip("Maximum length of the text input."))
                .IsExpanded(m => m.DialogType == DialogType.TextInput);
            b.Section("Navigation")
                .Property(m => m.HasPreviousButton, p => p.ToolTip("Whether the dialog has a previous step."))
                .Property(m => m.HasNextButton, p => p.ToolTip("Whether the dialog has a next step."))
                .IsExpanded(m => m.DialogType != DialogType.CloseDialog);
            b.Section("Unknown")
                .Property(m => m.Unknown1, p => p.ShowHex())
                .Property(m => m.Unknown2, p => p.ShowHex())
                .IsExpanded(m => m.DialogType != DialogType.CloseDialog);
        });
    }

    private static void RegisterServerShowDialogMenuMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerScreenMenuMessage>(b =>
        {
            b.Section("Menu")
                .Property(m => m.MenuType, p => p.ToolTip("Type of dialog menu."))
                .Property(m => m.PursuitId, p => p.ToolTip("ID of the pursuit."));
            b.Section("Source")
                .Property(m => m.EntityType, p => p.ToolTip("Type of entity responsible for the dialog menu."))
                .Property(m => m.EntityId,
                    p => p.ShowHex().ToolTip("ID of the entity responsible for the dialog menu."))
                .Property(m => m.Name, p => p.ToolTip("Display name of the entity in the dialog menu."));
            b.Section("Content")
                .Property(m => m.Sprite, p => p.ToolTip("Sprite displayed in the dialog menu."))
                .Property(m => m.Color, p => p.ToolTip("Override color applied to the sprite."))
                .Property(m => m.Content, p => p.ShowMultiline().ToolTip("Message displayed in the dialog menu."))
                .Property(m => m.ShowGraphic, p => p.ToolTip("Whether to show the graphic."));
            b.Section("Arguments")
                .Property(m => m.Prompt, p => p.ShowMultiline().ToolTip("Prompt displayed for input arguments."))
                .IsExpanded(m => m.MenuType is DialogMenuType.MenuWithArgs or DialogMenuType.TextInputWithArgs);
            b.Section("Menu Choices")
                .Property(m => m.MenuChoices)
                .IsExpanded(m => m.MenuType is DialogMenuType.Menu or DialogMenuType.MenuWithArgs);
            b.Section("Item Choices")
                .Property(m => m.ItemChoices)
                .IsExpanded(m => m.MenuType == DialogMenuType.ItemChoices);
            b.Section("Skill Choices")
                .Property(m => m.SkillChoices)
                .IsExpanded(m => m.MenuType == DialogMenuType.SkillChoices);
            b.Section("Spell Choices")
                .Property(m => m.SpellChoices)
                .IsExpanded(m => m.MenuType == DialogMenuType.SpellChoices);
            b.Section("Inventory")
                .Property(m => m.InventorySlots, p => p.ToolTip("Inventory slots to be displayed as choices."))
                .IsExpanded(m => m.MenuType == DialogMenuType.UserInventory);
            b.Section("Unknown")
                .Property(m => m.Unknown1, p => p.ShowHex())
                .Property(m => m.Unknown2, p => p.ShowHex());
        });
    }

    private static void RegisterServerShowEffectMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerEffectLayerMessage>(b =>
        {
            b.Section("Target Entity")
                .Property(m => m.TargetId, p => p.ShowHex().ToolTip("ID of the target entity."))
                .IsExpanded(m => m.TargetId != 0);
            b.Section("Target Location")
                .Property(m => m.TargetX, p => p.ToolTip("X-coordinate of the target map location."))
                .Property(m => m.TargetY, p => p.ToolTip("Y-coordinate of the target map location."))
                .IsExpanded(m => m.TargetId == 0);
            b.Section("Source Entity")
                .Property(m => m.SourceId, p => p.ShowHex().ToolTip("ID of the source entity."))
                .IsExpanded(m => m.TargetId != 0);
            b.Section("Visual Effect")
                .Property(m => m.TargetAnimation, p => p.ToolTip("Visual effect to be played on the target entity."))
                .Property(m => m.SourceAnimation, p => p.ToolTip("Visual effect to be played on the source entity."))
                .Property(m => m.AnimationDuration,
                    p => p.ToolTip("Duration of the visual effect animation (lower is faster)."));
        });
    }

    private static void RegisterServerShowMapHelpMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerScreenShotMessage>(b =>
        {
            b.Section("Map")
                .Property(m => m.MapIndex, p => p.ToolTip("Index of the map to show help for."));
        });
    }

    private static void RegisterServerShowNotepadMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerEnterEditingModeMessage>(b =>
        {
            b.Section("Notepad")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot containing the note item."))
                .Property(m => m.Style, p => p.ToolTip("Display style of the notepad."));
            b.Section("Dimensions")
                .Property(m => m.Width, p => p.ToolTip("Width of the notepad."))
                .Property(m => m.Height, p => p.ToolTip("Height of the notepad."));
            b.Section("Content")
                .Property(m => m.Content, p => p.ShowMultiline().ToolTip("Current content of the notepad."));
        });
    }

    private static void RegisterServerShowSpinnerMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerBlockInputMessage>(b =>
        {
            b.Section("Visibility")
                .Property(m => m.IsVisible, p => p.ToolTip("Whether the spinner is visible."));
        });
    }

    private static void RegisterServerShowUserMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerDrawHumanObjectsMessage>(b =>
        {
            b.Section("User")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the user."))
                .Property(m => m.NameStyle, p => p.ToolTip("Display style of the user's name tag."))
                .Property(m => m.Name, p => p.ToolTip("Display name of the user."));
            b.Section("Group Box")
                .Property(m => m.GroupBox, p => p.ShowMultiline().ToolTip("Display text for the floating group box."));
            b.Section("Position")
                .Property(m => m.X, p => p.ToolTip("X-coordinate of the user."))
                .Property(m => m.Y, p => p.ToolTip("Y-coordinate of the user."))
                .Property(m => m.Direction, p => p.ToolTip("Direction the user is currently facing."));
            b.Section("Body")
                .Property(m => m.HeadSprite, p => p.ToolTip("Sprite displayed for the user's head."))
                .Property(m => m.FaceShape, p => p.ToolTip("Face shape of the user."))
                .Property(m => m.HairColor, p => p.ToolTip("Hair color of the user."))
                .Property(m => m.BodySprite, p => p.ShowHex().ToolTip("Sprite displayed for the user's body."))
                .Property(m => m.SkinColor, p => p.ToolTip("Skin color of the user."));
            b.Section("Visibility")
                .Property(m => m.IsTranslucent, p => p.ToolTip("Whether the user is translucent."))
                .Property(m => m.IsHidden, p => p.ToolTip("Whether the user is hidden (invisible)."))
                .IsExpanded(m => m.IsTranslucent || m.IsHidden);
            b.Section("Equipment")
                .Property(m => m.ArmsSprite,
                    p => p.ToolTip("Sprite displayed for the user's arms, paired with their armor."))
                .Property(m => m.ArmorSprite,
                    p => p.ToolTip("Sprite displayed for the user's armor."))
                .Property(m => m.PantsColor, p => p.ToolTip("Color applied to the user's pants."))
                .Property(m => m.BootsSprite, p => p.ToolTip("Sprite displayed for the user's boots."))
                .Property(m => m.BootsColor, p => p.ToolTip("Color applied to the user's boots."))
                .Property(m => m.WeaponSprite, p => p.ToolTip("Sprite displayed for the user's weapon."))
                .Property(m => m.ShieldSprite, p => p.ToolTip("Sprite displayed for the user's shield."))
                .Property(m => m.Accessory1Sprite,
                    p => p.ToolTip("Sprite displayed for the user's first accessory."))
                .Property(m => m.Accessory1Color,
                    p => p.ToolTip("Override (dye) color applied to the user's first accessory."))
                .Property(m => m.Accessory2Sprite,
                    p => p.ToolTip("Sprite displayed for the user's second accessory."))
                .Property(m => m.Accessory2Color,
                    p => p.ToolTip("Override (dye) color applied to the user's second accessory."))
                .Property(m => m.Accessory3Sprite,
                    p => p.ToolTip("Sprite displayed for the user's third accessory."))
                .Property(m => m.Accessory3Color,
                    p => p.ToolTip("Override (dye) color applied to the user's third accessory."))
                .Property(m => m.OvercoatColor, p => p.ToolTip("Color applied to the user's overcoat."))
                .Property(m => m.OvercoatSprite, p => p.ToolTip("Sprite displayed for the user's overcoat."))
                .Property(m => m.Lantern, p => p.ToolTip("Type of lantern equipped by the user."))
                .IsExpanded(m => m.HeadSprite != 0xFFFF);
            b.Section("Resting")
                .Property(m => m.RestPosition, p => p.ToolTip("Resting posture to be shown for the user."))
                .IsExpanded(m => m.RestPosition != RestPosition.None);
            b.Section("Monster Form")
                .Property(m => m.MonsterSprite, p => p.ToolTip("Sprite to display the user as a monster."))
                .Property(m => m.MonsterUnknown)
                .IsExpanded(m => m.HeadSprite == 0xFFFF);
        });
    }

    private static void RegisterServerStatPointsMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerLevelPointMessage>(b =>
        {
            b.Section("Stat Points")
                .Property(m => m.FlashButtons,
                    p => p.ToolTip("Whether the stat buttons should play their flash animation."))
                .Property(m => m.StatPoints, p => p.ToolTip("Number of available stat points."));
        });
    }

    private static void RegisterServerStatusEffectMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerSpelledMessage>(b =>
        {
            b.Section("Effect")
                .Property(m => m.Icon, p => p.ToolTip("Icon displayed for the status effect."))
                .Property(m => m.Duration, p => p.ToolTip("Remaining duration of the status effect."));
        });
    }

    private static void RegisterServerSwitchPaneMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerWindowChangeMessage>(b =>
        {
            b.Section("Interface")
                .Property(m => m.Pane, p => p.ToolTip("Interface pane to be switched to."));
        });
    }

    private static void RegisterServerSyncTicksMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerCheckTimeMessage>(b =>
        {
            b.Section("Ticks")
                .Property(m => m.TickCount, p => p.ToolTip("Server-side tick count, used for synchronizing timing."));
        });
    }

    private static void RegisterServerUpdateStatsMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerStatusMessage>(b =>
        {
            b.Section("Fields")
                .Property(m => m.Fields,
                    p => p.ShowMultiline().ToolTip("Which fields are included within the update."));
            b.Section("Flags")
                .Property(m => m.IsAdmin, p => p.ToolTip("Whether the user is an administrator (GM)."))
                .Property(m => m.IsSwimming, p => p.ToolTip("Whether the user is currently swimming."))
                .IsExpanded(_ => false);
            b.Section("Level")
                .Property(m => m.Level, p => p.ToolTip("Level of the user's base class."))
                .Property(m => m.AbilityLevel, p => p.ToolTip("Level of the user's ability class."))
                .IsExpanded(m => m.Level.HasValue);
            b.Section("Vitals")
                .Property(m => m.Health, p => p.ToolTip("Current health of the user."))
                .Property(m => m.MaxHealth, p => p.ToolTip("Maximum health of the user."))
                .Property(m => m.Mana, p => p.ToolTip("Current mana of the user."))
                .Property(m => m.MaxMana, p => p.ToolTip("Maximum mana of the user."))
                .IsExpanded(m => m.Fields.HasFlag(StatsFieldFlags.Vitals));
            b.Section("Stats")
                .Property(m => m.Strength, p => p.ToolTip("Current strength stat of the user."))
                .Property(m => m.Intelligence, p => p.ToolTip("Current intelligence stat of the user."))
                .Property(m => m.Wisdom, p => p.ToolTip("Current wisdom stat of the user."))
                .Property(m => m.Constitution, p => p.ToolTip("Current constitution stat of the user."))
                .Property(m => m.Dexterity, p => p.ToolTip("Current dexterity stat of the user."))
                .Property(m => m.HasStatPoints, p => p.ToolTip("Whether the user has stat points to spent."))
                .Property(m => m.StatPoints, p => p.ToolTip("Number of stat points the user has available to spend."))
                .IsExpanded(m => m.Fields.HasFlag(StatsFieldFlags.Stats));
            b.Section("Modifiers")
                .Property(m => m.ArmorClass, p => p.ToolTip("Current armor class of the user (lower is better)."))
                .Property(m => m.MagicResist,
                    p => p.ToolTip("Current magic resistance of the user (higher is better)."))
                .Property(m => m.DamageModifier,
                    p => p.ToolTip("Current damage modifier of the user (higher is better)."))
                .Property(m => m.HitModifier, p => p.ToolTip("Current hit modifier of the user (higher is better)."))
                .Property(m => m.AttackElement, p => p.ToolTip("Current attack element of the user."))
                .Property(m => m.DefenseElement, p => p.ToolTip("Current defense element of the user."))
                .Property(m => m.IsBlinded, p => p.ToolTip("Whether the user is currently blinded (black screen)."))
                .Property(m => m.CanMove, p => p.ToolTip("Whether the user can move."))
                .IsExpanded(m => m.Fields.HasFlag(StatsFieldFlags.Modifiers));
            b.Section("Experience")
                .Property(m => m.TotalExperience, p => p.ToolTip("Total experience points the user has accumulated."))
                .Property(m => m.ToNextLevel, p => p.ToolTip("Experience points needed to reach the next level."))
                .Property(m => m.TotalAbility, p => p.ToolTip("Total ability points the user has accumulated."))
                .Property(m => m.ToNextAbility, p => p.ToolTip("Ability points needed to reach the next level."))
                .IsExpanded(m => m.Fields.HasFlag(StatsFieldFlags.ExperienceGold));
            b.Section("Currency")
                .Property(m => m.Gold, p => p.ToolTip("Current gold amount the user has."))
                .Property(m => m.GamePoints, p => p.ToolTip("Current game points the user has."))
                .IsExpanded(m => m.Fields.HasFlag(StatsFieldFlags.ExperienceGold));
            b.Section("Weight")
                .Property(m => m.Weight, p => p.ToolTip("Current carrying weight of the user."))
                .Property(m => m.MaxWeight, p => p.ToolTip("Maximum carrying weight of the user."))
                .IsExpanded(m => m.Fields.HasFlag(StatsFieldFlags.Stats));
            b.Section("Mail")
                .Property(m => m.HasUnreadMail, p => p.ToolTip("Whether the user has unread mail."))
                .Property(m => m.HasParcelsAvailable,
                    p => p.ToolTip("Whether the user has available parcels to pickup."))
                .IsExpanded(m => m.Fields.HasFlag(StatsFieldFlags.Modifiers));
            b.Section("Unknown")
                .Property(m => m.Unknown, p => p.ShowHex().ToolTip("Unknown data."))
                .IsExpanded(_ => false);
        });
    }

    private static void RegisterServerUserIdMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerUserAppearanceMessage>(b =>
        {
            b.Section("User")
                .Property(m => m.UserId, p => p.ShowHex().ToolTip("ID of the user."))
                .Property(m => m.Class, p => p.ToolTip("Base class of the user."));
            b.Section("Movement")
                .Property(m => m.Direction, p => p.ToolTip("Direction the user is currently facing."))
                .Property(m => m.CanMove, p => p.ToolTip("Whether the user is allowed to move."));
            b.Section("Guild")
                .Property(m => m.HasGuild, p => p.ToolTip("Whether the user is a member of a guild."));
        });
    }

    private static void RegisterServerUserIdResponseMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerUserIdMessage>(b =>
        {
            b.Section("User")
                .Property(m => m.UserId, p => p.ShowHex().ToolTip("ID of the user."));
            b.Section("Response")
                .Property(m => m.Nonce, p => p.ToolTip("Nonce that was sent by the client."));
        });
    }

    private static void RegisterServerUserProfileMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerObjectInfoMessage>(b =>
        {
            b.Section("User")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the user."));
            b.Section("Profile")
                .Property(m => m.Name, p => p.ToolTip("Display name of the user."))
                .Property(m => m.DisplayClass, p => p.ToolTip("Display class of the user."))
                .Property(m => m.Guild, p => p.ToolTip("Guild the user is a member of."))
                .Property(m => m.GuildRank, p => p.ToolTip("Guild rank of the user."))
                .Property(m => m.Title, p => p.ToolTip("Title of the user."));
            b.Section("Citizenship")
                .Property(m => m.Nation, p => p.ToolTip("Nation flag displayed for the user."));
            b.Section("Status")
                .Property(m => m.Status, p => p.ToolTip("Social status of the user."))
                .Property(m => m.IsGroupOpen,
                    p => p.ToolTip("Whether the user is currently accepting group invitations."));
            b.Section("Equipment")
                .Property(m => m.Equipment)
                .IsExpanded(_ => false);
            b.Section("Legend")
                .Property(m => m.LegendMarks)
                .IsExpanded(_ => false);
            b.Section("Legend (Text)")
                .Computed("Text", m => string.Join(System.Environment.NewLine, m.LegendMarks.Select(l => l.Text)),
                    p => p.ShowMultiline().ToolTip("List of the user's legend marks."));
            b.Section("Portrait")
                .Property(m => m.Portrait, p => p.ShowMultiline().ToolTip("Raw portrait data for the user."))
                .IsExpanded(m => m.Portrait is not null && m.Portrait.Count > 0);
            b.Section("Bio")
                .Property(m => m.Bio, p => p.ShowMultiline().ToolTip("Biography of the user."))
                .IsExpanded(m => !string.IsNullOrEmpty(m.Bio));
        });
    }

    private static void RegisterServerWalkResponseMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerMoveMessage>(b =>
        {
            b.Section("Movement")
                .Property(m => m.Direction, p => p.ToolTip("Direction the user has walked."));
            b.Section("Position")
                .Property(m => m.PreviousX, p => p.ToolTip("Previous X-coordinate of the user."))
                .Property(m => m.PreviousY, p => p.ToolTip("Previous Y-coordinate of the user."));
            b.Section("Unknown")
                .Property(m => m.UnknownX, p => p.ShowHex())
                .Property(m => m.UnknownY, p => p.ShowHex())
                .Property(m => m.Unknown, p => p.ShowHex());
        });
    }

    private static void RegisterServerWorldListMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerShowUsersMessage>(b =>
        {
            b.Section("Totals")
                .Property(m => m.WorldCount, p => p.ToolTip("Total number of users across all world servers."))
                .Property(m => m.CountryCount, p => p.ToolTip("Total number of users in the current world server."));
            b.Section("Users")
                .Property(m => m.Users)
                .IsExpanded(_ => false);
            b.Section("Users (Text)")
                .Computed("Text", m => string.Join(System.Environment.NewLine, m.Users.Select(l => l.Name)),
                    p => p.ShowMultiline().ToolTip("List of users in the current world server."));
        });
    }

    private static void RegisterServerWorldMapMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerFieldMapMessage>(b =>
        {
            b.Section("Field")
                .Property(m => m.FieldIndex, p => p.ToolTip("Index of the field to be displayed."))
                .Property(m => m.FieldName, p => p.ToolTip("Name of the field to be displayed."));
            b.Section("Locations")
                .Property(m => m.Locations);
        });
    }

    private static void RegisterServerWorldMessageMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ServerSystemMessage>(b =>
        {
            b.Section("Message")
                .Property(m => m.MessageType, p => p.ToolTip("Type of the message to be displayed."))
                .Property(m => m.Message, p => p.ShowMultiline().ToolTip("Content of the message."));
        });
    }
}
