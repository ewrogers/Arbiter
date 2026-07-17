using System.Collections.Generic;
using System.Text;
using Arbiter.App.Models.Player;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Client;

public partial class ClientManagerViewModel
{
    private static readonly IReadOnlyList<string> FreeRepairSlotArgs = [Encoding.ASCII.GetString(new byte[] { 1 })];

    private const string TrueLookSkillName = "True Look";
    private const string TrueLookSpellName = "True Look At";

    private void AddTrueLookToClients()
    {
        foreach (var client in _clients.Values)
        {
            AddTrueLookSkill(client);
            AddTrueLookTileSpell(client);
        }
    }

    private void RemoveTrueLookFromClients()
    {
        foreach (var client in _clients.Values)
        {
            client.RemoveVirtualSkill(TrueLookSkillName);
            client.RemoveVirtualSpell(TrueLookSpellName);
        }
    }

    private static void AddTrueLookSkill(ClientViewModel client)
    {
        if (client.Player.Skills.HasSkill(TrueLookSkillName))
        {
            return;
        }

        client.AddVirtualSkill(73, 8, TrueLookSkillName, () => PerformTrueLook(client));
    }

    private static void AddTrueLookTileSpell(ClientViewModel client)
    {
        if (client.Player.Spells.HasSpell(TrueLookSpellName))
        {
            return;
        }

        client.AddVirtualSpell(73, 200, TrueLookSpellName, SpellTargetType.Target,
            parameters => PerformTrueLookTile(client, parameters));
    }

    private static void PerformTrueLook(ClientViewModel client)
    {
        var lookAction = new ClientLookMessage();
        client.EnqueueMessage(lookAction);
    }

    private static void PerformTrueLookTile(ClientViewModel client, SpellCastParameters castParameters)
    {
        var lookTileAction = new ClientFarLookMessage
        {
            TileX = (ushort)castParameters.TargetX,
            TileY = (ushort)castParameters.TargetY
        };
        client.EnqueueMessage(lookTileAction);
    }
}