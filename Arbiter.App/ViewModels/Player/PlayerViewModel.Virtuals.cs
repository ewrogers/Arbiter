using System.Collections.Concurrent;
using System.Threading.Tasks;
using Arbiter.App.Models.Player;
using Arbiter.Net;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Client.Types;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Player;

public partial class PlayerViewModel
{
    private const string FilterPrefix = nameof(PlayerViewModel);

    private readonly ConcurrentDictionary<int, (InventoryItem Item, int TargetSlot)> _pendingVirtualItemSwaps = new();
    private readonly ConcurrentDictionary<int, (SkillbookItem Skill, int TargetSlot)> _pendingVirtualSkillSwaps = new();
    private readonly ConcurrentDictionary<int, (SpellbookItem Spell, int TargetSlot)> _pendingVirtualSpellSwaps = new();

    private NetworkFilterRef? _virtualItemFilter;
    private NetworkFilterRef? _virtualSkillFilter;
    private NetworkFilterRef? _virtualSpellFilter;
    private NetworkFilterRef? _virtualSwapSlotFilter;

    private void AddVirtualFilters(ProxyConnection connection)
    {
        _virtualItemFilter = connection.AddFilter<ClientUseMessage>(HandleClientUseItemMessage,
            $"{FilterPrefix}_VirtualItemFilter",
            int.MaxValue);

        _virtualSkillFilter = connection.AddFilter<ClientUseSkillMessage>(HandleClientUseSkillMessage,
            $"{FilterPrefix}_VirtualSkillFilter", int.MaxValue);

        _virtualSpellFilter = connection.AddFilter<ClientUseSpellMessage>(HandleClientCastSpellMessage,
            $"{FilterPrefix}_VirtualSpellFilter", int.MaxValue);

        _virtualSwapSlotFilter = connection.AddFilter<ClientChangeSlotMessage>(HandleClientSwapSlotMessage,
            $"{FilterPrefix}_VirtualSwapSlotFilter", int.MaxValue);
    }

    private void RemoveVirtualFilters()
    {
        _virtualItemFilter?.Unregister();
        _virtualSkillFilter?.Unregister();
        _virtualSpellFilter?.Unregister();
        _virtualSwapSlotFilter?.Unregister();
    }

    private NetworkPacket? HandleClientUseItemMessage(ProxyConnection connection, ClientUseMessage message,
        object? parameter, NetworkMessageFilterResult<ClientUseMessage> result)
    {
        if (!Inventory.TryGetSlot(message.Slot, out var slotted) || !slotted.Value.IsVirtual is not true)
        {
            return result.Passthrough();
        }

        // Block virtual using of items but still fire off the action internally
        var item = slotted.Value;
        if (item.OnUse is not null)
        {
            Task.Run(() => item.OnUse());
        }

        return result.Block();
    }

    private NetworkPacket? HandleClientUseSkillMessage(ProxyConnection connection, ClientUseSkillMessage message,
        object? parameter, NetworkMessageFilterResult<ClientUseSkillMessage> result)
    {
        if (!Skills.TryGetSlot(message.Slot, out var slotted) || !slotted.Value.IsVirtual)
        {
            return result.Passthrough();
        }

        // Block virtual using of skills but still fire off the action internally
        var skill = slotted.Value;
        if (skill.OnUse is not null)
        {
            Task.Run(() => skill.OnUse());
        }

        return result.Block();
    }

    private NetworkPacket? HandleClientCastSpellMessage(ProxyConnection connection, ClientUseSpellMessage message,
        object? parameter, NetworkMessageFilterResult<ClientUseSpellMessage> result)
    {
        if (!Spells.TryGetSlot(message.Slot, out var slotted) || !slotted.Value.IsVirtual)
        {
            return result.Passthrough();
        }

