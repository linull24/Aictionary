using ReactiveUI;

namespace Aictionary.ViewModels;

public class DownloadProgressViewModel : ViewModelBase
{
    private string _statusMessage = "Initializing download...";
    private bool _isIndeterminate = true;
    private double _progress;
    private bool _isCompleted;

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => this.RaiseAndSetIfChanged(ref _isIndeterminate, value);
    }

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => this.RaiseAndSetIfChanged(ref _isCompleted, value);
    }
}
