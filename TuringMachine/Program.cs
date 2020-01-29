using JavaScriptEngineSwitcher.V8;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TuringMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            //V8ScriptEngine v8 = new V8ScriptEngine();
            //V8JsEngine jsEngine = new V8JsEngine();
            //v8.
            //var instrTable = JsonConvert.DeserializeObject<Dictionary<(char, char), (TapeMoveDirection, char, char)>>("");
            var instrTable = new Dictionary<(char, char), (TapeMoveDirection, char, char)>
            {
                [('1', '0')] = (TapeMoveDirection.Right, '1', '1'),
                [('1', 'c')] = (TapeMoveDirection.None, '2', '1')
            };
            InstructionTableTuringMachine<char, char> turingMachine = new InstructionTableTuringMachine<char, char>(instrTable, new Tape<char>(' ', "0000c000".ToCharArray()), '1', 0);
            while(true)
            {
                while(!Console.KeyAvailable)
                {

                }
                Console.ReadKey(true);
                Render(turingMachine);
                turingMachine.Step();
            }
        }

        static void Render(InstructionTableTuringMachine<char, char> tM)
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
        }
    }
}
