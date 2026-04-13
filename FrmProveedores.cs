using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmProveedores : Form
    {
        private DataGridView dgv;

        public FrmProveedores()
        {
            Text = "Proveedores"; Size = new Size(1000, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(255, 248, 240);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(160, 90, 20) };
            hdr.Controls.Add(new Label { Text = "Proveedores", ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });
            var btnN = Btn("+ Nuevo", Color.FromArgb(40,160,80), new Point(680,8));
            var btnE = Btn("Editar",  Color.FromArgb(60,100,180),new Point(778,8));
            var btnD = Btn("Eliminar",Color.FromArgb(180,60,60), new Point(876,8));
            hdr.Controls.AddRange(new Control[]{ btnN, btnE, btnD });

            dgv = new DataGridView { Dock=DockStyle.Fill, ReadOnly=true, SelectionMode=DataGridViewSelectionMode.FullRowSelect, MultiSelect=false, AllowUserToAddRows=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor=Color.White, BorderStyle=BorderStyle.None, RowHeadersVisible=false };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(160,90,20);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI",9f,FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255,245,230);
            dgv.EnableHeadersVisualStyles = false;

            var footer = new Panel { Dock=DockStyle.Bottom, Height=40, BackColor=Color.FromArgb(240,228,210) };
            var lblInfo = new Label { AutoSize=true, Location=new Point(12,10), Font=new Font("Segoe UI",9.5f,FontStyle.Bold), ForeColor=Color.FromArgb(100,60,10) };
            var btnC = new Button { Text="Cerrar", Size=new Size(90,27), Location=new Point(880,6), BackColor=Color.FromArgb(100,100,120), ForeColor=Color.White, FlatStyle=FlatStyle.Flat }; btnC.FlatAppearance.BorderSize=0; btnC.Click+=(s,e)=>Close();
            footer.Controls.AddRange(new Control[]{ lblInfo, btnC });

            Controls.AddRange(new Control[]{ dgv, hdr, footer });

            btnN.Click += (s,e) => { using var d=new DlgProveedor(null); if(d.ShowDialog()==DialogResult.OK){DatabaseService.InsertarProveedor(d.Resultado);CargarDatos(lblInfo);} };
            btnE.Click += (s,e) => { var p=GetSel(); if(p==null)return; using var d=new DlgProveedor(p); if(d.ShowDialog()==DialogResult.OK){DatabaseService.ActualizarProveedor(d.Resultado);CargarDatos(lblInfo);} };
            btnD.Click += (s,e) => { var p=GetSel(); if(p==null)return; if(MessageBox.Show($"Eliminar {p.Codigo}?","Confirmar",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){DatabaseService.EliminarProveedor(p.Codigo);CargarDatos(lblInfo);} };

            CargarDatos(lblInfo);
        }

        private void CargarDatos(Label lbl)
        {
            var lista = DatabaseService.ObtenerProveedores();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(p => new { p.Codigo, Nombre=p.Descripcion, p.Representante, p.Telefono1, p.CiRif, p.Nit, DiasCredito=p.DiasCredito, Retencion=$"{p.PctRetencion}%", p.Tipo }).ToList();
            lbl.Text = $"  Proveedores: {lista.Count}";
        }

        private Proveedor GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            var cod = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            return DatabaseService.ObtenerProveedores().Find(x => x.Codigo == cod);
        }

        private Button Btn(string t, Color c, Point p) { var b=new Button{Text=t,Size=new Size(90,33),Location=p,BackColor=c,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),Cursor=Cursors.Hand}; b.FlatAppearance.BorderSize=0; return b; }
    }

    public class DlgProveedor : Form
    {
        public Proveedor Resultado { get; private set; }
        private TextBox txtCod, txtDesc, txtRep, txtDir, txtT1, txtT2, txtF1, txtF2, txtCiRif, txtNit, txtObs;
        private NumericUpDown numDias, numPct;
        private ComboBox cmbTipo;
        private DateTimePicker dtpFecha;

        public DlgProveedor(Proveedor p)
        {
            Text = p == null ? "Nuevo Proveedor" : "Editar Proveedor";
            Size = new Size(480, 580); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(255,248,240);

            int y = 12;
            L("Codigo:", y);         txtCod  = T(y); y+=33;
            L("Nombre:", y);         txtDesc = T(y); y+=33;
            L("Representante:", y);  txtRep  = T(y); y+=33;
            L("Direccion:", y);      txtDir  = T(y); y+=33;
            L("Telefono 1:", y);     txtT1   = T(y); y+=33;
            L("Telefono 2:", y);     txtT2   = T(y); y+=33;
            L("Fax 1:", y);          txtF1   = T(y); y+=33;
            L("Fax 2:", y);          txtF2   = T(y); y+=33;
            L("CI/RIF:", y);         txtCiRif= T(y); y+=33;
            L("NIT:", y);            txtNit  = T(y); y+=33;
            L("Dias Credito:", y);   numDias = Num(y, 365); y+=33;
            L("% Retencion:", y);    numPct  = Num(y, 100); y+=33;
            L("Tipo:", y);
            cmbTipo = new ComboBox{Location=new Point(155,y),Size=new Size(270,24),DropDownStyle=ComboBoxStyle.DropDownList};
            cmbTipo.Items.AddRange(new object[]{"Nacional","Internacional","Servicio","Mixto"}); cmbTipo.SelectedIndex=0; Controls.Add(cmbTipo); y+=33;
            L("Fecha Inicio:", y);
            dtpFecha = new DateTimePicker{Location=new Point(155,y),Size=new Size(270,24),Format=DateTimePickerFormat.Short}; Controls.Add(dtpFecha); y+=33;
            L("Observaciones:", y);  txtObs = new TextBox{Location=new Point(155,y),Size=new Size(270,45),Multiline=true}; Controls.Add(txtObs); y+=55;

            if (p != null)
            {
                txtCod.Text=p.Codigo; txtDesc.Text=p.Descripcion; txtRep.Text=p.Representante;
                txtDir.Text=p.Direccion; txtT1.Text=p.Telefono1; txtT2.Text=p.Telefono2;
                txtF1.Text=p.Fax1; txtF2.Text=p.Fax2; txtCiRif.Text=p.CiRif; txtNit.Text=p.Nit;
                numDias.Value=p.DiasCredito; numPct.Value=p.PctRetencion;
                cmbTipo.SelectedItem=p.Tipo; dtpFecha.Value=p.FechaInicio; txtObs.Text=p.Observaciones;
                txtCod.ReadOnly=true;
            }

            var bOk  = BB("Guardar",  Color.FromArgb(160,90,20), new Point(90, y));
            var bCan = BB("Cancelar", Color.FromArgb(160,50,50), new Point(260,y));
            bCan.DialogResult = DialogResult.Cancel;
            bOk.Click += (s,e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCod.Text)) { MessageBox.Show("Ingrese el codigo.","Validacion",MessageBoxButtons.OK,MessageBoxIcon.Warning); return; }
                Resultado = p ?? new Proveedor();
                Resultado.Codigo=txtCod.Text.Trim(); Resultado.Descripcion=txtDesc.Text.Trim();
                Resultado.Representante=txtRep.Text.Trim(); Resultado.Direccion=txtDir.Text.Trim();
                Resultado.Telefono1=txtT1.Text.Trim(); Resultado.Telefono2=txtT2.Text.Trim();
                Resultado.Fax1=txtF1.Text.Trim(); Resultado.Fax2=txtF2.Text.Trim();
                Resultado.CiRif=txtCiRif.Text.Trim(); Resultado.Nit=txtNit.Text.Trim();
                Resultado.DiasCredito=(int)numDias.Value; Resultado.PctRetencion=numPct.Value;
                Resultado.Tipo=cmbTipo.SelectedItem?.ToString(); Resultado.FechaInicio=dtpFecha.Value;
                Resultado.Observaciones=txtObs.Text.Trim();
                DialogResult=DialogResult.OK;
            };
            Controls.AddRange(new Control[]{bOk,bCan});
        }

        private void L(string t,int y)=>Controls.Add(new Label{Text=t,Location=new Point(12,y+3),AutoSize=true,Font=new Font("Segoe UI",9f,FontStyle.Bold)});
        private TextBox T(int y){var tb=new TextBox{Location=new Point(155,y),Size=new Size(270,24)};Controls.Add(tb);return tb;}
        private NumericUpDown Num(int y,decimal max){var n=new NumericUpDown{Location=new Point(155,y),Size=new Size(270,24),Maximum=max,DecimalPlaces=2};Controls.Add(n);return n;}
        private Button BB(string t,Color c,Point p){var b=new Button{Text=t,Location=p,Size=new Size(130,32),BackColor=c,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",9f,FontStyle.Bold)};b.FlatAppearance.BorderSize=0;Controls.Add(b);return b;}
    }
}
