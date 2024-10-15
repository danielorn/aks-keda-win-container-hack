using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Billing.BatchSupport.BatchJob.Event
{
    public class JsonParameterConverter : JsonConverter<Parameter>
    {
        public override Parameter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var param = new Parameter();

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of JSON object.");

            string type = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return param;  // End of object

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();

                    reader.Read(); // Move to the value

                    if (propertyName.Equals("type", StringComparison.OrdinalIgnoreCase))
                    {
                        type = reader.GetString();  // Store the type
                    }
                    else if (propertyName.Equals("value", StringComparison.OrdinalIgnoreCase))
                    {
                        if (type == null)
                            throw new JsonException("Type must be read before the value.");

                        param.Value = ReadValue(ref reader, type);  // Use the type to parse the value
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON object.");
        }


        private object ReadValue(ref Utf8JsonReader reader, string type)
        {
            switch (type.ToLower())
            {
                case "string":
                    return reader.GetString();
                case "int":
                    return reader.GetInt32();
                case "int32":
                    return reader.GetInt32();
                case "boolean":
                    return reader.GetBoolean();
                case "decimal":
                    return reader.GetDecimal();
                default:
                    throw new JsonException("Unknown type in JSON");
            }
        }

        public override void Write(Utf8JsonWriter writer, Parameter param, JsonSerializerOptions options)
        {

            writer.WriteStartObject();

            writer.WriteString("type", param.Value.GetType().Name);
            writer.WritePropertyName("value");
            WriteValue(writer, param.Value);

            writer.WriteEndObject();
        }

        private void WriteValue(Utf8JsonWriter writer, object value)
        {
            switch (value)
            {
                case string strValue:
                    writer.WriteStringValue(strValue);
                    break;
                case int intValue:
                    writer.WriteNumberValue(intValue);
                    break;
                case decimal decimalValue:
                    writer.WriteNumberValue(decimalValue);
                    break;
                case bool boolValue:
                    writer.WriteBooleanValue(boolValue);
                    break;
                default:
                    throw new JsonException($"Unsupported type: {value.GetType()}");
            }
        }
    }
}
