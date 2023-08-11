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
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasculaHuevo_V2._0
{
    public partial class Form1 : Form
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

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                c.Configuracion_Load(sender, e);

                string s = c.txtIPServer.Text;
                string b = c.txtBase.Text;
                string p = c.txtPortDB.Text;
                string us = c.txtUsuario.Text;
                string pass = c.txtPass.Text;
                string com = c.txtCOM.Text;
                int ba = Convert.ToInt32(c.txtBaudio.Text);



                cnn = new Conexion(s, b, p, us, pass);
                dataGridHistorial.DataSource = load();
                dataGridHistorial.Columns[0].HeaderText = "ID";
                


                serialPort1 = new SerialPort(com, ba, Parity.None, 8, StopBits.One);
                serialPort1.Handshake = Handshake.None;
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                serialPort1.ReadTimeout = 500;
                serialPort1.WriteTimeout = 500;
                serialPort1.Open();
                serialPort1.Write("P");

            }
            catch
            {
                MessageBox.Show("Error de comunicación", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Indicador.Text = "Error";
                c.Configuracion_Load(sender, e);

                string s = c.txtIPServer.Text;
                string b = c.txtBase.Text;
                string p = c.txtPortDB.Text;
                string us = c.txtUsuario.Text;
                string pass = c.txtPass.Text;


                cnn = new Conexion(s, b, p, us, pass);
                dataGridHistorial.DataSource = load();

            }
        }

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (this.Enabled == false)
            {
                MessageBox.Show("Error de comunicación", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                try
                {
                    string data = serialPort1.ReadLine();

                    data = data.Replace("(kg)", "");
                    data = data.Replace("=", "");
                    string cadenaLimpia = data.Replace("\n", string.Empty).Replace("\t", string.Empty).Replace("", "").Replace(" ", "");
                    // Eliminar los ceros no significativos antes del punto decimal
                    cadenaLimpia = cadenaLimpia.TrimStart('0');

                    if (cadenaLimpia.StartsWith("."))

                        // Comprobar si solo hay un dígito después del punto decimal
                        cadenaLimpia = "0" + cadenaLimpia;  // Agregar un cero antes del punto decimal si es necesario
                    else
                        cadenaLimpia = cadenaLimpia.TrimStart('.');  // Eliminar el punto decimal inicial si hay más de un dígito después de él




                    this.BeginInvoke(new DelegadoAcceso(si_DataReceived), new object[] { cadenaLimpia });



                }
                catch (Exception)
                {
                    MessageBox.Show("El indicador no envia datos", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }

        }
        private void si_DataReceived(string accion)
        {
            Indicador.Text = accion;
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

        private void textParv_Click(object sender, EventArgs e)
        {
            string progFiles = @"C:\Program Files\Common Files\Microsoft Shared\ink";
            string keyboardPath = Path.Combine(progFiles, "TabTip.exe");
            oskProcess = Process.Start(keyboardPath);
        }

        private void closeOnscreenKeyboard()
        {

            int iHandle = FindWindow("IPTIP_Main_Window", "");
            if (iHandle > 0)
            {

                SendMessage(iHandle, WM_SYSCOMMAND, SC_CLOSE, 0);
            }
        }

        private void textParv_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                closeOnscreenKeyboard();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboProd.Text == String.Empty || textParv.Text == string.Empty)
            {
                MessageBox.Show("Selecciona el tipo de producto o introduce el numero de Parvada/Caseta para continuar", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                try
                {

                    string GuardarEt = "INSERT INTO Historial (Fecha,Hora,Producto,Caseta) values ('" +
                       DateTime.Now.ToString("d") + "','" + DateTime.Now.ToString("t") + "','" + comboProd.Text + "','" + textParv.Text  + "');";

                    MySqlCommand comando = new MySqlCommand(GuardarEt, cnn.GetConexion());
                    comando.ExecuteNonQuery();
                    dataGridHistorial.DataSource = load();

                    string contenido = comboProd.Text + "  P/C" + textParv.Text + Indicador.Text + "Kg" + " " + DateTime.Now.ToString("d") + " " + DateTime.Now.ToString("t");
                    QRCodeGenerator qrGenerador = new QRCodeGenerator();
                    QRCodeData qrDatos = qrGenerador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.H);
                    QRCode qrCodigo = new QRCode(qrDatos);

                    Bitmap qrImagen = qrCodigo.GetGraphic(5, Color.Black, Color.White, true);
                    pictureBox1.Image = qrImagen;

                    printDocument1 = new PrintDocument();
                    PrinterSettings ps = new PrinterSettings();
                    printDocument1.PrinterSettings = ps;
                    printDocument1.PrintPage += Imprimir;
                    printDocument1.Print();



                }
                catch (Exception ex)
                {

                }
            }
        }

        private void Imprimir(object sender, PrintPageEventArgs e)
        {
            //FormQR q = new FormQR();
            string f = DateTime.Now.ToString("d"), h = DateTime.Now.ToString("t"), p = Indicador.Text, pr = comboProd.Text, c = textParv.Text;
            Font font = new Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Point);
            int width = 600;

            e.Graphics.DrawString("Etiqueta de Producto", font, Brushes.Black, new RectangleF(10, 20, width, 20));
            e.Graphics.DrawString("Fecha:  " + f, font, Brushes.Black, new RectangleF(10, 60, width, 20));
            e.Graphics.DrawString("Hora:  " + h, font, Brushes.Black, new RectangleF(10, 80, width, 20));
            e.Graphics.DrawString("Tipo de producto:  " + pr, font, Brushes.Black, new RectangleF(10, 100, width, 20));
            e.Graphics.DrawString("Parvada:  " + c, font, Brushes.Black, new RectangleF(10, 120, width, 20));
            e.Graphics.DrawString("Peso del producto:  " + p + "Kg", font, Brushes.Black, new RectangleF(10, 140, width, 20));
            e.Graphics.DrawImage(pictureBox1.Image, 240, 250, 130, 130);
        }

        private void historialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Historial h = new Historial();
            h.Show();
            this.Visible = false;
        }

        private void configuracionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Configuracion c = new Configuracion();
            c.Show();
            

        }
    }
}
