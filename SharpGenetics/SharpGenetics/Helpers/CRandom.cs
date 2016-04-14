using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpGenetics.Logging
{
    [DataContractAttribute(IsReference=true)]
    public class CRandom
    {
        [DataMember]
        //Troschuetz.Random.Generators.MT19937Generator rand;
        Random rand;
        [DataMember]
        bool Log;
        [DataMember]
        int Seed;
        [DataMember]
        public int RunningTotal = 0;

        public CRandom(int Seed, bool Log = false)
        {
            this.Seed = Seed;

            //rand = new Troschuetz.Random.Generators.MT19937Generator(Seed);
            rand = new Random(Seed);

            this.Log = Log;
        }

        public CRandom()
        { }

        public int Next()
        {
            int _rand = 0;
            lock (rand)
            {
                _rand = rand.Next();
            }

            if(Log)
                Logger.Log(_rand);

            RunningTotal += _rand;

            return _rand;
        }

        public int Next(int MaxValue)
        {
            int _rand = 0;
            lock (rand)
            {
                _rand = rand.Next(MaxValue);
            }

            if(Log)
                Logger.Log(_rand + " " + MaxValue);

            RunningTotal += _rand;

            return _rand;
        }

        public int Next(int MinValue, int MaxValue)
        {
            int _rand = 0;
            lock(rand)
            {
                _rand = rand.Next(MinValue, MaxValue);
            }
            RunningTotal += _rand;
            return _rand;
        }

        public double NextDouble(double MinValue, double MaxValue)
        {
            double _rand = 0;
            lock (rand)
            {
                _rand = rand.NextDouble();
            }
            _rand *= MaxValue - MinValue;
            _rand += MinValue;
            //RunningTotal += _rand;
            return _rand;
        }
    }
}
