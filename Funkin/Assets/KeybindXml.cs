using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using SonicOrca.Input;

namespace SonicOrca.Funkin.Assets
{
    internal static class KeybindXml
    {
        public static int[][] Load(string path)
        {
            XDocument doc = XDocument.Load(path);
            XElement root = doc.Root;
            if (root == null || root.Name.LocalName != "keybinds")
                throw new InvalidOperationException("Expected root element keybinds.");

            var list = new List<int[]>();
            foreach (XElement bind in root.Elements())
            {
                if (bind.Name.LocalName != "bind")
                    continue;
                string main = bind.Attribute("main")?.Value;
                string alt = bind.Attribute("alt")?.Value;
                if (!KeyboardNameMap.TryGet(main, out int k0))
                    throw new InvalidOperationException("Unknown key: " + main);
                if (!KeyboardNameMap.TryGet(alt, out int k1))
                    throw new InvalidOperationException("Unknown key: " + alt);
                list.Add(new[] { k0, k1 });
            }
            return list.ToArray();
        }
    }

    internal static class KeyboardNameMap
    {
        private static readonly Dictionary<string, int> Map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "A", KeyboardState.KEY_A },
            { "B", KeyboardState.KEY_B },
            { "C", KeyboardState.KEY_C },
            { "D", KeyboardState.KEY_D },
            { "E", KeyboardState.KEY_E },
            { "F", KeyboardState.KEY_F },
            { "G", KeyboardState.KEY_G },
            { "H", KeyboardState.KEY_H },
            { "I", KeyboardState.KEY_I },
            { "J", KeyboardState.KEY_J },
            { "K", KeyboardState.KEY_K },
            { "L", KeyboardState.KEY_L },
            { "M", KeyboardState.KEY_M },
            { "N", KeyboardState.KEY_N },
            { "O", KeyboardState.KEY_O },
            { "P", KeyboardState.KEY_P },
            { "Q", KeyboardState.KEY_Q },
            { "R", KeyboardState.KEY_R },
            { "S", KeyboardState.KEY_S },
            { "T", KeyboardState.KEY_T },
            { "U", KeyboardState.KEY_U },
            { "V", KeyboardState.KEY_V },
            { "W", KeyboardState.KEY_W },
            { "X", KeyboardState.KEY_X },
            { "Y", KeyboardState.KEY_Y },
            { "Z", KeyboardState.KEY_Z },
            { "LEFT", KeyboardState.KEY_LEFT },
            { "RIGHT", KeyboardState.KEY_RIGHT },
            { "UP", KeyboardState.KEY_UP },
            { "DOWN", KeyboardState.KEY_DOWN },
        };

        public static bool TryGet(string name, out int scancode)
        {
            scancode = 0;
            if (string.IsNullOrEmpty(name))
                return false;
            return Map.TryGetValue(name.Trim(), out scancode);
        }
    }
}