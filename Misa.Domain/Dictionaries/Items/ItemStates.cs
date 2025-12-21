namespace Misa.Domain.Dictionaries.Items;

public enum ItemStates
{
    // Base
    Draft      = 1,   // Noch nie daran gearbeitet (System)
    
    Undefined  = 2,   // Unklar / muss präzisiert werden (User → Draft)

    // Work
    InProgress = 4,   // Wurde bereits bearbeitet
    Active     = 5,   // Aktive Session läuft
    Paused     = 6,   // Session pausiert (max 6h → Auto InProgress)
    Pending    = 7,   // Auto: lange nicht angefasst / zurückgestellt

    // Blocked
    WaitForResponse       = 8,   // Wartet auf Antwort (Person/Kontakt)

    // Final
    Done      = 10,  // Erfolgreich abgeschlossen
    Canceled  = 11,  // Abgebrochen
    Failed    = 12,  // Gescheitert
    Expired   = 13   // Automatisch abgelaufen
}

