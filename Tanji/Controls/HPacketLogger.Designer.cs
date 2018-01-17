namespace Tanji.Controls
{
    partial class HPacketLogger
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LoggerTxt = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // LoggerTxt
            // 
            this.LoggerTxt.BackColor = System.Drawing.Color.Black;
            this.LoggerTxt.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LoggerTxt.DetectUrls = false;
            this.LoggerTxt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LoggerTxt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoggerTxt.ForeColor = System.Drawing.Color.White;
            this.LoggerTxt.HideSelection = false;
            this.LoggerTxt.Location = new System.Drawing.Point(0, 0);
            this.LoggerTxt.Name = "LoggerTxt";
            this.LoggerTxt.ReadOnly = true;
            this.LoggerTxt.Size = new System.Drawing.Size(700, 500);
            this.LoggerTxt.TabIndex = 0;
            this.LoggerTxt.Text = "";
            // 
            // HPacketLogger
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LoggerTxt);
            this.Name = "HPacketLogger";
            this.Size = new System.Drawing.Size(700, 500);
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.RichTextBox LoggerTxt;
    }
}
