using NCalc;
using SharpGenetics.BaseClasses;
using SharpGenetics.FunctionRegression;
using SharpGenetics.SelectionAlgorithms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace FunctionRegressionExample
{
    public partial class FunctionRegressionExample : Form
    {
        private GPRunManager<FunctionRegression, double, double> RunManager;

        public FunctionRegressionExample()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            statusOfRuns.Text = "Waiting for " + generationsToRun.Value + " generations to finish.";

            System.Threading.Thread t1 = new System.Threading.Thread
                  (delegate()
                  {
                      int res = 0;
                      res = RunManager.StartRun((int)generationsToRun.Value);

                      this.Invoke(
                          (MethodInvoker) delegate() { 
                              button1.Enabled = true;
                              currentGenLabel.Text = RunManager.GetGenerationsRun().ToString();
                              statusOfRuns.Text = "Idle";

                              listView1.Items.Clear();

                              int i = 0;
                              foreach (FunctionRegression FN in RunManager.GetBestMembers())
                              {
                                  listView1.Items.Add("Population " + i + " - " + FN + " - " + FN.Fitness);
                                  //Console.WriteLine(FN + " - " + FN.Fitness);
                                  i++;
                              }

                          }
                      );

                      

                  });
            t1.Start();


        }

        private void button2_Click(object sender, EventArgs e)
        {
            FileStream stream = File.Open("test.dat", FileMode.Create);

            var serializer = new DataContractSerializer(typeof(GPRunManager<FunctionRegression, double, double>));

            serializer.WriteObject(stream, RunManager);

            Console.WriteLine("Saving to file done");
            
            stream.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FileStream fs = new FileStream("test.dat", FileMode.Open);

            XmlDictionaryReader reader =
                XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
            DataContractSerializer ser = new DataContractSerializer(typeof(GPRunManager<FunctionRegression, double, double>));

            // Deserialize the data and read it from the instance.
            RunManager =
                (GPRunManager<FunctionRegression, double, double>)ser.ReadObject(reader, true);
            reader.Close();
            fs.Close();

            Console.WriteLine("Loading from file done");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Expression ex = new Expression("Pow([X], 3) + Pow([X], 2) + X + 1");
            Expression ex = new Expression(expressionBox.Text);

            //Generate tests
            List<GenericTest<double, double>> tests = new List<GenericTest<double, double>>();

            

            GenericTest<double, double> test;
            for (int i = 1; i < 14; i++)
            {
                for (int y = 1; y < 14; y++)
                {
                    test = new GenericTest<double, double>();

                    if (expressionBox.Text.Contains("X"))
                    {
                        test.AddInput("X", i);
                        ex.Parameters["X"] = (double)i;
                    }

                    if (expressionBox.Text.Contains("Y"))
                    {
                        test.AddInput("Y", y);
                        ex.Parameters["Y"] = (double)y;
                    }

                    //test.AddInput("y", y);
                    //test.AddOutput(3 * y * i * y * i);
                    //test.AddOutput(i * i * i + i * i + i + 1);
                    test.AddOutput((double)ex.Evaluate());

                    tests.Add(test);
                }
            }

            //Pi Test
            //test = new GenericTest<double, double>();
            //test.AddOutput(Math.PI);
            //tests.Add(test);

            RunManager = new GPRunManager<FunctionRegression, double, double>("RunParams/Run1.txt", tests);

            RunManager.InitRun();

            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;

            statusOfRuns.Text = "New run initialized";
        }
    }
}
