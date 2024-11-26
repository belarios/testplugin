namespace EQ_ACT_Plugin
{
    partial class EQ_ACT_Plugin
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
            this.lstMessages = new System.Windows.Forms.ListBox();
            this.cmdClearMessages = new System.Windows.Forms.Button();
            this.cmdCopyProblematic = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lstMessages
            // 
            this.lstMessages.FormattingEnabled = true;
            this.lstMessages.Location = new System.Drawing.Point(13, 45);
            this.lstMessages.Name = "lstMessages";
            this.lstMessages.ScrollAlwaysVisible = true;
            this.lstMessages.Size = new System.Drawing.Size(700, 173);
            this.lstMessages.TabIndex = 62;
            // 
            // cmdClearMessages
            // 
            this.cmdClearMessages.Location = new System.Drawing.Point(51, 324);
            this.cmdClearMessages.Name = "cmdClearMessages";
            this.cmdClearMessages.Size = new System.Drawing.Size(106, 26);
            this.cmdClearMessages.TabIndex = 65;
            this.cmdClearMessages.Text = "Clear";
            this.cmdClearMessages.UseVisualStyleBackColor = true;
            this.cmdClearMessages.Click += new System.EventHandler(this.cmdClearMessages_Click);
            // 
            // cmdCopyProblematic
            // 
            this.cmdCopyProblematic.Location = new System.Drawing.Point(549, 324);
            this.cmdCopyProblematic.Name = "cmdCopyProblematic";
            this.cmdCopyProblematic.Size = new System.Drawing.Size(118, 26);
            this.cmdCopyProblematic.TabIndex = 64;
            this.cmdCopyProblematic.Text = "Copy to Clipboard";
            this.cmdCopyProblematic.UseVisualStyleBackColor = true;
            this.cmdCopyProblematic.Click += new System.EventHandler(this.cmdCopyProblematic_Click);
            // 
            // EQ_ACT_Plugin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cmdClearMessages);
            this.Controls.Add(this.cmdCopyProblematic);
            this.Controls.Add(this.lstMessages);
            this.Name = "EQ_ACT_Plugin";
            this.Size = new System.Drawing.Size(747, 388);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lstMessages;
        private System.Windows.Forms.Button cmdClearMessages;
        private System.Windows.Forms.Button cmdCopyProblematic;
    }
}
