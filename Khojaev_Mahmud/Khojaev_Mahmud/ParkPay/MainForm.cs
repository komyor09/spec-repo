using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaidParking;

public class MainForm : Form
{
    // ---------- Цвета оформления (светлая тема, «парковочный» сине-жёлтый стиль) ----------
    private static readonly Color Navy = ColorTranslator.FromHtml("#1F2A44");
    private static readonly Color Amber = ColorTranslator.FromHtml("#F5B301");
    private static readonly Color Blue = ColorTranslator.FromHtml("#2F6FED");
    private static readonly Color PageBack = ColorTranslator.FromHtml("#F3F5F9");
    private static readonly Color Line = ColorTranslator.FromHtml("#DDE2EC");
    private static readonly Color Muted = ColorTranslator.FromHtml("#6B778C");
    private static readonly Color Green = ColorTranslator.FromHtml("#1F9D63");
    private static readonly Color Red = ColorTranslator.FromHtml("#D64545");
    private static readonly Color Track = ColorTranslator.FromHtml("#E6EAF2");
    private static readonly Color HeaderMuted = ColorTranslator.FromHtml("#AAB4CC");

    private readonly Client client;

    // Заголовок
    private Label lblClientName;
    private Label lblDebt;

    // Списки
    private ListView lvSessions;
    private ListView lvPayments;

    // Чек
    private Label lblTicketNo;
    private Label lblPlate;
    private Label lblTimes;
    private Label lblDuration;
    private Label lblTicketCost;
    private Label lblBalance;
    private Label lblPercent;
    private Label lblPill;
    private Label lblMessage;
    private Panel pnlTrack;
    private Panel pnlFill;
    private NumericUpDown nudAmount;
    private ComboBox cmbMethod;
    private Button btnPay;
    private Button btnPayAll;

    // Новая сессия
    private TextBox txtPlate;
    private DateTimePicker dtpEntry;
    private DateTimePicker dtpExit;
    private Label lblPreview;

