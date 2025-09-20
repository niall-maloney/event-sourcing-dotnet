namespace NiallMaloney.EventSourcing.IntegrationTests;

public static class Options
{
    public static readonly KurrentDBClientOptions KurrentDB =
        new("kurrentdb+discover://localhost:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000");
}
