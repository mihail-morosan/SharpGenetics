using SharpGenetics.Logging;
using SharpGenetics.SelectionAlgorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    [DataContractAttribute]
    [KnownType("GetKnownType")]
    public class GPRunManager<T, InputT, OutputT> where T : PopulationMember
    {
        [DataMember]
        public RunParameters Parameters;

        [DataMember]
        private Stopwatch Timer;
        [DataMember]
        public CRandom mainRandom;
        [DataMember]
        private int CurrentGen = 0;

        [DataMember]
        private List<GenericTest<InputT, OutputT>> tests = null;

        [DataMember]
        public List<PopulationManager<T, InputT, OutputT>> Populations = new List<PopulationManager<T, InputT, OutputT>>();

        [DataMember]
        private SelectionAlgorithm SelectionAlgorithm;

        private static Type[] GetKnownType()
        {
            Type openGenericType = typeof(SelectionAlgorithm);
            var res = new List<Type>();

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var x in assembly.GetTypes())
                    {
                        var y = x.BaseType;

                        if (!x.IsAbstract && !x.IsInterface && y != null)
                        {
                            if (openGenericType.GUID == y.GUID)
                            {
                                res.Add(x);
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
        public GPRunManager(string Filename, List<GenericTest<InputT, OutputT>> Tests, int RandomSeed = -1) : this(LoadParamsFromFile(Filename), Tests, RandomSeed)
        {
            //Parameters = LoadParamsFromFile(Filename);
            //mainRandom = new CRandom((int)(double)Parameters.GetParameter("Par_Seed"));
            //SetTests(Tests);
        }

        public GPRunManager(RunParameters Params, List<GenericTest<InputT, OutputT>> Tests, int RandomSeed = -1)
        {
            Parameters = Params;
            mainRandom = new CRandom(RandomSeed != -1 ? RandomSeed : (int)(double)Parameters.GetParameter("Par_Seed"));
            SetTests(Tests);

            var SelAlg = (string)Parameters.GetParameter("string_SelectionAlgorithm");
            if(SelAlg.Length < 1)
            {
                SelAlg = "SharpGenetics.SelectionAlgorithms.TournamentSelection";
            }
            SelectionAlgorithm = (SelectionAlgorithm)Activator.CreateInstance(Type.GetType(SelAlg), new object[] { (int)(double)Parameters.GetParameter("Par_TournamentSize"), mainRandom.Next() });
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

            for (int i = 0; i < (int)(double)Parameters.GetParameter("Par_IslandClusters"); i++)
            {
                RunParameters InstanceParams = Parameters.Clone();
                InstanceParams.AddToParameters("Instance", i + 1);

                Populations.Add(new PopulationManager<T, InputT, OutputT>(mainRandom.Next(), InstanceParams, false, false));

                Populations[i].SetTests(tests);
                Populations[i].GenerateRandomMembers();
                Populations[i].FinalizeGeneration();
                Populations[i].SelectionAlgorithm = SelectionAlgorithm;
            }

            Timer.Stop();
        }

        public void ReloadParameters()
        {
            foreach(var Pop in Populations)
            {
                for(int i=0;i<Pop.GetNumberOfIndividuals();i++)
                {
                    Pop.GetMember(i).ReloadParameters(Parameters);
                }
            }
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

            if (GenerationsBeforePause <= 0)
            {
                GenerationsBeforePause = (int)(double)Parameters.GetParameter("Par_MaxGenerations");
            }

            int RefreshGenCount = (int)(double)Parameters.GetParameter("Par_GenCountBeforeRefresh");

            ManualResetEvent[] doneEvents = new ManualResetEvent[(int)(double)Parameters.GetParameter("Par_IslandClusters")];

            int MaxGenerations = GenerationsBeforePause;
            
            while (CurrentGen < (int)(double)Parameters.GetParameter("Par_MaxGenerations") && !Completed && (GenerationsThisSubRun < GenerationsBeforePause))
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
                foreach (PopulationManager<T, InputT, OutputT> pop in Populations)
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
                            pop.GetTopXMembers(1);
                        }

                        doneEvents[(int)threadContext].Set();
                    }, i);

                    i++;
                }
                //Done creating threads

                foreach (var e in doneEvents)
                    e.WaitOne();

                Console.WriteLine(NumberOfRuns + " generations passed");

                CurrentGen += NumberOfRuns;
                GenerationsThisSubRun += NumberOfRuns;

                //Check if a population has achieved a good result
                if ((RefreshGenCount > 0) && (CurrentGen % RefreshGenCount == 0))
                    foreach (PopulationManager<T, InputT, OutputT> pop in Populations)
                    {
                        if (pop.GetTopXMembers(1)[0].CalculateFitness(CurrentGen, tests.ToArray()) < 0.000001)
                        {
                            Completed = true;
                        }
                    }
                //End check


                Console.WriteLine("Completion test");

                //If Generation Island Refresh is set, create a new population with the best members from other populations and remove the worst one
                if ((RefreshGenCount > 0) && (CurrentGen % RefreshGenCount == 0))
                {
                    //Get top X from each population and add them into a new population
                    //Find the population with the worst fitness and remove it
                    //Add the new population


                    Console.WriteLine("Time to refresh islands");

                    int _rand = mainRandom.Next();

                    PopulationManager<T, InputT, OutputT> NewPop = new PopulationManager<T, InputT, OutputT>(_rand, Parameters.Clone(), false);

                    NewPop.SetTests(tests);
                    NewPop.SelectionAlgorithm = SelectionAlgorithm;

                    PopulationManager<T, InputT, OutputT> worstPop = null;
                    double worstFitness = 0;

                    foreach (PopulationManager<T, InputT, OutputT> pop in Populations)
                    {

                        Console.WriteLine(" REFRESH: Population elite calculation");

                        int EliteMembers = (int)((double)Parameters.GetParameter("Par_KeepEliteRatio") * (double)Parameters.GetParameter("Par_MaxPopMembers"));
                        NewPop.AddMembers(pop.GetTopXMembers(EliteMembers));

                        /*double tempFit = pop.GetAverageFitness();*/

                        if (pop.GetMember(0).CalculateFitness<InputT, OutputT>(CurrentGen, tests.ToArray()) >= worstFitness)
                        {
                            worstFitness = pop.GetMember(0).CalculateFitness<InputT, OutputT>(CurrentGen, tests.ToArray());
                            worstPop = pop;
                        }
                    }


                    Console.WriteLine(" REFRESH: Add instance");

                    NewPop.AddToParameters("Instance", worstPop.GetParameter("Instance"));

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

            if (Completed || CurrentGen >= (int)(double)Parameters.GetParameter("Par_MaxGenerations"))
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
            foreach (PopulationManager<T, InputT, OutputT> pop in Populations)
            {
                for (int y = 0; y < 1; y++)
                {
                    T best = pop.GetTopXMembers(1)[0];

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
            XmlReader xmlReader = XmlReader.Create(filename);
            while (xmlReader.Read())
            {
                if ((xmlReader.Name.Substring(0, Math.Min(4, xmlReader.Name.Length)) == "Par_"))
                {
                    rp.AddToParameters(xmlReader.Name, double.Parse(xmlReader.GetAttribute(0)));
                }

                if ((xmlReader.Name.Substring(0, Math.Min(5, xmlReader.Name.Length)) == "extra"))
                {
                    rp.AddToParameters(xmlReader.Name, double.Parse(xmlReader.GetAttribute(0)));
                }

                if ((xmlReader.Name.Substring(0, Math.Min(6, xmlReader.Name.Length)) == "string"))
                {
                    rp.AddToParameters(xmlReader.Name, xmlReader.GetAttribute(0));
                }
            }

            xmlReader.Close();

            return rp;
        }

        /// <summary>
        /// Loads the tests from the file. Automatically called by the constructor.
        /// </summary>
        /// <param name="Tests"></param>
        public void SetTests(List<GenericTest<InputT, OutputT>> Tests)
        {
            tests = Tests;
        }

        public List<GenericTest<InputT, OutputT>> GetTests()
        {
            return tests;
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