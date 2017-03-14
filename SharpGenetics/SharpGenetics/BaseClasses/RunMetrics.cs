﻿using GeneticAlgorithm.Helpers;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [ImplementPropertyChanged]
    [DataContract]
    public class MetricPoint
    {
        [DataMember]
        public int Generation { get; set; }
        [DataMember]
        public double Value { get; set; }

        public MetricPoint(int Gen, double Val)
        {
            Generation = Gen;
            Value = Val;
        }
    }

    [ImplementPropertyChanged]
    [DataContract]
    public class RunMetrics
    {
        [DataMember]
        public AsyncObservableCollection<MetricPoint> AverageFitnesses { get; set; }
        [DataMember]
        public AsyncObservableCollection<MetricPoint> BestFitnesses { get; set; }
        [DataMember]
        public AsyncObservableCollection<MetricPoint> ThirdQuartileOfFitnesses { get; set; }
        [DataMember]
        public AsyncObservableCollection<MetricPoint> MedianOfFitnesses { get; set; }
        [DataMember]
        public AsyncObservableCollection<MetricPoint> FitnessCalculations { get; set; }

        private static object FitLock = new object();

        public RunMetrics()
        {
            AverageFitnesses = new AsyncObservableCollection<MetricPoint>();
            BestFitnesses = new AsyncObservableCollection<MetricPoint>();
            ThirdQuartileOfFitnesses = new AsyncObservableCollection<MetricPoint>();
            MedianOfFitnesses = new AsyncObservableCollection<MetricPoint>();
            FitnessCalculations = new AsyncObservableCollection<MetricPoint>();
    }

        public void AddFitnessCalculation(int Generation)
        {
            lock (FitLock)
            {
                while (Generation >= FitnessCalculations.Count)
                {
                    FitnessCalculations.Add(new MetricPoint(Generation, 0));
                }

                FitnessCalculations[Generation].Value++;
            }
        }

        public void AddGeneration(List<double> Values)
        {
            if (Values.Count == 0)
                return;
            var Sorted = GetSortedList(Values);
            int Gen = AverageFitnesses.Count;
            AverageFitnesses.Add(new MetricPoint(Gen, Sorted.Average()));
            BestFitnesses.Add(new MetricPoint(Gen, Sorted[0]));
            ThirdQuartileOfFitnesses.Add(new MetricPoint(Gen, GetThirdQuartile(Sorted)));
            MedianOfFitnesses.Add(new MetricPoint(Gen, GetMedian(Sorted)));
        }

        public static List<double> GetSortedList(List<double> Values)
        {
            var Sorted = new List<double>(Values);
            Sorted.Sort();
            return Sorted;
        }

        public static double GetThirdQuartile(List<double> Values)
        {
            Accord.DoubleRange range;
            Accord.Statistics.Measures.Quartiles(Values.ToArray(), out range, true);

            return range.Max;
        }

        public static double GetMedian(List<double> Values)
        {
            return Accord.Statistics.Measures.Median(Values.ToArray());
        }
    }
}
