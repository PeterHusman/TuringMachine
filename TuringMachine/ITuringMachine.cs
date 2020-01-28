using System;
using System.Collections.Generic;
using System.Text;

namespace TuringMachine
{
    public interface ITuringMachine<TState, TTape>
    {
        TState State { get; }
        Tape<TTape> Tape { get; }
        int Head { get; }
        bool Step();
    }
}
