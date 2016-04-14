using SharpGenetics.BaseClasses;
using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BlindImmobilePeople
{
    class BlindImmobilePeopleExample
    {
        static void Main(string[] args)
        {
            //Generate tests
            List<GenericTest<Point, double>> tests = new List<GenericTest<Point, double>>();

            CRandom rand = new CRandom(256);

            int MapBounds = 20;
            GenericTest<Point, double> test = null;
            for (int i = 0; i < 3; i++)
            {
                test = new GenericTest<Point, double>();
                test.AddInput("ILoc", new Point(rand.Next(MapBounds), rand.Next(MapBounds)));
                test.AddInput("BLoc", new Point(rand.Next(MapBounds), rand.Next(MapBounds)));
                //test.AddOutput(i * i * i + i * i + i + 1);

                tests.Add(test);
            }

            //Pi Test
            //test = new GenericTest<double, double>();
            //test.AddOutput(Math.PI);
            //tests.Add(test);

            GPRunManager<BlindImmobilePeople.BlindImmobileGeneration, Point, double> RunManager = new GPRunManager<BlindImmobilePeople.BlindImmobileGeneration, Point, double>("RunParams/RunSettingsImob.xml", tests);

            RunManager.InitRun();

            int res = 0;
            while (res == 0)
            {
                res = RunManager.StartRun(6);

                Console.WriteLine("Generations: " + RunManager.GetGenerationsRun());

                BlindImmobilePeople.BlindImmobileGeneration Best = null;
                foreach (var FN in RunManager.GetBestMembers())
                {
                    Console.WriteLine("Best - " + FN + " - " + FN.Fitness);
                    if (Best == null || Best.Fitness > FN.Fitness)
                        Best = FN;
                }

                if (Best.Fitness < 400)
                {
                    Console.WriteLine("Do you want to test the best member?");

                    if (Console.ReadKey().Key == ConsoleKey.Y)
                    {
                        Best.Simulate(test, true);
                    }
                }
            }


            Console.WriteLine("Time elapsed - " + RunManager.GetElapsedTime() + " seconds");

            SharpGenetics.Logging.Logger.Log("Time elapsed - " + RunManager.GetElapsedTime() + " seconds and " + RunManager.GetGenerationsRun() + " generations.");

            Console.ReadKey();
        }
    }
}
