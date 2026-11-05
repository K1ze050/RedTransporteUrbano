using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProyectoRedTransporte
{
    public partial class FormPrincipal : Form
    {
        private TextBox txtBusquedaOrigen;
        private ListBox lstSugerenciasOrigen;
        private TextBox txtBusquedaDestino;
        private ListBox lstSugerenciasDestino;
        private Button btnEjecutarBFS;
        private Button btnEjecutarDFS;
        private Button btnCargarDatos;
        private Button btnCalcularRuta;
        private Button btnVerGrafo;
        private Button btnGestionRuta;
        // NUEVOS BOTONES DE PARADAS
        private Button btnAgregarParada;
        private Button btnEliminarParada;
        
        private RichTextBox rtbConsola;
        private Label lblOrigenSel;
        private Label lblDestinoSel;

        private void ConfigurarVentana()
        {
            this.Text = "Red de Transporte Urbano (Flujo Unificado)";
            this.Size = new Size(820, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            Label lblTitulo = new Label { Text = "SISTEMA DE RUTAS (Trie -> Hash -> UnionFind -> Grafo)", Font = new Font("Arial", 12, FontStyle.Bold), Bounds = new Rectangle(20, 15, 600, 25) };
            
            // Fila superior de botones rediseñada para incluir todas las funciones
            btnCargarDatos = new Button { Text = "Cargar CSV", Bounds = new Rectangle(20, 50, 130, 36), BackColor = Color.LightBlue, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnCargarDatos.Click += BtnCargarDatos_Click;

            btnVerGrafo = new Button { Text = "Ver Mapa", Bounds = new Rectangle(160, 50, 120, 36), BackColor = Color.Thistle, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnVerGrafo.Click += BtnVerGrafo_Click;

            btnGestionRuta = new Button { Text = "+ Ruta", Bounds = new Rectangle(290, 50, 120, 36), BackColor = Color.SandyBrown, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnGestionRuta.Click += BtnGestionRuta_Click;

            btnAgregarParada = new Button { Text = "+ Parada", Bounds = new Rectangle(420, 50, 120, 36), BackColor = Color.LightGreen, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnAgregarParada.Click += BtnAgregarParada_Click;

            btnEliminarParada = new Button { Text = "- Eliminar", Bounds = new Rectangle(550, 50, 120, 36), BackColor = Color.LightCoral, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnEliminarParada.Click += BtnEliminarParada_Click;


            // Buscadores
            Label lblOri = new Label { Text = "Origen (Autocompletado):", Bounds = new Rectangle(20, 105, 200, 20) };
            txtBusquedaOrigen = new TextBox { Bounds = new Rectangle(20, 125, 250, 25) };
            txtBusquedaOrigen.TextChanged += (s, e) => BuscarSugerencias(txtBusquedaOrigen.Text, lstSugerenciasOrigen);
            lstSugerenciasOrigen = new ListBox { Bounds = new Rectangle(20, 155, 250, 100) };
            lstSugerenciasOrigen.SelectedIndexChanged += (s, e) => SeleccionarParada(lstSugerenciasOrigen, ref codigoOrigenSeleccionado, lblOrigenSel, "Origen");
            lblOrigenSel = new Label { Text = "Origen seleccionado: Ninguno", Bounds = new Rectangle(20, 260, 250, 40), ForeColor = Color.DarkBlue };

            Label lblDes = new Label { Text = "Destino (Autocompletado):", Bounds = new Rectangle(300, 105, 200, 20) };
            txtBusquedaDestino = new TextBox { Bounds = new Rectangle(300, 125, 250, 25) };
            txtBusquedaDestino.TextChanged += (s, e) => BuscarSugerencias(txtBusquedaDestino.Text, lstSugerenciasDestino);
            lstSugerenciasDestino = new ListBox { Bounds = new Rectangle(300, 155, 250, 100) };
            lstSugerenciasDestino.SelectedIndexChanged += (s, e) => SeleccionarParada(lstSugerenciasDestino, ref codigoDestinoSeleccionado, lblDestinoSel, "Destino");
            lblDestinoSel = new Label { Text = "Destino seleccionado: Ninguno", Bounds = new Rectangle(300, 260, 250, 40), ForeColor = Color.DarkBlue };

            // Ejecutores de ruta
            btnCalcularRuta = new Button { Text = "DIJKSTRA (Ruta Óptima)", Bounds = new Rectangle(580, 125, 200, 42), BackColor = Color.LightGreen, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnCalcularRuta.Click += BtnCalcularRuta_Click;

            btnEjecutarBFS = new Button { Text = "RECORRIDO BFS (Anchura)", Bounds = new Rectangle(580, 180, 200, 42), BackColor = Color.LightSkyBlue, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnEjecutarBFS.Click += BtnEjecutarBFS_Click;

            btnEjecutarDFS = new Button { Text = "RECORRIDO DFS (Profundidad)", Bounds = new Rectangle(580, 235, 200, 42), BackColor = Color.LightCoral, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnEjecutarDFS.Click += BtnEjecutarDFS_Click;

            rtbConsola = new RichTextBox { Bounds = new Rectangle(20, 315, 760, 280), ReadOnly = true, Font = new Font("Consolas", 10), BackColor = Color.Black, ForeColor = Color.Lime };

            this.Controls.AddRange(new Control[] { lblTitulo, btnCargarDatos, btnVerGrafo, btnGestionRuta, btnAgregarParada, btnEliminarParada, lblOri, txtBusquedaOrigen, lstSugerenciasOrigen, lblOrigenSel, lblDes, txtBusquedaDestino, lstSugerenciasDestino, lblDestinoSel, btnCalcularRuta, btnEjecutarBFS, btnEjecutarDFS, rtbConsola });
        }
    }
}    