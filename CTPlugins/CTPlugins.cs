using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace CTPlugins
{
    public class CTPlugins
    {
        public static List<ICTPlugin> FindPlugins()
        {
            var plugins = new List<ICTPlugin>();
            if (!Directory.Exists("Plugins")) 
                Directory.CreateDirectory("Plugins");

            string pluginFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            foreach (string filePath in Directory.EnumerateFiles(pluginFolder, "*.dll"))
            {
                try
                {
                    // LoadFrom (not LoadFile) is required so satellite resource assemblies
                    // (e.g. Strings.pl.resx's compiled output) resolve relative to the plugin's
                    // own directory — LoadFile loads into a context that skips that probing.
                    Assembly pluginAssembly = Assembly.LoadFrom(filePath);
                    var pluginTypes = pluginAssembly.GetTypes()
                        .Where(t => t.GetCustomAttributes(typeof(CustomToolsPluginAttribute), true).Any());

                    foreach (var type in pluginTypes)
                    {
                        plugins.Add(Activator.CreateInstance(type) as ICTPlugin);
                    }
                }
                catch(Exception exc)
                {
                    MessageBox.Show(exc.Message, "Plugins scanning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            return plugins;
        }
    }
}
