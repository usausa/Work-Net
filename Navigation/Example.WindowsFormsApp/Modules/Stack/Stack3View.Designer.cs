namespace Example.WindowsFormsApp.Modules.Stack
{
    partial class Stack3View
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region コンポーネント デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.Pop2Button = new System.Windows.Forms.Button();
            this.PopButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // Pop2Button
            //
            this.Pop2Button.Location = new System.Drawing.Point(320, 176);
            this.Pop2Button.Name = "Pop2Button";
            this.Pop2Button.Size = new System.Drawing.Size(160, 32);
            this.Pop2Button.TabIndex = 11;
            this.Pop2Button.Text = "Pop2";
            this.Pop2Button.UseVisualStyleBackColor = true;
            this.Pop2Button.Click += new System.EventHandler(this.OnPop2ButtonClick);
            //
            // PopButton
            //
            this.PopButton.Location = new System.Drawing.Point(160, 176);
            this.PopButton.Name = "PopButton";
            this.PopButton.Size = new System.Drawing.Size(160, 32);
            this.PopButton.TabIndex = 10;
            this.PopButton.Text = "Pop";
            this.PopButton.UseVisualStyleBackColor = true;
            this.PopButton.Click += new System.EventHandler(this.OnPopButtonClick);
            //
            // Stack3View
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Pop2Button);
            this.Controls.Add(this.PopButton);
            this.Name = "Stack3View";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Pop2Button;
        private System.Windows.Forms.Button PopButton;
    }
}