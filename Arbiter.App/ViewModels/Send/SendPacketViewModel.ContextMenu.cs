using System;
using System.Threading.Tasks;
using Arbiter.App.Extensions;
using Avalonia;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Send;

public partial class SendPacketViewModel
{
    private bool CanEditSelection() => HasSelection && !IsSending;

    private bool CanCopyToClipboard(string fieldName) => fieldName switch
    {
        "selection" => Math.Abs(SelectionEnd - SelectionStart) > 0,
        _ => true
    };

    [RelayCommand(CanExecute = nameof(CanCopyToClipboard))]
    private async Task CopyToClipboard(string fieldName)
    {
        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null)
        {
            return;
        }

        // Ensure that the selection is not reversed
        var selectionStart = Math.Min(SelectionStart, SelectionEnd);
        var selectionEnd = Math.Max(SelectionStart, SelectionEnd);

        var textToCopy = fieldName switch
        {
            "selection" => InputText.Substring(selectionStart, selectionEnd - selectionStart),
            _ => InputText
        };

        if (!string.IsNullOrEmpty(textToCopy))
        {
            await clipboard.SetTextAsync(textToCopy);
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditSelection))]
    private async Task CutSelection()
    {
        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null)
        {
            return;
        }

        var (selectionStart, selectionEnd) = GetSelectionRange();
        var selectedText = InputText[selectionStart..selectionEnd];

        await clipboard.SetTextAsync(selectedText);
        ReplaceRange(selectionStart, selectionEnd, string.Empty);
    }

    [RelayCommand]
    private async Task PasteFromClipboard()
    {
        if (IsSending)
        {
            return;
        }

        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null)
        {
            return;
        }

        var newText = await clipboard.TryGetTextAsync();
        if (newText is not null)
        {
            var (selectionStart, selectionEnd) = GetSelectionRange();
            ReplaceRange(selectionStart, selectionEnd, SendPasteFormatter.Format(newText));
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        SelectionStart = 0;
        SelectionEnd = InputText.Length;
    }

    [RelayCommand(CanExecute = nameof(CanEditSelection))]
    private void CommentSelection()
    {
        TransformSelectedLines(CommentLine);
    }

    [RelayCommand(CanExecute = nameof(CanEditSelection))]
    private void UncommentSelection()
    {
        TransformSelectedLines(UncommentLine);
    }

    [RelayCommand]
    private void ClearAll()
    {
        InputText = string.Empty;
        SelectionStart = 0;
        SelectionEnd = 0;
    }

    private (int Start, int End) GetSelectionRange()
    {
        var start = Math.Clamp(Math.Min(SelectionStart, SelectionEnd), 0, InputText.Length);
        var end = Math.Clamp(Math.Max(SelectionStart, SelectionEnd), 0, InputText.Length);
        return (start, end);
    }

    private void ReplaceRange(int start, int end, string replacement)
    {
        InputText = InputText[..start] + replacement + InputText[end..];

        var caret = start + replacement.Length;
        SelectionStart = caret;
        SelectionEnd = caret;
    }

    private void TransformSelectedLines(Func<string, string> transform)
    {
        var (selectionStart, selectionEnd) = GetSelectionRange();
        if (selectionStart == selectionEnd)
        {
            return;
        }

        var previousLineBreak = selectionStart > 0 ? InputText.LastIndexOf('\n', selectionStart - 1) : -1;
        var lineStart = previousLineBreak + 1;

        var endProbe = selectionEnd;
        if (endProbe > lineStart && InputText[endProbe - 1] == '\n')
        {
            endProbe--;
        }

        var nextLineBreak = InputText.IndexOf('\n', endProbe);
        var lineEnd = nextLineBreak >= 0 ? nextLineBreak : InputText.Length;

        var lines = InputText[lineStart..lineEnd].Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = transform(lines[i]);
        }

        var replacement = string.Join('\n', lines);
        InputText = InputText[..lineStart] + replacement + InputText[lineEnd..];
        SelectionStart = lineStart;
        SelectionEnd = lineStart + replacement.Length;
    }

    private static string CommentLine(string line)
    {
        var index = 0;
        while (index < line.Length && line[index] is ' ' or '\t')
        {
            index++;
        }

        return line.Insert(index, "// ");
    }

    private static string UncommentLine(string line)
    {
        var index = 0;
        while (index < line.Length && line[index] is ' ' or '\t')
        {
            index++;
        }

        if (index + 1 >= line.Length || line[index] != '/' || line[index + 1] != '/')
        {
            return line;
        }

        var length = index + 2 < line.Length && line[index + 2] == ' ' ? 3 : 2;
        return line.Remove(index, length);
    }
}
