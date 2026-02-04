using System.Collections.ObjectModel;
using NodaTime;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Startup;

public sealed class TimeZoneService
{
    public ReadOnlyCollection<string> Ids { get; } = DateTimeZoneProviders.Tzdb.Ids;
}