        // Block virtual using of spells but still fire off the action internally
        var spell = slotted.Value;
        if (spell.OnCast is not null)
        {
            var casterId = connection.UserId ?? 0;
            Task.Run(() =>
            {
                var parameters = spell.TargetType switch
                {
                    SpellTargetType.NoTarget => SpellCastParameters.NoTarget(casterId),
                    SpellTargetType.Target => SpellCastParameters.Target(casterId, message.TargetId ?? 0,
                        message.TargetX ?? 0, message.TargetY ?? 0),
                    SpellTargetType.Prompt =>
                        SpellCastParameters.WithTextInput(casterId, message.TextInput ?? string.Empty),
                    _ => SpellCastParameters.WithNumericInput(casterId, message.NumericInputs)
                };
                spell.OnCast(parameters);
            });
        }

        return result.Block();
    }

    private NetworkPacket HandleClientSwapSlotMessage(ProxyConnection connection, ClientChangeSlotMessage message,
        object? parameter, NetworkMessageFilterResult<ClientChangeSlotMessage> result)
    {
        var slotA = message.SourceSlot;
        var slotB = message.TargetSlot;

        if (message.Pane == ClientSlotSwapType.Inventory)
        {
            var hasItemA = Inventory.TryGetSlot(slotA, out var itemA);
            var hasItemB = Inventory.TryGetSlot(slotB, out var itemB);

            if (hasItemA && itemA.Value.IsVirtual)
            {
                _pendingVirtualItemSwaps[slotB] = (itemA.Value, slotB);
            }

            if (hasItemB && itemB.Value.IsVirtual)
            {
                _pendingVirtualItemSwaps[slotA] = (itemB.Value, slotA);
            }
        }
        else if (message.Pane == ClientSlotSwapType.Skills)
        {
            var hasSkillA = Skills.TryGetSlot(slotA, out var skillA);
            var hasSkillB = Skills.TryGetSlot(slotB, out var skillB);

            if (hasSkillA && skillA.Value.IsVirtual)
            {
                _pendingVirtualSkillSwaps[slotB] = (skillA.Value, slotB);
            }

            if (hasSkillB && skillB.Value.IsVirtual)
            {
                _pendingVirtualSkillSwaps[slotA] = (skillB.Value, slotA);
            }
        }
        else if (message.Pane == ClientSlotSwapType.Spells)
        {
            var hasSpellA = Spells.TryGetSlot(slotA, out var spellA);
            var hasSpellB = Spells.TryGetSlot(slotB, out var spellB);

            if (hasSpellA && spellA.Value.IsVirtual)
            {
                _pendingVirtualSpellSwaps[slotB] = (spellA.Value, slotB);
            }

            if (hasSpellB && spellB.Value.IsVirtual)
            {
                _pendingVirtualSpellSwaps[slotA] = (spellB.Value, slotA);
            }
        }

        return result.Passthrough();
    }

    private void SendVirtualItem(ProxyConnection connection, InventoryItem item, int targetSlot)
    {
        Inventory.SetSlot(targetSlot, item);

        var addItemMessage = new ServerAddInventoryMessage
        {
            Slot = (byte)targetSlot,
            Name = item.Name,
            Sprite = item.Sprite,
            Color = (DyeColor)item.Color,
            Durability = (uint)(item.Durability ?? 0),
            MaxDurability = (uint)(item.MaxDurability ?? 0),
            Quantity = (uint)item.Quantity,
            IsStackable = item.IsStackable
        };
        connection.EnqueueMessage(addItemMessage);
    }

    private void SendVirtualSkill(ProxyConnection connection, SkillbookItem skill, int targetSlot)
    {
        Skills.SetSlot(targetSlot, skill);

        var addSkillMessage = new ServerAddSkillMessage
        {
            Slot = (byte)targetSlot,
            Name = skill.Name,
            Icon = skill.Sprite
        };

        connection.EnqueueMessage(addSkillMessage);
    }

    private void SendVirtualSpell(ProxyConnection connection, SpellbookItem spell, int targetSlot)
    {
        Spells.SetSlot(targetSlot, spell);

        var addSpellMessage = new ServerAddSpellMessage
        {
            Slot = (byte)targetSlot,
            Name = spell.Name,
            Icon = spell.Sprite,
            TargetType = spell.TargetType,
            Prompt = spell.Prompt ?? string.Empty,
            CastLines = (byte)spell.CastLines
        };

        connection.EnqueueMessage(addSpellMessage);
    }
}