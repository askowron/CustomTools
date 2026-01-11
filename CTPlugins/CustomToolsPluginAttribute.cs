using System;

namespace CTPlugins
{
    public class CustomToolsPluginAttribute : Attribute
    {
        public string Name { get; set; }
        public Version Version { get; set; }

        public CustomToolsPluginAttribute(string name, Version version) 
        {
            Name = name;
            Version = version;
        }

        public CustomToolsPluginAttribute(string name, string version) 
        {
            Name = name;
            Version = new Version(version);
        }

    }
}
