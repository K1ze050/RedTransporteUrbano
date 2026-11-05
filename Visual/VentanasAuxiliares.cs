using System;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace ProyectoRedTransporte
{
    // =====================================================================
    // VENTANA 1: VISUALIZADOR SIMPLE DEL GRAFO
    // =====================================================================
    public class FormVisualizadorGrafo : Form
    {
        private Grafo grafo;
        private TablaHash<string, Parada> tablaParadas;
        private RutaResultado? ultimaRuta;

        public FormVisualizadorGrafo(Grafo grafo, TablaHash<string, Parada> tablaParadas, RutaResultado? ultimaRuta = null)
        {
            this.grafo = grafo;
            this.tablaParadas = tablaParadas;
            this.ultimaRuta = ultimaRuta;

            this.Text = "Mapa Esquemático de la Red de Transporte";
            this.Size = new Size(720, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.DoubleBuffered = true; 
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int n = grafo.CapacidadIndices;
            if (n == 0)
            {
                g.DrawString("No hay paradas cargadas para visualizar.", new Font("Arial", 12), Brushes.Gray, new PointF(20, 20));
                return;
            }

            PointF[] posiciones = new PointF[n];
            float centroX = this.ClientSize.Width / 2f;
            float centroY = (this.ClientSize.Height / 2f) - 15f;
            float radio = Math.Min(centroX, centroY) - 70f;

            for (int i = 0; i < n; i++)
            {
                double angulo = (2 * Math.PI / n) * i - (Math.PI / 2);
                float x = centroX + (float)(radio * Math.Cos(angulo));
                float y = centroY + (float)(radio * Math.Sin(angulo));
                posiciones[i] = new PointF(x, y);
            }

            using (Pen penArista = new Pen(Color.FromArgb(180, 200, 215), 2))
            using (Font fontPesos = new Font("Arial", 7.5f))
            {
                for (int u = 0; u < n; u++)
                {
                    if (!grafo.EsIndiceActivo(u)) continue;
                    NodoArista? actual = grafo.ObtenerCabeza(u);
                    while (actual != null)
                    {
                        int v = actual.Destino;
                        if (u < v && grafo.EsIndiceActivo(v)) 
                        {
                            g.DrawLine(penArista, posiciones[u], posiciones[v]);
                            float midX = (posiciones[u].X + posiciones[v].X) / 2f;
                            float midY = (posiciones[u].Y + posiciones[v].Y) / 2f;
                            g.DrawString($"{actual.Peso:F1} km", fontPesos, Brushes.DimGray, midX - 10, midY - 6);
                        }
                        actual = actual.Siguiente;
                    }
                }
            }

            if (ultimaRuta != null && ultimaRuta.Existe && ultimaRuta.CantidadParadas > 1)
            {
                using (Pen penRuta = new Pen(Color.LimeGreen, 4))
                {
                    for (int i = 0; i < ultimaRuta.CantidadParadas - 1; i++)
                    {
                        int u = grafo.ObtenerIndice(ultimaRuta.Codigos[i]);
                        int v = grafo.ObtenerIndice(ultimaRuta.Codigos[i + 1]);
                        if (u != -1 && v != -1)
                        {
                            g.DrawLine(penRuta, posiciones[u], posiciones[v]);
                        }
                    }
                }
            }

            float radioNodo = 18f;
            using (Font fontCodigo = new Font("Arial", 7.5f, FontStyle.Bold))
            using (Font fontNombre = new Font("Arial", 7f))
            {
                for (int i = 0; i < n; i++)
                {
                    if (!grafo.EsIndiceActivo(i)) continue;
                    
                    string codigo = grafo.ObtenerCodigoPorIndice(i);
                    bool perteneceARuta = EsNodoDeRuta(codigo);

                    Brush pincelNodo = perteneceARuta ? Brushes.LightGreen : Brushes.CornflowerBlue;
                    Pen bordeNodo = perteneceARuta ? new Pen(Color.DarkGreen, 2) : new Pen(Color.MidnightBlue, 2);

                    float x = posiciones[i].X - radioNodo;
                    float y = posiciones[i].Y - radioNodo;

                    g.FillEllipse(pincelNodo, x, y, radioNodo * 2, radioNodo * 2);
                    g.DrawEllipse(bordeNodo, x, y, radioNodo * 2, radioNodo * 2);

                    SizeF tamCod = g.MeasureString(codigo, fontCodigo);
                    g.DrawString(codigo, fontCodigo, Brushes.Black, posiciones[i].X - (tamCod.Width / 2f), posiciones[i].Y - (tamCod.Height / 2f));

                    Parada? p = tablaParadas.Buscar(codigo);
                    if (p != null)
                    {
                        g.DrawString(p.Nombre, fontNombre, Brushes.DarkSlateGray, posiciones[i].X - 25, posiciones[i].Y + radioNodo + 2);
                    }
                }
            }
        }

        private bool EsNodoDeRuta(string codigo)
        {
            if (ultimaRuta == null || !ultimaRuta.Existe) return false;
            for (int i = 0; i < ultimaRuta.CantidadParadas; i++)
            {
                if (ultimaRuta.Codigos[i] == codigo) return true;
            }
            return false;
        }
    }

    // =====================================================================
    // VENTANA 2: REGISTRO Y MODIFICACIÓN DE CONEXIONES (RUTAS)
    // =====================================================================
    public class FormGestionRuta : Form
    {
        private Grafo grafo;
        private UnionFind uf;
        private Action<string> delegadolog;

        private ComboBox cmbOrigen;
        private ComboBox cmbDestino;
        private TextBox txtPeso;
        private Button btnGuardar;

        public FormGestionRuta(Grafo grafo, UnionFind uf, Action<string> log)
        {
            this.grafo = grafo;
            this.uf = uf;
            this.delegadolog = log;

            this.Text = "Gestionar Conexión de Ruta";
            this.Size = new Size(380, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            ConstruirFormulario();
        }

        private void ConstruirFormulario()
        {
            Label lblOri = new Label { Text = "Parada Origen:", Bounds = new Rectangle(20, 20, 120, 20) };
            cmbOrigen = new ComboBox { Bounds = new Rectangle(140, 20, 200, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblDes = new Label { Text = "Parada Destino:", Bounds = new Rectangle(20, 60, 120, 20) };
            cmbDestino = new ComboBox { Bounds = new Rectangle(140, 60, 200, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblPeso = new Label { Text = "Distancia / Peso:", Bounds = new Rectangle(20, 100, 120, 20) };
            txtPeso = new TextBox { Bounds = new Rectangle(140, 100, 200, 25) };

            Label lblAyuda = new Label { Text = "* Deje vacío el peso para calcular por coordenadas Haversine.", Font = new Font("Arial", 7.5f), Bounds = new Rectangle(20, 130, 320, 20), ForeColor = Color.DimGray };

            btnGuardar = new Button { Text = "Guardar / Modificar Ruta", Bounds = new Rectangle(90, 165, 180, 35), BackColor = Color.LightSkyBlue, Font = new Font("Arial", 9, FontStyle.Bold) };
            btnGuardar.Click += BtnGuardar_Click;

            for (int i = 0; i < grafo.CapacidadIndices; i++)
            {
                if (grafo.EsIndiceActivo(i))
                {
                    string cod = grafo.ObtenerCodigoPorIndice(i);
                    cmbOrigen.Items.Add(cod);
                    cmbDestino.Items.Add(cod);
                }
            }

            if (cmbOrigen.Items.Count > 0) cmbOrigen.SelectedIndex = 0;
            if (cmbDestino.Items.Count > 1) cmbDestino.SelectedIndex = 1;

            this.Controls.AddRange(new Control[] { lblOri, cmbOrigen, lblDes, cmbDestino, lblPeso, txtPeso, lblAyuda, btnGuardar });
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (cmbOrigen.SelectedItem == null || cmbDestino.SelectedItem == null)
            {
                MessageBox.Show("Seleccione las paradas.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string origen = cmbOrigen.SelectedItem.ToString()!;
            string destino = cmbDestino.SelectedItem.ToString()!;

            if (origen == destino)
            {
                MessageBox.Show("El origen y destino no pueden ser el mismo nodo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double? peso = null;
            if (!string.IsNullOrWhiteSpace(txtPeso.Text))
            {
                if (double.TryParse(txtPeso.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double valor) && valor >= 0)
                    peso = valor;
                else
                {
                    MessageBox.Show("El peso debe ser un número decimal válido mayor o igual a cero.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (grafo.AgregarArista(origen, destino, peso))
            {
                uf.Union(grafo.ObtenerIndice(origen), grafo.ObtenerIndice(destino));
                try
                {
                    string rutaConexiones = Path.Combine("Datos", "conexiones.csv");
                    string linea = peso.HasValue ? $"{origen},{destino},{peso.Value.ToString("F2", CultureInfo.InvariantCulture)}" : $"{origen},{destino}";
                    File.AppendAllText(rutaConexiones, Environment.NewLine + linea);
                }
                catch { }
                delegadolog($"-> Conexión {origen} <-> {destino} guardada con éxito.");
                this.Close();
            }
        }
    }

    // =====================================================================
    // VENTANA 3: AGREGAR NUEVA PARADA AL SISTEMA
    // =====================================================================
    public class FormAgregarParada : Form
    {
        private Trie trie;
        private TablaHash<string, Parada> hash;
        private Grafo grafo;
        private UnionFind uf;
        private Action<string> delegadolog;

        private TextBox txtCodigo;
        private TextBox txtNombre;
        private TextBox txtLat;
        private TextBox txtLon;

        public FormAgregarParada(Trie trie, TablaHash<string, Parada> hash, Grafo grafo, UnionFind uf, Action<string> log)
        {
            this.trie = trie; this.hash = hash; this.grafo = grafo; this.uf = uf; this.delegadolog = log;
            this.Text = "Agregar Nueva Parada";
            this.Size = new Size(350, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label l1 = new Label { Text = "Código:", Bounds = new Rectangle(20, 20, 80, 20) };
            txtCodigo = new TextBox { Bounds = new Rectangle(100, 20, 200, 25) };
            Label l2 = new Label { Text = "Nombre:", Bounds = new Rectangle(20, 60, 80, 20) };
            txtNombre = new TextBox { Bounds = new Rectangle(100, 60, 200, 25) };
            Label l3 = new Label { Text = "Latitud:", Bounds = new Rectangle(20, 100, 80, 20) };
            txtLat = new TextBox { Bounds = new Rectangle(100, 100, 200, 25) };
            Label l4 = new Label { Text = "Longitud:", Bounds = new Rectangle(20, 140, 80, 20) };
            txtLon = new TextBox { Bounds = new Rectangle(100, 140, 200, 25) };

            Button btn = new Button { Text = "Registrar Parada", Bounds = new Rectangle(80, 185, 180, 35), BackColor = Color.LightGreen, Font = new Font("Arial", 9, FontStyle.Bold) };
            btn.Click += BtnGuardar_Click;

            this.Controls.AddRange(new Control[] { l1, txtCodigo, l2, txtNombre, l3, txtLat, l4, txtLon, btn });
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            string cod = txtCodigo.Text.Trim();
            string nom = txtNombre.Text.Trim();

            if (string.IsNullOrEmpty(cod) || string.IsNullOrEmpty(nom)) { MessageBox.Show("Código y nombre son obligatorios."); return; }
            if (hash.Buscar(cod) != null) { MessageBox.Show("El código ya existe."); return; }
            if (!double.TryParse(txtLat.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(txtLon.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
            {
                MessageBox.Show("Coordenadas inválidas."); return;
            }

            Parada p = new Parada(cod, nom, lat, lon);
            trie.Insertar(nom, cod);
            hash.Insertar(cod, p);
            if (grafo.AgregarVertice(p))
            {
                uf.AsegurarCapacidad(grafo.CapacidadIndices);
                try { File.AppendAllText(Path.Combine("Datos", "paradas.csv"), Environment.NewLine + $"{cod},{nom},{lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}"); } catch { }
                delegadolog($"-> Parada {nom} [{cod}] agregada a todas las estructuras.");
                this.Close();
            }
        }
    }

    // =====================================================================
    // VENTANA 4: ELIMINAR PARADA (RECONSTRUYE RELACIONES)
    // =====================================================================
    public class FormEliminarParada : Form
    {
        private Trie trie;
        private TablaHash<string, Parada> hash;
        private Grafo grafo;
        private UnionFind uf;
        private Action<string> delegadolog;
        private ComboBox cmbParadas;

        public FormEliminarParada(Trie trie, TablaHash<string, Parada> hash, Grafo grafo, UnionFind uf, Action<string> log)
        {
            this.trie = trie; this.hash = hash; this.grafo = grafo; this.uf = uf; this.delegadolog = log;
            this.Text = "Eliminar Parada";
            this.Size = new Size(350, 180);
            this.StartPosition = FormStartPosition.CenterParent;

            Label l1 = new Label { Text = "Seleccione Parada:", Bounds = new Rectangle(20, 20, 120, 20) };
            cmbParadas = new ComboBox { Bounds = new Rectangle(140, 20, 180, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            
            for (int i = 0; i < grafo.CapacidadIndices; i++)
            {
                if (grafo.EsIndiceActivo(i)) cmbParadas.Items.Add(grafo.ObtenerCodigoPorIndice(i));
            }
            if (cmbParadas.Items.Count > 0) cmbParadas.SelectedIndex = 0;

            Button btn = new Button { Text = "Eliminar", Bounds = new Rectangle(90, 80, 150, 35), BackColor = Color.LightCoral, Font = new Font("Arial", 9, FontStyle.Bold) };
            btn.Click += BtnEliminar_Click;

            this.Controls.AddRange(new Control[] { l1, cmbParadas, btn });
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (cmbParadas.SelectedItem == null) return;
            string codigo = cmbParadas.SelectedItem.ToString()!;
            Parada? p = hash.Buscar(codigo);
            if (p == null) return;

            hash.Eliminar(codigo);
            trie.Eliminar(p.Nombre);
            grafo.EliminarVertice(codigo);

            // Reconstruye Union-Find porque la eliminación pudo desconectar zonas
            uf.Reiniciar();
            for (int u = 0; u < grafo.CapacidadIndices; u++)
            {
                if (!grafo.EsIndiceActivo(u)) continue;
                NodoArista? actual = grafo.ObtenerCabeza(u);
                while (actual != null)
                {
                    if (grafo.EsIndiceActivo(actual.Destino)) uf.Union(u, actual.Destino);
                    actual = actual.Siguiente;
                }
            }

            ActualizarCSV(codigo);
            delegadolog($"-> Parada {codigo} eliminada completamente del sistema.");
            MessageBox.Show("Parada eliminada correctamente.");
            this.Close();
        }

        private void ActualizarCSV(string codigoEliminar)
        {
            try
            {
                string rutaP = Path.Combine("Datos", "paradas.csv");
                if (File.Exists(rutaP))
                {
                    string[] lineas = File.ReadAllLines(rutaP);
                    string[] nuevas = new string[lineas.Length];
                    int c = 0;
                    for (int i = 0; i < lineas.Length; i++)
                    {
                        if (lineas[i].Split(',')[0].Trim() != codigoEliminar) nuevas[c++] = lineas[i];
                    }
                    string[] final = new string[c];
                    Array.Copy(nuevas, final, c);
                    File.WriteAllLines(rutaP, final);
                }

                string rutaC = Path.Combine("Datos", "conexiones.csv");
                if (File.Exists(rutaC))
                {
                    string[] lineasC = File.ReadAllLines(rutaC);
                    string[] nuevasC = new string[lineasC.Length];
                    int c2 = 0;
                    for (int i = 0; i < lineasC.Length; i++)
                    {
                        string[] pts = lineasC[i].Split(',');
                        if (pts.Length >= 2 && pts[0].Trim() != codigoEliminar && pts[1].Trim() != codigoEliminar) nuevasC[c2++] = lineasC[i];
                    }
                    string[] finalC = new string[c2];
                    Array.Copy(nuevasC, finalC, c2);
                    File.WriteAllLines(rutaC, finalC);
                }
            }
            catch { }
        }
    }
}