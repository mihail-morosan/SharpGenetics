using Microsoft.Win32;
using SharpGenetics.BaseClasses;
using SharpGenetics.FunctionRegression;
using SharpGenetics.SelectionAlgorithms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace SGGUI
{
    /// <summary>
    /// Interaction logic for LoadTestData.xaml
    /// </summary>
    public partial class LoadTestData : Page
    {
        MainWindow mw;

        public List<GenericTest<double, double>> tests { get; set; }

        public RunParameters RunParams { get; set; }

        private static object _syncLock = new object();

        public LoadTestData(MainWindow mainWindow)
        {
            InitializeComponent();

            mw = mainWindow;

            tests = new List<GenericTest<double, double>>();

            RunParams = new RunParameters();

            //BindingOperations.EnableCollectionSynchronization(tests, _syncLock);

            DataContext = this;
        }

        private void Reset_Manager_Button(object sender, RoutedEventArgs e)
        {
            //Expression ex = new Expression("Pow([X], 3) + Pow([X], 2) + X + 1");
            NCalc.Expression ex = new NCalc.Expression(expressionBox.Text);

            //Generate tests
            //List<GenericTest<double, double>> tests = new List<GenericTest<double, double>>();

            GenericTest<double, double> test;

            tests.Clear();

            List<char> Variables = new List<char>();
            List<double> VariableValues = new List<double>();

            int MinValue = 0;
            double StepValue = 1.5;

            foreach(var c in expressionBox.Text)
            {
                if(c>='A' && c<='Z')
                {
                    Variables.Add(c);
                    VariableValues.Add(MinValue);
                }
            }

            int TestCount = int.Parse(NrTestsBox.Text);
            int i = 0;

            int maxVal = (int)(Math.Round(Math.Pow(TestCount, 1.0 / (double)Variables.Count), MidpointRounding.AwayFromZero) * StepValue);

            while (i < TestCount)
            {
                test = new GenericTest<double, double>();

                for (int y = 0; y < Variables.Count; y++ )
                    //foreach (var c in Variables)
                    {
                        char c = Variables[y];
                        test.AddInput(c.ToString(), VariableValues[y]);
                        ex.Parameters[c.ToString()] = VariableValues[y];
                    }

                /*if (expressionBox.Text.Contains("X"))
                {
                    test.AddInput("X", i);
                    ex.Parameters["X"] = (double)i;
                }

                if (expressionBox.Text.Contains("Y"))
                {
                    test.AddInput("Y", y);
                    ex.Parameters["Y"] = (double)y;
                }*/

                //test.AddInput("y", y);
                //test.AddOutput(3 * y * i * y * i);
                //test.AddOutput(i * i * i + i * i + i + 1);
                test.AddOutput((double)ex.Evaluate());

                tests.Add(test);

                i++;
                VariableValues[0]+=StepValue;

                for(int y=0;y<VariableValues.Count;y++)
                {
                    if(VariableValues[y] == maxVal)
                    {
                        VariableValues[y] = MinValue;
                        if(y+1 < VariableValues.Count)
                            VariableValues[y + 1] += StepValue;
                    }
                }
            }

            //Pi Test
            //test = new GenericTest<double, double>();
            //test.AddOutput(Math.PI);
            //tests.Add(test);

            /*foreach(var t in tests)
            {
                dataGrid.Items.Add(t.Outputs);
            }*/

            //dataGrid.Items.Clear();

            resetItemsSources();

            //BindingOperations.GetBindingExpressionBase(dataGrid, DataGrid.proper)
            //dataGrid.GetBindingExpression(DataGrid.ItemsSourceProperty).UpdateTarget();
            //OnPropertyChanged("tests");

            

            //mw.button1.IsEnabled = true;
            //mw.button2.IsEnabled = true;
            //mw.button3.IsEnabled = true;

            mw.statusOfRuns.Content = "Test data generated";
        }

        private void resetItemsSources()
        {
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = tests;

            treeView.ItemsSource = null;
            treeView.ItemsSource = RunParams._parameters;

            //dataGrid1.ItemsSource = null;
            //dataGrid1.ItemsSource = tests[0].Outputs;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            mw.RunManager = new GPRunManager<FunctionRegression, double, double>(RunParams, tests);

            mw.RunManager.InitRun();
            
            mw.statusOfRuns.Content = "Run initialized";

            mw.frame.Content = new DoActualRun(mw);
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Tests"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "XML documents (.xml)|*.xml"; // Filter files by extension 

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            string filename = "";

            // Process save file dialog box results 
            if (result == true)
            {
                // Save document 
                filename = dlg.FileName;
            } else
            {
                return;
            }

            FileStream stream = File.Open(filename, FileMode.Create);

            var serializer = new DataContractSerializer(typeof(List<GenericTest<double,double>>));

            serializer.WriteObject(stream, tests);

            Console.WriteLine("Saving to file done");
            mw.statusOfRuns.Content = "Saving tests to file complete";

            stream.Close();
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            string filename = "";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                filename = openFileDialog.FileName;
            else
            {

                mw.statusOfRuns.Content = "Failed to load file";
                return;
            }

            try {
                FileStream fs = new FileStream(filename, FileMode.Open);

                XmlDictionaryReader reader =
                    XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                DataContractSerializer ser = new DataContractSerializer(typeof(List<GenericTest<double, double>>));

                // Deserialize the data and read it from the instance.
                tests =
                    (List<GenericTest<double, double>>)ser.ReadObject(reader, true);
                reader.Close();
                fs.Close();
            } catch (Exception ex)
            {

                mw.statusOfRuns.Content = "Failed to load test data";
            }

            resetItemsSources();

            Console.WriteLine("Loading from file done");
            mw.statusOfRuns.Content = "Test data loaded from file";
        }

        private void saveButton_Settings(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "RunSettings"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "XML documents (.xml)|*.xml"; // Filter files by extension 

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            string filename = "";

            // Process save file dialog box results 
            if (result == true)
            {
                // Save document 
                filename = dlg.FileName;
            }
            else
            {
                return;
            }

            FileStream stream = File.Open(filename, FileMode.Create);

            StreamWriter writer = new StreamWriter(stream);

            writer.WriteLine("<Run>");
            foreach(var i in RunParams._parameters)
            {
                writer.WriteLine("<" + i.Key + " value='" + i.Value + "'/>");
            }
            writer.WriteLine("</Run>");

            writer.Close();

            mw.statusOfRuns.Content = "Saving settings to file complete";

            stream.Close();
        }

        private void loadButton_Settings(object sender, RoutedEventArgs e)
        {
            string filename = "";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                filename = openFileDialog.FileName;
            else
            {

                mw.statusOfRuns.Content = "Failed to load file";
                return;
            }

            try
            {
                RunParams = GPRunManager<FunctionRegression, double, double>.LoadParamsFromFile(filename);
            }
            catch (Exception ex)
            {

                mw.statusOfRuns.Content = "Failed to load test data";
            }

            resetItemsSources();

            Console.WriteLine("Loading from file done");
            mw.statusOfRuns.Content = "Test data loaded from file";
        }
    }
}
