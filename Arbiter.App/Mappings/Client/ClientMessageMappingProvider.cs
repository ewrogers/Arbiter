using Arbiter.Net.Client.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.Mappings.Client;

public class ClientMessageMappingProvider : IInspectorMappingProvider
{
    public void RegisterMappings(InspectorMappingRegistry registry)
    {
        RegisterClientAssailMapping(registry);
        RegisterClientAuthenticateMapping(registry);
        RegisterClientBeginSpellCastMapping(registry);
        RegisterClientBoardActionMapping(registry);
        RegisterClientCastSpellMapping(registry);
        RegisterClientChangePasswordMapping(registry);
        RegisterClientCreateCharacterAppearance(registry);
        RegisterClientCreateCharacterNameMapping(registry);
        RegisterClientDialogChoiceMapping(registry);
        RegisterClientDialogMenuChoiceMapping(registry);
        RegisterClientDropGoldMapping(registry);
        RegisterClientDropItemMapping(registry);
        RegisterClientEatItemMapping(registry);
        RegisterClientEditNotepadMapping(registry);
        RegisterClientEmoteMapping(registry);
        RegisterClientExceptionMapping(registry);
        RegisterClientExchangeActionMapping(registry);
        RegisterClientGiveGoldMapping(registry);
        RegisterClientGiveItemMapping(registry);
        RegisterClientGroupInviteMapping(registry);
        RegisterClientHeartbeatMapping(registry);
        RegisterClientIgnoreUserMapping(registry);
        RegisterClientInteractMapping(registry);
        RegisterClientLoginMapping(registry);
        RegisterClientLookTileMapping(registry);
        RegisterClientManufactureMapping(registry);
        RegisterClientPickupItemMapping(registry);
        RegisterClientRaiseStatMapping(registry);
        RegisterClientRequestEntityMapping(registry);
        RegisterClientRequestExitMapping(registry);
        RegisterClientRequestHomepageMapping(registry);
        RegisterClientRequestLoginNoticeMapping(registry);
        RegisterClientRequestMetadataMapping(registry);
        RegisterClientRequestProfileMapping(registry);
        RegisterClientRequestSequenceMapping(registry);
        RegisterClientRequestServerTableMapping(registry);
        RegisterClientRequestUserIdMapping(registry);
        RegisterClientSayMapping(registry);
        RegisterClientSetStatusMapping(registry);
        RegisterClientSpellChantMapping(registry);
        RegisterClientSwapSlotMapping(registry);
        RegisterClientSyncTicksMapping(registry);
        RegisterClientToggleSettingMapping(registry);
        RegisterClientTurnMapping(registry);
        RegisterClientUnequipItemMapping(registry);
        RegisterClientUseItem(registry);
        RegisterClientUserPortraitMapping(registry);
        RegisterClientUseSkillMapping(registry);
        RegisterClientVersionMapping(registry);
        RegisterClientWalkMapping(registry);
        RegisterClientWhisperMapping(registry);
        RegisterClientWorldMapClickMapping(registry);
    }

