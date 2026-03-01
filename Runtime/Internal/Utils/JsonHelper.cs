using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Weppy.AIProvider
{
    public static class JsonHelper
    {
        public static string Serialize(Dictionary<string, object> data_)
        {
            if (data_ == null)
                return "null";

            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            bool first = true;
            foreach (KeyValuePair<string, object> kvp in data_)
            {
                if (!first)
                    sb.Append(",");
                first = false;

                sb.Append("\"");
                sb.Append(EscapeString(kvp.Key));
                sb.Append("\":");
                sb.Append(SerializeValueInternal(kvp.Value));
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string SerializeValueInternal(object value_)
        {
            if (value_ == null)
                return "null";

            if (value_ is string strValue)
                return "\"" + EscapeString(strValue) + "\"";

            if (value_ is bool boolValue)
                return boolValue ? "true" : "false";

            if (value_ is int intValue)
                return intValue.ToString(CultureInfo.InvariantCulture);

            if (value_ is long longValue)
                return longValue.ToString(CultureInfo.InvariantCulture);

            if (value_ is float floatValue)
                return floatValue.ToString(CultureInfo.InvariantCulture);

            if (value_ is double doubleValue)
                return doubleValue.ToString(CultureInfo.InvariantCulture);

            if (value_ is Dictionary<string, object> dictValue)
                return Serialize(dictValue);

            if (value_ is IEnumerable<object> enumValue)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                bool first = true;
                foreach (object item in enumValue)
                {
                    if (!first)
                        sb.Append(",");
                    first = false;
                    sb.Append(SerializeValueInternal(item));
                }
                sb.Append("]");
                return sb.ToString();
            }

            if (value_ is Array arrayValue)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                bool first = true;
                foreach (object item in arrayValue)
                {
                    if (!first)
                        sb.Append(",");
                    first = false;
                    sb.Append(SerializeValueInternal(item));
                }
                sb.Append("]");
                return sb.ToString();
            }

            return "\"" + EscapeString(value_.ToString()) + "\"";
        }

        private static string EscapeString(string value_)
        {
            if (string.IsNullOrEmpty(value_))
                return value_;

            StringBuilder sb = new StringBuilder();
            foreach (char c in value_)
            {
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public static Dictionary<string, object> Deserialize(string json_)
        {
            if (string.IsNullOrEmpty(json_))
                return null;

            int index = 0;
            return ParseObject(json_, ref index);
        }

        private static Dictionary<string, object> ParseObject(string json_, ref int index_)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            SkipWhitespace(json_, ref index_);
            if (index_ >= json_.Length || json_[index_] != '{')
                return null;

            index_++;
            SkipWhitespace(json_, ref index_);

            if (index_ < json_.Length && json_[index_] == '}')
            {
                index_++;
                return result;
            }

            while (index_ < json_.Length)
            {
                SkipWhitespace(json_, ref index_);

                string key = ParseString(json_, ref index_);
                if (key == null)
                    break;

                SkipWhitespace(json_, ref index_);
                if (index_ >= json_.Length || json_[index_] != ':')
                    break;
                index_++;

                SkipWhitespace(json_, ref index_);
                object value = ParseValue(json_, ref index_);
                result[key] = value;

                SkipWhitespace(json_, ref index_);
                if (index_ >= json_.Length)
                    break;

                if (json_[index_] == '}')
                {
                    index_++;
                    break;
                }

                if (json_[index_] == ',')
                    index_++;
            }

            return result;
        }

        private static object ParseValue(string json_, ref int index_)
        {
            SkipWhitespace(json_, ref index_);

            if (index_ >= json_.Length)
                return null;

            char c = json_[index_];

            if (c == '"')
                return ParseString(json_, ref index_);

            if (c == '{')
                return ParseObject(json_, ref index_);

            if (c == '[')
                return ParseArray(json_, ref index_);

            if (c == 't' || c == 'f')
                return ParseBool(json_, ref index_);

            if (c == 'n')
                return ParseNull(json_, ref index_);

            if (c == '-' || char.IsDigit(c))
                return ParseNumber(json_, ref index_);

            return null;
        }

        private static string ParseString(string json_, ref int index_)
        {
            if (index_ >= json_.Length || json_[index_] != '"')
                return null;

            index_++;
            StringBuilder sb = new StringBuilder();

            while (index_ < json_.Length)
            {
                char c = json_[index_];

                if (c == '"')
                {
                    index_++;
                    return sb.ToString();
                }

                if (c == '\\' && index_ + 1 < json_.Length)
                {
                    index_++;
                    char escaped = json_[index_];
                    switch (escaped)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index_ + 4 < json_.Length)
                            {
                                string hex = json_.Substring(index_ + 1, 4);
                                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                                    sb.Append((char)code);
                                index_ += 4;
                            }
                            break;
                        default:
                            sb.Append(escaped);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }

                index_++;
            }

            return sb.ToString();
        }

        private static List<object> ParseArray(string json_, ref int index_)
        {
            List<object> result = new List<object>();

            if (index_ >= json_.Length || json_[index_] != '[')
                return null;

            index_++;
            SkipWhitespace(json_, ref index_);

            if (index_ < json_.Length && json_[index_] == ']')
            {
                index_++;
                return result;
            }

            while (index_ < json_.Length)
            {
                object value = ParseValue(json_, ref index_);
                result.Add(value);

                SkipWhitespace(json_, ref index_);
                if (index_ >= json_.Length)
                    break;

                if (json_[index_] == ']')
                {
                    index_++;
                    break;
                }

                if (json_[index_] == ',')
                    index_++;

                SkipWhitespace(json_, ref index_);
            }

            return result;
        }

        private static object ParseNumber(string json_, ref int index_)
        {
            int start = index_;

            if (index_ < json_.Length && json_[index_] == '-')
                index_++;

            while (index_ < json_.Length && char.IsDigit(json_[index_]))
                index_++;

            bool isFloat = false;
            if (index_ < json_.Length && json_[index_] == '.')
            {
                isFloat = true;
                index_++;
                while (index_ < json_.Length && char.IsDigit(json_[index_]))
                    index_++;
            }

            if (index_ < json_.Length && (json_[index_] == 'e' || json_[index_] == 'E'))
            {
                isFloat = true;
                index_++;
                if (index_ < json_.Length && (json_[index_] == '+' || json_[index_] == '-'))
                    index_++;
                while (index_ < json_.Length && char.IsDigit(json_[index_]))
                    index_++;
            }

            string numStr = json_.Substring(start, index_ - start);

            if (isFloat)
            {
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return d;
            }
            else
            {
                if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                    return l;
            }

            return 0;
        }

        private static bool ParseBool(string json_, ref int index_)
        {
            if (json_.Substring(index_).StartsWith("true"))
            {
                index_ += 4;
                return true;
            }
            if (json_.Substring(index_).StartsWith("false"))
            {
                index_ += 5;
                return false;
            }
            return false;
        }

        private static object ParseNull(string json_, ref int index_)
        {
            if (json_.Substring(index_).StartsWith("null"))
            {
                index_ += 4;
                return null;
            }
            return null;
        }

        private static void SkipWhitespace(string json_, ref int index_)
        {
            while (index_ < json_.Length && char.IsWhiteSpace(json_[index_]))
                index_++;
        }

        public static T GetValue<T>(Dictionary<string, object> data_, string key_, T defaultValue_ = default)
        {
            if (data_ == null || !data_.TryGetValue(key_, out object value) || value == null)
                return defaultValue_;

            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue_;
            }
        }

        public static Dictionary<string, object> GetObject(Dictionary<string, object> data_, string key_)
        {
            if (data_ == null || !data_.TryGetValue(key_, out object value))
                return null;

            return value as Dictionary<string, object>;
        }

        public static List<object> GetArray(Dictionary<string, object> data_, string key_)
        {
            if (data_ == null || !data_.TryGetValue(key_, out object value))
                return null;

            return value as List<object>;
        }
    }
}
