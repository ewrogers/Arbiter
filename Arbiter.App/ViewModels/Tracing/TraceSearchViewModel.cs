using Arbiter.App.Models.Tracing;
using Arbiter.App.Models.Tracing.Queries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Tracing;

public partial class TraceSearchViewModel : ViewModelBase
{
    private TraceQuery _query = TraceQuery.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQueryText))]
    private string _queryText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQueryError))]
    [NotifyPropertyChangedFor(nameof(QueryErrorText))]
    private TraceQueryDiagnostic? _queryError;

    [ObservableProperty]
    private bool _isTextCaseSensitive;

    public TraceQuery Query
    {
        get => _query;
        private set => SetProperty(ref _query, value);
    }

    public bool HasQueryError => QueryError is not null;
    public bool HasQueryText => !string.IsNullOrWhiteSpace(QueryText);
    public string? QueryErrorText => QueryError?.Message;

    partial void OnQueryTextChanged(string value)
    {
        ParseQuery(value);
    }

    partial void OnIsTextCaseSensitiveChanged(bool value)
    {
        ParseQuery(QueryText);
    }

    private void ParseQuery(string value)
    {
        var result = TraceQueryParser.Parse(value, IsTextCaseSensitive);
        QueryError = result.Diagnostic;

        if (result.Query is not null)
        {
            Query = result.Query;
        }
    }

    public void SetCommand(PacketDirection direction, byte command)
    {
        var field = direction == PacketDirection.Client ? "client" : "server";
        QueryText = $"{field}={command:X2}";
    }

    [RelayCommand]
    public void Clear()
    {
        QueryText = string.Empty;
    }
}
