using System.ComponentModel;
using System.Reflection;

namespace Smart
{
    static class HelpingFuctions
    {
        public static string getEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString())!;
            var attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes[0] as DescriptionAttribute)!.Description;
        }
    }
}
