using Microsoft.Win32;
using SharpGenetics.BaseClasses;
using SharpGenetics.FunctionRegression;
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
            Random rand = new Random(253);

            int MapBounds = 20;

            GenericTest<double, double> test;

            tests.Clear();

            int TestCount = int.Parse(NrTestsBox.Text);
            int i = 0;

            while (i < TestCount)
            {
                test = new GenericTest<double, double>();
                test.AddInput("X1", rand.Next(MapBounds));
                test.AddInput("Y1", rand.Next(MapBounds));
                test.AddInput("X2", rand.Next(MapBounds));
                test.AddInput("Y2", rand.Next(MapBounds));

                tests.Add(test);

                i++;
            }

            resetItemsSources();

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
            mw.RunManager = new GPRunManager<HeuristicRegression, double, double>(RunParams, tests);


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

            var serializer = new DataContractSerializer(typeof(List<GenericTest<double, double>>));

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
                RunParams = GPRunManager<HeuristicRegression, double, double>.LoadParamsFromFile(filename);
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
