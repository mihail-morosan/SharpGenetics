using Newtonsoft.Json;
using PropertyChanged;
using SharpGenetics.Logging;
using SharpGenetics.Predictor;
using SharpGenetics.SelectionAlgorithms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;

namespace SharpGenetics.BaseClasses
{
    /// <summary>
    /// Generic manager for GP runs.
    /// </summary>
    /// <typeparam name="T">PopulationMember type to evolve</typeparam>
    /// <typeparam name="InputT">Input type it requires in tests</typeparam>
    /// <typeparam name="OutputT">Output type it requires in tests</typeparam>
    /// 
    [AddINotifyPropertyChangedInterface]
    [DataContractAttribute]
    [KnownType("GetKnownType")]
    public class GPRunManager<T> where T : PopulationMember
    {
        [DataMember]
        public RunParameters Parameters;

        [DataMember]
        private Stopwatch Timer;
        [DataMember]
        public CRandom mainRandom;
        [DataMember]
        public int CurrentGen { get; set; }
        [DataMember]
        public int RandomSeed = -1;

        [DataMember]
        public ObservableCollection<PopulationManager<T>> Populations { get; set; }

        [DataMember]
        private SelectionAlgorithm SelectionAlgorithm;

        public T BestIndividualSoFar
        {
            get
            {
                if (Populations.Count > 0 && CurrentGen > 0)
                {
                    return GetBestMembers()[0];
                }
                return null;
                //return new BalanceGA(null, new List<double>());
            }
        }

        private static Type[] GetKnownType()
        {
            //Type openGenericType = typeof(SelectionAlgorithm);
            //Type openGenericType2 = typeof(FitnessComparer);
            Type[] openGenericTypes = {
                typeof(SelectionAlgorithm)
                ,typeof(FitnessComparer)
                ,typeof(ResultPredictor)
            };
            var res = new List<Type>();

            var assembly = Assembly.GetExecutingAssembly();
            //foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var x in assembly.GetTypes())
                    {
                        var y = x.BaseType;

                        if (!x.IsAbstract && !x.IsInterface && y != null)
                        {
                            foreach(var T in openGenericTypes)
                            {
                                if(T.GUID == y.GUID || (x.GetInterfaces().Count() > 0 && T.GUID == x.GetInterfaces()[0].GUID))
                                {
                                    res.Add(x);
                                }
                            }
                        }
                    }
                } catch { }
            }

