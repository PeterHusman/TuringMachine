using JavaScriptEngineSwitcher.V8;

using Microsoft.ClearScript.V8;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace TuringMachine
{
    partial class Program
    {
        static TransitionRuleTableTuringMachine<string, char> turingMachine;
        static Timer runTimer;
        static bool timerRunning = false;
        static int period = 400;

        static object printLock = new object();

        // Prompts the user to pick a file from a provided list.
        // Used to select Turing machines and UTM programs.
        // OR: returns null if none selected.
        static string PickFileFromList(string[] files)
        {
            // Prompt and display all files.
            Console.WriteLine("Please select one of the following files by number, or just press enter for none:");
            for (int i = 0; i < files.Length; i++)
            {
                // No need to show more than just the name.
                Console.WriteLine($"{i}:\t{Path.GetFileNameWithoutExtension(files[i])}");
            }
            int j = -1;
            // Keep asking until they give us something.
            while (j == -1)
            {
                string inp1 = Console.ReadLine();
                if (inp1 == "")
                {
                    return null;
                }
                if (!int.TryParse(inp1, out j) || j < 0 || j >= files.Length)
                {
                    Console.WriteLine("Please enter a valid index.");
                    j = -1;
                }
            }
            return files[j];
        }

        static bool Run()
        {
            
            // Get the TM to run.
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt", SearchOption.TopDirectoryOnly);
            string selectedFile = PickFileFromList(files);

            // Summarize instructions for giving tape input.
            Console.WriteLine($"{Path.GetFileNameWithoutExtension(selectedFile)} selected.\nPlease enter tape input, 'C' for until-empty-line, 'F' for from-file, or 'D' for default.");
            string inp = Console.ReadLine();
            
            if (inp == "D") // Read it from the configuration file.
            {
                turingMachine = Parse(File.ReadAllText(selectedFile));
            }

            else if (inp == "C") // Keep reading input until an empty line, designed for pasting.
            {
                string final = "";
                while (inp != "")
                {
                    inp = Console.ReadLine();
                    final += inp;
                }
                turingMachine = Parse(File.ReadAllText(selectedFile), final.ToCharArray());
            }

            else if (inp == "F") // Get a file!
            {
                // Offer all UTM programs
                string[] options = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "UTMPrograms"), "*.txt", SearchOption.TopDirectoryOnly);
                turingMachine = Parse(File.ReadAllText(selectedFile), File.ReadAllText(PickFileFromList(options)).ToCharArray());
            }
            else
            {
                turingMachine = Parse(File.ReadAllText(selectedFile), inp.ToCharArray());
            }
            lock (printLock)
            {
                RenderSetup();
                FastRender(turingMachine);
            }
            runTimer = new Timer(TimerUpdate);
            (int width, int height) = (Console.BufferWidth, Console.BufferHeight);
            RunMode runningMode = RunMode.OncePerPress;
            int tempHead = 0;
            string stateToJumpTo = null;
            bool nextCharClear = false;
            while (true)
            {
                while (!Console.KeyAvailable)
                {

                }
                bool advance = true;
                while (Console.KeyAvailable)
                {
                    lock (printLock)
                    {
                        if (nextCharClear)
                        {
                            RenderSetup();
                            FastRender(turingMachine);
                            nextCharClear = false;
                        }

                        ConsoleKeyInfo key = Console.ReadKey(true);
                        switch (key.Key)
                        {
                            case ConsoleKey.Enter:
                                advance = false;
                                Console.WriteLine();
                                stateToJumpTo = Console.ReadLine();
                                RenderSetup();
                                FastRender(turingMachine);
                                continue;
                            case ConsoleKey.U:
                                advance = false;
                                Console.Clear();
                                Console.WriteLine(UTMify(turingMachine));
                                nextCharClear = true;
                                continue;
                            case ConsoleKey.T:
                                advance = false;
                                RenderSetup();
                                FastRender(turingMachine);
                                Console.WriteLine();
                                Dictionary<char, int> counts = new Dictionary<char, int>();
                                foreach (char c in ((Tape<char>)turingMachine.Tape).Values.Values)
                                {
                                    if (counts.ContainsKey(c))
                                    {
                                        counts[c]++;
                                    }
                                    else
                                    {
                                        counts.Add(c, 1);
                                    }
                                }
                                Console.WriteLine();
                                Console.WriteLine("Tape makeup:");
                                foreach (char c in counts.Keys)
                                {
                                    Console.WriteLine($"{c}:\t{counts[c]}");
                                }
                                nextCharClear = true;
                                continue;
                            case ConsoleKey.D1:
                            case ConsoleKey.D2:
                            case ConsoleKey.D3:
                            case ConsoleKey.D4:
                                advance = false;
                                runningMode = (RunMode)(key.Key - ConsoleKey.D1);
                                if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
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

                    Update(runningMode, stateToJumpTo);

                    tempHead = turingMachine.Head;
                }
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
            while (Run())
            {
                runTimer.Change(Timeout.Infinite, Timeout.Infinite);
                timerRunning = false;
                Console.Clear();
            }
        }

        static void TimerUpdate(object state)
        {
            lock (printLock)
            {
                if (turingMachine.IsHalted)
                {
                    runTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    timerRunning = false;
                    return;
                }
                Update((RunMode)state);
            }
        }

        static void Update(RunMode mode = RunMode.OncePerPress, string stateToJumpTo = null)
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
                        while (turingMachine.State == state && !turingMachine.IsHalted)
                        {
                            turingMachine.Step();
                        }
                        break;
                    case RunMode.UntilDone:
                        while (!turingMachine.IsHalted)
                        {
                            turingMachine.Step();
                        }
                        break;
                    case RunMode.UntilSpecific:
                        while (turingMachine.State == stateToJumpTo)
                        {
                            turingMachine.Step();
                        }
                        while (turingMachine.State != stateToJumpTo && !turingMachine.IsHalted && stateToJumpTo != null)
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
            Console.SetCursorPosition(halfWidth - (tM.State.Length / 2) - 20, 2);
            Console.WriteLine($"                    {tM.State}                    ");
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

        static string UTMify(TransitionRuleTableTuringMachine<string, char> tM)
        {
            Tape<char> tape = tM.Tape as Tape<char>;
            //(char val, int pos)[] items = tape.Values.OrderBy(a => a.Key).Select(a => (a.Value, a.Key)).ToArray();
            int minPos = tape.Values.Min(a => a.Key);
            minPos = tM.Head < minPos ? tM.Head : minPos;
            int maxPos = tape.Values.Max(a => a.Key);
            maxPos = tM.Head > maxPos ? tM.Head : maxPos;
            Dictionary<string, int> stateInds = new Dictionary<string, int>();
            Dictionary<char, int> charInds = new Dictionary<char, int>();
            StringBuilder sB = new StringBuilder("ee ");

            int maxStateIndAdded = 1;
            int GetStateInd(string s)
            {
                if (stateInds.ContainsKey(s))
                {
                    return stateInds[s];
                }

                stateInds.Add(s, maxStateIndAdded);
                return maxStateIndAdded++;
            }

            int maxCharIndAdded = 1;
            int GetCharInd(char s)
            {
                if (s == tape.Blank)
                {
                    return 0;
                }
                if (charInds.ContainsKey(s))
                {
                    return charInds[s];
                }

                charInds.Add(s, maxCharIndAdded);
                return maxCharIndAdded++;
            }

            void AddState(string state)
            {
                sB.Append("D ");
                int n = GetStateInd(state);
                for (int i = 0; i < n; i++)
                {
                    sB.Append("A ");
                }
            }

            void AddChar(char c)
            {
                sB.Append("D ");
                int n = GetCharInd(c);
                for (int i = 0; i < n; i++)
                {
                    sB.Append("C ");
                }
            }

            foreach (var v in tM.Instructions)
            {
                sB.Append("; ");
                AddState(v.Key.state);
                AddChar(v.Key.tape);
                AddChar(v.Value.tapeSymbol);
                if (v.Value.movement == TapeMoveDirection.None)
                {

                }
                sB.Append(v.Value.movement == TapeMoveDirection.Right ? "R " : "L ");
                AddState(v.Value.newState);
            }
            sB.Append(":: ");
            sB.Append("D ");
            int n = GetStateInd(tM.State);
            for (int i = 0; i < n - 1; i++)
            {
                sB.Append("A ");
            }
            sB.Append("A");
            if (tape.Values.Count == 0)
            {
                sB.Append("hD");
                return sB.ToString();
            }

            for (int i = minPos; i <= maxPos; i++)
            {
                if (tM.Head == i)
                {
                    sB.Append("hD");
                }
                else
                {
                    sB.Append(" D");
                }

                int end = GetCharInd(tape[i]);
                for (int j = 0; j < end; j++)
                {
                    sB.Append(" C");
                }
            }

            return sB.ToString();
        }
    }

    public enum RunMode
    {
        OncePerPress = 0,
        UntilStateChange = 1,
        UntilDone = 2,
        UntilSpecific = 3
    }
}
