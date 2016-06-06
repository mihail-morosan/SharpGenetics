﻿using SharpGenetics.BaseClasses;
using SharpGenetics.Logging;
using SharpGenetics.SelectionAlgorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.SelectionAlgorithms
{
    [DataContract]
    public class TournamentSelection : SelectionAlgorithm
    {
        [DataMember]
        public int TournamentSize = 1;

        public TournamentSelection(int TournamentSize = 1)
        {
            this.TournamentSize = TournamentSize;
        }

        public override T Select<T, InputT, OutputT>(PopulationManager<T, InputT, OutputT> Manager, List<T> Population)
        {
            if (TournamentSize < 1 || Population.Count < 1)
                return default(T);
            List<T> Tournament = new List<T>();
            for (int i = 0; i < TournamentSize; i++)
            {
                Tournament.Add(Population.ElementAt(Manager.rand.Next(Population.Count)));
            }
            T BestMember = Tournament[0];
            for (int i = 1; i < TournamentSize; i++)
            {
                //double Fit = Tournament[i].CalculateFitness(Manager.GenerationsRun, Manager.GetTests().ToArray());
                if (Tournament[i].CalculateFitness(Manager.GenerationsRun, Manager.GetTests().ToArray()) < BestMember.CalculateFitness(Manager.GenerationsRun, Manager.GetTests().ToArray()))
                {
                    BestMember = Tournament[i];
                }
            }

            return BestMember;
        }
    }
}
