namespace Rewards_fast
{
    partial class Initial_form
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.label_explanatory_text = new System.Windows.Forms.Label();
            this.button_your_template = new System.Windows.Forms.Button();
            this.label_Application_name = new System.Windows.Forms.Label();
            this.button_available_template = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label_explanatory_text
            // 
            this.label_explanatory_text.AutoSize = true;
            this.label_explanatory_text.Font = new System.Drawing.Font("MingLiU_HKSCS-ExtB", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_explanatory_text.ForeColor = System.Drawing.Color.White;
            this.label_explanatory_text.Location = new System.Drawing.Point(23, 108);
            this.label_explanatory_text.Name = "label_explanatory_text";
            this.label_explanatory_text.Size = new System.Drawing.Size(180, 30);
            this.label_explanatory_text.TabIndex = 0;
            this.label_explanatory_text.Text = "Начало работы";
            // 
            // button_your_template
            // 
            this.button_your_template.BackColor = System.Drawing.Color.Green;
            this.button_your_template.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_your_template.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_your_template.Font = new System.Drawing.Font("Nirmala UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_your_template.ForeColor = System.Drawing.Color.White;
            this.button_your_template.Location = new System.Drawing.Point(29, 177);
            this.button_your_template.Name = "button_your_template";
            this.button_your_template.Size = new System.Drawing.Size(509, 55);
            this.button_your_template.TabIndex = 1;
            this.button_your_template.Text = "Создать свой шаблон наградного материала";
            this.button_your_template.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button_your_template.UseVisualStyleBackColor = false;
            this.button_your_template.Click += new System.EventHandler(this.button_your_template_Click);
            // 
            // label_Application_name
            // 
            this.label_Application_name.AutoSize = true;
            this.label_Application_name.Font = new System.Drawing.Font("Yu Gothic", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label_Application_name.ForeColor = System.Drawing.Color.White;
            this.label_Application_name.Location = new System.Drawing.Point(21, 9);
            this.label_Application_name.Name = "label_Application_name";
            this.label_Application_name.Size = new System.Drawing.Size(277, 52);
            this.label_Application_name.TabIndex = 2;
            this.label_Application_name.Text = "Rewards fast";
            // 
            // button_available_template
            // 
            this.button_available_template.BackColor = System.Drawing.Color.Green;
            this.button_available_template.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_available_template.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_available_template.Font = new System.Drawing.Font("Nirmala UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_available_template.ForeColor = System.Drawing.Color.White;
            this.button_available_template.Location = new System.Drawing.Point(29, 258);
            this.button_available_template.Name = "button_available_template";
            this.button_available_template.Size = new System.Drawing.Size(509, 55);
            this.button_available_template.TabIndex = 3;
            this.button_available_template.Text = "Выбрать готовый шаблон";
            this.button_available_template.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button_available_template.UseVisualStyleBackColor = false;
            this.button_available_template.Click += new System.EventHandler(this.button_available_template_Click);
            // 
            // Initial_form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.SeaGreen;
            this.ClientSize = new System.Drawing.Size(584, 368);
            this.Controls.Add(this.button_available_template);
            this.Controls.Add(this.label_Application_name);
            this.Controls.Add(this.button_your_template);
            this.Controls.Add(this.label_explanatory_text);
            this.Name = "Initial_form";
            this.ShowIcon = false;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_explanatory_text;
        private System.Windows.Forms.Button button_your_template;
        private System.Windows.Forms.Label label_Application_name;
        private System.Windows.Forms.Button button_available_template;
    }
}

