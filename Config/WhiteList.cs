
// ===========================================================================
// CILPE - Partial Evaluator for Common Intermediate Language
// ===========================================================================
// File: 
//     Attributes.cs
//
// Description:
//     White list of methods
//
// Author: 
//     Sergei Skorobogatov (Sergei.Skorobogatov@supercompilers.com)
// ===========================================================================


using System;

namespace CILPE.Config
{
    using System.Collections;
    using System.Reflection;
    using System.IO;
    using System.Xml;

    public class WhiteList
    {
        private Hashtable methods;

        public WhiteList()
        {
            methods = new Hashtable();
        }

        public bool Contains(MethodBase method) { return methods.ContainsKey(method); }

        public void AddMethod(MethodBase method) { methods.Add(method,true); }

        public void AddMethod(string className, string methodName, string[] paramTypes)
        {
            Type type = Type.GetType(className);

            Type[] parms = new Type[paramTypes.Length];
            for (int i = 0; i < paramTypes.Length; i++)
                parms[i] = Type.GetType(paramTypes[i]);

            MethodBase method = type.GetMethod(
                methodName,
                (BindingFlags)(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static),
                null,
                parms,
                null
                );

            AddMethod(method);
        }

        public void AddFromXml(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = false;

            XmlTextReader reader = new XmlTextReader(fileName);
            doc.Load(reader);

            XmlNode root = doc.DocumentElement;
            foreach (XmlNode methodNode in root)
            {
                string className = methodNode["class"].InnerText,
                       methodName = methodNode["name"].InnerText;

                XmlNodeList paramList = methodNode["parameters"].ChildNodes;
                string[] paramTypes = new string[paramList.Count];
                for (int i = 0; i < paramList.Count; i++)
                    paramTypes[i] = paramList[i].InnerText;

                AddMethod(className,methodName,paramTypes);
            }
        }

        public void SaveToXml(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<white_list>\n</white_list>");

            XmlNode root = doc.DocumentElement;
            foreach (MethodBase method in methods.Keys)
            {
                XmlElement methodNode = doc.CreateElement("method");
                root.AppendChild(methodNode);

                XmlElement className = doc.CreateElement("class");
                methodNode.AppendChild(className);
                className.AppendChild(doc.CreateTextNode(method.DeclaringType.ToString()));

                XmlElement methodName = doc.CreateElement("name");
                methodNode.AppendChild(methodName);
                methodName.AppendChild(doc.CreateTextNode(method.Name));

                XmlElement paramNode = doc.CreateElement("parameters");
                methodNode.AppendChild(paramNode);

                ParameterInfo[] paramTypes = method.GetParameters();
                foreach (ParameterInfo pType in paramTypes)
                {
                    XmlElement typeNode = doc.CreateElement("type");
                    paramNode.AppendChild(typeNode);
                    typeNode.AppendChild(doc.CreateTextNode(pType.ParameterType.ToString()));
                }
            }

            doc.Save(fileName);
        }
    }
}
