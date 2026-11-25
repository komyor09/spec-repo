
using System;
using System.Windows.Forms;

namespace GreenhouseControl
{
    public class SettingsForm : Form
    {
        private readonly NumericUpDown _minTemperature;
        private readonly NumericUpDown _maxTemperature;

        public double MinimumTemperature =>
            (double)_minTemperature.Value;

        public double MaximumTemperature =>
            (double)_maxTemperature.Value;

        public SettingsForm(
            double minTemperature,
            double maxTemperature)
        {
            Text = "Настройки теплицы";
            Width = 440;
            Height = 260;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var minLabel = new Label
            {
                Text = "Минимальная температура (°C):",
                Left = 20,
                Top = 30,
                Width = 230
            };

            var maxLabel = new Label
            {
                Text = "Максимальная температура (°C):",
                Left = 20,
                Top = 80,
                Width = 230
            };

            _minTemperature = new NumericUpDown
            {
                Left = 270,
                Top = 25,
                Width = 120,
                Minimum = -50,
                Maximum = 100,
                DecimalPlaces = 1,
                Value = (decimal)minTemperature
            };

            _maxTemperature = new NumericUpDown
            {
                Left = 270,
                Top = 75,
                Width = 120,
                Minimum = -50,
                Maximum = 100,
                DecimalPlaces = 1,
                Value = (decimal)maxTemperature
            };

            var saveButton = new Button
            {
                Text = "Сохранить",
                Left = 190,
                Top = 150,
                Width = 100,
                Height = 35
            };

            var cancelButton = new Button
            {
                Text = "Отмена",
                Left = 300,
                Top = 150,
                Width = 90,
                Height = 35
            };

            saveButton.Click += (_, _) =>
            {
                if (_minTemperature.Value >=
                    _maxTemperature.Value)
                {
                    MessageBox.Show(
                        this,
                        "Минимальная температура должна быть " +
                        "ниже максимальной.",
                        "Проверьте настройки",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };

            cancelButton.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(minLabel);
            Controls.Add(maxLabel);
            Controls.Add(_minTemperature);
            Controls.Add(_maxTemperature);
            Controls.Add(saveButton);
            Controls.Add(cancelButton);

            AcceptButton = saveButton;
            CancelButton = cancelButton;
        }
    }
}
