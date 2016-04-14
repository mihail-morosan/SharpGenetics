using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharpGenetics.ImageGeneration
{
    public partial class ShowImage : Form
    {
        int generation = 0;
        public ShowImage(Bitmap image)
        {
            InitializeComponent();
            pictureBox1.Image = image;
        }

        public void SetImages(Bitmap[] images, double[] values)
        {
            if(images.Length > 0)
                pictureBox2.Image = images[0];
            if (images.Length > 1)
                pictureBox3.Image = images[1];
            if (images.Length > 2)
                pictureBox4.Image = images[2];
            if (images.Length > 3)
                pictureBox5.Image = images[3];
            if (images.Length > 4)
                pictureBox6.Image = images[4];

            generation++;

            label1.Text = "Generation " + generation;

            if (images.Length > 0)
                label2.Text = values[0].ToString();
            if (images.Length > 1)
                label3.Text = values[1].ToString();
            if (images.Length > 2)
                label4.Text = values[2].ToString();
            if (images.Length > 3)
                label5.Text = values[3].ToString();
            if (images.Length > 4)
                label6.Text = values[4].ToString();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
