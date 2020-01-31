using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TuringMachine
{
    public enum TapeMoveDirection
    {
        Left = -1,
        Right = 1,
        None = 0
    }

    public class TransitionRuleTableTuringMachine<TState, TTape> : ITuringMachine<TState, TTape> where TState : IEquatable<TState>
    {
        public TState State { get; private set; }

        public ITape<TTape> Tape { get; private set; }

        public int Head { get; private set; }

        public IReadOnlyDictionary<(TState state, TTape tape), (TapeMoveDirection movement, TState newState, TTape tapeSymbol)> Instructions { get; private set; }

        public TransitionRuleTableTuringMachine(IReadOnlyDictionary<(TState state, TTape tape), (TapeMoveDirection movement, TState newState, TTape tapeSymbol)> instructions, ITape<TTape> initialTape, TState initialState, int initialHead)
        {
            Instructions = instructions;
            Head = initialHead;
            Tape = initialTape;
            State = initialState;
        }

        public bool Step()
        {
            if (!Instructions.ContainsKey((State, Tape[Head])))
            {
                return false;
            }
            var x = Instructions[(State, Tape[Head])];
            State = x.newState;
            Tape[Head] = x.tapeSymbol;
            Head += (int)x.movement;
            return true;
        }

        public bool InFinalState => !Instructions.Keys.Any(a => State.Equals(a.Item1));

        public bool IsHalted => !Instructions.ContainsKey((State, Tape[Head]));
    }
}
