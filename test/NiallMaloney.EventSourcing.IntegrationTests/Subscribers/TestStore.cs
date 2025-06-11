namespace NiallMaloney.EventSourcing.IntegrationTests.Subscribers;

public class TestStore
{
    public IDictionary<string, int> Tests { get; } = new Dictionary<string, int>();

    public void Add(string key)
    {
        if (Tests.ContainsKey(key))
        {
            Tests[key]++;
        }
        else
        {
            Tests.Add(key, 1);
        }
    }
}
