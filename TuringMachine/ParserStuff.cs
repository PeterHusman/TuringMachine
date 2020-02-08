using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TuringMachine
{
    partial class Program
    {
        static TransitionRuleTableTuringMachine<string, char> Parse(string s, char[] startingTapeValues)
        {
            var x = ParseParts(s);
            return new TransitionRuleTableTuringMachine<string, char>(x.rules, new Tape<char>(x.blank, startingTapeValues), x.startingState, 0);
        }

        static (Dictionary<(string, char), (TapeMoveDirection, string, char)> rules, char[] startingTape, char blank, string startingState) ParseParts(string s)
        {
            string[] lines = s.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string startingState = null;
            char blank = ' ';
            char[] input = null;
            Dictionary<string, char> charVars = new Dictionary<string, char>();
            Dictionary<(string, char), (TapeMoveDirection, string, char)> rules = new Dictionary<(string, char), (TapeMoveDirection, string, char)>();
            bool blankRead = false;
            int tableStart = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Split('#')[0];
                string l = lines[i].Trim();
                if (l.StartsWith("blank:"))
                {
                    string l2 = l.Remove(0, 6).Trim();
                    if (l2.StartsWith('\''))
                    {
                        blank = l2[1];
                    }
                    else
                    {
                        blank = charVars[l2];
                    }
                    blankRead = true;
                    continue;
                }

                if (l.StartsWith("input:"))
                {
                    string l2 = l.Remove(0, 6).Trim();
                    if (l2.StartsWith('\'') && l2.EndsWith('\''))
                    {
                        input = l2.Remove(0, 1).Remove(l2.Length - 2).ToCharArray();
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                    continue;
                }

                if (l.StartsWith("start state:"))
                {
                    string l2 = l.Remove(0, 12).Trim();
                    startingState = l2;
                    continue;
                }

                if (l == "table:")
                {
                    tableStart = i;
                    break;
                }
            }
            string[] table = lines.AsSpan().Slice(tableStart + 1).ToArray().Select(a => a.Split('#')[0].TrimEnd()).Where(a => a.Contains(':')).ToArray();
            string la = table[0];
            int indentLevel = la.Length - la.TrimStart().Length;
            string state = null;

            for (int i = 0; i < table.Length; i++)
            {
                string l = table[i].TrimStart();
                int indent = table[i].Length - l.Length;
                if (indent == indentLevel)
                {
                    state = Regex.Split(l, ":(?!')")[0];
                    //state = l.Split(':')[0];
                    continue;
                }
                List<char> triggerValues = new List<char>();
                int firstColon = Regex.Match(l, ":(?!')").Index;
                //int firstColon = l.IndexOf(':');
                string access = l.Remove(firstColon);
                string action = l.Remove(0, firstColon);

                TapeMoveDirection dir = TapeMoveDirection.None;
                string nextState = state;
                char? newTapeChar = null;


                if (access.StartsWith('['))
                {
                    string[] things = access.Remove(0, 1).Split(',').Select(a => a.Trim()).ToArray();
                    for (int j = 0; j < things.Length; j++)
                    {
                        string remaining = null;
                        if (things[j].StartsWith('\''))
                        {
                            triggerValues.Add(things[j][1]);
                            remaining = things[j].Remove(0, 3);

                        }
                        else
                        {
                            triggerValues.Add(things[j][0]);
                            remaining = things[j].Remove(0, 1);
                        }

                        if (remaining.Length > 0)
                        {
                            break;
                        }
                    }
                }
                else if (access.StartsWith('\''))
                {
                    triggerValues.Add(access[1]);
                }
                else
                {
                    triggerValues.Add(access[0]);
                }

                //string action = l.Split(':', 2)[1].Trim();
                //string action = access;
                string[] items = action.Split(new char[] { '{', '}', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Regex.Split(a, ":(?!')").Where(a => !string.IsNullOrEmpty(a))).SelectMany(a => a).Select(a => a.Trim()).Select(a => a.StartsWith('\'') ? ("" + a[1]) : a).ToArray();
                for (int j = 0; j < items.Length; j++)
                {
                    if (items[j] == "write")
                    {
                        newTapeChar = items[j + 1][0];
                        j++;
                        continue;
                    }

                    if (items[j] == "L")
                    {
                        dir = TapeMoveDirection.Left;
                    }
                    else if (items[j] == "R")
                    {
                        dir = TapeMoveDirection.Right;
                    }
                    else
                    {
                        nextState = items[j];
                    }
                }

                foreach (char c in triggerValues)
                {
                    if (newTapeChar == null)
                    {
                        rules.Add((state, c), (dir, nextState, c));
                        continue;
                    }

                    rules.Add((state, c), (dir, nextState, (char)newTapeChar));
                }
            }

            if (!blankRead)
            {
                throw new ArgumentException("Blank symbol must be specified.");
            }

            if (startingState == null)
            {
                throw new ArgumentException("Start state must be specified.");
            }

            if (input == null)
            {
                throw new ArgumentException("Input must be specified.");
            }

            return (rules, input, blank, startingState);
        }

        static TransitionRuleTableTuringMachine<string, char> Parse(string s)
        {
            var x = ParseParts(s);
            return new TransitionRuleTableTuringMachine<string, char>(x.rules, new Tape<char>(x.blank, x.startingTape), x.startingState, 0);
        }

    }
}
