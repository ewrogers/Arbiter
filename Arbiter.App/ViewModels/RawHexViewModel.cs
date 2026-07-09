using System;
using System.Buffers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arbiter.App.Extensions;
using Arbiter.App.ViewModels.Tracing;
using Arbiter.Net.Client;
using Arbiter.Net.Server;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels;

public partial class RawHexViewModel : ViewModelBase
{
    private readonly TracePacketViewModel _viewModel;
    private readonly byte[] _decryptedPayload;
    private readonly byte[] _filteredPayload;

    private byte[]? _payload;
    private bool _showValuesAsHex;
    private int _startIndex;
    private int _endIndex;
    private int _hexSelectionStart;
    private int _hexSelectionEnd;
    private int _textSelectionStart;
    private int _textSelectionEnd;
    private bool _isSyncing;
    private int _copyFeedbackVersion;

    [NotifyPropertyChangedFor(nameof(FormattedCommand))] [ObservableProperty]
    private byte _command;

    [NotifyPropertyChangedFor(nameof(FormattedSequence))] [ObservableProperty]
    private byte? _sequence;

    [ObservableProperty] private string? _rawHex;
    [ObservableProperty] private string? _decodedText;
    [ObservableProperty] private string? _copyFeedbackField;
    [ObservableProperty] private string? _formattedSignedByte;
    [ObservableProperty] private string? _formattedUnsignedByte;
    [ObservableProperty] private string? _formattedSignedShort;
    [ObservableProperty] private string? _formattedUnsignedShort;
    [ObservableProperty] private string? _formattedSignedInt;
    [ObservableProperty] private string? _formattedUnsignedInt;
    [ObservableProperty] private string? _formattedSignedLong;
    [ObservableProperty] private string? _formattedUnsignedLong;
    [ObservableProperty] private string? _formattedIpAddress;
    [ObservableProperty] private string? _formattedBitFlags;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowFilterToggle))]
    private bool _useFiltered = true;

    public bool ShowFilterToggle => _viewModel is { WasReplaced: true, FilteredPacket: not null };

    public bool WasReplaced => _viewModel.WasReplaced;
    public bool WasBlocked => _viewModel.WasBlocked;

    public bool ShowValuesAsHex
    {
        get => _showValuesAsHex;
        set
        {
            if (SetProperty(ref _showValuesAsHex, value))
            {
                RefreshValues();
            }
        }
    }

    public string FormattedCommand => $"0x{Command:X2}";
    public string? FormattedSequence => Sequence is not null ? $"0x{Sequence:X2}" : null;

    public int HexSelectionStart
    {
        get => _hexSelectionStart;
        set
        {
            if (SetProperty(ref _hexSelectionStart, value))
            {
                SyncHexToTextSelection();
                RecalculateBufferIndex();
                RefreshValues();
            }
        }
    }

    public int HexSelectionEnd
    {
        get => _hexSelectionEnd;
        set
        {
            if (SetProperty(ref _hexSelectionEnd, value))
            {
                SyncHexToTextSelection();
                RecalculateBufferIndex();
                RefreshValues();
            }
        }
    }

    public int SelectedByteCount => Math.Abs(_endIndex - _startIndex);

    public int TextSelectionStart
    {
        get => _textSelectionStart;
        set
        {
            if (SetProperty(ref _textSelectionStart, value))
            {
                SyncTextToHexSelection();
            }
        }
    }

    public int TextSelectionEnd
    {
        get => _textSelectionEnd;
        set
        {
            if (SetProperty(ref _textSelectionEnd, value))
            {
                SyncTextToHexSelection();
            }
        }
    }

    partial void OnUseFilteredChanged(bool value) => RebuildHexView();

    public RawHexViewModel(TracePacketViewModel viewModel)
    {
        _viewModel = viewModel;
        _decryptedPayload = viewModel.DecryptedPacket.Data.ToArray();
        _filteredPayload = viewModel.FilteredPacket?.Data.ToArray() ?? [];

        RebuildHexView();
    }

    private void RebuildHexView()
    {
        var packet = WasReplaced && UseFiltered && _viewModel.FilteredPacket is not null
            ? _viewModel.FilteredPacket
            : _viewModel.DecryptedPacket;

        _payload = WasReplaced && UseFiltered ? _filteredPayload : _decryptedPayload;

        RawHex = string.Join(" ", _payload.Select(b => b.ToString("X2")));
        DecodedText = GetAsciiText();

        Command = packet.Command;
        Sequence = _viewModel.EncryptedPacket switch
        {
            ClientPacket clientPacket => clientPacket.Sequence,
            ServerPacket serverPacket => serverPacket.Sequence,
            _ => null
        };

        RefreshValues();
    }

    private void SyncTextToHexSelection()
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;

        try
        {
            var textStart = TextSelectionStart;
            var textEnd = TextSelectionEnd;

            if (textStart > textEnd)
            {
                (textStart, textEnd) = (textEnd, textStart);
            }

            var newHexStart = textStart * 3;
            var newHexEnd = textEnd * 3 - 1;

            if (Math.Abs(newHexStart - newHexEnd) < 2)
            {
                HexSelectionStart = 0;
                HexSelectionEnd = 0;
                return;
            }

            HexSelectionStart = newHexStart;
            HexSelectionEnd = newHexEnd;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void SyncHexToTextSelection()
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;

        try
        {
            var hexStart = HexSelectionStart;
            var hexEnd = HexSelectionEnd;

            if (hexStart > hexEnd)
            {
                (hexStart, hexEnd) = (hexEnd, hexStart);
            }

            var newTextStart = hexStart / 3;
            var newTextEnd = (hexEnd + 1) / 3;

            TextSelectionStart = newTextStart;
            TextSelectionEnd = newTextEnd;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void RecalculateBufferIndex()
    {
        var startIndex = HexSelectionStart / 3;
        var endIndex = (HexSelectionEnd + 1) / 3;

        // If selection is reversed, swap it for consistency
        if (startIndex > endIndex)
        {
            (startIndex, endIndex) = (endIndex, startIndex);
        }

        _startIndex = Math.Max(0, startIndex);
        _endIndex = Math.Min(endIndex, _payload?.Length ?? 0);

        OnPropertyChanged(nameof(SelectedByteCount));
    }

    private void RefreshValues()
    {
        var selectedSpan = _payload.AsSpan(_startIndex, Math.Abs(_endIndex - _startIndex));

        FormattedSignedByte = TryReadFormattedValue<sbyte>(selectedSpan, ShowValuesAsHex) ?? "--";
        FormattedUnsignedByte = TryReadFormattedValue<byte>(selectedSpan, ShowValuesAsHex) ?? "--";
        FormattedSignedShort = TryReadFormattedValue<short>(selectedSpan, ShowValuesAsHex) ?? "--";
        FormattedUnsignedShort = TryReadFormattedValue<ushort>(selectedSpan, ShowValuesAsHex) ?? "--";
        FormattedSignedInt = TryReadFormattedValue<int>(selectedSpan, ShowValuesAsHex) ?? "--";
        FormattedUnsignedInt = TryReadFormattedValue<uint>(selectedSpan, ShowValuesAsHex) ?? "--";
        FormattedSignedLong = TryReadFormattedValue<long>(selectedSpan, ShowValuesAsHex) ?? "--";
        FormattedUnsignedLong = TryReadFormattedValue<ulong>(selectedSpan, ShowValuesAsHex) ?? "--";

        FormattedIpAddress = selectedSpan.Length >= 4
            ? $"{selectedSpan[3]}.{selectedSpan[2]}.{selectedSpan[1]}.{selectedSpan[0]}"
            : "--";

        FormattedBitFlags = FormatBits(selectedSpan, 4) ?? "--";
    }

    private string GetAsciiText()
    {
        if (_payload is null)
        {
            return string.Empty;
        }
        
        var buffer = ArrayPool<char>.Shared.Rent(_payload.Length + 1);

        try
        {
            var decodedLength = Encoding.ASCII.GetChars(_payload, buffer);
            if (decodedLength < 1)
            {
                return string.Empty;
            }

            for (var i = 0; i < decodedLength; i++)
            {
                buffer[i] = buffer[i] switch
                {
                    var c when char.IsLetterOrDigit(c) => c,
                    var c when char.IsPunctuation(c) => c,
                    var c when char.IsSymbol(c) => c,
                    _ => '.'
                };
            }

            return new string(buffer, 0, decodedLength);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    [RelayCommand]
    public void SelectAll()
    {
        HexSelectionStart = 0;
        HexSelectionEnd = RawHex?.Length ?? 0;
    }

    [RelayCommand]
    public void ClearSelection()
    {
        HexSelectionStart = 0;
        HexSelectionEnd = 0;
    }

    private bool CanCopyToClipboard(string fieldName)
    {
        return fieldName switch
        {
            "command" => true,
            "sequence" => Sequence.HasValue,
            "hex-selection" => SelectedByteCount > 0,
            "hex-all" => RawHex?.Length is > 0,
            "ascii-selection" => SelectedByteCount > 0,
            "ascii-all" => DecodedText?.Length is > 0,
            "i8" or "u8" => SelectedByteCount > 0,
            "i16" or "u16" => SelectedByteCount >= 2,
            "i32" or "u32" => SelectedByteCount >= 4,
            "i64" or "u64" => SelectedByteCount >= 8,
            "ipv4" => SelectedByteCount >= 4,
            "flags" => SelectedByteCount > 0,
            _ => false
        };
    }

    [RelayCommand(CanExecute = nameof(CanCopyToClipboard))]
    private async Task CopyToClipboard(string fieldName)
    {
        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null)
        {
            return;
        }

        var textToCopy = fieldName switch
        {
            "command" => FormattedCommand,
            "sequence" => FormattedSequence,
            "i8" => FormattedSignedByte,
            "u8" => FormattedUnsignedByte,
            "i16" => FormattedSignedShort,
            "u16" => FormattedUnsignedShort,
            "i32" => FormattedSignedInt,
            "u32" => FormattedUnsignedInt,
            "i64" => FormattedSignedLong,
            "u64" => FormattedUnsignedLong,
            "ipv4" => FormattedIpAddress,
            "flags" => FormattedBitFlags,
            "hex-selection" => GetSelectedText(RawHex, HexSelectionStart, HexSelectionEnd),
            "hex-all" => RawHex,
            "ascii-selection" => GetSelectedText(DecodedText, TextSelectionStart, TextSelectionEnd),
            "ascii-all" => DecodedText,
            _ => null
        };

        if (!string.IsNullOrEmpty(textToCopy))
        {
            await clipboard.SetTextAsync(textToCopy);
            ShowCopyFeedback(fieldName);
        }
    }

    private void ShowCopyFeedback(string fieldName)
    {
        CopyFeedbackField = fieldName;

        var version = ++_copyFeedbackVersion;
        DispatcherTimer.RunOnce(() =>
        {
            if (version == _copyFeedbackVersion)
            {
                CopyFeedbackField = null;
            }
        }, TimeSpan.FromMilliseconds(350));
    }

    private static string GetSelectedText(string? value, int selectionStart, int selectionEnd)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var start = Math.Clamp(Math.Min(selectionStart, selectionEnd), 0, value.Length);
        var end = Math.Clamp(Math.Max(selectionStart, selectionEnd), 0, value.Length);
        return value[start..end];
    }

    private static string? FormatBits(ReadOnlySpan<byte> buffer, int groupBits = 8, char groupSeparator = ' ',
        int maxBytes = 4)
    {
        if (buffer.Length == 0)
        {
            return null;
        }

        var bytes = Math.Min(buffer.Length, maxBytes);
        var totalBits = bytes * 8;
        var groups = (totalBits + groupBits - 1) / groupBits;
        var separators = Math.Max(0, groups - 1);
        var outLen = totalBits + separators;

        var outSpan = outLen <= 1024 ? stackalloc char[outLen] : new char[outLen];

        var pos = 0;
        var bitsWritten = 0;

        for (var b = 0; b < bytes; b++)
        {
            var val = b < buffer.Length ? buffer[b] : (byte)0;

            // Big Endian
            for (var bit = 7; bit >= 0; bit--)
            {
                outSpan[pos++] = (((val >> bit) & 1) != 0) ? '1' : '0';
                bitsWritten++;

                if (bitsWritten % groupBits == 0 && bitsWritten < totalBits)
                    outSpan[pos++] = groupSeparator;
            }
        }

        return new string(outSpan);
    }

    private static string? TryReadFormattedValue<T>(ReadOnlySpan<byte> buffer, bool isHex = false)
        where T : struct
    {
        var t = typeof(T);
        var numberOfBytes = GetSizeOfType(t);

        if (numberOfBytes > buffer.Length)
        {
            return null;
        }

        ulong value = 0;
        for (var i = 0; i < numberOfBytes; i++)
        {
            value = (value << 8) | buffer[i];
        }

        if (isHex)
        {
            return t switch
            {
                _ when t == typeof(bool) || t == typeof(char) => $"0x{value:X2}",
                _ when t == typeof(sbyte) || t == typeof(byte) => $"0x{value:X2}",
                _ when t == typeof(short) || t == typeof(ushort) => $"0x{value:X4}",
                _ when t == typeof(int) || t == typeof(uint) => $"0x{value:X8}",
                _ when t == typeof(long) || t == typeof(ulong) => $"0x{value:X16}",
                _ => $"0x{value:X}"
            };
        }

        return t switch
        {
            _ when t == typeof(sbyte) => ((sbyte)value).ToString(),
            _ when t == typeof(short) => ((short)value).ToString(),
            _ when t == typeof(int) => ((int)value).ToString(),
            _ when t == typeof(long) => ((long)value).ToString(),
            _ => value.ToString()
        };
    }

    private static int GetSizeOfType(Type type)
    {
        return type switch
        {
            _ when type == typeof(bool) => 1,
            _ when type == typeof(byte) || type == typeof(sbyte) => 1,
            _ when type == typeof(short) || type == typeof(ushort) => 2,
            _ when type == typeof(int) || type == typeof(uint) => 4,
            _ when type == typeof(long) || type == typeof(ulong) => 8,
            _ => throw new NotSupportedException($"Size for type {type.Name} not supported")
        };
    }
}
