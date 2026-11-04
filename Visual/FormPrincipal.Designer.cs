using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProyectoRedTransporte
{
    // La palabra 'partial' permite dividir la clase
    public partial class FormPrincipal : Form
    {
        // Controles de la Interfaz
        private TextBox txtBusquedaOrigen;
        private ListBox lstSugerenciasOrigen;
        private TextBox txtBusquedaDestino;
        private ListBox lstSugerenciasDestino;
        private Button btnCargarDatos;
        private Button btnCalcularRuta;
        private RichTextBox rtbConsola;
        private Label lblOrigenSel;
        private Label lblDestinoSel;

        // Configuración puramente visual
        private void ConfigurarVentana()
        {
            this.Text = "Red de Transporte Urbano (Flujo Unificado)";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            Label lblTitulo = new Label { Text = "SISTEMA DE RUTAS (Trie -> Hash -> UnionFind -> Grafo)", Font = new Font("Arial", 12, FontStyle.Bold), Bounds = new Rectangle(20, 20, 600, 30) };
            
            btnCargarDatos = new Button { Text = "Cargar CSV (Paradas y Conexiones)", Bounds = new Rectangle(20, 60, 250, 40), BackColor = Color.LightBlue };
            btnCargarDatos.Click += BtnCargarDatos_Click;

            Label lblOri = new Label { Text = "Origen (Autocompletado):", Bounds = new Rectangle(20, 120, 200, 20) };
            txtBusquedaOrigen = new TextBox { Bounds = new Rectangle(20, 140, 250, 25) };
            txtBusquedaOrigen.TextChanged += (s, e) => BuscarSugerencias(txtBusquedaOrigen.Text, lstSugerenciasOrigen);
            lstSugerenciasOrigen = new ListBox { Bounds = new Rectangle(20, 170, 250, 100) };
            lstSugerenciasOrigen.SelectedIndexChanged += (s, e) => SeleccionarParada(lstSugerenciasOrigen, ref codigoOrigenSeleccionado, lblOrigenSel, "Origen");
            lblOrigenSel = new Label { Text = "Origen seleccionado: Ninguno", Bounds = new Rectangle(20, 280, 250, 40), ForeColor = Color.DarkBlue };

            Label lblDes = new Label { Text = "Destino (Autocompletado):", Bounds = new Rectangle(300, 120, 200, 20) };
            txtBusquedaDestino = new TextBox { Bounds = new Rectangle(300, 140, 250, 25) };
            txtBusquedaDestino.TextChanged += (s, e) => BuscarSugerencias(txtBusquedaDestino.Text, lstSugerenciasDestino);
            lstSugerenciasDestino = new ListBox { Bounds = new Rectangle(300, 170, 250, 100) };
            lstSugerenciasDestino.SelectedIndexChanged += (s, e) => SeleccionarParada(lstSugerenciasDestino, ref codigoDestinoSeleccionado, lblDestinoSel, "Destino");
            lblDestinoSel = new Label { Text = "Destino seleccionado: Ninguno", Bounds = new Rectangle(300, 280, 250, 40), ForeColor = Color.DarkBlue };

            btnCalcularRuta = new Button { Text = "EJECUTAR FLUJO (Calcular Ruta)", Bounds = new Rectangle(580, 170, 180, 100), BackColor = Color.LightGreen, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnCalcularRuta.Click += BtnCalcularRuta_Click;

            rtbConsola = new RichTextBox { Bounds = new Rectangle(20, 330, 740, 200), ReadOnly = true, Font = new Font("Consolas", 10), BackColor = Color.Black, ForeColor = Color.Lime };

            this.Controls.AddRange(new Control[] { lblTitulo, btnCargarDatos, lblOri, txtBusquedaOrigen, lstSugerenciasOrigen, lblOrigenSel, lblDes, txtBusquedaDestino, lstSugerenciasDestino, lblDestinoSel, btnCalcularRuta, rtbConsola });
        }
    }
}