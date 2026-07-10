using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Dialogs;

public partial class DialogManagerViewModel
{
    private void AddObservers()
    {
        _proxyServer.AddObserver<ServerShowDialogMessage>(OnDialogMessage);
        _proxyServer.AddObserver<ServerShowDialogMenuMessage>(OnDialogMenuMessage);
    }

    private void OnDialogMessage(ProxyConnection connection, ServerShowDialogMessage message, object? parameter)
    {
        if (!ShouldSync || !_clientManager.TryGetClient(connection.Id, out var client))
        {
            return;
        }

        var dialog = BuildDialogView(message);
        SetActiveDialogForClient(client.Id, dialog);

        if (client == SelectedClient)
        {
            ActiveDialog = dialog;
        }
    }

    private void OnDialogMenuMessage(ProxyConnection connection, ServerShowDialogMenuMessage message, object? parameter)
    {
        if (!ShouldSync || !_clientManager.TryGetClient(connection.Id, out var client))
        {
            return;
        }

        var dialog = BuildDialogView(message);
        SetActiveDialogForClient(client.Id, dialog);

        if (client == SelectedClient)
        {
            ActiveDialog = dialog;
        }
    }

    private void SetActiveDialogForClient(long clientId, DialogViewModel? dialog)
        => _activeDialogs.AddOrUpdate(clientId, dialog, (_, _) => dialog);

    private DialogViewModel? BuildDialogView(ServerShowDialogMessage message)
    {
        if (message.DialogType == DialogType.CloseDialog)
        {
            return null;
        }
        
        var name = !string.IsNullOrWhiteSpace(message.Name) ? message.Name : message.EntityType.ToString();
        var dialog = new DialogViewModel(_spriteService)
        {
            Name = name,
            EntityId = message.EntityId,
            EntityType = message.EntityType,
            Sprite = message.Sprite,
            SpriteType = message.SpriteType,
            Color = message.Color,
            PursuitId = message.PursuitId,
            StepId = message.StepId,
            Content = message.Content,
            CanNavigatePrevious = message.HasPreviousButton,
            CanNavigateNext = message.HasNextButton
        };

        if (message.DialogType == DialogType.Menu)
        {
            for (var i = 0; i < message.MenuChoices.Count; i++)
            {
                dialog.MenuChoices.Add(new DialogMenuChoiceViewModel
                {
                    Index = i + 1,
                    Text = message.MenuChoices[i]
                });
            }
        }
        else if (message.DialogType == DialogType.TextInput)
        {
            dialog.IsTextInput = true;
            dialog.PromptLine1 = message.InputPrompt;
            dialog.PromptLine2 = message.InputDescription;
            dialog.InputMaxLength = message.InputMaxLength ?? 255;
            dialog.CanNavigateNext = false;
        }

        return dialog;
    }

    private DialogViewModel BuildDialogView(ServerShowDialogMenuMessage message)
    {
        var name = !string.IsNullOrWhiteSpace(message.Name) ? message.Name : message.EntityType.ToString();
        var dialog = new DialogViewModel(_spriteService)
        {
            Name = name,
            EntityId = message.EntityId,
            EntityType = message.EntityType,
            Sprite = message.Sprite,
            SpriteType = message.SpriteType,
            Color = message.Color,
            PursuitId = message.PursuitId,
            Content = message.Content,
            CanNavigateTop = true
        };

        if (message.MenuChoices.Count > 0)
        {
            for (var i = 0; i < message.MenuChoices.Count; i++)
            {
                var choice = message.MenuChoices[i];
                dialog.MenuChoices.Add(new DialogMenuChoiceViewModel
                {
                    Index = i + 1,
                    Text = choice.Text,
                    PursuitId = choice.PursuitId,
                });
            }
        }

        return dialog;
    }
}
