namespace Canary.Bird;

internal abstract record BirdCommand(
    string Line)
{
    public static BirdCommand Parse(string line)
    {
        if (line.Split(' ') is not { Length: 2 } split)
            return new UnknownBirdCommand(line);

        return split[0] switch
        {
            "disable" => new DisableProtocolBirdCommand(line, split[1]),
            "enable" => new EnableProtocolBirdCommand(line, split[1]),
            _ => new UnknownBirdCommand(line)
        };
    }
}

internal sealed record UnknownBirdCommand(string Line) : BirdCommand(Line);
internal sealed record DisableProtocolBirdCommand(string Line, string Protocol) : BirdCommand(Line);
internal sealed record EnableProtocolBirdCommand(string Line, string Protocol) : BirdCommand(Line);
