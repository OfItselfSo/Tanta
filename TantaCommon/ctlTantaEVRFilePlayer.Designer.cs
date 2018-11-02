namespace TantaCommon
{
    partial class ctlTantaEVRFilePlayer
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
            this.panelDisplayPanel = new System.Windows.Forms.Panel();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.buttonPause = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.labelCurrentTime = new System.Windows.Forms.Label();
            this.labelDuration = new System.Windows.Forms.Label();
            this.button5SecPlus = new System.Windows.Forms.Button();
            this.button5SecMinus = new System.Windows.Forms.Button();
            this.buttonFastForward = new System.Windows.Forms.Button();
            this.buttonRewind = new System.Windows.Forms.Button();
            this.buttonVolumeUp = new System.Windows.Forms.Button();
            this.buttonVolumeDown = new System.Windows.Forms.Button();
            this.buttonMute = new System.Windows.Forms.Button();
            this.scrollBarVideoPosition = new System.Windows.Forms.HScrollBar();
            this.buttonTakeSnapShot = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // panelDisplayPanel
            // 
            this.panelDisplayPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelDisplayPanel.Location = new System.Drawing.Point(0, 0);
            this.panelDisplayPanel.Name = "panelDisplayPanel";
            this.panelDisplayPanel.Size = new System.Drawing.Size(480, 339);
            this.panelDisplayPanel.TabIndex = 0;
            // 
            // buttonPlay
            // 
            this.buttonPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonPlay.Location = new System.Drawing.Point(80, 362);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(48, 28);
            this.buttonPlay.TabIndex = 1;
            this.buttonPlay.Text = "Play";
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // buttonPause
            // 
            this.buttonPause.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonPause.Location = new System.Drawing.Point(134, 362);
            this.buttonPause.Name = "buttonPause";
            this.buttonPause.Size = new System.Drawing.Size(61, 28);
            this.buttonPause.TabIndex = 2;
            this.buttonPause.Text = "Pause";
            this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStop.Location = new System.Drawing.Point(201, 362);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(51, 28);
            this.buttonStop.TabIndex = 3;
            this.buttonStop.Text = "Stop";
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // labelCurrentTime
            // 
            this.labelCurrentTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelCurrentTime.AutoSize = true;
            this.labelCurrentTime.Location = new System.Drawing.Point(3, 342);
            this.labelCurrentTime.Name = "labelCurrentTime";
            this.labelCurrentTime.Size = new System.Drawing.Size(92, 17);
            this.labelCurrentTime.TabIndex = 4;
            this.labelCurrentTime.Text = "00:00:00 (1x)";
            // 
            // labelDuration
            // 
            this.labelDuration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDuration.AutoSize = true;
            this.labelDuration.Location = new System.Drawing.Point(420, 342);
            this.labelDuration.Name = "labelDuration";
            this.labelDuration.Size = new System.Drawing.Size(64, 17);
            this.labelDuration.TabIndex = 5;
            this.labelDuration.Text = "00:00:00";
            // 
            // button5SecPlus
            // 
            this.button5SecPlus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button5SecPlus.Location = new System.Drawing.Point(258, 362);
            this.button5SecPlus.Name = "button5SecPlus";
            this.button5SecPlus.Size = new System.Drawing.Size(37, 28);
            this.button5SecPlus.TabIndex = 6;
            this.button5SecPlus.Text = "+5";
            this.button5SecPlus.Click += new System.EventHandler(this.button5SecPlus_Click);
            // 
            // button5SecMinus
            // 
            this.button5SecMinus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button5SecMinus.Location = new System.Drawing.Point(39, 362);
            this.button5SecMinus.Name = "button5SecMinus";
            this.button5SecMinus.Size = new System.Drawing.Size(35, 28);
            this.button5SecMinus.TabIndex = 7;
            this.button5SecMinus.Text = "-5";
            this.button5SecMinus.Click += new System.EventHandler(this.button5SecMinus_Click);
            // 
            // buttonFastForward
            // 
            this.buttonFastForward.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonFastForward.Location = new System.Drawing.Point(301, 362);
            this.buttonFastForward.Name = "buttonFastForward";
            this.buttonFastForward.Size = new System.Drawing.Size(27, 28);
            this.buttonFastForward.TabIndex = 8;
            this.buttonFastForward.Text = ">>";
            this.buttonFastForward.Click += new System.EventHandler(this.buttonFastForward_Click);
            // 
            // buttonRewind
            // 
            this.buttonRewind.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonRewind.Location = new System.Drawing.Point(6, 362);
            this.buttonRewind.Name = "buttonRewind";
            this.buttonRewind.Size = new System.Drawing.Size(27, 28);
            this.buttonRewind.TabIndex = 9;
            this.buttonRewind.Text = "<<";
            this.buttonRewind.Click += new System.EventHandler(this.buttonRewind_Click);
            // 
            // buttonVolumeUp
            // 
            this.buttonVolumeUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonVolumeUp.Location = new System.Drawing.Point(400, 364);
            this.buttonVolumeUp.Name = "buttonVolumeUp";
            this.buttonVolumeUp.Size = new System.Drawing.Size(21, 24);
            this.buttonVolumeUp.TabIndex = 10;
            this.buttonVolumeUp.Text = "+";
            this.buttonVolumeUp.Click += new System.EventHandler(this.buttonVolumeUp_Click);
            // 
            // buttonVolumeDown
            // 
            this.buttonVolumeDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonVolumeDown.Location = new System.Drawing.Point(423, 364);
            this.buttonVolumeDown.Name = "buttonVolumeDown";
            this.buttonVolumeDown.Size = new System.Drawing.Size(21, 24);
            this.buttonVolumeDown.TabIndex = 11;
            this.buttonVolumeDown.Text = "-";
            this.buttonVolumeDown.Click += new System.EventHandler(this.buttonVolumeDown_Click);
            // 
            // buttonMute
            // 
            this.buttonMute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMute.Location = new System.Drawing.Point(446, 364);
            this.buttonMute.Name = "buttonMute";
            this.buttonMute.Size = new System.Drawing.Size(21, 24);
            this.buttonMute.TabIndex = 12;
            this.buttonMute.Text = "m";
            this.buttonMute.Click += new System.EventHandler(this.buttonMute_Click);
            // 
            // scrollBarVideoPosition
            // 
            this.scrollBarVideoPosition.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scrollBarVideoPosition.Location = new System.Drawing.Point(106, 344);
            this.scrollBarVideoPosition.Name = "scrollBarVideoPosition";
            this.scrollBarVideoPosition.Size = new System.Drawing.Size(309, 13);
            this.scrollBarVideoPosition.TabIndex = 13;
            this.scrollBarVideoPosition.Scroll += new System.Windows.Forms.ScrollEventHandler(this.scrollBarVideoPosition_Scroll);
            // 
            // buttonTakeSnapShot
            // 
            this.buttonTakeSnapShot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonTakeSnapShot.Location = new System.Drawing.Point(358, 364);
            this.buttonTakeSnapShot.Name = "buttonTakeSnapShot";
            this.buttonTakeSnapShot.Size = new System.Drawing.Size(21, 24);
            this.buttonTakeSnapShot.TabIndex = 14;
            this.buttonTakeSnapShot.Text = "P";
            this.buttonTakeSnapShot.Click += new System.EventHandler(this.buttonTakeSnapShot_Click);
            // 
            // ctlTantaEVRFilePlayer
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.buttonTakeSnapShot);
            this.Controls.Add(this.scrollBarVideoPosition);
            this.Controls.Add(this.buttonMute);
            this.Controls.Add(this.buttonVolumeDown);
            this.Controls.Add(this.buttonVolumeUp);
            this.Controls.Add(this.buttonRewind);
            this.Controls.Add(this.buttonFastForward);
            this.Controls.Add(this.button5SecMinus);
            this.Controls.Add(this.button5SecPlus);
            this.Controls.Add(this.labelDuration);
            this.Controls.Add(this.labelCurrentTime);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonPause);
            this.Controls.Add(this.buttonPlay);
            this.Controls.Add(this.panelDisplayPanel);
            this.Name = "ctlTantaEVRFilePlayer";
            this.Size = new System.Drawing.Size(480, 389);
            this.SizeChanged += new System.EventHandler(this.ctlTantaEVRFilePlayer_SizeChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelDisplayPanel;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonPause;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Label labelCurrentTime;
        private System.Windows.Forms.Label labelDuration;
        private System.Windows.Forms.Button button5SecPlus;
        private System.Windows.Forms.Button button5SecMinus;
        private System.Windows.Forms.Button buttonFastForward;
        private System.Windows.Forms.Button buttonRewind;
        private System.Windows.Forms.Button buttonVolumeUp;
        private System.Windows.Forms.Button buttonVolumeDown;
        private System.Windows.Forms.Button buttonMute;
        private System.Windows.Forms.HScrollBar scrollBarVideoPosition;
        private System.Windows.Forms.Button buttonTakeSnapShot;
    }
}
