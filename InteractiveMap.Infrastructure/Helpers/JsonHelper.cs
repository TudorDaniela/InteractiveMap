using System.Text.Json;

namespace InteractiveMap.Infrastructure.Helpers
{
    public static class JsonHelper
    {
        private static string? GetJsonStringValue(JsonElement element, params string[] path)
        {
            JsonElement current = element;
            foreach (var key in path)
            {
                if (current.TryGetProperty(key, out var next))
                {
                    current = next;
                }
                else
                {
                    return null;
                }
            }
            return current.GetString();
        }

        private static string? GetJsonArrayFirstString(JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
            {
                var enumerator = arrayElement.EnumerateArray();
                if (enumerator.MoveNext())
                {
                    return enumerator.Current.GetString();
                }
            }
            return null;
        }

        private static long GetJsonLongValue(JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out var value) && value.TryGetInt64(out var result))
            {
                return result;
            }
            return 0;
        }

        private static double GetJsonDoubleValue(JsonElement element, string key, int arrayIndex)
        {
            if (element.TryGetProperty(key, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
            {
                var items = arrayElement.EnumerateArray().ToList();
                if (arrayIndex < items.Count && items[arrayIndex].TryGetDouble(out var result))
                {
                    return result;
                }
            }
            return 0;
        }

    }
}