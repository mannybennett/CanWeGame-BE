using System.Text.Json;
using System.Text.Json.Serialization; // Required for JsonConverter

namespace CanWeGame.API.Converters
{
    // Custom JsonConverter for System.TimeOnly objects.
    // This converter handles the serialization and deserialization of TimeOnly
    // to and from "HH:mm" string format, which is common for <input type="time">.
    public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        // Defines how to read (deserialize) a JSON value into a TimeOnly object.
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Check if the current token is a string. TimeOnly is expected to be sent as a string.
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string but got {reader.TokenType}.");
            }

            // Get the string value from the JSON reader.
            string? timeString = reader.GetString();

            // Attempt to parse the string into a TimeOnly object.
            // TimeOnly.ParseExact is used to ensure the string matches a specific format ("HH:mm").
            // You can add more formats if your frontend might send different ones.
            if (TimeOnly.TryParseExact(timeString, "HH:mm", out TimeOnly result))
            {
                return result;
            }

            // If parsing fails, throw a JsonException.
            throw new JsonException($"Unable to parse \"{timeString}\" to TimeOnly. Expected format: HH:mm.");
        }

        // Defines how to write (serialize) a TimeOnly object into a JSON value.
        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            // Format the TimeOnly value into an "HH:mm" string and write it to the JSON output.
            writer.WriteStringValue(value.ToString("HH:mm"));
        }
    }
}