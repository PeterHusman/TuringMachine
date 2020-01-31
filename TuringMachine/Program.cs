using JavaScriptEngineSwitcher.V8;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace TuringMachine
{
    class Program
    {
        static (string state, char tape) oldStatus = ("", ' ');
        static TransitionRuleTableTuringMachine<string, char> turingMachine;

        static TransitionRuleTableTuringMachine<string, char> Parse(string s)
        {
            string[] lines = s.Split('\n',StringSplitOptions.RemoveEmptyEntries);
            string startingState = null;
            char blank = ' ';
            char[] input = null;
            Dictionary<string, char> charVars = new Dictionary<string, char>();
            Dictionary<(string, char), (TapeMoveDirection, string, char)> rules = new Dictionary<(string, char), (TapeMoveDirection, string, char)>();
            bool blankRead = false;
            int tableStart = 0;
            for(int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Split('#')[0];
                string l = lines[i].Trim();
                if(l.StartsWith("blank:"))
                {
                    string l2 = l.Remove(0, 6).Trim();
                    if(l2.StartsWith('\''))
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

                if(l.StartsWith("input:"))
                {
                    string l2 = l.Remove(0, 6).Trim();
                    if(l2.StartsWith('\'') && l2.EndsWith('\''))
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

            for(int i = tableStart + 1; i < lines.Length; i++)
            {
                lines[i] = lines[i].Split('#')[0];
                string l = lines[i].Trim();
            }

            if(!blankRead)
            {
                throw new ArgumentException("Blank symbol must be specified.");
            }

            if(startingState == null)
            {
                throw new ArgumentException("Start state must be specified.");
            }

            if (input == null)
            {
                throw new ArgumentException("Input must be specified.");
            }


            return new TransitionRuleTableTuringMachine<string, char>(rules, new Tape<char>(blank, input), startingState, 0);
        }

        static void Main(string[] args)
        {
            //V8ScriptEngine v8 = new V8ScriptEngine();
            //V8JsEngine jsEngine = new V8JsEngine();
            //v8.
            //var instrTable = JsonConvert.DeserializeObject<Dictionary<(char, char), (TapeMoveDirection, char, char)>>("");
            /*var instrTable = new Dictionary<(string, char), (TapeMoveDirection, string, char)>
            {
                [("left", '1')] = (TapeMoveDirection.Left, "left", '1'),
                [("left", ' ')] = (TapeMoveDirection.Right, "right1", ' '),
                [("right1", '1')] = (TapeMoveDirection.Right, "right2", ' '),
                [("right1", '+')] = (TapeMoveDirection.Right, "done", ' '),
                [("right2", '1')] = (TapeMoveDirection.Right, "right2", '1'),
                [("right2", '+')] = (TapeMoveDirection.Right, "right2", '+'),
                [("right2", ' ')] = (TapeMoveDirection.Left, "left", '1'),
                [("left", '+')] = (TapeMoveDirection.Left, "left", '+')
            };
            turingMachine = new InstructionTableTuringMachine<string, char>(instrTable, new Tape<char>(' ', "1111+111".ToCharArray()), "left", 0);
            */
            turingMachine = Parse(File.ReadAllText("TransitionRules.txt"));
            RenderSetup();
            FastRender(turingMachine, " ", ' ');
            Timer t = new Timer(Update);
            t.Change(Timeout.Infinite, Timeout.Infinite);
            while(true)
            {
                while(!Console.KeyAvailable)
                {

                }
                Console.ReadKey(true);
                //Render(turingMachine);
                Update();
            }
        }

        static void Update(object state = null)
        {
            oldStatus.state = turingMachine.State;
            oldStatus.tape = turingMachine.Tape[turingMachine.Head];
            turingMachine.Step();
            FastRender(turingMachine, oldStatus.state, oldStatus.tape);
            turingMachine.Step();
        }

        static void RenderSetup()
        {
            Console.Clear();
            Console.WriteLine($"Halted:\nFinal State:");
            int halfWidth = Console.BufferWidth / 2;
            Console.SetCursorPosition(halfWidth, 3);
            Console.WriteLine("v");
        }

        static void FastRender(TransitionRuleTableTuringMachine<string, char> tM, string oldS, char oldT)
        {
            Console.SetCursorPosition(20, 0);
            Console.Write($"{tM.IsHalted}         ");
            Console.SetCursorPosition(20, 1);
            Console.Write($"{tM.InFinalState}         ");
            int halfWidth = Console.BufferWidth / 2;
            Console.SetCursorPosition(halfWidth - (tM.State.Length / 2) - 10, 2);
            Console.WriteLine($"          {tM.State}          ");
            Console.WriteLine();
            for (int i = tM.Head - halfWidth; i < tM.Head + halfWidth; i++)
            {
                Console.Write(tM.Tape[i]);
            }
            Console.SetCursorPosition(0, 6);
            foreach (var key in tM.Instructions.Keys)
            {
                if(key.state == oldS && key.tape == oldT)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine($"({key.state}, {key.tape})\t->\t{tM.Instructions[key].movement}, {tM.Instructions[key].newState}, {tM.Instructions[key].tapeSymbol}");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void Render(TransitionRuleTableTuringMachine<char, char> tM)
        {
            Console.Clear();
            Console.WriteLine($"Halted:\t{tM.IsHalted}\nFinal State:\t{tM.InFinalState}\n\n");
            int halfWidth = Console.BufferWidth / 2;
            Console.CursorLeft = halfWidth;
            Console.WriteLine(tM.Tape[tM.Head]);
            for(int i = tM.Head - halfWidth; i < tM.Head + halfWidth; i++)
            {
                Console.Write(tM.Tape[i]);
            }
            Console.WriteLine();
            Console.WriteLine();
            foreach(var key in tM.Instructions.Keys)
            {
                Console.WriteLine($"({key.state}, {key.tape})\t->\t{tM.Instructions[key].movement}, {tM.Instructions[key].newState}, {tM.Instructions[key].tapeSymbol}");
            }
        }
    }
}
