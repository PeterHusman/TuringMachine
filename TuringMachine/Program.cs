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
        static TransitionRuleTableTuringMachine<string, char> turingMachine;
        static Timer runTimer;
        static bool timerRunning = false;
        static int period = 400;


        static bool Run()
        {
            Console.WriteLine("Please select one of the following files by number:");
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt");
            for (int i = 0; i < files.Length; i++)
            {
                Console.WriteLine($"{i}:\t{Path.GetFileNameWithoutExtension(files[i])}");
            }
            int j = -1;
            while (j == -1)
            {
                string inp1 = Console.ReadLine();
                if(inp1 == "")
                {
                    return false;
                }
                if (!int.TryParse(inp1, out j) || j < 0 || j >= files.Length)
                {
                    Console.WriteLine("Please enter a valid index.");
                    j = -1;
                }
            }
            string selectedFile = files[j];

            Console.WriteLine($"{Path.GetFileNameWithoutExtension(files[j])} selected.\nPlease enter tape input or 'D' for default.");
            string inp = Console.ReadLine();
            if (inp == "D")
            {
                turingMachine = Parse(File.ReadAllText(selectedFile));
            }
            else
            {
                turingMachine = Parse(File.ReadAllText(selectedFile), inp.ToCharArray());
            }
            RenderSetup();
            FastRender(turingMachine);
            runTimer = new Timer(TimerUpdate);
            (int width, int height) = (Console.BufferWidth, Console.BufferHeight);
            RunMode runningMode = RunMode.OncePerPress;
            int tempHead = 0;
            while (true)
            {
                while (!Console.KeyAvailable)
                {

                }
                bool advance = true;
                while (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.D1:
                        case ConsoleKey.D2:
                        case ConsoleKey.D3:
                            advance = false;
                            runningMode = (RunMode)(key.Key - ConsoleKey.D1);
                            if(key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                            {
                                period = 400;
                                runTimer.Dispose();
                                runTimer = new Timer(TimerUpdate, runningMode, 400, 400);
                                timerRunning = true;
                            }
                            else
                            {
                                runTimer.Change(Timeout.Infinite, Timeout.Infinite);
                                timerRunning = false;
                            }
                            break;
                        case ConsoleKey.OemPlus:
                            period = period <= 1 ? 1 : (period / 2);
                            if (timerRunning)
                            {
                                runTimer.Change(period, period);
                            }
                            advance = !timerRunning;
                            break;
                        case ConsoleKey.OemMinus:
                            period *= 2;
                            if (timerRunning)
                            {
                                runTimer.Change(period, period);
                            }
                            advance = !timerRunning;
                            break;
                        case ConsoleKey.LeftArrow:
                            tempHead--;
                            advance = false;
                            RenderTapeFromArbitrary(turingMachine, tempHead);
                            continue;
                        case ConsoleKey.RightArrow:
                            tempHead++;
                            advance = false;
                            RenderTapeFromArbitrary(turingMachine, tempHead);
                            continue;
                        case ConsoleKey.Escape:
                            return true;
                        default:
                            tempHead = turingMachine.Head;
                            advance = true;
                            break;
                    }
                }
                if (!advance)
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
                tempHead = turingMachine.Head;
            }
        }

        static void RenderTapeFromArbitrary(TransitionRuleTableTuringMachine<string, char> tM, int center)
        {
            int halfWidth = Console.BufferWidth / 2;
            Console.SetCursorPosition(0, 4);
            for (int i = center - halfWidth; i < center + halfWidth; i++)
            {
                Console.Write(tM.Tape[i]);
            }
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
            while(Run())
            {
                runTimer.Change(Timeout.Infinite, Timeout.Infinite);
                timerRunning = false;
                Console.Clear();
            }
        }

        static void TimerUpdate(object state)
        {
            if(turingMachine.IsHalted)
            {
                runTimer.Change(Timeout.Infinite, Timeout.Infinite);
                timerRunning = false;
                return;
            }
            Update((RunMode)state);
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
        UntilDone = 2
    }
}
