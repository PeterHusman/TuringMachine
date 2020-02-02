using JavaScriptEngineSwitcher.V8;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace TuringMachine
{
    partial class Program
    {
        static (string state, char tape) oldStatus = ("", ' ');
        static TransitionRuleTableTuringMachine<string, char> turingMachine;
        static Timer runTimer;

        
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
            turingMachine = Parse(File.ReadAllText("DivisibleByThree.txt"), "110".ToCharArray());//, "111".ToCharArray());
            RenderSetup();
            FastRender(turingMachine);
            runTimer = new Timer(TimerUpdate);
            (int width, int height) = (Console.BufferWidth, Console.BufferHeight);
            RunMode runningMode = RunMode.OncePerPress;
            while (true)
            {
                while (!Console.KeyAvailable)
                {

                }
                bool advance = true;
                while (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch(key.Key)
                    {
                        case ConsoleKey.D1:
                        case ConsoleKey.D2:
                        case ConsoleKey.D3:
                        case ConsoleKey.D4:
                            advance = false;
                            runningMode = (RunMode)(key.Key - ConsoleKey.D1);
                            break;
                        default:
                            advance = true;
                            break;
                    }
                }
                if(!advance)
                {
                    continue;
                }
                //Render(turingMachine);
                if ((width, height) != (Console.BufferWidth, Console.BufferHeight))
                {
                    (width, height) = (Console.BufferWidth, Console.BufferHeight);
                    RenderSetup();
                }
                Update(runningMode);
             }
        }

        static void TimerUpdate(object state)
        {
            if(turingMachine.IsHalted)
            {
                runTimer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }
            turingMachine.Step();
            //FastRender
        }

        static void Update(RunMode mode = RunMode.OncePerPress)
        {
            //oldStatus.state = turingMachine.State;
            //oldStatus.tape = turingMachine.Tape[turingMachine.Head];
            switch (mode)
            {
                case RunMode.OncePerPress:
                    turingMachine.Step();
                    break;
                case RunMode.UntilStateChange:
                    string state = turingMachine.State;
                    while(turingMachine.State == state && !turingMachine.IsHalted)
                    {
                        turingMachine.Step();
                    }
                    break;
                case RunMode.UntilDone:
                    while(!turingMachine.IsHalted)
                    {
                        turingMachine.Step();
                    }
                    break;
                case RunMode.OnTimer:
                    throw new NotImplementedException();
            }
            
            FastRender(turingMachine);//, oldStatus.state, oldStatus.tape);
            //turingMachine.Step();
        }

        static void RenderSetup()
        {
            Console.Clear();
            Console.WriteLine($"Halted:\nFinal State:");
            int halfWidth = Console.BufferWidth / 2;
            Console.SetCursorPosition(halfWidth, 3);
            Console.WriteLine("v");
            Console.SetCursorPosition(0, 8);
            Console.Write("Nonblank cell count:");
        }

        static void FastRender(TransitionRuleTableTuringMachine<string, char> tM)//, string oldS, char oldT)
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
            /*foreach (var key in tM.Instructions.Keys)
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
            Console.ForegroundColor = ConsoleColor.White;*/
            if (tM.Instructions.ContainsKey((tM.State, tM.Tape[tM.Head])))
            {
                var v = tM.Instructions[(tM.State, tM.Tape[tM.Head])];
                Console.WriteLine($"({tM.State}, {tM.Tape[tM.Head]})   ->   {v.movement}, {v.newState}, {v.tapeSymbol}                                        ");
            }

            Console.SetCursorPosition(25, 8);
            Console.Write($"{((Tape<char>)tM.Tape).Values.Count}                        ");
        }

        static void Render(TransitionRuleTableTuringMachine<char, char> tM)
        {
            Console.Clear();
            Console.WriteLine($"Halted:\t{tM.IsHalted}\nFinal State:\t{tM.InFinalState}\n\n");
            int halfWidth = Console.BufferWidth / 2;
            Console.CursorLeft = halfWidth;
            Console.WriteLine(tM.Tape[tM.Head]);
            for (int i = tM.Head - halfWidth; i < tM.Head + halfWidth; i++)
            {
                Console.Write(tM.Tape[i]);
            }
            Console.WriteLine();
            Console.WriteLine();
            foreach (var key in tM.Instructions.Keys)
            {
                Console.WriteLine($"({key.state}, {key.tape})\t->\t{tM.Instructions[key].movement}, {tM.Instructions[key].newState}, {tM.Instructions[key].tapeSymbol}");
            }
        }
    }

    public enum RunMode
    {
        OncePerPress = 0,
        UntilStateChange = 1,
        UntilDone = 2,
        OnTimer = 3
    }
}
