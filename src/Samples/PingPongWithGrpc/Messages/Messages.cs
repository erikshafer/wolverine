namespace Messages;

// These are shared message contracts used by both the Pinger and Ponger services.
// In production you would typically put these in a shared NuGet package.

public class Ping
{
    public int Number { get; set; }
}

public class Pong
{
    public int Number { get; set; }
}
