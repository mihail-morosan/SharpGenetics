using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BlindImmobilePeople.BlindImmobilePeople
{
    [DataContractAttribute]
    public class BDState
    {
        [DataMember]
        public Point ImmobileLocation;
        [DataMember]
        public Point BlindLocation;

        [DataMember]
        public Dictionary<string, int> ImmobileFlags;
        [DataMember]
        public Dictionary<string, int> BlindFlags;

        [DataMember]
        public int LastMessageD = 0;
        [DataMember]
        public int LastMessageB = 0;

        [DataMember]
        public int EnergyLeft = 100;

        [DataMember]
        public int EnergyUsageMove = 1;
        [DataMember]
        public int EnergyUsageMessage = 1;

        [DataMember]
        public bool CurrentIsBlind = true;

        public BDState()
        {
            ImmobileFlags = new Dictionary<string, int>();
            BlindFlags = new Dictionary<string, int>();
        }

        public double GetDistanceNonSqrt()
        {
            return Math.Pow(ImmobileLocation.X - BlindLocation.X, 2) + Math.Pow(ImmobileLocation.Y - BlindLocation.Y, 2);
        }

        public double GetDistance()
        {
            return Math.Sqrt(Math.Pow(ImmobileLocation.X - BlindLocation.X, 2) + Math.Pow(ImmobileLocation.Y - BlindLocation.Y, 2));
        }

        public bool IsOver()
        {
            return EnergyLeft <= 0 || GetDistanceNonSqrt() == 0;
        }
    }
}
