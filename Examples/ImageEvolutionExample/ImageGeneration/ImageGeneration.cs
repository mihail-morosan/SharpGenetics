using AForge.Imaging;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using SharpGenetics.BaseClasses;
using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using XnaFan.ImageComparison;

namespace SharpGenetics.ImageGeneration
{
    [DataContractAttribute]
    [KnownType("Circle")]
    [KnownType("Tri")]
    abstract class ImgShape
    {
        [DataMember]
        public Pen Colour;
        public abstract Point AveragePoint();
        public abstract void ChangeCenter(Point newCenter);
        public abstract void ModifySize(int val);
        public static int ComparePoints(Point a, Point b)
        {
            if (a.Y < b.Y)
                return -1;
            else
            {
                if (b.Y < a.Y)
                    return 1;
                else
                {
                    if (a.X < b.X)
                        return -1;
                    else
                        if (b.X < a.X)
                            return 1;
                        else
                            return 0;
                }
            }
        }

        public ImgShape(Pen Col)
        {
            this.Colour = Col;
        }
    }

    class Circle : ImgShape
    {
        [DataMember]
        public Point Center;
        [DataMember]
        public double Diameter;

        public Circle(Point Cent, double Diam, Pen Pe) : base(Pe)
        {
            this.Center = Cent;
            this.Diameter = Diam;
        }

        public override Point AveragePoint()
        {
            return Center;
        }

