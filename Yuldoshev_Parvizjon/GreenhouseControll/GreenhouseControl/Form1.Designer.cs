namespace GreenhouseControl
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitle = new Label();
            btnTempDown = new Button();
            lblTemperature = new Label();
            lblWateringStatus = new Label();
            lblTemperatureStatus = new Label();
            btnTempUp = new Button();
            btnCheckTemperature = new Button();
            btnStartWatering = new Button();
            btnStopWatering = new Button();
            label1 = new Label();
            lblMoisture = new Label();
            lblModeStatus = new Label();
            btnMoistureUp = new Button();
            btnMoistureDown = new Button();
            chkAutomaticMode = new CheckBox();
            btnHistory = new Button();
            btnSettings = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(261, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(63, 15);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "ТЕПЛИЦА";
            // 
            // btnTempDown
            // 
            btnTempDown.Location = new Point(169, 55);
            btnTempDown.Name = "btnTempDown";
            btnTempDown.Size = new Size(174, 35);
            btnTempDown.TabIndex = 1;
            btnTempDown.Text = "− Уменьшить температуру";
            btnTempDown.UseVisualStyleBackColor = true;
            // 
            // lblTemperature
            // 
            lblTemperature.AutoSize = true;
            lblTemperature.Location = new Point(12, 65);
            lblTemperature.Name = "lblTemperature";
            lblTemperature.Size = new Size(113, 15);
            lblTemperature.TabIndex = 2;
            lblTemperature.Text = "Температура: 22 °C";
            // 
            // lblWateringStatus
            // 
            lblWateringStatus.AutoSize = true;
            lblWateringStatus.Location = new Point(12, 178);
            lblWateringStatus.Name = "lblWateringStatus";
            lblWateringStatus.Size = new Size(107, 15);
            lblWateringStatus.TabIndex = 3;
            lblWateringStatus.Text = "Полив: выключен";
            // 
            // lblTemperatureStatus
            // 
            lblTemperatureStatus.AutoSize = true;
            lblTemperatureStatus.Location = new Point(12, 120);
            lblTemperatureStatus.Name = "lblTemperatureStatus";
            lblTemperatureStatus.Size = new Size(127, 15);
            lblTemperatureStatus.TabIndex = 4;
            lblTemperatureStatus.Text = "Температура в норме";
            // 
            // btnTempUp
            // 
            btnTempUp.Location = new Point(372, 55);
            btnTempUp.Name = "btnTempUp";
            btnTempUp.Size = new Size(176, 35);
            btnTempUp.TabIndex = 5;
            btnTempUp.Text = "+ Увеличить температуру";
            btnTempUp.UseVisualStyleBackColor = true;
            // 
            // btnCheckTemperature
            // 
            btnCheckTemperature.Location = new Point(169, 110);
            btnCheckTemperature.Name = "btnCheckTemperature";
            btnCheckTemperature.Size = new Size(174, 35);
            btnCheckTemperature.TabIndex = 6;
            btnCheckTemperature.Text = "Проверить температуру";
            btnCheckTemperature.UseVisualStyleBackColor = true;
            // 
            // btnStartWatering
            // 
            btnStartWatering.Location = new Point(169, 168);
            btnStartWatering.Name = "btnStartWatering";
            btnStartWatering.Size = new Size(174, 35);
            btnStartWatering.TabIndex = 7;
            btnStartWatering.Text = "Включить полив";
            btnStartWatering.UseVisualStyleBackColor = true;
            // 
            // btnStopWatering
            // 
            btnStopWatering.Location = new Point(372, 168);
            btnStopWatering.Name = "btnStopWatering";
            btnStopWatering.Size = new Size(174, 35);
            btnStopWatering.TabIndex = 8;
            btnStopWatering.Text = "Выключить полив";
            btnStopWatering.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(111, 268);
            label1.Name = "label1";
            label1.Size = new Size(0, 15);
            label1.TabIndex = 9;
            // 
            // lblMoisture
            // 
            lblMoisture.AutoSize = true;
            lblMoisture.Location = new Point(12, 302);
            lblMoisture.Name = "lblMoisture";
            lblMoisture.Size = new Size(134, 15);
            lblMoisture.TabIndex = 10;
            lblMoisture.Text = "Влажность почвы: 45%";
            // 
            // lblModeStatus
            // 
            lblModeStatus.AutoSize = true;
            lblModeStatus.Location = new Point(12, 241);
            lblModeStatus.Name = "lblModeStatus";
            lblModeStatus.Size = new Size(92, 15);
            lblModeStatus.TabIndex = 11;
            lblModeStatus.Text = "Режим: ручной";
            // 
            // btnMoistureUp
            // 
            btnMoistureUp.Location = new Point(169, 231);
            btnMoistureUp.Name = "btnMoistureUp";
            btnMoistureUp.Size = new Size(174, 35);
            btnMoistureUp.TabIndex = 12;
            btnMoistureUp.Text = "+ Влажность";
            btnMoistureUp.UseVisualStyleBackColor = true;
            // 
            // btnMoistureDown
            // 
            btnMoistureDown.Location = new Point(372, 231);
            btnMoistureDown.Name = "btnMoistureDown";
            btnMoistureDown.Size = new Size(174, 35);
            btnMoistureDown.TabIndex = 13;
            btnMoistureDown.Text = "− Влажность";
            btnMoistureDown.UseVisualStyleBackColor = true;
            // 
            // chkAutomaticMode
            // 
            chkAutomaticMode.AutoSize = true;
            chkAutomaticMode.Location = new Point(169, 298);
            chkAutomaticMode.Name = "chkAutomaticMode";
            chkAutomaticMode.Size = new Size(155, 19);
            chkAutomaticMode.TabIndex = 14;
            chkAutomaticMode.Text = "Автоматический полив";
            chkAutomaticMode.UseVisualStyleBackColor = true;
            // 
            // btnHistory
            // 
            btnHistory.Location = new Point(37, 366);
            btnHistory.Name = "btnHistory";
            btnHistory.Size = new Size(220, 40);
            btnHistory.TabIndex = 15;
            btnHistory.Text = "История измерений и поливов";
            btnHistory.UseVisualStyleBackColor = true;
            // 
            // btnSettings
            // 
            btnSettings.Location = new Point(326, 366);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(220, 40);
            btnSettings.TabIndex = 16;
            btnSettings.Text = "Настройки температуры";
            btnSettings.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(596, 446);
            Controls.Add(btnSettings);
            Controls.Add(btnHistory);
            Controls.Add(chkAutomaticMode);
            Controls.Add(btnMoistureDown);
            Controls.Add(btnMoistureUp);
            Controls.Add(lblModeStatus);
            Controls.Add(lblMoisture);
            Controls.Add(label1);
            Controls.Add(btnStopWatering);
            Controls.Add(btnStartWatering);
            Controls.Add(btnCheckTemperature);
            Controls.Add(btnTempUp);
            Controls.Add(lblTemperatureStatus);
            Controls.Add(lblWateringStatus);
            Controls.Add(lblTemperature);
            Controls.Add(btnTempDown);
            Controls.Add(lblTitle);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Теплица — контроль температуры и полива";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Button btnTempDown;
        private Label lblTemperature;
        private Label lblWateringStatus;
        private Label lblTemperatureStatus;
        private Button btnTempUp;
        private Button btnCheckTemperature;
        private Button btnStartWatering;
        private Button btnStopWatering;
        private Label label1;
        private Label lblMoisture;
        private Label lblModeStatus;
        private Button btnMoistureUp;
        private Button btnMoistureDown;
        private CheckBox chkAutomaticMode;
        private Button btnHistory;
        private Button btnSettings;
    }
}
