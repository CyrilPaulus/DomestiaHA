using DomestiaHA.DomestiaProtocol.Enums;

namespace DomestiaHA.DomestiaProtocol;
public record class DomestiaRelayConfiguration
(
    int RelayId,
    RelayType RelayType,
    string Label
);
