using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmMoldes : Form
    {
        private DataGridView dgv;

        public FrmMoldes()
        {
            Text = "Moldes"; Size = new Size(950, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(240, 250, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(20, 100, 140) };
            hdr.Controls.Add(new Label { Text = "Tabla de Moldes", ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });
            var btnN = Btn("+ Nuevo",   Color.FromArgb(40,160,80),  new Point(550,8));
            var btnMant = Btn("Mantenimiento", Color.FromArgb(20,80,120), new Point(648,8), 120);
            var btnE = Btn("Editar",    Color.FromArgb(60,100,180), new Point(778,8));
            var btnD = Btn("Eliminar",  Color.FromArgb(180,60,60),  new Point(876,8));
            hdr.Controls.AddRange(new Control[]{ btnN, btnMant, btnE, btnD });

            dgv = new DataGridView { Dock=DockStyle.Fill, ReadOnly=true, SelectionMode=DataGridViewSelectionMode.FullRowSelect, MultiSelect=false, AllowUserToAddRows=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor=Color.White, BorderStyle=BorderStyle.None, RowHeadersVisible=false };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20,100,140);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI",9f,FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235,247,255);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s,e) => ColorearMantenimiento();

            var footer = new Panel { Dock=DockStyle.Bottom, Height=40, BackColor=Color.FromArgb(218,238,250) };
            var lblInfo = new Label { AutoSize=true, Location=new Point(12,10), Font=new Font("Segoe UI",9.5f,FontStyle.Bold), ForeColor=Color.FromArgb(10,70,110) };
            var btnC = new Button{Text="Cerrar",Size=new Size(90,27),Location=new Point(840,6),BackColor=Color.FromArgb(100,100,120),ForeColor=Color.White,FlatStyle=FlatStyle.Flat}; btnC.FlatAppearance.BorderSize=0; btnC.Click+=(s,e)=>Close();
            footer.Controls.AddRange(new Control[]{ lblInfo, btnC });

            Controls.AddRange(new Control[]{ dgv, hdr, footer });

            btnN.Click += (s,e) => { using var d=new DlgMolde(null); if(d.ShowDialog()==DialogResult.OK){DatabaseService.InsertarMolde(d.Resultado);CargarDatos(lblInfo);} };
            btnE.Click += (s,e) => { var m=GetSel(); if(m==null)return; using var d=new DlgMolde(m); if(d.ShowDialog()==DialogResult.OK){DatabaseService.ActualizarMolde(d.Resultado);CargarDatos(lblInfo);} };
            btnD.Click += (s,e) => { var m=GetSel(); if(m==null)return; if(MessageBox.Show($"Eliminar molde {m.CodigoMold}?","Confirmar",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){DatabaseService.EliminarMolde(m.CodigoMold);CargarDatos(lblInfo);} };

            btnMant.Click += (s,e) => new FrmMantenimientoMoldes().ShowDialog();
            CargarDatos(lblInfo);
        }

        private void CargarDatos(Label lbl)
        {
            var lista = DatabaseService.ObtenerMoldes();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(m => new {
                m.CodigoMold, m.Descripcion, m.ModeloQfabrica,
                Fecha         = m.Fecha.ToShortDateString(),
                UltMant       = m.FechaLastMant.ToShortDateString(),
                m.DescMant,
                MatXUnid      = m.CantMatXunid,
                DiasDesdeUltMant = (DateTime.Today - m.FechaLastMant).Days
            }).ToList();
            int vencidos = lista.Count(m => (DateTime.Today - m.FechaLastMant).Days > 180);
            lbl.Text = $"  Moldes: {lista.Count}   |   Mant. vencido (>180 dias): {vencidos}";
        }

        private void ColorearMantenimiento()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["DiasDesdeUltMant"].Value == null) continue;
                int dias = Convert.ToInt32(row.Cells["DiasDesdeUltMant"].Value);
                if (dias > 180)      row.DefaultCellStyle.BackColor = Color.FromArgb(255, 210, 210);
                else if (dias > 90)  row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 200);
            }
        }

        private TbMolde GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            var cod = dgv.CurrentRow.Cells["CodigoMold"].Value?.ToString();
            return DatabaseService.ObtenerMoldes().Find(x => x.CodigoMold == cod);
        }

        private Button Btn(string t, Color c, Point p, int w = 90) { var b=new Button{Text=t,Size=new Size(w,33),Location=p,BackColor=c,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),Cursor=Cursors.Hand}; b.FlatAppearance.BorderSize=0; return b; }
    }

    public class DlgMolde : Form
    {
        public TbMolde Resultado { get; private set; }
        private TextBox txtCod, txtDesc, txtModelo, txtDescMant, txtObs;
        private DateTimePicker dtpFecha, dtpLastMant;
        private NumericUpDown numCantMat;

        public DlgMolde(TbMolde m)
        {
            Text = m == null ? "Nuevo Molde" : "Editar Molde";
            Size = new Size(440, 420); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(240,250,255);

            int y = 12;
            L("Codigo Molde:", y); txtCod   = T(y); y+=33;
            L("Descripcion:", y); txtDesc   = T(y); y+=33;
            L("Modelo Fabrica:", y); txtModelo= T(y); y+=33;
            L("Fecha:", y);       dtpFecha  = D(DateTime.Today, y); y+=33;
            L("Ult. Mant.:", y);  dtpLastMant=D(DateTime.Today, y); y+=33;
            L("Desc. Mant.:", y); txtDescMant=T(y); y+=33;
            L("Mat x Unidad:", y);numCantMat = new NumericUpDown{Location=new Point(155,y),Size=new Size(245,24),Maximum=9999,DecimalPlaces=2}; Controls.Add(numCantMat); y+=33;
            L("Observaciones:",y);txtObs    = new TextBox{Location=new Point(155,y),Size=new Size(245,45),Multiline=true}; Controls.Add(txtObs); y+=55;

            if (m != null)
            {
                txtCod.Text=m.CodigoMold; txtDesc.Text=m.Descripcion; txtModelo.Text=m.ModeloQfabrica;
                dtpFecha.Value=m.Fecha; dtpLastMant.Value=m.FechaLastMant;
                txtDescMant.Text=m.DescMant; numCantMat.Value=m.CantMatXunid; txtObs.Text=m.Observaciones;
                txtCod.ReadOnly=true;
            }

            var bOk  = BB("Guardar",  Color.FromArgb(20,100,140), new Point(80, y));
            var bCan = BB("Cancelar", Color.FromArgb(160,50,50),  new Point(240,y));
            bCan.DialogResult=DialogResult.Cancel;
            bOk.Click += (s,e) =>
            {
                if(string.IsNullOrWhiteSpace(txtCod.Text)){MessageBox.Show("Ingrese el codigo.","Validacion",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                Resultado=m??new TbMolde();
                Resultado.CodigoMold=txtCod.Text.Trim(); Resultado.Descripcion=txtDesc.Text.Trim();
                Resultado.ModeloQfabrica=txtModelo.Text.Trim(); Resultado.Fecha=dtpFecha.Value;
                Resultado.FechaLastMant=dtpLastMant.Value; Resultado.DescMant=txtDescMant.Text.Trim();
                Resultado.CantMatXunid=numCantMat.Value; Resultado.Observaciones=txtObs.Text.Trim();
                DialogResult=DialogResult.OK;
            };
            Controls.AddRange(new Control[]{bOk,bCan});
        }

        private void L(string t,int y)=>Controls.Add(new Label{Text=t,Location=new Point(12,y+3),AutoSize=true,Font=new Font("Segoe UI",9f,FontStyle.Bold)});
        private TextBox T(int y){var tb=new TextBox{Location=new Point(155,y),Size=new Size(245,24)};Controls.Add(tb);return tb;}
        private DateTimePicker D(DateTime v,int y){var dp=new DateTimePicker{Value=v,Location=new Point(155,y),Size=new Size(245,24),Format=DateTimePickerFormat.Short};Controls.Add(dp);return dp;}
        private Button BB(string t,Color c,Point p){var b=new Button{Text=t,Location=p,Size=new Size(130,32),BackColor=c,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",9f,FontStyle.Bold)};b.FlatAppearance.BorderSize=0;Controls.Add(b);return b;}
    }
}
