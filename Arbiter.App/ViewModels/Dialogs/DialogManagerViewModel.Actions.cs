using System;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Dialogs;

public partial class DialogManagerViewModel
{
    private void OnDialogMenuChoiceSelected(object? sender, DialogMenuEventArgs e)
    {
        if (SelectedClient is null || ActiveDialog?.EntityId is null)
        {
            return;
        }

        // If no pursuit, this is just a standard dialog with index-based menu choices
        if (e.SelectedChoice.PursuitId is null)
        {
            TryNavigateWithStepOffset(ActiveDialog, 1, e.SelectedChoice.Index);
            return;
        }

        // With a pursuit this is a dialog menu that begins the pursuit
        var menuChoiceMessage = new ClientMerchantMessage
        {
            EntityType = ActiveDialog.EntityType,
            EntityId = (uint)ActiveDialog.EntityId.Value,
            PursuitId = (ushort)e.SelectedChoice.PursuitId
        };
        SelectedClient.EnqueueMessage(menuChoiceMessage);
    }
    
    private void OnTextInputConfirmed(object? sender, DialogEventArgs e)
    {
        if (SelectedClient is null || ActiveDialog is null)
        {
            return;
        }

        TryNavigateWithTextInput(ActiveDialog, ActiveDialog.InputText ?? string.Empty);
    }

    private void OnDialogNavigatePrevious(object? sender, DialogEventArgs e)
    {
        if (SelectedClient is null || ActiveDialog is null)
        {
            return;
        }

        TryNavigateWithStepOffset(ActiveDialog, -1);
    }

    private void OnDialogNavigateNext(object? sender, DialogEventArgs e)
    {
        if (SelectedClient is null || ActiveDialog is null)
        {
            return;
        }

        TryNavigateWithStepOffset(ActiveDialog, 1);
    }

    private void OnDialogNavigateTop(object? sender, DialogEventArgs e)
    {
        if (SelectedClient is null)
        {
            return;
        }

        var entityId = ActiveDialog?.EntityId;
        if (entityId is null or 0)
        {
            return;
        }

        // This just re-interacts with the entity
        var interactMessage = new ClientRequestObjectInfoMessage
        {
            InteractionType = InteractionType.Entity,
            TargetId = (uint)entityId.Value
        };

        SelectedClient.EnqueueMessage(interactMessage);
    }

    private void OnDialogClose(object? sender, DialogEventArgs e)
    {
        CloseCurrentDialog();

        if (ShouldSync && SelectedClient is not null)
        {
            SetActiveDialogForClient(SelectedClient.Id, null);
        }

        ActiveDialog = null;
    }

    private void CloseCurrentDialog()
    {
        if (SelectedClient == null)
        {
            return;
        }

        // If there is no step ID, we can just act like the dialog was closed by the server
        if (ActiveDialog?.StepId is null)
        {
            var closeDialogMessage = new ServerPursuitMessage
            {
                DialogType = DialogType.CloseDialog
            };
            SelectedClient.EnqueueMessage(closeDialogMessage);
            return;
        }

        TryNavigateWithStepOffset(ActiveDialog, 0);
    }

    private bool TryNavigateWithStepOffset(DialogViewModel dialog, int offset, int? menuChoice = null)
    {
        if (SelectedClient is null || dialog.EntityId is null or 0 || dialog.PursuitId is null ||
            dialog.StepId is null or 0)
        {
            return false;
        }

        // Next/Prev dialogs require an explicit client action to prevent "You are busy" messages
        var closeInteractMessage = new ClientPursuitMessage
        {
            EntityId = (uint)dialog.EntityId,
            EntityType = dialog.EntityType,
            PursuitId = (ushort)dialog.PursuitId,
            StepId = (ushort)Math.Max(0, dialog.StepId.Value + offset),
            ArgsType = menuChoice.HasValue ? DialogArgsType.MenuChoice : DialogArgsType.None,
            MenuChoice = (byte?)menuChoice,
        };
        return SelectedClient.EnqueueMessage(closeInteractMessage);
    }
    
    private bool TryNavigateWithTextInput(DialogViewModel dialog, string textInput)
    {
        if (SelectedClient is null || dialog.EntityId is null or 0 || dialog.PursuitId is null ||
            dialog.StepId is null or 0)
        {
            return false;
        }

        var closeInteractMessage = new ClientPursuitMessage
        {
            EntityId = (uint)dialog.EntityId,
            EntityType = dialog.EntityType,
            PursuitId = (ushort)dialog.PursuitId,
            StepId = (ushort)Math.Max(0, dialog.StepId.Value + 1),
            ArgsType = DialogArgsType.TextInput,
            TextInputs = [textInput],
        };
        return SelectedClient.EnqueueMessage(closeInteractMessage);
    }
}