            return res.ToArray();
        }

        /// <summary>
        /// Default constructor for GPRunManager.
        /// </summary>
        /// <param name="Filename">File to load parameters from</param>
        /// <param name="Tests">Tests to be used for calculating fitness of population members</param>
        public GPRunManager(string Filename, int RandomSeed = -1) : this(LoadParamsFromFile(Filename), RandomSeed)
        {
        }

        [JsonConstructor]
        public GPRunManager(RunParameters Parameters, int RandomSeed = -1)
        {
            this.CurrentGen = 0;
            this.Populations = new ObservableCollection<PopulationManager<T>>();
            this.Parameters = Parameters;
            this.RandomSeed = RandomSeed;
            mainRandom = new CRandom(RandomSeed != -1 ? RandomSeed : Parameters.GetParameter<int>("Par_Seed", 1));

            var SelAlg = Parameters.GetParameter<string>("string_SelectionAlgorithm", "SharpGenetics.SelectionAlgorithms.TournamentSelection,SharpGenetics");
            /*if(SelAlg.Length < 1)
            {
                SelAlg = "SharpGenetics.SelectionAlgorithms.TournamentSelection,SharpGenetics";
            }*/
            
            SelectionAlgorithm = (SelectionAlgorithm)Activator.CreateInstance(Type.GetType(SelAlg), new object[] { this.Parameters, mainRandom.Next() });
        }

        /// <summary>
        /// Returns the number of generations that have been run so far. 
        /// </summary>
        /// <returns>Int</returns>
        public int GetGenerationsRun()
        {
            return CurrentGen;
        }

        /// <summary>
        /// Initializes the GPRunManager's populations. Automatically called by the constructor.
        /// </summary>
        public void InitRun()
        {
            Timer = Stopwatch.StartNew();

            for (int i = 0; i < Parameters.GetParameter<int>("Par_IslandClusters", 1); i++)
            {
                //RunParameters InstanceParams = Parameters.Clone();
                //InstanceParams.AddToParameters("Instance", i + 1);
                
                Populations.Add(new PopulationManager<T>(this, mainRandom.Next(), false, false));

                Populations[i].Instance = i + 1;
                //Populations[i].GenerateRandomMembers();
                //Populations[i].FinalizeGeneration();
                Populations[i].SelectionAlgorithm = SelectionAlgorithm;
            }

            Timer.Stop();
        }

        public void ReloadParameters()
        {
            if (Parameters.JsonParameters.Length > 0)
            {
                Parameters.JsonParams = JsonConvert.DeserializeObject(Parameters.JsonParameters);
            }

            foreach (var Pop in Populations)
            {
                Pop.ReloadParameters(this);
                for(int i=0;i<Pop.GetNumberOfIndividuals();i++)
                {
                    Pop.GetMember(i).ReloadParameters(Pop);
                }
            }
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            ReloadParameters();
        }

        /// <summary>
        /// Starts the run for a number of generations. -1 means it will run until it achieves a fitness of 0 or gets to MAXGENERATIONS. A value higher than 0 will mean it runs for
        /// GenerationsBeforePause generations before pausing 
        /// </summary>
        /// <param name="GenerationsBeforePause"></param>
        public int StartRun(int GenerationsBeforePause = -1)
        {
            //Resume the timer
            Timer.Start();

            int GenerationsThisSubRun = 0;

            bool Completed = false;
            int MaxTGenerations = Parameters.GetParameter<int>("Par_MaxGenerations", 100);

            if (GenerationsBeforePause <= 0)
            {
                GenerationsBeforePause = MaxTGenerations;
            }

            int RefreshGenCount = Parameters.GetParameter<int>("Par_GenCountBeforeRefresh", 0);

            ManualResetEvent[] doneEvents = new ManualResetEvent[Parameters.GetParameter<int>("Par_IslandClusters", 1)];

            int MaxGenerations = GenerationsBeforePause;
            
            while (CurrentGen < MaxTGenerations && !Completed && (GenerationsThisSubRun < GenerationsBeforePause))
            {
                int NumberOfRuns = 0;

                if (RefreshGenCount > 0)
                    NumberOfRuns = ((((int)(CurrentGen / RefreshGenCount) + 1) * RefreshGenCount) - CurrentGen);
                else
                    NumberOfRuns = GenerationsBeforePause;

                if (NumberOfRuns > MaxGenerations)
                {
                    NumberOfRuns = MaxGenerations;
                }

                int i = 0;
                //Create threads for evolving each population
                foreach (var pop in Populations)
                {
                    doneEvents[i] = new ManualResetEvent(false);

                    ThreadPool.QueueUserWorkItem((threadContext) =>
                    {
                        for (int Gen2 = 0; Gen2 < NumberOfRuns; Gen2++)
                        {
                            pop.GreedyKeep();

                            pop.RegenerateMembers(true);
                        }

                        if ((GenerationsThisSubRun + NumberOfRuns >= GenerationsBeforePause) || ((CurrentGen + NumberOfRuns) % RefreshGenCount == 0 && RefreshGenCount > 0))
                        {
                            //pop.GetTopXMembers(1);
                        }

                        doneEvents[(int)threadContext].Set();
                    }, i);

                    i++;
                }
                //Done creating threads

                foreach (var e in doneEvents)
                    e.WaitOne();

                //Console.WriteLine(NumberOfRuns + " generations passed - Current generation: " + CurrentGen);

                CurrentGen += NumberOfRuns;
                GenerationsThisSubRun += NumberOfRuns;

                //Check if a population has achieved a good result
                if ((RefreshGenCount > 0) && (CurrentGen % RefreshGenCount == 0))
                    foreach (var pop in Populations)
                    {
                        if (pop.GetTopXMembers(1)[0].CalculateFitness(CurrentGen) < 0.000001)
                        {
                            Completed = true;
                        }
                    }
                //End check


                //Console.WriteLine("Completion test");

                //If Generation Island Refresh is set, create a new population with the best members from other populations and remove the worst one
                if ((RefreshGenCount > 0) && (CurrentGen % RefreshGenCount == 0))
                {
                    //Get top X from each population and add them into a new population
                    //Find the population with the worst fitness and remove it
                    //Add the new population
                    
                    //Console.WriteLine("Time to refresh islands");

                    int _rand = mainRandom.Next();

                    var NewPop = new PopulationManager<T>(this, _rand, false);
                    
                    NewPop.SelectionAlgorithm = SelectionAlgorithm;

                    PopulationManager<T> worstPop = null;
                    double worstFitness = 0;

                    foreach (var pop in Populations)
                    {

                        Console.WriteLine(" REFRESH: Population elite calculation");

                        int EliteMembers = (int)(Parameters.GetParameter<double>("Par_KeepEliteRatio", 0.1) * Parameters.GetParameter<double>("Par_MaxPopMembers", 100));
                        NewPop.AddMembers(pop.GetTopXMembers(EliteMembers));

                        /*double tempFit = pop.GetAverageFitness();*/

                        if (pop.GetMember(0).CalculateFitness(CurrentGen) >= worstFitness)
                        {
                            worstFitness = pop.GetMember(0).CalculateFitness(CurrentGen);
                            worstPop = pop;
                        }
                    }


                    Console.WriteLine(" REFRESH: Add instance");

                    //NewPop.AddToParameters("Instance", worstPop.GetParameter("Instance"));
                    NewPop.Instance = worstPop.Instance;

                    //Logger.Log("Removed " + Populations.IndexOf(worstPop) + " from Populations. It had " + worstFitness + " fitness");

                    //NewPop.RegenerateMembers();
                    NewPop.GenerateRandomMembers();
                    NewPop.FinalizeGeneration();

                    //Logger.Log("New pop has fitness of " + NewPop.GetAverageFitness() + " and running total of " + NewPop.rand.RunningTotal);

                    Populations.Remove(worstPop);
                    Populations.Add(NewPop);

                    //worstPop.clear();


                    Console.WriteLine(" REFRESH: Done");
                }

                MaxGenerations -= NumberOfRuns;
            }

            Timer.Stop();

            if (Completed || CurrentGen >= Parameters.GetParameter<int>("Par_MaxGenerations", 100))
                return 1;
            return 0;
        }

        /// <summary>
        /// Returns the best members from each population, as a list.
        /// </summary>
        /// <returns></returns>
        public List<T> GetBestMembers()
        {
            List<T> ret = new List<T>();
            foreach (var pop in Populations)
            {
                for (int y = 0; y < 1; y++)
                {
                    T best = pop.GetTopXMembers(1,true)[0];

                    ret.Add(best);
                }
            }
            return ret;
        }

        /// <summary>
        /// Loads the run's parameters from the given filename. Automatically called by the constructor.
        /// </summary>
        /// <param name="filename"></param>
        public static RunParameters LoadParamsFromFile(string filename)
        {
            RunParameters rp = new RunParameters();

            if(filename.Contains(".json"))
            {
                rp.JsonParameters = File.ReadAllText(filename);
                rp.JsonParams = JsonConvert.DeserializeObject(rp.JsonParameters);

                rp.JsonParams.gaparams["string_SpecsFile"] = filename;
            }

            return rp;
        }

        /// <summary>
        /// Returns the time elapsed so far during the run.
        /// </summary>
        /// <returns></returns>
        public double GetElapsedTime()
        {
            return Timer.Elapsed.TotalSeconds;
        }
    }
}