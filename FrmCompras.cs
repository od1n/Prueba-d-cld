using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmCompras : Form
    {
        private DataGridView dgv;
        private Label lblResumen;

        public FrmCompras()
        {
            Text = "Compras"; Size = new Size(950, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 245, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(100, 50, 160) };
            hdr.Controls.Add(new Label { Text = "Registro de Compras", ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });
            var btnN = Btn("+ Nueva",   Color.FromArgb(40,160,80),  new Point(660,8));
            var btnE = Btn("Editar",    Color.FromArgb(60,100,180), new Point(758,8));
            var btnD = Btn("Eliminar",  Color.FromArgb(180,60,60),  new Point(856,8));
            hdr.Controls.AddRange(new Control[]{ btnN, btnE, btnD });

            dgv = new DataGridView { Dock=DockStyle.Fill, ReadOnly=true, SelectionMode=DataGridViewSelectionMode.FullRowSelect, MultiSelect=false, AllowUserToAddRows=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor=Color.White, BorderStyle=BorderStyle.None, RowHeadersVisible=false };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(100,50,160);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI",9f,FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245,240,255);
            dgv.EnableHeadersVisualStyles = false;

            var footer = new Panel { Dock=DockStyle.Bottom, Height=40, BackColor=Color.FromArgb(232,225,248) };
            lblResumen = new Label { AutoSize=true, Location=new Point(12,10), Font=new Font("Segoe UI",9.5f,FontStyle.Bold), ForeColor=Color.FromArgb(70,30,120) };
            var btnC = new Button{Text="Cerrar",Size=new Size(90,27),Location=new Point(830,6),BackColor=Color.FromArgb(100,100,120),ForeColor=Color.White,FlatStyle=FlatStyle.Flat}; btnC.FlatAppearance.BorderSize=0; btnC.Click+=(s,e)=>Close();
            footer.Controls.AddRange(new Control[]{ lblResumen, btnC });

            Controls.AddRange(new Control[]{ dgv, hdr, footer });

            btnN.Click += (s,e) => { using var d=new DlgCompra(null); if(d.ShowDialog()==DialogResult.OK){DatabaseService.InsertarCompra(d.Resultado);CargarDatos();} };
            btnE.Click += (s,e) => { var cp=GetSel(); if(cp==null)return; using var d=new DlgCompra(cp); if(d.ShowDialog()==DialogResult.OK){DatabaseService.ActualizarCompra(d.Resultado);CargarDatos();} };
            btnD.Click += (s,e) => { var cp=GetSel(); if(cp==null)return; if(MessageBox.Show($"Eliminar compra {cp.CompraN}?","Confirmar",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){DatabaseService.EliminarCompra(cp.CompraN);CargarDatos();} };

            CargarDatos();
        }

        private void CargarDatos()
        {
            var lista = DatabaseService.ObtenerCompras();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(cp => new {
                cp.CompraN, Fecha=cp.FechaC.ToShortDateString(), Proveedor=cp.CodigoP,
                cp.FormaDePago, Descuento=$"{cp.DescuentoC}%",
                IVA=cp.IvaC.ToString("C2"), Total=cp.TotalC.ToString("C2"),
                cp.DiasDeCred
            }).ToList();
            lblResumen.Text = $"  Compras: {lista.Count}   |   Total comprado: {lista.Sum(c=>c.TotalC):C2}   |   Abonado: {lista.Sum(c=>c.AbonoC):C2}";
        }

        private Compra GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            var num = dgv.CurrentRow.Cells["CompraN"].Value?.ToString();
            return DatabaseService.ObtenerCompras().Find(x => x.CompraN == num);
        }

        private Button Btn(string t, Color c, Point p) { var b=new Button{Text=t,Size=new Size(90,33),Location=p,BackColor=c,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),Cursor=Cursors.Hand}; b.FlatAppearance.BorderSize=0; return b; }
    }

    public class DlgCompra : Form
    {
        public Compra Resultado { get; private set; }
        private TextBox txtNum, txtProv, txtFPago, txtTransp, txtObs;
        private NumericUpDown numDesc, numAbono, numIva, numTotal;
        private NumericUpDown numDias;
        private DateTimePicker dtpFecha;

        public DlgCompra(Compra cp)
        {
            Text = cp == null ? "Nueva Compra" : "Editar Compra";
            Size = new Size(440, 480); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(248,245,255);

            int y = 12;
            L("No. Compra:", y);    txtNum  = T(y); y+=33;
            L("Proveedor:", y);     txtProv = T(y); y+=33;
            L("Fecha:", y);         dtpFecha = new DateTimePicker{Location=new Point(150,y),Size=new Size(245,24),Format=DateTimePickerFormat.Short}; Controls.Add(dtpFecha); y+=33;
            L("Forma de Pago:", y); txtFPago = T(y); y+=33;
            L("Descuento (%):", y); numDesc  = N(y, 100); y+=33;
            L("Abono:", y);         numAbono = N(y, 9999999); y+=33;
            L("IVA:", y);           numIva   = N(y, 9999999); y+=33;
            L("Dias Credito:", y);  numDias  = N(y, 365); numDias.DecimalPlaces = 0; y+=33;
            L("Total:", y);         numTotal = N(y, 9999999); y+=33;
            L("Transporte:", y);    txtTransp= T(y); y+=33;
            L("Observaciones:", y); txtObs   = new TextBox{Location=new Point(150,y),Size=new Size(245,45),Multiline=true}; Controls.Add(txtObs); y+=55;

            if (cp != null)
            {
                txtNum.Text=cp.CompraN; txtProv.Text=cp.CodigoP; dtpFecha.Value=cp.FechaC;
                txtFPago.Text=cp.FormaDePago; numDesc.Value=cp.DescuentoC; numAbono.Value=cp.AbonoC;
                numIva.Value=cp.IvaC; numDias.Value=cp.DiasDeCred; numTotal.Value=cp.TotalC;
                txtTransp.Text=cp.Transporte; txtObs.Text=cp.Observaciones; txtNum.ReadOnly=true;
            }

            var bOk  = BB("Guardar",  Color.FromArgb(100,50,160), new Point(80, y));
            var bCan = BB("Cancelar", Color.FromArgb(160,50,50),  new Point(240,y));
            bCan.DialogResult=DialogResult.Cancel;
            bOk.Click += (s,e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNum.Text)){MessageBox.Show("Ingrese el numero.","Validacion",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                Resultado=cp??new Compra();
                Resultado.CompraN=txtNum.Text.Trim(); Resultado.CodigoP=txtProv.Text.Trim();
                Resultado.FechaC=dtpFecha.Value; Resultado.FormaDePago=txtFPago.Text.Trim();
                Resultado.DescuentoC=numDesc.Value; Resultado.AbonoC=numAbono.Value;
                Resultado.IvaC=numIva.Value; Resultado.DiasDeCred=(int)numDias.Value;
                Resultado.TotalC=numTotal.Value; Resultado.Transporte=txtTransp.Text.Trim();
                Resultado.Observaciones=txtObs.Text.Trim();
                DialogResult=DialogResult.OK;
            };
            Controls.AddRange(new Control[]{bOk,bCan});
        }

        private void L(string t,int y)=>Controls.Add(new Label{Text=t,Location=new Point(12,y+3),AutoSize=true,Font=new Font("Segoe UI",9f,FontStyle.Bold)});
        private TextBox T(int y){var tb=new TextBox{Location=new Point(150,y),Size=new Size(245,24)};Controls.Add(tb);return tb;}
        private NumericUpDown N(int y,decimal max){var n=new NumericUpDown{Location=new Point(150,y),Size=new Size(245,24),Maximum=max,DecimalPlaces=2};Controls.Add(n);return n;}
        private Button BB(string t,Color c,Point p){var b=new Button{Text=t,Location=p,Size=new Size(130,32),BackColor=c,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",9f,FontStyle.Bold)};b.FlatAppearance.BorderSize=0;Controls.Add(b);return b;}
    }
}
