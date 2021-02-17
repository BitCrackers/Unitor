using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Unitor.Core.Deobfuscation
{
    public class JsonTypeMapping
    {
        public string Name { get; set; }
        public List<string> KnownTranslations { get; set; }
        public List<JsonFieldMapping> Fields { get; set; }
    }

    public class JsonFieldMapping
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Offset { get; set; }
        public List<string> KnownTranslations { get; set; }
    }

    public class JsonMethodMapping
    {
        public string Name { get; set; }
        public List<string> KnownTranslations { get; set; }
    }

    public class JsonTranslations
    {
        public Dictionary<string, string> Types { get; set; }
        public Dictionary<string, string> Fields { get; set; }
    }

    public static class JsonLoader
    {
        public static List<JsonTypeMapping> DeserialzeMappings(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<JsonTypeMapping>>(json); 
            }
            catch(Exception)
            {
                return null;
            }
        }

        public static JsonTranslations DeserialzeTranslations(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<JsonTranslations>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
