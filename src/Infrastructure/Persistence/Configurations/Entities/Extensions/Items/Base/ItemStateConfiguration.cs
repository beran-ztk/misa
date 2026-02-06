using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Infrastructure.Persistence.Configurations.Entities.Extensions.Items.Base;

public class ItemStateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired();
        
        builder.Property(x => x.Synopsis);
        
        builder.HasData(
            new State(1,  "Draft",     "Entwurf; noch nie daran gearbeitet"),
            new State(2,  "Undefined", "Unklar; muss präzisiert werden"),
            new State(3,  "Scheduled", "Geplant für einen zukünftigen Zeitpunkt"),

            new State(4,  "InProgress","Bereits bearbeitet, aktuell keine aktive Session"),
            new State(5,  "Active",    "Aktive Session läuft"),
            new State(6,  "Paused",    "Session pausiert (max. 6h, danach Auto-Fortsetzung)"),
            new State(7,  "Pending",   "Zurückgestellt; lange nicht bearbeitet"),

            new State(8,  "WaitForResponse",       "Wartet auf Rückmeldung einer Person oder Stelle"),
            new State(9,  "BlockedByRelationship", "Blockiert durch Relation oder Abhängigkeit"),

            new State(10, "Done",     "Erfolgreich abgeschlossen"),
            new State(11, "Canceled", "Abgebrochen; nicht weiter erforderlich"),
            new State(12, "Failed",   "Gescheitert; Ziel nicht erreicht"),
            new State(13, "Expired",  "Automatisch abgelaufen (Deadline überschritten)")
        );
    }
}