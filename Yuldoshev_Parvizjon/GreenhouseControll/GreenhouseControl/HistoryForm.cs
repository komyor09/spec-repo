
using System;
using System.Windows.Forms;

namespace GreenhouseControl
{
    public class HistoryForm : Form
    {
        private readonly DatabaseHelper _database;
        private readonly DataGridView _temperatureGrid;
        private readonly DataGridView _wateringGrid;

        public HistoryForm(DatabaseHelper database)
        {
            _database = database;

            Text = "История работы теплицы";
            Width = 900;
            Height = 550;
            StartPosition = FormStartPosition.CenterParent;

            _temperatureGrid = CreateGrid();
            _wateringGrid = CreateGrid();

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill
            };

            var temperaturePage = new TabPage("Температура");
            var wateringPage = new TabPage("Полив");

            temperaturePage.Controls.Add(
                CreateHistoryPage(
                    _temperatureGrid,
                    LoadTemperatureHistory));

            wateringPage.Controls.Add(
                CreateHistoryPage(
                    _wateringGrid,
                    LoadWateringHistory));

            tabs.TabPages.Add(temperaturePage);
            tabs.TabPages.Add(wateringPage);

            Controls.Add(tabs);

            LoadTemperatureHistory();
            LoadWateringHistory();
        }

        private DataGridView CreateGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false
            };
        }

        private Panel CreateHistoryPage(
            DataGridView grid, Action refreshAction)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var refreshButton = new Button
            {
                Text = "Обновить данные",
                Dock = DockStyle.Top,
                Height = 38
            };

            refreshButton.Click +=
                (_, _) => refreshAction();

            panel.Controls.Add(grid);
            panel.Controls.Add(refreshButton);

            return panel;
        }

        private void LoadTemperatureHistory()
        {
            try
            {
                _temperatureGrid.DataSource =
                    _database.GetTemperatureHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить историю температуры.\n"
                    + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadWateringHistory()
        {
            try
            {
                _wateringGrid.DataSource =
                    _database.GetWateringHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить историю полива.\n"
                    + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
