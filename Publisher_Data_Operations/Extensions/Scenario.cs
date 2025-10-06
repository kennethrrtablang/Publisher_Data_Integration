using Publisher_Data_Operations.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Publisher_Data_Operations.Extensions
{
    /// <summary>
    /// Represents a single flag and it's list of values
    /// </summary>
    public class Flag
    {
        public string FieldName { get; set; }
        public List<string> Values { get; set; }

        public Flag(string fieldName, List<string> values)
        {
            FieldName = fieldName;
            Values = values;
        }
    }

    /// <summary>
    /// Flags are equal if their fieldnames are the same
    /// </summary>
    class FlagEqualityComparer : IEqualityComparer<Flag>
    {
        public bool Equals(Flag x, Flag y)
        {
            // Two items are equal if their keys are equal.
            return x.FieldName == y.FieldName;
        }

        public int GetHashCode(Flag obj)
        {
            return obj.FieldName.GetHashCode();
        }
    }

    /// <summary>
    /// A list of flag to be collected into a scenario
    /// </summary>
    public class Flags : List<Flag>
    {
        public const string ValStart = "{";
        public const string ValEnd = "}";
        public const string ValSeperator = ",";
        public const string FlagSeparator = "&";
        public static string ScenarioMatch = string.Format(@"(?<field>\w+)(?:\s*{0})(?<values>[^{1}]+{1})", Regex.Escape(ValStart), Regex.Escape(ValEnd));
        public Flags()
        {
        }

        public Flags(string scenarioText)
        {
            this.ParseScenario(scenarioText);  
        }

        /// <summary>
        /// Given a string containing flags and values parse it into the list as flag  objects
        /// </summary>
        /// <param name="scenarioText">The scenario string</param>
        public void ParseScenario(string scenarioText)
        {
            // start by using regex to grab all the field names and their associated csv value lists
            MatchCollection matches = Regex.Matches(scenarioText, ScenarioMatch);
            MatchCollection values;
            if (matches.Count == 0)
                this.Add(new Flag(scenarioText, null));
            else
            {
                foreach (Match match in matches)
                {
                    values = Regex.Matches(match.Groups["values"].Value, string.Format(@"(?<value>[^{0}]+)", Regex.Escape(ValSeperator + ValStart + ValEnd))); //,|\{ } \\+ FlagSeperator
                    List<string> valueList = new List<string>();
                    foreach (Match value in values)
                        valueList.Add(value.Groups["value"].Value.Trim());

                    this.Add(new Flag(match.Groups["field"].Value.Trim(), valueList));
                }
            }
        }

        /// <summary>
        /// If the flaglist contains only "default" return true
        /// </summary>
        /// <returns>A boolean indicating if the </returns>
        public bool IsDefault()
        {
            return this.ToString().IndexOf("default", StringComparison.OrdinalIgnoreCase) >= 0;
        }


        /// <summary>
        /// Return only distinct FieldNames from the contained flags
        /// </summary>
        /// <returns>The distinct list of fieldnames as string</returns>
        public List<string> DistinctFieldNames()
        {
            if (!IsDefault())
                return this.Select(e => e.FieldName).Distinct().ToList();
            else
                return new List<string>();
        }

        public Flag Find(string fieldName)
        {
            return this.Find(x => x.FieldName == fieldName);
        }
        /// <summary>
        /// Output the string representation of the list of flags
        /// </summary>
        /// <returns>The flags as a string</returns>
        public override string ToString()
        {
            
            StringBuilder sb = new StringBuilder("");
            foreach (Flag s in this)
            {
                if (!(s.Values is null) && s.Values.Count > 0)
                {
                    sb.Append(s.FieldName + ValStart);
                    sb.Append(string.Join(ValSeperator, s.Values) + ValEnd + FlagSeparator);
                }
                else
                    sb.Append(s.FieldName + FlagSeparator);
                
            }
            sb.Remove(sb.Length - FlagSeparator.Length, FlagSeparator.Length);
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// Contains the scenario values of the converted flag list as well as the English and French values
    /// TODO: Combine with Flags list?
    /// </summary>
    public class Scenario
    {
        public string EnglishText { get; set; }
        public string FrenchText { get; set; }
        public DateTime LastUpdated { get; set; }
        public Flags FlagList { get; set; }
        public Scenario()
        {
        }

        public Scenario(string scenarioText, string english, string french, DateTime lastUpdated)
        {
            EnglishText = english;
            FrenchText = french;
            FlagList = new Flags(scenarioText);
            LastUpdated = lastUpdated;
        }

        /// <summary>
        /// Determine if the fields provided by the dictionary match any of the values for each field name
        /// </summary>
        /// <param name="docFields">The dictionary of fields, values to compare against</param>
        /// <returns></returns>
        internal bool MatchFields(Dictionary<string, string> docFields)
        {
            if (FlagList.IsDefault())
                return true;

            if (docFields is null || docFields.Count == 0)
                return false;

            string val = string.Empty;
            foreach (Flag f in FlagList)
            {
                if (!docFields.Keys.Contains(f.FieldName)) // check that the flag we're looking for exists
                {
                    Logger.AddError(null, $"Scenario Error: Could not find key {f.FieldName} in the provided document fields");
                    continue;
                }
                    
                val = docFields[f.FieldName];
                if (!f.Values.Any(item => item.ToUpper() == val.ToUpper() || (item.IsNaOrBlank() && val.IsNaOrBlank()) || (item.IsNotNaOrBlank() && !val.IsNaOrBlank()))) // N/A, blank and nulls are also equal - 20220307 add !N/A match
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Make sure when ranking that the default value is last
        /// </summary>
        /// <returns></returns>
        public int RankCount()
        {
            if (!this.FlagList.IsDefault())
                return FlagList.Count();
            else
                return -1;
        }

        /// <summary>
        /// Custom CompareTo to order by RankCount and then LastUpdated
        /// </summary>
        /// <param name="other">The Scenario to compare to</param>
        /// <returns>0 if even, -1 if less, 1 if greater</returns>
        public int CompareTo(Scenario other)
        {
            // if the RankCount is the same sort by the LastUpdated date
            if (other != null && this.RankCount() == other.RankCount())
                return this.LastUpdated.CompareTo(other.LastUpdated);
            else
                return this.RankCount().CompareTo(other.RankCount());
        }
    }

    /// <summary>
    /// The collection of all active Scenarios
    /// </summary>
    public class Scenarios : List<Scenario>
    {
        /// <summary>
        /// Re-order the list based on the RankCount value from highest to lowest
        /// </summary>
        public void RankOrder() => this.Sort((x, y) => y.CompareTo(x));

        /// <summary>
        /// Return a distinct list of FieldNames from all contained scenario flags
        /// </summary>
        /// <returns>The distinct list of FieldNames </returns>
        public List<string> AllDistinctFieldNames(Dictionary<string, string> stageFields = null)
        {

            //TODO: Find a more efficient method of retrieving the unique field list
            List<string> allDistinct = new List<string>();
            foreach (Scenario sc in this)
            {
                foreach (string f in sc.FlagList.DistinctFieldNames())
                {
                    if (!allDistinct.Any(item => item == f) && (stageFields is null || !stageFields.ContainsKey(f))) // don't add any of the stage supplied fields
                        allDistinct.Add(f);
                }
            }
            return allDistinct;
        }
    }
}
