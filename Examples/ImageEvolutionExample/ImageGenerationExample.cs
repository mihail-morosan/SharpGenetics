using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpGenetics.BaseClasses;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SharpGenetics.ImageGeneration;
using AForge.Imaging;
using XnaFan.ImageComparison;

namespace SharpGenetics
{
    class ImageGenerationExample
    {
        public static Bitmap ResizeImage(Bitmap imgToResize, Size size)
        {
            try
            {
                Bitmap b = new Bitmap(size.Width, size.Height);
                using (Graphics g = Graphics.FromImage((System.Drawing.Image)b))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
                }
                return b;
            }
            catch {
                return null;
            }
        }


        static void Main(string[] args)
        {

            List<GenericTest<float, Bitmap>> tests = new List<GenericTest<float, Bitmap>>();

            GenericTest<float, Bitmap> test = new GenericTest<float, Bitmap>();

            Bitmap reference = null;

            //Load reference
            var t = new Thread((ThreadStart)(() =>
            {
                OpenFileDialog dlg = new OpenFileDialog();

                dlg.Title = "Open Image";
                //dlg.Filter = "bmp files (*.bmp)|*.bmp";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    reference = ResizeImage(new Bitmap(dlg.FileName), new Size(100, 100));
                }

                dlg.Dispose();
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            Console.WriteLine(reference.PercentageDifference(new Bitmap("F:\\GitHub\\SharpGenetics\\Examples\\Binaries\\Images\\Untitled.png"),5));

            /*reference = ImageGeneration.ImageGeneration.ChangePixelFormat(reference, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var tm = new ExhaustiveTemplateMatching(0);

            Bitmap b2 = ResizeImage(new Bitmap("E:\\GitHub\\GeneticProgrammingCS\\Examples\\Binaries\\Images\\Untitled.png"), new Size(100, 100));
            b2 = ImageGeneration.ImageGeneration.ChangePixelFormat(b2, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


            // Process the images
            var results = tm.ProcessImage(reference, b2);

            Console.WriteLine("Similarity is " + results[0].Similarity); */

            //Just generate demo
            //reference = ImageGeneration.ImageGeneration.GenerateImage(ImageGeneration.ImageGeneration.GenerateRandomTriangles(100, 5), 100);

            if (reference == null)
                return;

            test.AddOutput(reference);
            tests.Add(test);

            ShowImage baseForm = new ShowImage(new Bitmap(reference));
            baseForm.Text = "Base Image";

            new Thread(() => baseForm.ShowDialog()).Start();
            //baseForm.Show();

            GPRunManager<ImageGeneration.ImageGeneration, float, Bitmap> RunManager = new GPRunManager<ImageGeneration.ImageGeneration, float, Bitmap>("RunParams/ImageRun1.txt", tests);

            RunManager.InitRun();

            int res = 0;
            while (res == 0)
            {
                res = RunManager.StartRun(1);

                Console.WriteLine("Generations: " + RunManager.GetGenerationsRun());


                List<Bitmap> images = new List<Bitmap>();
                List<double> fitnesses = new List<double>();

                foreach (ImageGeneration.ImageGeneration FN in RunManager.GetBestMembers())
                {
                    Console.WriteLine(FN + " - " + FN.Fitness);

                    images.Add(new Bitmap(FN.BImage));
                    fitnesses.Add(FN.Fitness);
                }

                if (baseForm.Visible)
                {
                    baseForm.Invoke((MethodInvoker)delegate()
                    {
                        baseForm.SetImages(images.ToArray(), fitnesses.ToArray());

                        baseForm.Invalidate();
                    });
                }
            }



            Console.ReadKey();
        }
    }
}
