using System.Runtime.Serialization;

namespace PxApiConverter.Business
{
    public class EnumConverter
    {

        public static T ToEnum<T>(string str)
        {
            var enumType = typeof(T);
            foreach (var name in Enum.GetNames(enumType))
            {
                var type = enumType.GetField(name);
                ArgumentNullException.ThrowIfNull(type);
                var enumMemberAttribute = ((EnumMemberAttribute[])type.GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                if (enumMemberAttribute.Value is not null && enumMemberAttribute.Value.Equals(str, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)Enum.Parse(enumType, name);
                }
            }
            throw new InvalidOperationException($"Invalid value for enum {enumType.Name}");
        }

        public static string ToEnumString<T>(T type)
        {
            if (type is null)
            {
                return "";
            }
            var enumType = typeof(T);
            var name = Enum.GetName(enumType, type);
            if (name is null)
            {
                return "";
            }

            var attributes = enumType.GetField(name);

            if (attributes is not null)
            {
                var enumMemberAttribute = ((EnumMemberAttribute[])attributes.GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                return enumMemberAttribute.Value ?? "";
            }

            return "";
        }
    }
}
