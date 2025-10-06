using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace Publisher_Data_Operations.Extensions
{
    
    public class ParameterValidator
    {
        private Dictionary<string, string> _validParams;
        private XmlDocument _xmlDoc;
        public string LastError { get; private set; }

        public ParameterValidator(Dictionary<string,string> validParams)
        {
            _validParams = validParams;
            _xmlDoc = new XmlDocument();
        }

        /// <summary>
        /// Use XMLDocument load to check if the string is valid XML - Since the tested fields can contain non-XML content nulls and blank are returned as true, as well as strings that do not contain tags after replacement
        /// </summary>
        /// <param name="xmlStr"></param>
        /// <returns></returns>
        public bool IsValidXML(string xmlStr, bool cleanup = true)
        {
            string temp = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(xmlStr))
                {
                    temp = xmlStr;
                    if (cleanup)
                    {
                        temp = xmlStr.ReplaceByDictionary(_validParams);
                        if (!temp.ContainsXML()) // short circuit when no XML left after removal of replacement tags (invalid XML)
                            return true;

                        temp = AddRoot(temp);
                        //temp = temp.ExcelTextClean();
                        //temp = temp.ReplaceCI("&nbsp;", "&#160;"); // replace nbsp; with it's equivalent but valid numeric entity reference
                        //temp = temp.ReplaceCI("&", "&amp;");
                        temp = temp.ReplaceCI("<br>", ""); // remove <br> tags which are invalid in the simplified XML being tested; //"\n"
                        //temp = temp.ReplaceCI("<br/>", ""); // remove <br/> tags which are invalid in the simplified XML being tested; //"\n"
                    }
                    if (temp.ContainsXML())
                        _xmlDoc.LoadXml(temp);
                } 
            }
            catch (XmlException e)
            {
                LastError = e.Message + AddPositionError(e.Message, temp); 
                return false;
            }
            return true;
        }

        public static string IsValidXMLStatic(string xmlStr, bool addRoot = true)
        {
            try
            {
                if (xmlStr.ContainsXML())
                {
                    if (addRoot)
                        xmlStr = AddRoot(xmlStr);
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlStr);
                }
            }
            catch (XmlException e)
            {
                return e.Message;// + AddPositionError(e.Message, xmlStr);
            }
            return true.ToString();
        }

        /// <summary>
        /// Returns the text around the position of the error
        /// </summary>
        /// <param name="errorMessage">The error message text containing Line and position values</param>
        /// <param name="xml">the xml text with the error</param>
        /// <returns>up to a 40 character string from around the error position</returns>
        private static string AddPositionError(string errorMessage, string xml)
        {
            if (errorMessage.Contains("Line"))
            {
                Match m = Regex.Match(errorMessage, @"Line (\d+), position (\d+)\.");
                if (m.Success && m.Groups.Count == 3)
                {
                    if (int.TryParse(m.Groups[1].Value, out int l) && int.TryParse(m.Groups[2].Value, out int p))
                    {
                        string[] lines = xml.Split('\n');
                        if (lines.Length >= l)
                        {
                            int start = Math.Max(0, p - 20);
                            int length = Math.Min(lines[l - 1].Length-start, 40);
                            return $" Check text: '{lines[l - 1].Substring(start, length)}'";
                        }
                    }    
                }
            }
            return string.Empty;
        }

        private static string AddRoot(string testString)
        {
            //if (testString.IndexOf("<row>", StringComparison.OrdinalIgnoreCase) == 0 && testString.CountOccurances("<row>") > 1) // add a temp root for more than one row
            //    return $"<root>{testString}</root>";
            //else if (testString.IndexOf("<p>", StringComparison.OrdinalIgnoreCase) == 0 && (testString.CountOccurances("<p>") > 1 || testString.CountOccurances("<ul>") >1)) // add a temp root for paragraph strings with more than one paragraph or ul
            //    return $"<root>{testString}</root>";
            //else if (testString.IndexOf("<ul>", StringComparison.OrdinalIgnoreCase) == 0)
            //    return $"<root>{testString}</root>";
            //return testString;

            return $"<XmlTestRoot>\n{testString}\n</XmlTestRoot>"; // the fancy checking was failing on edge case - safer to always add a root as publisher will do it's own fixes for valid HTML but not valid XML and the extra root element won't change valid or invalid XML EXCEPT for the cases we want to cover where there are multiple root elements - added newlines so that the fakeroot won't be displayed in any error messages
        }

        /// <summary>
        /// Using the constants exposed by Flags check that the string is a valid scenario
        /// </summary>
        /// <param name="scenario">The string to validate</param>
        /// <returns>true if all tests pass otherwise false</returns>
        public bool IsValidScenario(string scenario)
        {
            if (scenario.IndexOf("default", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (scenario.IndexOf(Generic.TABLEDELIMITER) >= 0)
            {
                LastError = $"You are not allowed to use {Generic.TABLEDELIMITER} as part of a Scenario";
                return false;
            }
            int valStart, valEnd;
            MatchCollection matches = Regex.Matches(scenario, Flags.ScenarioMatch);
            
            if (matches.Count < 1)
            {
                LastError = "Error in scenario format - not default and no scenario found";
                return false;
            }
            MatchCollection matchSeperator = Regex.Matches(scenario, string.Format(@"({0}[\s]*{1})", Regex.Escape(Flags.ValEnd), Regex.Escape(Flags.FlagSeparator)));
            if (matches.Count - 1 != matchSeperator.Count)
            {
                LastError = "Missing flag separator in scenario";
                return false;
            }
                
            foreach (Match m in matches) // scenario.Split(Flags.FlagSeperator.ToCharArray() - Split the scenario by its separate flags using the FlagSeperator
            {
                string s = m.Value;
                if (s.IndexOf("default", StringComparison.OrdinalIgnoreCase) < 0) // default scenario is allowed
                {
                    valStart = s.IndexOf(Flags.ValStart);
                    valEnd = s.IndexOf(Flags.ValEnd);
                    string temp = s;
                    if (valStart >= 0 && valEnd >= 0)   // Check that there is at least one value start indicator and value end indicator
                    {
                        temp = s.Substring(valStart + 1, valEnd - valStart-1);
                        foreach (string val in temp.Split(Flags.ValSeperator.ToCharArray())) // Check that each value is not empty or blank
                        {
                            if (val is null || val.Trim() == string.Empty)
                            {
                                LastError = "Empty or Null in scenario value";
                                return false;
                            }
                        }
                        temp = s.Remove(valStart, valEnd - valStart + 1);
                    }
                    else
                    {
                        LastError = "Missing start or end value markers";
                        return false;
                    }

                    temp = temp.ReplaceByDictionary(_validParams, false); // Check that the string is empty after removing the values and all recognized flags
                    if (temp.Trim().Length > 0)
                    {
                        LastError = "Unrecognized value encountered: " + temp;
                        return false;
                    }
                }
            }
            return true;
        }
    }
}

