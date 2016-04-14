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

            System.Threading.Thread t1 = new System.Threading.Thread
                  (delegate ()
                  {
                      int res = 0;
                      this.Dispatcher.Invoke((Action)(() =>
                      {
                          res = mw.RunManager.StartRun((int.Parse(generationsToRun.Text)));

                          //this.Invoke(
                          //    (MethodInvoker)delegate()
                          {
                              button1.IsEnabled = true;
                              mw.currentGenLabel.Content = mw.RunManager.GetGenerationsRun().ToString();
                              mw.statusOfRuns.Content = "Idle";

                              listView1.Items.Clear();

                              int i = 0;
                              foreach (FunctionRegression FN in mw.RunManager.GetBestMembers())
                              {
                                  listView1.Items.Add("Population " + i + " - " + FN + " - " + FN.Fitness);
                                  //Console.WriteLine(FN + " - " + FN.Fitness);
                                  i++;
                              }

                          }
                          //);
                      }));


                  });
            t1.Start();

        }
    }
}
