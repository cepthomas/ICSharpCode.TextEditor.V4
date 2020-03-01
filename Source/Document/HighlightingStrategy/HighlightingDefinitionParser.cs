// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

namespace ICSharpCode.TextEditor.Document
{
    public static class HighlightingDefinitionParser
    {
        public static HighlightingStrategy Parse(XmlReader xmlReader)
        {
            try
            {
                List<ValidationEventArgs> errors = null;
                XmlReaderSettings settings = new XmlReaderSettings();
                // was: Stream schemaStream = typeof(HighlightingDefinitionParser).Assembly.GetManifestResourceStream("ICSharpCode.TextEditor.Resources.Mode.xsd");
                string schemaStreamFile = Path.Combine("SyntaxDefinition", "Mode.xsd");
                Stream schemaStream = File.OpenRead(schemaStreamFile);
                settings.Schemas.Add("", new XmlTextReader(schemaStream));
                settings.Schemas.ValidationEventHandler += delegate(object sender, ValidationEventArgs args)
                {
                    if (errors == null)
                    {
                        errors = new List<ValidationEventArgs>();
                    }
                    errors.Add(args);
                };
                settings.ValidationType = ValidationType.Schema;
                XmlReader validatingReader = XmlReader.Create(xmlReader, settings);

                XmlDocument doc = new XmlDocument();
                doc.Load(validatingReader);

                HighlightingStrategy highlighter = new HighlightingStrategy(doc.DocumentElement.Attributes["name"].InnerText);

                //TODO2 this is not used right now:
                //if (doc.DocumentElement.HasAttribute("extends"))
                //{
                //    KeyValuePair<SyntaxMode, ISyntaxModeFileProvider> entry = HighlightingManager.Instance.FindHighlighterEntry(doc.DocumentElement.GetAttribute("extends"));
                //    if (entry.Key == null)
                //    {
                //        throw new HighlightingDefinitionInvalidException("Cannot find referenced highlighting source " + doc.DocumentElement.GetAttribute("extends"));
                //    }
                //    else
                //    {
                //        highlighter = Parse(highlighter, entry.Key, entry.Value.GetSyntaxModeFile(entry.Key));
                //        if (highlighter == null) return null;
                //    }
                //}

                if (doc.DocumentElement.HasAttribute("extensions"))
                {
                    highlighter.Extensions = doc.DocumentElement.GetAttribute("extensions").Split(new char[] { ';', '|' });
                }

                if (doc.DocumentElement.HasAttribute("folding"))
                {
                    highlighter.Folding = doc.DocumentElement.GetAttribute("folding");
                }

                // parse properties
                if (doc.DocumentElement["Properties"]!= null)
                {
                    foreach (XmlElement propertyElement in doc.DocumentElement["Properties"].ChildNodes)
                    {
                        highlighter.Properties[propertyElement.Attributes["name"].InnerText] =  propertyElement.Attributes["value"].InnerText;
                    }
                }

                if (doc.DocumentElement["Digits"]!= null)
                {
                    highlighter.DigitColor = new HighlightColor(doc.DocumentElement["Digits"]);
                }

                XmlNodeList nodes = doc.DocumentElement.GetElementsByTagName("RuleSet");
                foreach (XmlElement element in nodes)
                {
                    highlighter.AddRuleSet(new HighlightRuleSet(element));
                }

                xmlReader.Close();

                if (errors != null)
                {
                    StringBuilder msg = new StringBuilder();
                    foreach (ValidationEventArgs args in errors)
                    {
                        msg.AppendLine(args.Message);
                    }
                    throw new Exception(msg.ToString());
                }
                else
                {
                    return highlighter;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not load mode definition file", e);
            }
        }
    }
}
