using RecallIQ.Core.Models;

namespace RecallIQ.Core.Interfaces;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    event EventHandler<AppSettings>? SettingsChanged;
}
