namespace FunctionRegressionExample
{
    partial class FunctionRegressionExample
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listView1 = new System.Windows.Forms.ListView();
            this.generationsToRun = new System.Windows.Forms.NumericUpDown();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.currentGenLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusOfRuns = new System.Windows.Forms.ToolStripStatusLabel();
            this.expressionBox = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.generationsToRun)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Location = new System.Drawing.Point(12, 92);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(638, 198);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.List;
            // 
            // generationsToRun
            // 
            this.generationsToRun.Location = new System.Drawing.Point(12, 37);
            this.generationsToRun.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.generationsToRun.Name = "generationsToRun";
            this.generationsToRun.Size = new System.Drawing.Size(120, 20);
            this.generationsToRun.TabIndex = 1;
            this.generationsToRun.Value = new decimal(new int[] {
            7,
            0,
            0,
            0});
            // 
            // button1
            // 
            this.button1.Enabled = false;
            this.button1.Location = new System.Drawing.Point(137, 37);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(113, 20);
            this.button1.TabIndex = 2;
            this.button1.Text = "Run Generations";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(256, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "0 = run until done";
            // 
            // button2
            // 
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(12, 63);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Save";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Enabled = false;
            this.button3.Location = new System.Drawing.Point(93, 63);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 5;
            this.button3.Text = "Load";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.currentGenLabel,
            this.statusOfRuns});
            this.statusStrip1.Location = new System.Drawing.Point(0, 293);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(662, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(111, 17);
            this.toolStripStatusLabel1.Text = "Current Generation:";
            // 
            // currentGenLabel
            // 
            this.currentGenLabel.Name = "currentGenLabel";
            this.currentGenLabel.Size = new System.Drawing.Size(13, 17);
            this.currentGenLabel.Text = "0";
            // 
            // statusOfRuns
            // 
            this.statusOfRuns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.statusOfRuns.Name = "statusOfRuns";
            this.statusOfRuns.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
            this.statusOfRuns.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.statusOfRuns.Size = new System.Drawing.Size(492, 17);
            this.statusOfRuns.Spring = true;
            this.statusOfRuns.Text = "No manager initialized";
            // 
            // expressionBox
            // 
            this.expressionBox.Location = new System.Drawing.Point(12, 13);
            this.expressionBox.Name = "expressionBox";
            this.expressionBox.Size = new System.Drawing.Size(238, 20);
            this.expressionBox.TabIndex = 8;
            this.expressionBox.Text = "Pow(X, 3) + Pow(X,2) + Pow(X,1) + 1";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(257, 13);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 20);
            this.button4.TabIndex = 9;
            this.button4.Text = "Reset";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // FunctionRegressionExample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(662, 315);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.expressionBox);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.generationsToRun);
            this.Name = "FunctionRegressionExample";
            this.Text = "Demo";
            ((System.ComponentModel.ISupportInitialize)(this.generationsToRun)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.NumericUpDown generationsToRun;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel currentGenLabel;
        private System.Windows.Forms.ToolStripStatusLabel statusOfRuns;
        private System.Windows.Forms.TextBox expressionBox;
        private System.Windows.Forms.Button button4;
    }
}