    private static void RegisterClientAssailMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientAttackMessage>(b =>
        {
            // No mappings
        });
    }

    private static void RegisterClientAuthenticateMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientTransferServerMessage>(b =>
        {
            b.Section("Connection")
                .Property(m => m.ConnectionId, p => p.ShowHex().ToolTip("Connection ID of the client."))
                .Property(m => m.Name, p => p.ToolTip("Name of the client."));
            b.Section("Encryption")
                .Property(m => m.Seed, p => p.ToolTip("Seed table used for encryption."))
                .Property(m => m.PrivateKey, p => p.ShowMultiline().ToolTip("Private key used for encryption."));
        });
    }

    private static void RegisterClientBeginSpellCastMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientSpellDelayRequestMessage>(b =>
        {
            b.Section("Spell")
                .Property(m => m.LineCount, p => p.ToolTip("Number of chant lines to cast the spell."));
        });
    }

    private static void RegisterClientBoardActionMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientBulletinMessage>(b =>
        {
            b.Section("Action")
                .Property(m => m.Action, p => p.ToolTip("Message board action to perform."));
            b.Section("Board")
                .Property(m => m.BoardId, p => p.ToolTip("ID of the message board."))
                .Property(m => m.StartPostId, p => p.ToolTip("ID of the first post to fetch."))
                .IsExpanded(m => m.BoardId.HasValue && m.Action != MessageBoardAction.SendMail);
            b.Section("Post")
                .Property(m => m.PostId, p => p.ToolTip("ID of the post to fetch."))
                .IsExpanded(m => m.PostId.HasValue);
            b.Section("Navigation")
                .Property(m => m.Navigation, p => p.ToolTip("Navigation to perform on the board."))
                .IsExpanded(m => m.Action == MessageBoardAction.ViewPost);
            b.Section("Create Post")
                .Property(m => m.Subject, p => p.ShowMultiline().ToolTip("Subject for the post."))
                .Property(m => m.Body, p => p.ShowMultiline().ToolTip("Content of the post."))
                .IsExpanded(m => m.Action == MessageBoardAction.CreatePost);
            b.Section("Send Mail")
                .Property(m => m.Recipient, p => p.ToolTip("Recipient of the mail message."))
                .Property(m => m.MailSubject, p => p.ShowMultiline().ToolTip("Subject for the mail message."))
                .Property(m => m.MailBody, p => p.ShowMultiline().ToolTip("Content of the mail message."))
                .IsExpanded(m => m.Action == MessageBoardAction.SendMail);
            b.Section("Unknown")
                .Property(m => m.Unknown, p => p.ShowHex())
                .IsExpanded(m => m.Action == MessageBoardAction.ViewBoard);
        });
    }

    private static void RegisterClientCastSpellMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientUseSpellMessage>(b =>
        {
            b.Section("Spell")
                .Property(m => m.Slot, p => p.ToolTip("Slot of the spell to cast."));
            b.Section("Target")
                .Property(m => m.TargetId, p => p.ShowHex().ToolTip("ID of the target of the spell."))
                .Property(m => m.TargetX, p => p.ToolTip("X-coordinate of the target of the spell."))
                .Property(m => m.TargetY, p => p.ToolTip("Y-coordinate of the target of the spell."))
                .IsExpanded(m => m.TargetId.HasValue);
            b.Section("Text Input")
                .Property(m => m.TextInput, p => p.ShowMultiline().ToolTip("User input when casting the spell."))
                .IsExpanded(m => !string.IsNullOrEmpty(m.TextInput));
            b.Section("Numeric Input")
                .Property(m => m.NumericInputs, p => p.ToolTip("Numeric inputs when casting the spell."))
                .IsExpanded(m => m.NumericInputs.Count > 0);
        });
    }

    private static void RegisterClientChangePasswordMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientChangePasswordMessage>(b =>
        {
            b.Section("Credentials")
                .Property(m => m.Name, p => p.ToolTip("Name of the character."))
                .Property(m => m.CurrentPassword, p => p.Mask().ToolTip("Current password of the character."))
                .Property(m => m.NewPassword, p => p.Mask().ToolTip("New password for the character."));
        });
    }

    private static void RegisterClientCreateCharacterAppearance(InspectorMappingRegistry registry)
    {
        registry.Register<ClientNewUserAppearanceMessage>(b =>
        {
            b.Section("Appearance")
                .Property(m => m.HairStyle, p => p.ToolTip("Hair style of the character."))
                .Property(m => m.Gender, p => p.ToolTip("Gender of the character."))
                .Property(m => m.HairColor, p => p.ToolTip("Hair color of the character."));
        });
    }

    private static void RegisterClientCreateCharacterNameMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientNewUserMessage>(b =>
        {
            b.Section("Credentials")
                .Property(m => m.Name, p => p.ToolTip("Desired name for the character."))
                .Property(m => m.Password, p => p.Mask().ToolTip("Password for the character."))
                .Property(m => m.Email, p => p.ToolTip("Email address for the character."));
        });
    }

    private static void RegisterClientDialogChoiceMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientPursuitMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityType, p => p.ToolTip("Type of entity responsible for the dialog."))
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the entity responsible for the dialog."));
            b.Section("Dialog")
                .Property(m => m.StepId, p => p.ToolTip("ID of the dialog step requested."))
                .Property(m => m.PursuitId, p => p.ToolTip("ID of the pursuit the dialog belongs to."));
            b.Section("Arguments")
                .Property(m => m.ArgsType, p => p.ToolTip("Type of arguments passed to the dialog."))
                .Property(m => m.MenuChoice, p => p.ToolTip("Menu choice selected by the user."))
                .Property(m => m.TextInputs, p => p.ShowMultiline().ToolTip("Text inputs provided by the user."))
                .IsExpanded(m => m.ArgsType != DialogArgsType.None);
        });
    }
    
    private static void RegisterClientDialogMenuChoiceMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientMerchantMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityType, p => p.ToolTip("Type of entity responsible for the dialog menu."))
                .Property(m => m.EntityId,
                    p => p.ShowHex().ToolTip("ID of the entity responsible for the dialog menu."));
            b.Section("Menu")
                .Property(m => m.PursuitId, p => p.ToolTip("ID of the pursuit the menu belongs to."));
            b.Section("Slot")
                .Property(m => m.Slot, p => p.ToolTip("Slot selected by the user."))
                .IsExpanded(m => m.Slot.HasValue);
            b.Section("Argument")
                .Property(m => m.Arguments, p => p.ShowMultiline().ToolTip("Arguments associated with the dialog menu choice."))
                .IsExpanded(m => m.Arguments.Count > 0);
        });
    }

    private static void RegisterClientDropGoldMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientDropGoldMessage>(b =>
        {
            b.Section("Gold")
                .Property(m => m.Amount, p => p.ToolTip("Amount of gold to drop."));
            b.Section("Position")
                .Property(m => m.X, p => p.ToolTip("X-coordinate of the map position to drop the gold."))
                .Property(m => m.Y, p => p.ToolTip("Y-coordinate of the map position to drop the gold."));
        });
    }

    private static void RegisterClientDropItemMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientDropMessage>(b =>
        {
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot of the item to drop."))
                .Property(m => m.Quantity, p => p.ToolTip("Quantity of the item to drop."));
            b.Section("Position")
                .Property(m => m.X, p => p.ToolTip("X-coordinate of the map position to drop the item."))
                .Property(m => m.Y, p => p.ToolTip("Y-coordinate of the map position to drop the item."));
        });
    }

    private static void RegisterClientEatItemMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientEatMessage>(b =>
        {
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot of the item to eat."));
        });
    }

    private static void RegisterClientEditNotepadMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientExitEditingModeMessage>(b =>
        {
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot of the notepad to edit."));
            b.Section("Content")
                .Property(m => m.Content, p => p.ShowMultiline().ToolTip("New content of the notepad."));
        });
    }

    private static void RegisterClientEmoteMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientEmotionMessage>(b =>
        {
            b.Section("Emote")
                .Property(m => m.Emote, p => p.ToolTip("Emote to perform."));
        });
    }

    private static void RegisterClientExceptionMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientExceptionMessage>(b =>
        {
            b.Section("Exception")
                .Property(m => m.Message, p => p.ShowMultiline().ToolTip("Exception message and data."));
        });
    }

    private static void RegisterClientExchangeActionMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientExchangeMessage>(b =>
        {
            b.Section("Action")
                .Property(m => m.Action, p => p.ToolTip("Exchange action to perform."));
            b.Section("Target")
                .Property(m => m.TargetId, p => p.ShowHex().ToolTip("ID of the target of the exchange action."));
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot of the item to exchange."))
                .Property(m => m.Quantity, p => p.ToolTip("Quantity of the item to exchange."))
                .IsExpanded(m =>
                    m.Action is ExchangeClientActionType.AddItem or ExchangeClientActionType.AddStackableItem);
            b.Section("Gold")
                .Property(m => m.GoldAmount, p => p.ToolTip("Amount of gold to exchange."))
                .IsExpanded(m => m.Action == ExchangeClientActionType.SetGold);
        });
    }

    private static void RegisterClientGiveGoldMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientGiveGoldMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the entity to give gold to."));
            b.Section("Gold")
                .Property(m => m.Amount, p => p.ToolTip("Amount of gold to give."));
        });
    }

    private static void RegisterClientGiveItemMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientGiveMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the entity to give the item to."));
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot of the item to give."))
                .Property(m => m.Quantity, p => p.ToolTip("Quantity of the item to give."));
        });
    }

    private static void RegisterClientGroupInviteMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientGroupMessage>(b =>
        {
            b.Section("Action")
                .Property(m => m.Action, p => p.ToolTip("Group action to perform."));
            ;
            b.Section("Target")
                .Property(m => m.TargetName, p => p.ToolTip("Name of the target of the group action."));
            b.Section("Group Box")
                .Property(m => m.GroupBox, p => p.ToolTip("Display text of the floating group box."))
                .IsExpanded(m => m.GroupBox is not null);
        });
    }

    private static void RegisterClientHeartbeatMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientReplyCRCMessage>(b =>
        {
            b.Section("Heartbeat")
                .Property(m => m.Reply, p => p.ShowHex().ToolTip("Client reply to the heartbeat nonce."));
        });
    }

    private static void RegisterClientIgnoreUserMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientBlockListenMessage>(b =>
        {
            b.Section("Ignore")
                .Property(m => m.Action, p => p.ToolTip("Ignore action to perform."))
                .Property(m => m.Name, p => p.ToolTip("Name of the user to ignore or unignore."));
        });
    }

    private static void RegisterClientInteractMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientRequestObjectInfoMessage>(b =>
        {
            b.Section("Interaction")
                .Property(m => m.InteractionType, p => p.ToolTip("Type of world interaction to perform."));
            b.Section("Target Entity")
                .Property(m => m.TargetId, p => p.ShowHex().ToolTip("ID of the target entity."))
                .IsExpanded(m => m.InteractionType == InteractionType.Entity);
            b.Section("Target Tile")
                .Property(m => m.TargetX, p => p.ToolTip("X-coordinate of the target tile."))
                .Property(m => m.TargetY, p => p.ToolTip("Y-coordinate of the target tile."))
                .IsExpanded(m => m.InteractionType == InteractionType.Tile);
        });
    }

    private static void RegisterClientLoginMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientLoginMessage>(b =>
        {
            b.Section("Credentials")
                .Property(m => m.Name, p => p.ToolTip("Name of the character to login."))
                .Property(m => m.Password, p => p.Mask().ToolTip("Password of the character to login."));
            b.Section("Client")
                .Property(m => m.ClientId, p => p.ToolTip("ID of the local machine."))
                .Property(m => m.Checksum, p => p.ShowHex().ToolTip("CRC-16 checksum of the login credentials."));
        });
    }

    private static void RegisterClientLookTileMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientFarLookMessage>(b =>
        {
            b.Section("Location")
                .Property(m => m.TileX, p => p.ToolTip("X-coordinate of the tile to look at."))
                .Property(m => m.TileY, p => p.ToolTip("Y-coordinate of the tile to look at."));
        });
    }

    private static void RegisterClientManufactureMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientManufactureMessage>(b =>
        {
            b.Section("Message")
                .Property(m => m.MessageType, p => p.ToolTip("Type of manufacture message sent."))
                .Property(m => m.ManufactureId, p => p.ToolTip("ID of the manufacture dialog."));
            b.Section("Recipe")
                .Property(m => m.RecipeIndex, p => p.ToolTip("Index of the recipe requested."))
                .Property(m => m.RecipeName, p => p.ToolTip("Name of the recipe requested."));
        });
    }

    private static void RegisterClientPickupItemMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientGetMessage>(b =>
        {
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot to store the picked up item."));
            b.Section("Position")
                .Property(m => m.X, p => p.ToolTip("X-coordinate of the map position to pick up the item."))
                .Property(m => m.Y, p => p.ToolTip("Y-coordinate of the map position to pick up the item."));
        });
    }

    private static void RegisterClientRaiseStatMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientAddStatMessage>(b =>
        {
            b.Section("Stat")
                .Property(m => m.Stat, p => p.ToolTip("Stat to request to raise."));
        });
    }

    private static void RegisterClientRequestEntityMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientPutGroundMessage>(b =>
        {
            b.Section("Entity")
                .Property(m => m.EntityId, p => p.ShowHex().ToolTip("ID of the entity to request."));
        });
    }

    private static void RegisterClientRequestExitMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientQuitMessage>(b =>
        {
            b.Section("Request")
                .Property(m => m.Reason, p => p.ToolTip("Reason for the exit request."));
        });
    }

    private static void RegisterClientRequestHomepageMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientRequestHomepageMessage>(b =>
        {
            b.Section("Request")
                .Property(m => m.NeedsHomepage, p => p.ToolTip("Whether the client needs the homepage URL."));
        });
    }

    private static void RegisterClientRequestLoginNoticeMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientStipulationMessage>(b =>
        {
            // No mappings
        });
    }

    private static void RegisterClientRequestMetadataMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientMetadataMessage>(b =>
        {
            b.Section("Request")
                .Property(m => m.RequestType, p => p.ToolTip("Type of metadata to request."))
                .Property(m => m.Name, p => p.ToolTip("Name of the metadata file to request."));
        });
    }

    private static void RegisterClientRequestProfileMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientSelfLookMessage>(b =>
        {
            // No mappings
        });
    }

    private static void RegisterClientRequestSequenceMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientHelloMessage>(b =>
        {
            b.Section("Request")
                .Property(m => m.Sequence, p => p.ShowHex().ToolTip("Sequence counter value to request."))
                .Property(m => m.Unknown, p => p.ShowHex());
        });
    }

    private static void RegisterClientRequestServerTableMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientMultiServerMessage>(b =>
        {
            b.Section("Request")
                .Property(m => m.NeedsServerTable, p => p.ToolTip("Whether the client needs the server table."));
        });
    }

    private static void RegisterClientRequestUserIdMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientRequestUserIdMessage>(b =>
            b.Section("Request")
                .Property(m => m.Nonce, p => p.ToolTip("Nonce for the request, will be echoed back in the response.")));
    }

    private static void RegisterClientSayMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientSayMessage>(b =>
        {
            b.Section("Message")
                .Property(m => m.MessageType, p => p.ToolTip("Type of message to send."))
                .Property(m => m.Content, p => p.ShowMultiline().ToolTip("Message content to send."));
        });
    }

    private static void RegisterClientSetStatusMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientUserChangeStateMessage>(b =>
        {
            b.Section("Status")
                .Property(m => m.Status, p => p.ToolTip("Social status to set."));
        });
    }

    private static void RegisterClientSpellChantMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientSpellDelaySayMessage>(b =>
        {
            b.Section("Message")
                .Property(m => m.Content, p => p.ShowMultiline().ToolTip("Spell chant to send."));
        });
    }

    private static void RegisterClientSwapSlotMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientChangeSlotMessage>(b =>
        {
            b.Section("Interface")
                .Property(m => m.Pane, p => p.ToolTip("Interface pane to swap the slots in."));
            b.Section("Slot")
                .Property(m => m.SourceSlot, p => p.ToolTip("Source slot to swap."))
                .Property(m => m.TargetSlot, p => p.ToolTip("Target slot to swap."));
        });
    }

    private static void RegisterClientSyncTicksMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientCheckTimeMessage>(b =>
        {
            b.Section("Ticks")
                .Property(m => m.ClientTickCount, p => p.ToolTip("Client tick count."))
                .Property(m => m.ServerTickCount, p => p.ToolTip("Server tick count."));
            ;
        });
    }

    private static void RegisterClientToggleSettingMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientUserSettingMessage>(b =>
        {
            b.Section("Setting")
                .Property(m => m.OptionIndex, p => p.ToolTip("Index of the setting to toggle."));
        });
    }

    private static void RegisterClientTurnMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientChangeDirectionMessage>(b =>
        {
            b.Section("Movement")
                .Property(m => m.Direction, p => p.ToolTip("Direction to face."));
        });
    }

    private static void RegisterClientUnequipItemMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientRemoveEquipmentMessage>(b =>
        {
            b.Section("Equipment")
                .Property(m => m.Slot, p => p.ToolTip("Equipment slot to unequip."));
        });
    }

    private static void RegisterClientUseItem(InspectorMappingRegistry registry)
    {
        registry.Register<ClientUseMessage>(b =>
        {
            b.Section("Item")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot of the item to use."));
        });
    }

    private static void RegisterClientUserPortraitMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientSendPortraitMessage>(b =>
        {
            b.Section("Portrait")
                .Property(m => m.Portrait,
                    p => p.ShowMultiline().ToolTip("Raw portrait data to display to other users."))
                .IsExpanded(m => m.Portrait.Count > 0);
            b.Section("Bio")
                .Property(m => m.Bio, p => p.ShowMultiline().ToolTip("Biography to display to other users."))
                .IsExpanded(m => !string.IsNullOrEmpty(m.Bio));
        });
    }

    private static void RegisterClientUseSkillMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientUseSkillMessage>(b =>
        {
            b.Section("Skill")
                .Property(m => m.Slot, p => p.ToolTip("Inventory slot of the skill to use."));
            ;
        });
    }

    private static void RegisterClientVersionMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientVersionMessage>(b =>
        {
            b.Section("Client Version")
                .Property(m => m.Version, p => p.ToolTip("Client version."))
                .Property(m => m.Checksum, p => p.ShowHex().ToolTip("CRC-16 checksum of the client version."));
        });
    }

    private static void RegisterClientWalkMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientMoveMessage>(b =>
        {
            b.Section("Movement")
                .Property(m => m.Direction, p => p.ToolTip("Direction to walk."))
                .Property(m => m.StepCount, p => p.ToolTip("Incremental step counter value."));
        });
    }

    private static void RegisterClientWhisperMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientTellMessage>(b =>
        {
            b.Section("Whisper")
                .Property(m => m.Target, p => p.ToolTip("Name of the recipient or channel of the whisper."))
                .Property(m => m.Content, p => p.ShowMultiline().ToolTip("Content of the whisper message."));
        });
    }

    private static void RegisterClientWorldMapClickMapping(InspectorMappingRegistry registry)
    {
        registry.Register<ClientFieldMapMessage>(b =>
        {
            b.Section("Location")
                .Property(m => m.MapId, p => p.ToolTip("ID of the destination map the user clicked on."))
                .Property(m => m.X, p => p.ToolTip("X-coordinate of the destination map the user clicked on."))
                .Property(m => m.Y, p => p.ToolTip("Y-coordinate of the destination map the user clicked on."));
            b.Section("Checksum")
                .Property(m => m.Checksum, p => p.ShowHex().ToolTip("CRC-16 checksum of the selected destination."));
        });
    }
}