        public override int GetHashCode()
        {
            return (Center.GetHashCode() + Diameter.GetHashCode()).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Circle))
                return false;
            return (this.Diameter == ((Circle)obj).Diameter) && (this.Center.Equals(((Circle)obj).Center));
        }

        public override void ChangeCenter(Point newCenter)
        {
            this.Center = newCenter;
        }

        public override void ModifySize(int val)
        {
            this.Diameter += val;
        }
    }

    class Tri : ImgShape
    {
        [DataMember]
        public Point[] Points;

        public override Point AveragePoint()
        {
            double aX = 0, aY = 0;
            foreach(Point p in Points)
            {
                aX += p.X;
                aY += p.Y;
            }

            return new Point((int)(aX / Points.Count()),(int)(aY / Points.Count()));
        }

        public Tri(Point[] Ps, Pen Pe) : base(Pe)
        {
            Points = Ps;
        }

        public override int GetHashCode()
        {
            int ret = 0;
            for (int i = 0; i < Points.Count(); i++)
            {
                ret = ret + Points[i].GetHashCode();
            }
            ret += Colour.Color.GetHashCode();
            return ret.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Tri))
                return false;
            bool ret = true;
            Point[] P2 = ((Tri)obj).Points;
            for(int i=0;i<Points.Count();i++)
            {
                ret = ret && Points[i].Equals(P2[i]);
            }

            ret = ret && Colour.Color.Equals(((Tri)obj).Colour.Color);

            return ret;
        }

        public override void ChangeCenter(Point newCenter)
        {
            Point avg = AveragePoint();
            Point offset = new Point(- avg.X + newCenter.X, - avg.Y + newCenter.Y);

            for (int i = 0; i < Points.Count(); i++ )
            {
                Points[i].X += offset.X;
                Points[i].Y += offset.Y;
            }
        }

        public override void ModifySize(int val)
        {
            for (int i = 0; i < Points.Count(); i++)
            {
                val = -val;
                Points[i].X += val;
                Points[i].Y += val;
            }
        }
    }

    static class ShuffleListExtension
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    [DataContractAttribute]
    class ImageGeneration : BaseClasses.PopulationMember
    {
        [DataMember]
        RunParameters popParams;

        public PopulationMember Clone()
        {
            ImageGeneration ret = new ImageGeneration(popParams, Triangles, rand);
            return ret;
        }

        [DataMember]
        public Bitmap BImage { get { return _image;  } }

        [DataMember]
        LockBitmap LockBit;

        [DataMember]
        Bitmap _image;

        [DataMember]
        public double Fitness;

        [DataMember]
        int TriCount = 5;
        [DataMember]
        int Accuracy = 1;

        [DataMember]
        List<ImgShape> Triangles;

        [DataMember]
        CRandom rand;


        public CRandom GetRandomGenerator()
        {
            return rand;
        }
        public void SetRandomGenerator(CRandom rand)
        {
            //this.rand = rand;
        }


        public ImageGeneration(RunParameters _params, List<ImgShape> TriangleList = null, CRandom _random = null)
        {
            popParams = _params;

            TriCount = (int)(double)popParams.GetParameter("extra_tri_count");
            Accuracy = (int)(double)popParams.GetParameter("extra_accuracy");

            this.rand = _random;

            if (Accuracy < 1)
                Accuracy = 1;

            if (TriangleList == null)
            {
                Triangles = GenerateRandomTriangles((int)(double)popParams.GetParameter("extra_image_size"), TriCount);
            }
            else
            {
                Triangles = TriangleList;
            }

            //Triangles.Sort((a, b) => Tri.ComparePoints(a.AveragePoint(), b.AveragePoint()));

            //Triangles.Shuffle();

            RemoveHiddenTriangles(Triangles);

            _image = GenerateImage(Triangles, (int)(double)popParams.GetParameter("extra_image_size"));

            LockBit = new LockBitmap(_image);

            Fitness = -1;
        }

        static float sign(Point p1, Point p2, Point p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        static bool PointInTriangle(Point pt, Point v1, Point v2, Point v3)
        {
            bool b1, b2, b3;

            b1 = sign(pt, v1, v2) < 0.0f;
            b2 = sign(pt, v2, v3) < 0.0f;
            b3 = sign(pt, v3, v1) < 0.0f;

            return ((b1 == b2) && (b2 == b3));
        }

        public static void RemoveHiddenTriangles(List<ImgShape> TList)
        {
            Tri t1, t2;
            List<ImgShape> Remove = new List<ImgShape>();
            for (int i = 0; i < TList.Count - 1; i++)
            {
                for (int y = i + 1; y < TList.Count; y++)
                {
                    if (TList[i] is Tri && TList[y] is Tri)
                    {
                        t1 = (Tri)TList[i];
                        t2 = (Tri)TList[y];

                        if (PointInTriangle(t1.Points[0], t2.Points[0], t2.Points[1], t2.Points[2]) &&
                            PointInTriangle(t1.Points[1], t2.Points[0], t2.Points[1], t2.Points[2]) &&
                            PointInTriangle(t1.Points[2], t2.Points[0], t2.Points[1], t2.Points[2]))
                        {
                            Remove.Add(t1);
                        }
                    }
                }
            }

            foreach(Tri t in Remove)
            {
                TList.Remove(t);
            }
        }

        public Color GetRandomColor()
        {
            //return Color.Red;
            return Color.FromArgb(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255));
        }

        public List<ImgShape> GenerateRandomTriangles(int ImageSize, int TriCountMax)
        {
            List<ImgShape> ret = new List<ImgShape>();
            int tricount = rand.Next(TriCountMax+1);

            for (int i = 0; i < tricount; i++)
            {
                Pen p = new Pen(GetRandomColor());
                p.Width = 1;

                if (rand.Next(2) == 0)
                {
                    int x1, x2, x3, y1, y2, y3;
                    x1 = rand.Next(ImageSize);
                    x2 = rand.Next(ImageSize);
                    x3 = rand.Next(ImageSize);
                    y1 = rand.Next(ImageSize);
                    y2 = rand.Next(ImageSize);
                    y3 = rand.Next(ImageSize);

                    Point[] pnts = new Point[3] { new Point(x1, y1), new Point(x2, y2), new Point(x3, y3) };

                    ret.Add(new Tri(pnts, p));
                }
                else
                {
                    int x1, x2, d;

                    x1 = rand.Next(ImageSize);
                    x2 = rand.Next(ImageSize);
                    d = rand.Next(ImageSize) / 2;

                    ret.Add(new Circle(new Point(x1, x2), d, p));
                }
            }
            return ret;
        }

        public static Bitmap GenerateImage(List<ImgShape> Tris, int ImageSize)
        {
            Bitmap image = new Bitmap(ImageSize, ImageSize);
            Pen blackPen = new Pen(Color.Black);

            using(Graphics g = Graphics.FromImage(image)) {
                g.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, ImageSize, ImageSize));
                for(int i=0;i<Tris.Count;i++)
                {
                    if (Tris[i] is Tri)
                    {
                        g.FillPolygon(Tris[i].Colour.Brush, ((Tri)Tris[i]).Points);
                        g.DrawPolygon(blackPen, ((Tri)Tris[i]).Points);
                    }

                    if(Tris[i] is Circle)
                    {
                        int Diam = (int)((Circle)Tris[i]).Diameter;
                        g.FillEllipse(Tris[i].Colour.Brush, (int)((Circle)Tris[i]).Center.X - Diam, (int)((Circle)Tris[i]).Center.Y - Diam, Diam * 2, Diam * 2);
                        g.DrawEllipse(blackPen, (int)((Circle)Tris[i]).Center.X - Diam, (int)((Circle)Tris[i]).Center.Y - Diam, Diam * 2, Diam * 2);
                    }

                    //g.DrawString("Triangle " + i.ToString(), SystemFonts.CaptionFont, SystemBrushes.WindowText, Tris[i].AveragePoint());
                }

                g.Flush();
            }

            return image;
        }

        public static Bitmap ChangePixelFormat(Bitmap inputImage, System.Drawing.Imaging.PixelFormat newFormat)
        {
            return (inputImage.Clone(new Rectangle(0, 0, inputImage.Width, inputImage.Height), newFormat));
        }

        public double CalculateFitness<T, Y>(params BaseClasses.GenericTest<T, Y>[] values)
        {
            //Compare _image to test image

            if (Fitness < 0)
            {
                //Bitmap test = (Bitmap)(object)values[0].Outputs[0];
                Bitmap test = (Bitmap)(object)values[0].Outputs[0];

                double fit = 0;

                /*Color a, b;
                Rgb ra, rb;

                LockBit.LockBits();
                test.LockBits();

                Cie1976Comparison cie = new Cie1976Comparison();

                int factor = 1;

                double dif = 0;

                //double fitR = 0, fitG = 0, fitB = 0;
                for (int i = 0; i < test.Width; i+=Accuracy)
                {
                    for (int y = 0; y < test.Height; y+=Accuracy)
                    {
                        factor = 1;
                        a = test.GetPixel(i, y);
                        //b = _image.GetPixel(i, y);

                        b = LockBit.GetPixel(i, y);

                        ra = new Rgb { R = a.R, G = a.G, B = a.B };
                        rb = new Rgb { R = b.R, G = b.G, B = b.B };

                        //double add = (a.B - b.B + a.G - b.G + a.R - b.R) / 100.0;
                        //fit += add > 0 ? add : -add;

                        //fitR += Math.Sqrt((ra.R - rb.R) * (ra.R - rb.R));
                        //fitG += Math.Sqrt((ra.G - rb.G) * (ra.G - rb.G));
                        //fitB += Math.Sqrt((ra.B - rb.B) * (ra.B - rb.B));

                        double ColorDif = ra.Compare(rb, cie);

                        if((a.R > 250 && a.G > 250 && a.B > 250) || a.A == 0)
                        {
                            dif = (ColorDif > 0) ? 8 : 0;
                        }
                        else
                        {
                            dif = ColorDif / 100;
                        }

                        if ((b.R > 250 && b.G > 250 && b.B > 250) || b.A == 0)
                        {
                            dif *= 2;
                        }

                        //fit += (ColorDif > 60 ? 120 : ColorDif);
                        fit += dif;
                        //fit += ColorDif;
                    }
                }

                //fit += fitR * 0.212656f + fitG * 0.715158f + fitB * 0.072186f;

                LockBit.UnlockBits();
                test.UnlockBits(); */

                lock (test)
                {
                    fit = _image.PercentageDifference(test, 0) * 10000;
                }

                /*var imageOne = new Bitmap(_image);
                lock (test)
                {
                    var imageTwo = new Bitmap(test);

                    var newBitmap1 = ChangePixelFormat(new Bitmap(imageOne), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    var newBitmap2 = ChangePixelFormat(new Bitmap(imageTwo), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                    //newBitmap1 = SaveBitmapToFile(newBitmap1, filepath, image, BitMapExtension);
                    //newBitmap2 = SaveBitmapToFile(newBitmap2, filepath, targetImage, BitMapExtension);

                    // Setup the AForge library
                    float similarityThreshold = 0.00f;
                    var tm = new ExhaustiveTemplateMatching(similarityThreshold);

                    // Process the images
                    var results = tm.ProcessImage(newBitmap1, newBitmap2);

                    // Compare the results, 0 indicates no match so return false
                    if (results.Length <= 0)
                    {
                        fit = 10000;
                    }


                    // Return true if similarity score is equal or greater than the comparison level
                    //var match = results[0].Similarity >= compareLevel;
                    fit = (1.0f - results[0].Similarity) * 1000;

                }*/


                Fitness = fit + Triangles.Count;
            }


            return Fitness;
        }

        public T Crossover<T>(T b) where T : BaseClasses.PopulationMember
        {
            List<ImgShape> newList = new List<ImgShape>(this.Triangles);

            if (((ImageGeneration)(object)b).Triangles.Count > 0)
            {

                int m1, m2, e1, e2;
                m1 = rand.Next(this.Triangles.Count);
                m2 = rand.Next(((ImageGeneration)(object)b).Triangles.Count);

                e1 = rand.Next(m1, this.Triangles.Count);
                e2 = rand.Next(m2, ((ImageGeneration)(object)b).Triangles.Count);

                newList.RemoveRange(m1, e1 - m1);
                newList.InsertRange(rand.Next(newList.Count), ((ImageGeneration)(object)b).Triangles.GetRange(m2, e2 - m2 + 1));

                if (newList.Count > TriCount * 2)
                    newList.RemoveRange(TriCount, newList.Count - TriCount);
            }

            PopulationMember ret = new ImageGeneration(popParams, newList, rand);

            return (T)ret;
        }

        public T Mutate<T>() where T : BaseClasses.PopulationMember
        {
            List<ImgShape> newList = new List<ImgShape>(this.Triangles);

            int mutationType = rand.Next(3);

            if (mutationType == 0)
            {
                int m1, e1;
                m1 = rand.Next(this.Triangles.Count);

                e1 = rand.Next(m1, this.Triangles.Count);

                newList.RemoveRange(m1, e1 - m1);
                newList.InsertRange(m1, GenerateRandomTriangles(this._image.Height, TriCount));
            }

            if (mutationType == 1)
            {
                int cnt = rand.Next(newList.Count);

                Point newCenter = new Point(rand.Next(this._image.Height), rand.Next(this._image.Height));

                

                for(int i=0;i<cnt;i++)
                {
                    int locOrCol = rand.Next(2);

                    if (locOrCol == 0)
                        newList[rand.Next(newList.Count)].ChangeCenter(newCenter);
                    else
                        newList[rand.Next(newList.Count)].Colour = new Pen(GetRandomColor());
                }
            }

            if (mutationType == 2)
            {
                int cnt = rand.Next(newList.Count);

                for (int i = 0; i < cnt; i++)
                {
                    newList[rand.Next(newList.Count)].ModifySize(rand.Next(-5, 5));
                }
            }

            if (newList.Count > TriCount * 2)
                newList.RemoveRange(TriCount, newList.Count - TriCount);

            PopulationMember ret = new ImageGeneration(popParams, newList, rand);

            return (T)ret;
        }

        public override int GetHashCode()
        {
            int ret = 0;

            foreach(ImgShape t in Triangles)
            {
                ret += t.GetHashCode();
            }
            return ret.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            List<ImgShape> T2 = ((ImageGeneration)obj).Triangles;

            if(Triangles.Count != T2.Count)
                return false;

            for(int i=0;i<T2.Count;i++)
            {
                if(!Triangles[i].Equals(T2[i]))
                    return false;
            }

            return true;
        }
    }
}
