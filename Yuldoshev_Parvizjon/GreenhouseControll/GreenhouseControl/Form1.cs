
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GreenhouseControl
{
    public partial class Form1 : Form
    {
        private Greenhouse _greenhouse;
        private readonly DatabaseHelper _database;
        private readonly System.Windows.Forms.Timer _timer;
        private Panel? _moistureBarTrack;
        private Panel? _moistureBarFill;

        public Form1()
        {
            InitializeComponent();

            _database = new DatabaseHelper();
            _greenhouse = new Greenhouse("Теплица №1", 22);

            var settings = _database.GetSettings();

            _greenhouse.MinTemperature = settings.MinTemperature;
            _greenhouse.MaxTemperature = settings.MaxTemperature;

            _database.SaveTemperature(_greenhouse.Temperature);

            btnTempUp.Click += btnTempUp_Click;
            btnTempDown.Click += btnTempDown_Click;
            btnCheckTemperature.Click += btnCheckTemperature_Click;

            btnStartWatering.Click += btnStartWatering_Click;
            btnStopWatering.Click += btnStopWatering_Click;

            btnMoistureUp.Click += btnMoistureUp_Click;
            btnMoistureDown.Click += btnMoistureDown_Click;

            chkAutomaticMode.CheckedChanged +=
                chkAutomaticMode_CheckedChanged;

            btnHistory.Click += btnHistory_Click;
            btnSettings.Click += btnSettings_Click;

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;

            ApplyModernDesign();
            UpdateDisplay();

            _timer.Start();
        }

        private void SaveWateringIfChanged(
            bool previousState, string mode)
        {
            if (previousState != _greenhouse.IsWatering)
            {
                _database.SaveWatering(
                    _greenhouse.IsWatering,
                    mode,
                    _greenhouse.SoilMoisture);
            }
        }

        private void UpdateDisplay()
        {
            lblTemperature.Text =
                $"Температура: {_greenhouse.Temperature:F1} °C";

            lblTemperatureStatus.Text =
                _greenhouse.CheckTemperature();

            bool temperatureWarning =
                _greenhouse.Temperature < _greenhouse.MinTemperature ||
                _greenhouse.Temperature > _greenhouse.MaxTemperature;

            lblTemperatureStatus.ForeColor = temperatureWarning
                ? Color.FromArgb(230, 130, 110)
                : Color.FromArgb(164, 187, 170);

            lblWateringStatus.Text = _greenhouse.IsWatering
                ? "Полив: включён"
                : "Полив: выключен";

            lblWateringStatus.ForeColor = _greenhouse.IsWatering
                ? Color.FromArgb(125, 210, 150)
                : Color.FromArgb(237, 244, 239);

            lblMoisture.Text =
                $"Влажность почвы: {_greenhouse.SoilMoisture:F0}%";

            lblModeStatus.Text = chkAutomaticMode.Checked
                ? "Режим: автоматический"
                : "Режим: ручной";

            btnStartWatering.Enabled =
                !chkAutomaticMode.Checked;

            btnStopWatering.Enabled =
                !chkAutomaticMode.Checked;

            UpdateMoistureIndicator();
        }

        private void UpdateMoistureIndicator()
        {
            if (_moistureBarTrack == null ||
                _moistureBarFill == null)
                return;

            double moisture = Math.Max(
                0,
                Math.Min(100, _greenhouse.SoilMoisture));

            int trackWidth =
                _moistureBarTrack.ClientSize.Width;

            _moistureBarFill.Width =
                (int)Math.Round(trackWidth * moisture / 100.0);

            if (moisture <= 30)
            {
                _moistureBarFill.BackColor =
                    Color.FromArgb(205, 82, 73);
            }
            else if (moisture <= 60)
            {
                _moistureBarFill.BackColor =
                    Color.FromArgb(219, 171, 77);
            }
            else
            {
                _moistureBarFill.BackColor =
                    Color.FromArgb(75, 165, 105);
            }
        }

        private void btnTempUp_Click(
            object? sender, EventArgs e)
        {
            _greenhouse.SetTemperature(
                _greenhouse.Temperature + 1);

            _database.SaveTemperature(
                _greenhouse.Temperature);

            UpdateDisplay();
        }

        private void btnTempDown_Click(
            object? sender, EventArgs e)
        {
            _greenhouse.SetTemperature(
                _greenhouse.Temperature - 1);

            _database.SaveTemperature(
                _greenhouse.Temperature);

            UpdateDisplay();
        }

        private void btnCheckTemperature_Click(
            object? sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void btnStartWatering_Click(
            object? sender, EventArgs e)
        {
            bool previousState = _greenhouse.IsWatering;

            _greenhouse.StartWatering();

            SaveWateringIfChanged(previousState, "Ручной");

            UpdateDisplay();
        }

        private void btnStopWatering_Click(
            object? sender, EventArgs e)
        {
            bool previousState = _greenhouse.IsWatering;

            _greenhouse.StopWatering();

            SaveWateringIfChanged(previousState, "Ручной");

            UpdateDisplay();
        }

        private void btnMoistureUp_Click(
            object? sender, EventArgs e)
        {
            _greenhouse.SetSoilMoisture(
                _greenhouse.SoilMoisture + 5);

            UpdateDisplay();
        }

        private void btnMoistureDown_Click(
            object? sender, EventArgs e)
        {
            _greenhouse.SetSoilMoisture(
                _greenhouse.SoilMoisture - 5);

            UpdateDisplay();
        }

        private void chkAutomaticMode_CheckedChanged(
            object? sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void Timer_Tick(
            object? sender, EventArgs e)
        {
            bool previousState = _greenhouse.IsWatering;

            if (chkAutomaticMode.Checked)
            {
                if (_greenhouse.SoilMoisture <= 30 &&
                    !_greenhouse.IsWatering)
                {
                    _greenhouse.StartWatering();
                }
                else if (_greenhouse.SoilMoisture >= 60 &&
                         _greenhouse.IsWatering)
                {
                    _greenhouse.StopWatering();
                }
            }

            SaveWateringIfChanged(
                previousState,
                chkAutomaticMode.Checked
                    ? "Автоматический"
                    : "Ручной");

            if (_greenhouse.IsWatering)
            {
                _greenhouse.SetSoilMoisture(
                    _greenhouse.SoilMoisture + 3);
            }
            else
            {
                _greenhouse.SetSoilMoisture(
                    _greenhouse.SoilMoisture - 0.5);
            }

            UpdateDisplay();
        }

        private void btnHistory_Click(
            object? sender, EventArgs e)
        {
            using var historyForm =
                new HistoryForm(_database);

            historyForm.ShowDialog(this);
        }

        private void btnSettings_Click(
            object? sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm(
                _greenhouse.MinTemperature,
                _greenhouse.MaxTemperature);

            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                _greenhouse.MinTemperature =
                    settingsForm.MinimumTemperature;

                _greenhouse.MaxTemperature =
                    settingsForm.MaximumTemperature;

                _database.SaveSettings(
                    _greenhouse.MinTemperature,
                    _greenhouse.MaxTemperature);

                UpdateDisplay();

                MessageBox.Show(
                    "Настройки успешно сохранены!",
                    "Greenhouse Control",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void Form1_Load(
            object sender, EventArgs e)
        {
        }

        private void ApplyModernDesign()
        {
            SuspendLayout();

            Text = "Greenhouse Control | Smart Greenhouse";

            ClientSize = new Size(1120, 760);
            MinimumSize = new Size(1120, 760);
            MaximumSize = new Size(1120, 760);

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            BackColor = Color.FromArgb(12, 22, 17);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10);

            lblTitle.Visible = false;

            Color sidebarColor = Color.FromArgb(15, 32, 24);
            Color cardColor = Color.FromArgb(25, 43, 34);
            Color green = Color.FromArgb(49, 126, 82);
            Color darkGreen = Color.FromArgb(39, 65, 51);
            Color red = Color.FromArgb(139, 65, 59);
            Color white = Color.FromArgb(237, 244, 239);
            Color muted = Color.FromArgb(164, 187, 170);

            Panel sidebar = new Panel
            {
                Name = "Sidebar",
                Location = new Point(0, 0),
                Size = new Size(220, 760),
                BackColor = sidebarColor
            };

            Controls.Add(sidebar);
            sidebar.SendToBack();

            Label logo = CreateUiLabel(
                sidebar,
                "GH",
                new Rectangle(25, 32, 43, 43),
                16,
                white,
                true);

            logo.TextAlign = ContentAlignment.MiddleCenter;
            logo.BackColor = green;

            CreateUiLabel(
                sidebar,
                "GREENHOUSE",
                new Rectangle(80, 32, 125, 23),
                12,
                white,
                true);

            CreateUiLabel(
                sidebar,
                "CONTROL SYSTEM",
                new Rectangle(81, 55, 125, 18),
                8,
                muted,
                false);

            Panel divider = new Panel
            {
                Location = new Point(22, 105),
                Size = new Size(176, 1),
                BackColor = Color.FromArgb(48, 69, 55)
            };

            sidebar.Controls.Add(divider);

            CreateUiLabel(
                sidebar,
                "НАВИГАЦИЯ",
                new Rectangle(25, 145, 170, 25),
                9,
                muted,
                true);

            btnHistory.Text = "История данных";

            PlaceUiButton(
                btnHistory,
                sidebar,
                new Rectangle(18, 190, 184, 48),
                darkGreen,
                Color.FromArgb(55, 91, 68));

            btnHistory.TextAlign = ContentAlignment.MiddleLeft;
            btnHistory.Padding = new Padding(17, 0, 0, 0);

            btnSettings.Text = "Настройки системы";

            PlaceUiButton(
                btnSettings,
                sidebar,
                new Rectangle(18, 250, 184, 48),
                darkGreen,
                Color.FromArgb(55, 91, 68));

            btnSettings.TextAlign = ContentAlignment.MiddleLeft;
            btnSettings.Padding = new Padding(17, 0, 0, 0);

            CreateUiLabel(
                sidebar,
                "РЕЖИМ РАБОТЫ",
                new Rectangle(25, 650, 170, 22),
                8,
                muted,
                true);

            CreateUiLabel(
                sidebar,
                "Симуляция датчиков",
                new Rectangle(25, 678, 180, 25),
                10,
                white,
                false);

            CreateUiLabel(
                this,
                "Панель управления",
                new Rectangle(250, 30, 600, 45),
                25,
                white,
                true);

            CreateUiLabel(
                this,
                "Мониторинг температуры и управление поливом",
                new Rectangle(252, 76, 600, 27),
                10,
                muted,
                false);

            CreateUiLabel(
                this,
                "●  СИСТЕМА ГОТОВА",
                new Rectangle(870, 43, 220, 32),
                9,
                Color.FromArgb(133, 210, 157),
                true);

            Panel temperatureCard = CreateUiCard(
                "TemperatureCard",
                new Rectangle(250, 135, 390, 500),
                cardColor);

            Panel wateringCard = CreateUiCard(
                "WateringCard",
                new Rectangle(665, 135, 425, 500),
                cardColor);

            CreateUiLabel(
                temperatureCard,
                "ТЕМПЕРАТУРА ВОЗДУХА",
                new Rectangle(24, 22, 340, 25),
                9,
                muted,
                true);

            PlaceUiLabel(
                lblTemperature,
                temperatureCard,
                new Rectangle(24, 65, 342, 55),
                23,
                white,
                true);

            PlaceUiLabel(
                lblTemperatureStatus,
                temperatureCard,
                new Rectangle(24, 125, 342, 42),
                11,
                muted,
                false);

            PlaceUiButton(
                btnTempDown,
                temperatureCard,
                new Rectangle(24, 210, 160, 46),
                darkGreen,
                Color.FromArgb(56, 91, 69));

            btnTempDown.Text = "−  Уменьшить";

            PlaceUiButton(
                btnTempUp,
                temperatureCard,
                new Rectangle(205, 210, 160, 46),
                green,
                Color.FromArgb(65, 148, 97));

            btnTempUp.Text = "+  Увеличить";

            PlaceUiButton(
                btnCheckTemperature,
                temperatureCard,
                new Rectangle(24, 275, 341, 45),
                darkGreen,
                Color.FromArgb(56, 91, 69));

            btnCheckTemperature.Text = "Проверить температуру";

            CreateUiLabel(
                temperatureCard,
                "Контроль микроклимата теплицы",
                new Rectangle(24, 365, 342, 25),
                9,
                muted,
                false);

            CreateUiLabel(
                wateringCard,
                "СИСТЕМА ОРОШЕНИЯ",
                new Rectangle(24, 22, 370, 25),
                9,
                muted,
                true);

            PlaceUiLabel(
                lblWateringStatus,
                wateringCard,
                new Rectangle(24, 60, 377, 43),
                19,
                white,
                true);

            PlaceUiButton(
                btnStartWatering,
                wateringCard,
                new Rectangle(24, 120, 178, 47),
                green,
                Color.FromArgb(65, 148, 97));

            btnStartWatering.Text = "Включить полив";

            PlaceUiButton(
                btnStopWatering,
                wateringCard,
                new Rectangle(215, 120, 185, 47),
                red,
                Color.FromArgb(165, 79, 72));

            btnStopWatering.Text = "Выключить полив";

            CreateUiLabel(
                wateringCard,
                "ВЛАЖНОСТЬ ПОЧВЫ",
                new Rectangle(24, 200, 370, 23),
                9,
                muted,
                true);

            PlaceUiLabel(
                lblMoisture,
                wateringCard,
                new Rectangle(24, 230, 377, 40),
                19,
                white,
                true);

            PlaceUiLabel(
                lblModeStatus,
                wateringCard,
                new Rectangle(24, 272, 377, 27),
                10,
                muted,
                false);

            PlaceUiButton(
                btnMoistureUp,
                wateringCard,
                new Rectangle(24, 320, 178, 45),
                green,
                Color.FromArgb(65, 148, 97));

            btnMoistureUp.Text = "+  Влажность";

            PlaceUiButton(
                btnMoistureDown,
                wateringCard,
                new Rectangle(215, 320, 185, 45),
                darkGreen,
                Color.FromArgb(56, 91, 69));

            btnMoistureDown.Text = "−  Влажность";

            wateringCard.Controls.Add(chkAutomaticMode);

            chkAutomaticMode.Location = new Point(24, 390);
            chkAutomaticMode.Size = new Size(280, 35);
            chkAutomaticMode.AutoSize = false;
            chkAutomaticMode.BackColor = Color.Transparent;
            chkAutomaticMode.ForeColor = white;
            chkAutomaticMode.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);
            chkAutomaticMode.Cursor = Cursors.Hand;

            CreateUiLabel(
                wateringCard,
                "УРОВЕНЬ ВЛАЖНОСТИ",
                new Rectangle(24, 435, 370, 20),
                8,
                muted,
                true);

            _moistureBarTrack = new Panel
            {
                Location = new Point(24, 465),
                Size = new Size(376, 12),
                BackColor = Color.FromArgb(42, 61, 49)
            };

            _moistureBarFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(0, 12),
                BackColor = Color.FromArgb(75, 165, 105)
            };

            _moistureBarTrack.Controls.Add(_moistureBarFill);
            wateringCard.Controls.Add(_moistureBarTrack);

            CreateUiLabel(
                this,
                "GREENHOUSE CONTROL  •  SMART MONITORING",
                new Rectangle(250, 665, 600, 25),
                9,
                muted,
                false);

            ResumeLayout(true);
        }

        private Panel CreateUiCard(
            string name,
            Rectangle bounds,
            Color background)
        {
            Panel card = new Panel
            {
                Name = name,
                Location = bounds.Location,
                Size = bounds.Size,
                BackColor = background
            };

            card.Paint += (sender, e) =>
            {
                using Pen pen = new Pen(
                    Color.FromArgb(49, 72, 57));

                e.Graphics.DrawRectangle(
                    pen,
                    0,
                    0,
                    card.Width - 1,
                    card.Height - 1);
            };

            Controls.Add(card);
            card.SendToBack();

            Panel accent = new Panel
            {
                Location = new Point(24, 0),
                Size = new Size(55, 3),
                BackColor = Color.FromArgb(89, 166, 112)
            };

            card.Controls.Add(accent);

            return card;
        }

        private Label CreateUiLabel(
            Control parent,
            string text,
            Rectangle bounds,
            float fontSize,
            Color color,
            bool bold)
        {
            Label label = new Label
            {
                Text = text,
                Location = bounds.Location,
                Size = bounds.Size,
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = color,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(
                    "Segoe UI",
                    fontSize,
                    bold ? FontStyle.Bold : FontStyle.Regular)
            };

            parent.Controls.Add(label);

            return label;
        }

        private void PlaceUiLabel(
            Label label,
            Control parent,
            Rectangle bounds,
            float fontSize,
            Color color,
            bool bold)
        {
            parent.Controls.Add(label);

            label.Location = bounds.Location;
            label.Size = bounds.Size;
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.ForeColor = color;
            label.TextAlign = ContentAlignment.MiddleLeft;

            label.Font = new Font(
                "Segoe UI",
                fontSize,
                bold ? FontStyle.Bold : FontStyle.Regular);
        }

        private void PlaceUiButton(
            Button button,
            Control parent,
            Rectangle bounds,
            Color background,
            Color hover)
        {
            parent.Controls.Add(button);

            button.Location = bounds.Location;
            button.Size = bounds.Size;

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hover;
            button.FlatAppearance.MouseDownBackColor =
                Color.FromArgb(35, 91, 60);

            button.BackColor = background;
            button.ForeColor = Color.White;

            button.Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold);

            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
        }
    }
}
