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
    /// Interaction logic for DoActualRun.xaml
    /// </summary>
    public partial class DoActualRun : Page
    {
        MainWindow mw;

        public DoActualRun(MainWindow mainWindow)
        {
            InitializeComponent();

            mw = mainWindow;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = false;
            mw.statusOfRuns.Content = "Waiting for " + generationsToRun.Text + " generations to finish.";


            int Generations = 0;
            Generations = int.Parse(generationsToRun.Text);


            System.Threading.Thread t1 = new System.Threading.Thread
                  (delegate()
                  {
                      int res = 0;
                      while (Generations > 0)
                      {
                          if (Generations > 25)
                          {
                              res = mw.RunManager.StartRun(25);
                          }
                          else
                          {
                              res = mw.RunManager.StartRun(Generations);
                          }

                          Generations -= 25;

                          this.Dispatcher.Invoke((Action)(() =>
                          {

                              mw.currentGenLabel.Content = mw.RunManager.GetGenerationsRun().ToString();

                              if (Generations <= 0)
                              {
                                  button1.IsEnabled = true;
                                  mw.statusOfRuns.Content = "Idle";
                              }
                              else
                              {
                                  mw.statusOfRuns.Content = "Waiting for " + Generations + " generations to finish.";
                              }

                              listView1.Items.Clear();

                              int i = 0;
                              foreach (HeuristicRegression FN in mw.RunManager.GetBestMembers())
                              {
                                  listView1.Items.Add("Population " + i + " - " + FN.Fitness + " - " + FN);
                                  //Console.WriteLine(FN + " - " + FN.Fitness);
                                  i++;
                              }

                          }));

                      }
                  });
            t1.Start();


        }

        private void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listView1.SelectedIndex < 0)
                return;

            simulationView.Items.Clear();
            HeuristicRegression FN = mw.RunManager.GetBestMembers()[listView1.SelectedIndex];

            foreach (var test in mw.RunManager.GetTests())
            {
                simulationView.Items.Add("Test");
                StringWriter txt = new StringWriter();

                Point A, B;
                    A = new Point((double)(object)test.Inputs["X1"], (double)(object)test.Inputs["Y1"]);
                    B = new Point((double)(object)test.Inputs["X2"], (double)(object)test.Inputs["Y2"]);
                    FN.GetBestPathToTile(A, B, true, txt, false);

                    FN.GetBestPathToTile(A, B, true, txt, true);

                //FN.Simulate(test, true, txt);

                string[] lines = txt.ToString().Split('\n');
                foreach(var l in lines)
                    simulationView.Items.Add(l);
            }
        }
    }
}
