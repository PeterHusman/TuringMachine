using System;
using System.Collections.Generic;
using System.Text;

namespace TuringMachine
{
    public interface ITuringMachine<TState, TTape> where TState : IEquatable<TState>
    {
        TState State { get; }
        ITape<TTape> Tape { get; }
        bool IsHalted { get; }
        bool InFinalState { get; }
        int Head { get; }
        bool Step();
    }
}