    public MainForm(Client client)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        this.client = client;
        BuildUi();
        LoadSessions(0);
        UpdatePreview();
    }

    // ---------- Вспомогательные методы ----------
    private static Label MakeLabel(string text, float size, FontStyle style, Color color,
        int x, int y, int w, int h)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = Color.Transparent,
            Location = new Point(x, y),
            Size = new Size(w, h),
            AutoSize = false
        };
    }

    private static Label MakeSection(string text, int x, int y)
    {
        return MakeLabel(text, 8.5f, FontStyle.Bold, Muted, x, y, 420, 20);
    }

    private static Button MakeButton(string text, Color back, Color fore, int x, int y, int w, int h, bool outline)
    {
        Button button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = back,
            ForeColor = fore,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        if (outline)
        {
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Line;
            button.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#EEF1F7");
        }
        else
        {
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.25f);
        }

        return button;
    }

    private static Panel MakeCard(int x, int y, int w, int h, Color accent, int accentWidth)
    {
        Panel card = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.White
        };
        card.Paint += (s, e) =>
        {
            using (Pen pen = new Pen(Line))
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        card.Controls.Add(new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(accentWidth, 5),
            BackColor = accent
        });
        return card;
    }

    private static ListView MakeList(int x, int y, int w, int h)
    {
        return new ListView
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HideSelection = false,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            ForeColor = Navy,
            Font = new Font("Segoe UI", 9.5f)
        };
    }

    private static Panel MakeLine(int x, int y, int w)
    {
        return new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, 1),
            BackColor = Line
        };
    }

    // ---------- Построение интерфейса ----------
    private void BuildUi()
    {
        Text = "ParkPay — платная парковка";
        ClientSize = new Size(1180, 720);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = PageBack;
        Font = new Font("Segoe UI", 9f);
        DoubleBuffered = true;

        // ----- Верхняя панель -----
        Panel header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(1180, 76),
            BackColor = Navy
        };

        Label badge = MakeLabel("P", 20f, FontStyle.Bold, Navy, 24, 16, 44, 44);
        badge.BackColor = Amber;
        badge.TextAlign = ContentAlignment.MiddleCenter;
        header.Controls.Add(badge);
        header.Controls.Add(MakeLabel("Платная парковка", 16f, FontStyle.Bold, Color.White, 80, 10, 360, 32));
        header.Controls.Add(MakeLabel("Учёт парковочных сессий и платежей", 9.5f,
            FontStyle.Regular, HeaderMuted, 82, 42, 360, 22));

        lblClientName = MakeLabel("", 11f, FontStyle.Bold, Color.White, 480, 14, 520, 26);
        lblClientName.TextAlign = ContentAlignment.MiddleRight;
        lblDebt = MakeLabel("", 10f, FontStyle.Regular, Amber, 480, 42, 520, 22);
        lblDebt.TextAlign = ContentAlignment.MiddleRight;
        header.Controls.Add(lblClientName);
        header.Controls.Add(lblDebt);

        Button btnReport = MakeButton("Отчёт клиента", Amber, Navy, 1024, 18, 132, 40, false);
        btnReport.Click += ReportClick;
        header.Controls.Add(btnReport);

        // ----- Левая часть: сессии -----
        Label secSessions = MakeSection("ПАРКОВОЧНЫЕ СЕССИИ", 24, 96);

        lvSessions = MakeList(24, 120, 700, 190);
        lvSessions.Columns.Add("№", 60);
        lvSessions.Columns.Add("Авто", 100);
        lvSessions.Columns.Add("Въезд", 125);
        lvSessions.Columns.Add("Выезд", 125);
        lvSessions.Columns.Add("Стоимость", 85);
        lvSessions.Columns.Add("Остаток", 85);
        lvSessions.Columns.Add("Статус", 100);
        lvSessions.SelectedIndexChanged += (s, e) => ShowDetails();

        Label secPayments = MakeSection("ПЛАТЕЖИ ПО ВЫБРАННОЙ СЕССИИ", 24, 326);

        lvPayments = MakeList(24, 350, 700, 170);
        lvPayments.Columns.Add("Дата и время", 190);
        lvPayments.Columns.Add("Сумма", 150);
        lvPayments.Columns.Add("Способ оплаты", 320);

        // ----- Карточка «Новая сессия» -----
        Panel cardNew = MakeCard(24, 536, 700, 160, Blue, 60);
        cardNew.Controls.Add(MakeSection("НОВАЯ СЕССИЯ", 20, 18));

        cardNew.Controls.Add(MakeLabel("Номер автомобиля", 8.5f, FontStyle.Regular, Muted, 20, 44, 170, 18));
        txtPlate = new TextBox
        {
            Location = new Point(20, 66),
            Size = new Size(170, 28),
            Font = new Font("Segoe UI", 11f),
            ForeColor = Navy,
            PlaceholderText = "А123ВС77"
        };
        cardNew.Controls.Add(txtPlate);

        DateTime now = DateTime.Now;
        DateTime start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        cardNew.Controls.Add(MakeLabel("Время въезда", 8.5f, FontStyle.Regular, Muted, 206, 44, 200, 18));
        dtpEntry = new DateTimePicker
        {
            Location = new Point(206, 66),
            Size = new Size(200, 28),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd.MM.yyyy HH:mm",
            Font = new Font("Segoe UI", 10.5f),
            Value = start.AddHours(-2)
        };
        dtpEntry.ValueChanged += (s, e) => UpdatePreview();
        cardNew.Controls.Add(dtpEntry);

        cardNew.Controls.Add(MakeLabel("Время выезда", 8.5f, FontStyle.Regular, Muted, 420, 44, 200, 18));
        dtpExit = new DateTimePicker
        {
            Location = new Point(420, 66),
            Size = new Size(200, 28),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd.MM.yyyy HH:mm",
            Font = new Font("Segoe UI", 10.5f),
            Value = start
        };
        dtpExit.ValueChanged += (s, e) => UpdatePreview();
        cardNew.Controls.Add(dtpExit);

        lblPreview = MakeLabel("", 10.5f, FontStyle.Bold, Navy, 20, 108, 440, 26);
        cardNew.Controls.Add(lblPreview);

        Button btnCreate = MakeButton("Создать сессию", Blue, Color.White, 480, 104, 200, 38, false);
        btnCreate.Click += CreateClick;
        cardNew.Controls.Add(btnCreate);

        cardNew.Controls.Add(MakeLabel(
            $"Тариф: {Tariff.PricePerHour:N0} руб./час, первые {Tariff.FreeMinutes} минут бесплатно, максимум {Tariff.MaxPerDay:N0} руб./сутки",
            8.5f, FontStyle.Regular, Muted, 20, 136, 660, 18));

        // ----- Правая часть: чек парковки -----
        Panel ticket = MakeCard(748, 96, 408, 600, Amber, 408);

        ticket.Controls.Add(MakeSection("ЧЕК ПАРКОВКИ", 24, 22));
        lblTicketNo = MakeLabel("", 20f, FontStyle.Bold, Navy, 24, 44, 360, 36);
        ticket.Controls.Add(lblTicketNo);

        lblPlate = MakeLabel("", 16f, FontStyle.Bold, Navy, 24, 90, 180, 40);
        lblPlate.BackColor = Color.White;
        lblPlate.BorderStyle = BorderStyle.FixedSingle;
        lblPlate.TextAlign = ContentAlignment.MiddleCenter;
        ticket.Controls.Add(lblPlate);

        ticket.Controls.Add(MakeLine(24, 146, 360));

        lblTimes = MakeLabel("", 10f, FontStyle.Regular, Navy, 24, 158, 360, 44);
        lblDuration = MakeLabel("", 9.5f, FontStyle.Regular, Muted, 24, 206, 360, 22);
        lblTicketCost = MakeLabel("", 11f, FontStyle.Bold, Navy, 24, 236, 360, 26);
        ticket.Controls.Add(lblTimes);
        ticket.Controls.Add(lblDuration);
        ticket.Controls.Add(lblTicketCost);

        ticket.Controls.Add(MakeSection("К ОПЛАТЕ", 24, 278));
        lblBalance = MakeLabel("", 28f, FontStyle.Bold, Red, 24, 298, 360, 52);
        ticket.Controls.Add(lblBalance);

        ticket.Controls.Add(MakeSection("ОПЛАЧЕНО", 24, 358));
        lblPercent = MakeLabel("", 9f, FontStyle.Bold, Navy, 284, 358, 100, 18);
        lblPercent.TextAlign = ContentAlignment.MiddleRight;
        ticket.Controls.Add(lblPercent);

        pnlTrack = new Panel
        {
            Location = new Point(24, 382),
            Size = new Size(360, 14),
            BackColor = Track
        };
        pnlFill = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(0, 14),
            BackColor = Green
        };
        pnlTrack.Controls.Add(pnlFill);
        ticket.Controls.Add(pnlTrack);

        lblPill = MakeLabel("", 9f, FontStyle.Bold, Color.White, 24, 410, 150, 28);
        lblPill.TextAlign = ContentAlignment.MiddleCenter;
        ticket.Controls.Add(lblPill);

        lblMessage = MakeLabel("", 9f, FontStyle.Regular, Muted, 184, 410, 200, 28);
        lblMessage.TextAlign = ContentAlignment.MiddleRight;
        ticket.Controls.Add(lblMessage);

        ticket.Controls.Add(MakeLine(24, 454, 360));
        ticket.Controls.Add(MakeSection("ВНЕСТИ ПЛАТЁЖ", 24, 468));

        nudAmount = new NumericUpDown
        {
            Location = new Point(24, 494),
            Size = new Size(170, 32),
            DecimalPlaces = 2,
            Minimum = 0.01m,
            Maximum = 1000000m,
            Value = 50m,
            Font = new Font("Segoe UI", 11f),
            ForeColor = Navy
        };
        ticket.Controls.Add(nudAmount);

        cmbMethod = new ComboBox
        {
            Location = new Point(206, 494),
            Size = new Size(178, 32),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11f),
            ForeColor = Navy
        };
        cmbMethod.Items.AddRange(new object[] { "наличные", "банковская карта", "приложение" });
        cmbMethod.SelectedIndex = 1;
        ticket.Controls.Add(cmbMethod);

        btnPay = MakeButton("Оплатить", Blue, Color.White, 24, 544, 172, 44, false);
        btnPay.Click += PayClick;
        btnPayAll = MakeButton("Оплатить остаток", Color.White, Navy, 212, 544, 172, 44, true);
        btnPayAll.Click += PayAllClick;
        ticket.Controls.Add(btnPay);
        ticket.Controls.Add(btnPayAll);

        Controls.Add(header);
        Controls.Add(secSessions);
        Controls.Add(lvSessions);
        Controls.Add(secPayments);
        Controls.Add(lvPayments);
        Controls.Add(cardNew);
        Controls.Add(ticket);
    }

    // ---------- Данные на экране ----------
    private ParkingSession Selected
    {
        get
        {
            if (lvSessions.SelectedItems.Count == 0)
                return null;
            return lvSessions.SelectedItems[0].Tag as ParkingSession;
        }
    }

    private int SelectedIndex
    {
        get
        {
            return lvSessions.SelectedIndices.Count > 0 ? lvSessions.SelectedIndices[0] : -1;
        }
    }

    private void SetMessage(string text, Color color)
    {
        lblMessage.Text = text;
        lblMessage.ForeColor = color;
    }

    private void UpdateHeader()
    {
        lblClientName.Text = $"Клиент №{client.Id}  ·  {client.FullName}";
        lblDebt.Text = $"Общий остаток к оплате: {client.TotalBalance:N2} руб.";
    }

    private void LoadSessions(int selectIndex)
    {
        lvSessions.BeginUpdate();
        lvSessions.Items.Clear();

        foreach (ParkingSession session in client.Sessions)
        {
            ListViewItem item = new ListViewItem(session.Id.ToString());
            item.SubItems.Add(session.PlateNumber);
            item.SubItems.Add(session.EntryTime.ToString("dd.MM.yyyy HH:mm"));
            item.SubItems.Add(session.ExitTime.ToString("dd.MM.yyyy HH:mm"));
            item.SubItems.Add(session.Cost.ToString("N2"));
            item.SubItems.Add(session.Balance.ToString("N2"));
            item.SubItems.Add(session.Status);
            item.ForeColor = session.Balance == 0 ? Green : Red;
            item.Tag = session;
            lvSessions.Items.Add(item);
        }

        lvSessions.EndUpdate();
        UpdateHeader();

        if (lvSessions.Items.Count > 0)
        {
            int index = Math.Max(0, Math.Min(selectIndex, lvSessions.Items.Count - 1));
            lvSessions.Items[index].Selected = true;
        }
        else
        {
            ShowDetails();
        }
    }

    private void ShowDetails()
    {
        ParkingSession session = Selected;
        lvPayments.Items.Clear();

        if (session == null)
        {
            lblTicketNo.Text = "—";
            lblPlate.Text = "";
            lblTimes.Text = "";
            lblDuration.Text = "";
            lblTicketCost.Text = "";
            lblBalance.Text = "";
            lblPercent.Text = "";
            lblPill.Text = "";
            lblPill.BackColor = Color.Transparent;
            pnlFill.Width = 0;
            btnPay.Enabled = false;
            btnPayAll.Enabled = false;
            return;
        }

        lblTicketNo.Text = $"Сессия №{session.Id}";
        lblPlate.Text = session.PlateNumber;
        lblTimes.Text =
            $"Въезд:   {session.EntryTime:dd.MM.yyyy HH:mm}\n" +
            $"Выезд:  {session.ExitTime:dd.MM.yyyy HH:mm}";

        TimeSpan duration = session.ExitTime - session.EntryTime;
        lblDuration.Text = $"Время на парковке: {(int)duration.TotalHours} ч {duration.Minutes} мин";
        lblTicketCost.Text = $"Стоимость по тарифу: {session.Cost:N2} руб.";

        lblBalance.Text = $"{session.Balance:N2} руб.";
        lblBalance.ForeColor = session.Balance == 0 ? Green : Red;

        decimal paid = session.Cost - session.Balance;
        double ratio = (double)(paid / session.Cost);
        pnlFill.Width = (int)Math.Round(pnlTrack.Width * ratio);
        pnlFill.BackColor = session.Balance == 0 ? Green : Amber;
        lblPercent.Text = $"{ratio:P0}";

        lblPill.Text = session.Balance == 0 ? "ОПЛАЧЕНА" : "НЕ ОПЛАЧЕНА";
        lblPill.BackColor = session.Balance == 0 ? Green : Red;

        foreach (Payment payment in session.Payments)
        {
            ListViewItem item = new ListViewItem(payment.PaidAt.ToString("dd.MM.yyyy HH:mm"));
            item.SubItems.Add(payment.Amount.ToString("N2") + " руб.");
            item.SubItems.Add(payment.Method);
            lvPayments.Items.Add(item);
        }

        bool canPay = session.Balance > 0;
        btnPay.Enabled = canPay;
        btnPayAll.Enabled = canPay;
        if (canPay)
        {
            nudAmount.Maximum = session.Balance;
            nudAmount.Value = Math.Min(nudAmount.Value, session.Balance);
        }
    }

    private void UpdatePreview()
    {
        if (dtpExit.Value <= dtpEntry.Value)
        {
            lblPreview.Text = "Время выезда должно быть позже въезда";
            lblPreview.ForeColor = Red;
            return;
        }

        decimal cost = Tariff.Calculate(dtpEntry.Value, dtpExit.Value);
        lblPreview.ForeColor = Navy;
        lblPreview.Text = cost == 0
            ? $"Стоимость: бесплатно (до {Tariff.FreeMinutes} минут)"
            : $"Стоимость по тарифу: {cost:N2} руб.";
    }

    private int NextSessionId()
    {
        int max = 1000;
        foreach (ParkingSession session in client.Sessions)
        {
            if (session.Id > max)
                max = session.Id;
        }
        return max + 1;
    }

    // ---------- Обработчики ----------
    private void MakePayment(decimal amount)
    {
        ParkingSession session = Selected;
        if (session == null)
            return;

        int index = SelectedIndex;

        try
        {
            session.Pay(amount, cmbMethod.SelectedItem.ToString(), DateTime.Now);
            SetMessage("Платёж принят", Green);
        }
        catch (Exception ex)
        {
            SetMessage("Ошибка платежа", Red);
            MessageBox.Show(ex.Message, "Ошибка платежа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        LoadSessions(index);
    }

    private void PayClick(object sender, EventArgs e)
    {
        MakePayment(nudAmount.Value);
    }

    private void PayAllClick(object sender, EventArgs e)
    {
        ParkingSession session = Selected;
        if (session != null)
            MakePayment(session.Balance);
    }

    private void CreateClick(object sender, EventArgs e)
    {
        try
        {
            if (dtpExit.Value <= dtpEntry.Value)
                throw new ArgumentException("Время выезда должно быть позже времени въезда.");

            decimal cost = Tariff.Calculate(dtpEntry.Value, dtpExit.Value);
            if (cost <= 0)
                throw new ArgumentException(
                    $"Стоянка до {Tariff.FreeMinutes} минут бесплатна — сессия не создаётся.");

            ParkingSession session = new ParkingSession(
                NextSessionId(), txtPlate.Text.Trim().ToUpper(), dtpEntry.Value, dtpExit.Value, cost);

            client.AddSession(session);
            txtPlate.Clear();
            LoadSessions(client.Sessions.Count - 1);
            SetMessage("Сессия создана", Green);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Новая сессия", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ReportClick(object sender, EventArgs e)
    {
        // Вывод информации через переопределённый ToString()
        MessageBox.Show(client.ToString(), "Отчёт клиента", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
