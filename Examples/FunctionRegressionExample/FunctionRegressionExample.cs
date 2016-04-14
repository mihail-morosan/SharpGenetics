using FunctionRegressionExample;
using SharpGenetics.BaseClasses;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SharpGenetics
{
    internal class Demo
    {
        private static void Main(string[] args)
        {
            FunctionRegressionExample.FunctionRegressionExample demo = new FunctionRegressionExample.FunctionRegressionExample();

            demo.ShowDialog();

            //Console.WriteLine("Time elapsed - " + RunManager.GetElapsedTime() + " seconds");

            //Logging.Logger.Log("Time elapsed - " + RunManager.GetElapsedTime() + " seconds and " + RunManager.GetGenerationsRun() + " generations.");

            //Console.ReadKey();
        }
    }
}