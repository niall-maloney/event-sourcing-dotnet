using System.Text;
using Nito.Guids;

namespace NiallMaloney.EventSourcing.Utils;

internal static class DeterministicIdFactory
{
    private static readonly Guid Namespace = new("03EEA21D-2B32-4158-AFCA-5BFE80929C91");

    internal static string NewId(params string[] components)
    {
        if (!components.Any())
        {
            throw new ArgumentException("At least one component must be specified.",
                nameof(components));
        }

        var bytes = Encoding.UTF8.GetBytes(string.Join('-', components));
        return GuidFactory.CreateSha1(Namespace, bytes).ToString();
    }
}
