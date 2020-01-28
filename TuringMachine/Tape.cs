using System;
using System.Collections.Generic;
using System.Text;

namespace TuringMachine
{
    public class Tape<T>
    {
        private Dictionary<int, T> tapeValues { get; set; }

        public IReadOnlyDictionary<int, T> Values => tapeValues;
        
        public readonly T Blank;

        public Tape(T blank)
        {
            tapeValues = new Dictionary<int, T>();
            Blank = blank;
        }

        public Tape() : this(default(T))
        {

        }

        public Tape(T[] initial) : this(default(T), initial)
        {

        }

        public Tape(T blank, T[] initial)
        {
            Blank = blank;
            for(int i = 0; i < initial.Length; i++)
            {
                this[i] = initial[i];
            }
        }

        public T this[int pos]
        {
            get
            {
                if(!tapeValues.ContainsKey(pos))
                {
                    tapeValues.Add(pos, Blank);
                }
                return tapeValues[pos];
            }

            set
            {
                if(!tapeValues.ContainsKey(pos))
                {
                    tapeValues.Add(pos, value);
                    return;
                }

                tapeValues[pos] = value;
            }
        }
    }
}
