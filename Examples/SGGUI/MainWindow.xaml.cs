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
    public enum RunType
    {
        FunctionRegression,
        BlindImmobile
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public GPRunManager<FunctionRegression, double, double> RunManager;

        public Type WorkingOn;

        public MainWindow()
        {
            InitializeComponent();

            frame.Content = new LoadTestData(this);
        }


        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
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

            var serializer = new DataContractSerializer(typeof(GPRunManager<FunctionRegression, double, double>));

            serializer.WriteObject(stream, RunManager);

            Console.WriteLine("Saving to file done");
            statusOfRuns.Content = "Saving run state successful";

            stream.Close();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            string filename = "";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                filename = openFileDialog.FileName;
            else
                return;

            try
            {

                FileStream fs = new FileStream(filename, FileMode.Open);

                XmlDictionaryReader reader =
                    XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                DataContractSerializer ser = new DataContractSerializer(typeof(GPRunManager<FunctionRegression, double, double>));

                // Deserialize the data and read it from the instance.
                RunManager =
                    (GPRunManager<FunctionRegression, double, double>)ser.ReadObject(reader, true);
                reader.Close();
                fs.Close();
            }
            catch (Exception ex)
            {

                statusOfRuns.Content = "Failed to load file";
            }

            Console.WriteLine("Loading from file done");
            statusOfRuns.Content = "Loading run state successful";


            frame.Content = new DoActualRun(this);
        }
    }
}
