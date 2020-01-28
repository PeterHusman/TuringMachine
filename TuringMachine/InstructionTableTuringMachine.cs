using System;
using System.Collections.Generic;
using System.Text;

namespace TuringMachine
{
    public enum TapeMoveDirection
    {
        Left = -1,
        Right = 1,
        None = 0
    }

    public class InstructionTableTuringMachine<TState, TTape> : ITuringMachine<TState, TTape>
    {
        public TState State { get; private set; }

        public Tape<TTape> Tape { get; private set; }

        public int Head { get; private set; }

        public IReadOnlyDictionary<(TState, TTape), (TapeMoveDirection movement, TState newState, (bool act, TTape val) tapeSymbol)> Instructions { get; private set; }

        public InstructionTableTuringMachine(IReadOnlyDictionary<(TState, TTape), (TapeMoveDirection movement, TState newState, (bool act, TTape val) tapeSymbol)> instructions, TTape[] initialTape, int initialHead)
        {
            Instructions = instructions;
            Head = initialHead;
            Tape = new Tape<TTape>(initialTape);
        }

        public bool Step()
        {
            if(!Instructions.ContainsKey((State, Tape[Head])))
            {
                return false;
            }
            var x = Instructions[(State, Tape[Head])];
            State = x.newState;
            if(x.tapeSymbol.act)
            {
                Tape[Head] = x.tapeSymbol.val;
            }
            Head += (int)x.movement;
            return true;
        }
    }
}
