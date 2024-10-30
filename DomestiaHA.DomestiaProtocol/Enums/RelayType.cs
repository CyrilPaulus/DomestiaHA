namespace DomestiaHA.DomestiaProtocol.Enums;
public enum RelayType
{
    Toggle = 0,
    Relay = 1,

    TimerToggleMin = 2,
    TimerToggleSec = 3,

    TimerContinueMin = 4,
    TimerContinueSec = 5,

    DimmerStop = 6,
    DimmerContinue = 7,

    ShutterDown = 8,
    ShutterUp = 9,
    ShutterUnBP = 10,

    RelayCapt = 11,

    RGBRed = 12,
    RGBGreen = 13,
    RGBBlue = 14,
    RGBWhite = 15,

    Unused = 255
}
