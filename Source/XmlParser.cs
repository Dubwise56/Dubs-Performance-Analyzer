using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace DubsAnalyzer
{
    public static class XmlParser
    {
        public static List<string> meths = new List<string>();
        public static List<string> types = new List<string>();
        public static void Parse(XmlDocument doc, ref Dictionary<string, List<MethodInfo>> methodsToPatch)
        { 
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                meths = new List<string>();
                types = new List<string>();
                WriteToLists(node);
                methodsToPatch.Add(node.Name, new List<MethodInfo>());
                foreach(var meth in meths)
                {
                    MethodInfo method = null;
                    try
                    {
                        method = AccessTools.Method(meth);
                        methodsToPatch[node.Name].Add(method);
                    } catch (Exception) { }
                }
                foreach (var type in types)
                {
                    Type thetype = null;
                    try
                    {
                        thetype = AccessTools.TypeByName(type);
                        foreach (var method in AccessTools.GetDeclaredMethods(thetype))
                        {
                            if (method.DeclaringType == thetype && !method.IsSpecialName && !method.IsAssembly && method.HasMethodBody())
                            {
                                methodsToPatch[node.Name].Add(method);
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private static void WriteToLists(XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                switch (child.Name.ToLower())
                {
                    case "methods":
                        foreach (XmlNode baby in child.ChildNodes)
                            meths.Add(baby.InnerText);
                        break;
                    case "types":
                        foreach (XmlNode baby in child.ChildNodes)
                            types.Add(baby.InnerText);
                        break;
                    default:
                        Log.Error($"Attempting to read unknown value from an Analyzer.xml, the given input was {child.Name}, it should have been either '(M/m)ethods' or '(T/t)ypes'");
                        break;
                }
            }
        }
    }
}
