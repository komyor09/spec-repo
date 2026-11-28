using System.Data;
using RepairWorkshop.Data;
using RepairWorkshop.Models;
using RepairWorkshop.Services;
namespace RepairWorkshop;
public sealed class MainForm : Form
{
    private readonly Database db = new();
    private readonly WorkshopService service;
    private readonly TabControl tabs=new(){Dock=DockStyle.Fill};
    private readonly Dictionary<string,DataGridView> grids=new();
    private Label summary = new(){AutoSize=true,Font=new Font("Segoe UI",14,FontStyle.Bold)};
    public MainForm()
    {
        service=new WorkshopService(db);
        Text="Мастерская ремонта техники — ООП";Width=1160;Height=720;MinimumSize=new Size(900,550);
        Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(246,248,252);
        var header=new Label{Text="  МАСТЕРСКАЯ РЕМОНТА ТЕХНИКИ",ForeColor=Color.White,BackColor=Color.FromArgb(29,56,86),Font=new Font("Segoe UI",16,FontStyle.Bold),Height=58,Dock=DockStyle.Top,TextAlign=ContentAlignment.MiddleLeft};
        Controls.Add(tabs);Controls.Add(header);
        BuildDashboard();BuildCustomers();BuildDevices();BuildOrders();BuildTechnicians();BuildParts();BuildReports();BuildSettings();RefreshAll();
    }
    private TabPage Page(string name) {var page=new TabPage(name){Padding=new Padding(12),BackColor=Color.White};tabs.TabPages.Add(page);return page;}
    private FlowLayoutPanel Bar(TabPage page) {var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=50,FlowDirection=FlowDirection.LeftToRight,WrapContents=false};page.Controls.Add(bar);return bar;}
    private Button Action(FlowLayoutPanel bar,string title,Action click)
    {
        var btn=new Button{Text=title,AutoSize=true,Height=36,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(224,237,248),Margin=new Padding(4)};
        btn.Click+=(_,_)=>{try{click();RefreshAll();}catch(Exception ex){MessageBox.Show(ex.Message,"Ошибка",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
        bar.Controls.Add(btn);return btn;
    }
    private DataGridView Grid(TabPage page,string key)
    {
        // All query results in this application are displayed as text. Explicit
        // text columns prevent DataGridView from guessing an image column type.
        var grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,RowHeadersVisible=false,AutoGenerateColumns=false};
        grid.DataSourceChanged+=(_,_)=>
        {
            grid.Columns.Clear();
            if(grid.DataSource is DataTable table)
            {
                foreach(DataColumn column in table.Columns)
                    grid.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name=column.ColumnName,
                        DataPropertyName=column.ColumnName,
                        HeaderText=column.ColumnName,
                        ValueType=typeof(string)
                    });
            }
        };
        grid.DataError+=(_,e)=>
        {
            // Prevent the default DataGridView exception dialog; show null
            // for any unexpected value while preserving the real data.
            e.ThrowException=false;
        };
        page.Controls.Add(grid);grid.BringToFront();grids[key]=grid;return grid;
    }
    private static string Ask(string title,string defaultValue="")
    {
        using var f=new Form{Text=title,Width=440,Height=160,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false};
        var input=new TextBox{Left=15,Top=15,Width=390,Text=defaultValue};
        var ok=new Button{Text="Сохранить",DialogResult=DialogResult.OK,Left=180,Top=55,Width=110};
        var cancel=new Button{Text="Отмена",DialogResult=DialogResult.Cancel,Left=300,Top=55,Width=100};f.Controls.AddRange(new Control[]{input,ok,cancel});f.AcceptButton=ok;f.CancelButton=cancel;
        return f.ShowDialog()==DialogResult.OK?input.Text:throw new OperationCanceledException("Операция отменена.");
    }
    private static string[] MultiInput(string title, params string[] fields)
    {
        using var f=new Form{Text=title,Width=520,Height=Math.Min(750,fields.Length*48+110),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false};
        var boxes=new List<TextBox>();for(int i=0;i<fields.Length;i++) {f.Controls.Add(new Label{Text=fields[i],Left=16,Top=16+i*48,Width=170});var t=new TextBox{Left=190,Top=12+i*48,Width=290};f.Controls.Add(t);boxes.Add(t);}
        var ok=new Button{Text="Сохранить",Left=250,Top=fields.Length*48+17,Width=105,DialogResult=DialogResult.OK};
        var cancel=new Button{Text="Отмена",Left=365,Top=fields.Length*48+17,Width=105,DialogResult=DialogResult.Cancel};f.Controls.AddRange(new Control[]{ok,cancel});f.AcceptButton=ok;f.CancelButton=cancel;
        return f.ShowDialog()==DialogResult.OK?boxes.Select(t=>t.Text.Trim()).ToArray():throw new OperationCanceledException("Операция отменена.");
    }
    private int SelectedId(string key)
    {var g=grids[key];if(g.CurrentRow==null)throw new ArgumentException("Выберите строку таблицы.");return Convert.ToInt32(g.CurrentRow.Cells["Id"].Value);}
    private void BuildDashboard()
    {
        var p=Page("Главная");p.Controls.Add(new Label{Text="Обзор мастерской",Dock=DockStyle.Top,Height=55,Font=new Font("Segoe UI",18,FontStyle.Bold)});
        var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(25),FlowDirection=FlowDirection.TopDown};panel.Controls.Add(summary);
        panel.Controls.Add(new Label{AutoSize=true,MaximumSize=new Size(900,0),Text="Порядок работы: 1) добавьте клиента и мастера, 2) создайте заказ, 3) изменяйте его статус. Все записи сохраняются в SQLite.\n\nПринципы ООП: инкапсуляция — проверка данных в сервисах; наследование — классы устройств; полиморфизм — Describe(); абстракция — Device.",Font=new Font("Segoe UI",11)});p.Controls.Add(panel);panel.BringToFront();
    }
    private void BuildCustomers()
    {
        var p=Page("Клиенты");var bar=Bar(p);Grid(p,"Customers");
        Action(bar,"Добавить клиента",()=>{var a=MultiInput("Новый клиент","ФИО","Телефон");service.AddCustomer(a[0],a[1]);});
        Action(bar,"Изменить",()=>{int id=SelectedId("Customers");var a=MultiInput("Изменение клиента","ФИО","Телефон");if(a.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Заполните поля.");db.Execute("UPDATE Customers SET FullName=$n,Phone=$p WHERE Id=$id",("$n",a[0]),("$p",a[1]),("$id",id));});
        Action(bar,"Поиск",()=>{string term=Ask("Имя или телефон");grids["Customers"].DataSource=db.Query("SELECT * FROM Customers WHERE FullName LIKE $q OR Phone LIKE $q",("$q","%"+term+"%"));});
    }
    private void BuildDevices()
    {
        var p=Page("Техника");var bar=Bar(p);Grid(p,"Devices");
        Action(bar,"Новый заказ с техникой",()=>tabs.SelectedIndex=3);
    }
    private void BuildOrders()
    {
        var p=Page("Заказы");var bar=Bar(p);Grid(p,"Orders");
        Action(bar,"Создать заказ",()=>{
            var a=MultiInput("Новая заявка","ID клиента (из Клиенты)","Тип: Телефон/Ноутбук/Другое","Марка","Модель","Неисправность","ID мастера (можно пусто)","Стоимость, сомони");
            int? master=string.IsNullOrWhiteSpace(a[5])?null:int.Parse(a[5]);
            service.CreateOrder(int.Parse(a[0]),a[1],a[2],a[3],a[4],master,decimal.Parse(a[6]));
        });
        Action(bar,"Следующий статус",()=>{int id=SelectedId("Orders");var t=db.Query("SELECT Status FROM Orders WHERE Id=$id",("$id",id));int status=Convert.ToInt32(t.Rows[0][0]);if(status>=4)throw new InvalidOperationException("Заказ уже выдан.");service.UpdateStatus(id,(RepairStatus)(status+1));});
        Action(bar,"Изменить цену",()=>{int id=SelectedId("Orders");var cost=decimal.Parse(Ask("Новая стоимость, сомони"));if(cost<0)throw new ArgumentException("Цена должна быть неотрицательной.");db.Execute("UPDATE Orders SET Cost=$c WHERE Id=$id",("$c",(double)cost),("$id",id));});
        Action(bar,"Поиск",()=>{var q=Ask("Модель, неисправность или клиент");grids["Orders"].DataSource=db.Query("SELECT o.Id,c.FullName AS Клиент,o.DeviceType AS Тип,o.Brand AS Марка,o.Model AS Модель,o.Fault AS Неисправность,o.Status AS Статус,o.Cost AS Стоимость,o.CreatedAt AS Дата FROM Orders o JOIN Customers c ON c.Id=o.CustomerId WHERE c.FullName LIKE $q OR o.Model LIKE $q OR o.Fault LIKE $q",("$q","%"+q+"%"));});
    }
    private void BuildTechnicians()
    {
        var p=Page("Мастера");var bar=Bar(p);Grid(p,"Technicians");
        Action(bar,"Добавить мастера",()=>{var a=MultiInput("Новый мастер","ФИО","Специализация");service.AddTechnician(a[0],a[1]);});
    }
    private void BuildParts()
    {
        var p=Page("Склад");var bar=Bar(p);Grid(p,"Parts");
        Action(bar,"Добавить деталь",()=>{var a=MultiInput("Запасная часть","Название","Количество","Цена, сомони");service.AddPart(a[0],int.Parse(a[1]),decimal.Parse(a[2]));});
        Action(bar,"Изменить остаток",()=>{int id=SelectedId("Parts");int newQty=int.Parse(Ask("Новое количество"));if(newQty<0)throw new ArgumentException("Количество должно быть неотрицательным.");db.Execute("UPDATE Parts SET Quantity=$q WHERE Id=$id",("$q",newQty),("$id",id));});
    }
    private void BuildReports()
    {
        var p=Page("Отчёты");var bar=Bar(p);Grid(p,"Reports");
        Action(bar,"Обновить статистику",()=>{ });
        Action(bar,"Экспорт CSV",()=>{
            using var dlg=new SaveFileDialog{Filter="CSV (*.csv)|*.csv",FileName="repair_report.csv"};if(dlg.ShowDialog()!=DialogResult.OK)return;
            var table=(DataTable)grids["Reports"].DataSource!;
            static string Esc(object? s)=>"\""+(s?.ToString()??"").Replace("\"","\"\"")+"\"";
            var lines=new List<string>{string.Join(";",table.Columns.Cast<DataColumn>().Select(c=>Esc(c.ColumnName)))};
            lines.AddRange(table.Rows.Cast<DataRow>().Select(r=>string.Join(";",r.ItemArray.Select(Esc))));
            File.WriteAllLines(dlg.FileName,lines,System.Text.Encoding.UTF8);
            MessageBox.Show("Отчёт сохранён.");
        });
    }
    private void BuildSettings()
    {
        var p=Page("Настройки");var bar=Bar(p);
        var info=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,Font=new Font("Segoe UI",11),Text=$"Приложение: Мастерская ремонта техники\r\nПлатформа: C# .NET 8 Windows Forms\r\nБаза данных: SQLite\r\nФайл базы данных: {db.FilePath}\r\n\r\nДля резервного копирования закройте приложение и скопируйте файл workshop.db."};p.Controls.Add(info);info.BringToFront();
        Action(bar,"Расположение БД",()=>MessageBox.Show(db.FilePath,"Расположение SQLite"));
    }
    private void RefreshAll()
    {
        grids["Customers"].DataSource=db.Query("SELECT Id,FullName AS ФИО,Phone AS Телефон FROM Customers ORDER BY Id DESC");
        grids["Technicians"].DataSource=db.Query("SELECT Id,FullName AS ФИО,Specialty AS Специализация FROM Technicians ORDER BY Id DESC");
        grids["Parts"].DataSource=db.Query("SELECT Id,Name AS Деталь,Quantity AS Остаток,Price AS Цена FROM Parts ORDER BY Id DESC");
        grids["Devices"].DataSource=db.Query("SELECT o.Id,o.DeviceType AS Тип,o.Brand AS Марка,o.Model AS Модель,o.Fault AS Неисправность,c.FullName AS Клиент FROM Orders o JOIN Customers c ON c.Id=o.CustomerId ORDER BY o.Id DESC");
        grids["Orders"].DataSource=db.Query("SELECT o.Id,c.FullName AS Клиент,o.DeviceType AS Тип,o.Brand AS Марка,o.Model AS Модель,o.Fault AS Неисправность,CASE o.Status WHEN 0 THEN 'Принято' WHEN 1 THEN 'Диагностика' WHEN 2 THEN 'В ремонте' WHEN 3 THEN 'Готово' ELSE 'Выдано' END AS Статус,o.Cost AS Стоимость,o.CreatedAt AS Дата FROM Orders o JOIN Customers c ON c.Id=o.CustomerId ORDER BY o.Id DESC");
        grids["Reports"].DataSource=db.Query("SELECT Status AS КодСтатуса,COUNT(*) AS Заказов,ROUND(SUM(Cost),2) AS Сумма_сомони FROM Orders GROUP BY Status ORDER BY Status");
        var t=db.Query("SELECT COUNT(*) AS Total,COALESCE(SUM(CASE WHEN Status<4 THEN 1 ELSE 0 END),0) AS Active,COALESCE(SUM(CASE WHEN Status=4 THEN 1 ELSE 0 END),0) AS Issued,COALESCE(SUM(CASE WHEN Status=4 THEN Cost ELSE 0 END),0) AS Revenue FROM Orders");
        var row=t.Rows[0];summary.Text=$"Всего заказов: {row[0]}     В работе: {row[1]}\nВыдано: {row[2]}     Выручка по выданным: {row[3]} сомони";
    }
}
