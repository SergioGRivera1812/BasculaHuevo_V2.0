using MySql.Data.MySqlClient;
using QRCoder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasculaHuevo_V2._0
{
    public partial class Historial : Form
    {
        Conexion cnn;
        private delegate void DelegadoAcceso(string accion);
        Configuracion c = new Configuracion();
        System.Diagnostics.Process oskProcess = null;


        [DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_CLOSE = 0xF060;

        public Historial()
        {
            InitializeComponent();
        }

        private void txtFiltrar_Click(object sender, EventArgs e)
        {
            string progFiles = @"C:\Program Files\Common Files\Microsoft Shared\ink";
            string keyboardPath = Path.Combine(progFiles, "TabTip.exe");
            oskProcess = Process.Start(keyboardPath);

        }

        private BindingSource bs = new BindingSource();

        private DataTable load()
        {
            DataTable vista = new DataTable();
            string vista_gral = "select*from Historial;";
            using (MySqlCommand cmd = new MySqlCommand(vista_gral, cnn.GetConexion()))
            {
                MySqlDataReader reader = cmd.ExecuteReader();
                vista.Load(reader);

            }
            bs.DataSource = vista;
            return vista;
        }

        private void closeOnscreenKeyboard()
        {

            int iHandle = FindWindow("IPTIP_Main_Window", "");
            if (iHandle > 0)
            {

                SendMessage(iHandle, WM_SYSCOMMAND, SC_CLOSE, 0);
            }
        }

        private void Historial_Load(object sender, EventArgs e)
        {
            c.Configuracion_Load(sender, e);

            string s = c.txtIPServer.Text;
            string b = c.txtBase.Text;
            string p = c.txtPortDB.Text;
            string us = c.txtUsuario.Text;
            string pass = c.txtPass.Text;

            cnn = new Conexion(s, b, p, us, pass);
            dataGridHistorial.DataSource = load();
            dataGridHistorial.Columns[1].HeaderText = "ID";


        }

        private void txtFiltrar_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                closeOnscreenKeyboard();
            }
        }

        private void txtFiltrar_KeyUp(object sender, KeyEventArgs e)
        {
            FilterData();
        }

        void FilterData()
        {
            try
            {
                if (bs.DataSource != null)
                {
                    bs.Filter = $"Producto LIKE '%{txtFiltrar.Text}%' OR Fecha LIKE '%{txtFiltrar.Text}%'";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Filtrando datos...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void dataGridHistorial_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridHistorial.Columns[e.ColumnIndex].Name == "Imprimir")
            {
                string Fec = this.dataGridHistorial.SelectedRows[0].Cells[2].Value.ToString();
                string hora = this.dataGridHistorial.SelectedRows[0].Cells[3].Value.ToString();
                string Peso = this.dataGridHistorial.SelectedRows[0].Cells[4].Value.ToString();
                string Pro = this.dataGridHistorial.SelectedRows[0].Cells[5].Value.ToString();
                string Cas = this.dataGridHistorial.SelectedRows[0].Cells[6].Value.ToString();



                string contenido = Fec + "\n" + hora + "\n" + Pro + "\n" + "P/C " + Cas + "\n" + Peso + " KG";
                QRCodeGenerator qrGenerador = new QRCodeGenerator();
                QRCodeData qrDatos = qrGenerador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.H);
                QRCode qrCodigo = new QRCode(qrDatos);

                Bitmap qrImagen = qrCodigo.GetGraphic(5, Color.Black, Color.White, true);
                pictureBox2.Image = qrImagen;


                PrinterSettings ps = new PrinterSettings();
                printDocument1.PrinterSettings = ps;
                printDocument1.PrintPage += Reimpresion;
                printDocument1.Print();
            }
        }

        private void Reimpresion(object sender, PrintPageEventArgs e)
        {
            string Fec = this.dataGridHistorial.SelectedRows[0].Cells[2].Value.ToString();
            string hora = this.dataGridHistorial.SelectedRows[0].Cells[3].Value.ToString();
            string Peso = this.dataGridHistorial.SelectedRows[0].Cells[4].Value.ToString();
            string Pro = this.dataGridHistorial.SelectedRows[0].Cells[5].Value.ToString();
            string Cas = this.dataGridHistorial.SelectedRows[0].Cells[6].Value.ToString();

            Font font = new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Point);
            int width = 600;

            e.Graphics.DrawString("Etiqueta de Producto", font, Brushes.Black, new RectangleF(10, 20, width, 20));
            e.Graphics.DrawString("Fecha:  " + Fec, font, Brushes.Black, new RectangleF(10, 60, width, 20));
            e.Graphics.DrawString("Hora:  " + hora, font, Brushes.Black, new RectangleF(10, 80, width, 20));
            e.Graphics.DrawString("Tipo de producto:  " + Pro, font, Brushes.Black, new RectangleF(10, 100, width, 20));
            e.Graphics.DrawString("Parvada:  " + Cas, font, Brushes.Black, new RectangleF(10, 120, width, 20));
            e.Graphics.DrawString("Peso del producto:  " + Peso + " Kg", font, Brushes.Black, new RectangleF(10, 140, width, 20));
            e.Graphics.DrawImage(pictureBox2.Image, 240, 250, 130, 130);

        }

        private void inicioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1 f = new Form1();
            f.Show();
            this.Visible = false;
        }
    }
}