using System;
using System.IO;
using System.Windows.Forms;

namespace ProyectoRedTransporte
{
    public partial class FormPrincipal : Form
    {
        private Trie trieNombres = new Trie();
        private TablaHash<string, Parada> tablaParadas = new TablaHash<string, Parada>(101);
        private Grafo grafoRed = new Grafo(false);
        private UnionFind conjuntosUF = new UnionFind(16);

        private string codigoOrigenSeleccionado = "";
        private string codigoDestinoSeleccionado = "";
        private RutaResultado? ultimaRutaCalculada = null; 

        public FormPrincipal()
        {
            ConfigurarVentana();
        }

        private void BtnCargarDatos_Click(object sender, EventArgs e)
        {
            string rutaParadas = Path.Combine("Datos", "paradas.csv");
            string rutaConexiones = Path.Combine("Datos", "conexiones.csv");

            string msjP = LectorCSV.CargarParadas(rutaParadas, trieNombres, tablaParadas, grafoRed, conjuntosUF);
            string msjC = LectorCSV.CargarConexiones(rutaConexiones, grafoRed, conjuntosUF);
            Log("DATOS CARGADOS:\n" + msjP + "\n" + msjC);
        }

        private void BtnVerGrafo_Click(object sender, EventArgs e)
        {
            if (grafoRed.CapacidadIndices == 0) { MessageBox.Show("Cargue los datos primero."); return; }
            FormVisualizadorGrafo visor = new FormVisualizadorGrafo(grafoRed, tablaParadas, ultimaRutaCalculada);
            visor.ShowDialog(this);
        }

        private void BtnGestionRuta_Click(object sender, EventArgs e)
        {
            if (grafoRed.CapacidadIndices < 2) { MessageBox.Show("Debe haber al menos 2 paradas registradas."); return; }
            FormGestionRuta formRuta = new FormGestionRuta(grafoRed, conjuntosUF, Log);
            formRuta.ShowDialog(this);
        }

        // EVENTO NUEVO PARA ABRIR VENTANA DE AGREGAR PARADA
        private void BtnAgregarParada_Click(object sender, EventArgs e)
        {
            FormAgregarParada form = new FormAgregarParada(trieNombres, tablaParadas, grafoRed, conjuntosUF, Log);
            form.ShowDialog(this);
        }

        // EVENTO NUEVO PARA ABRIR VENTANA DE ELIMINAR PARADA
        private void BtnEliminarParada_Click(object sender, EventArgs e)
        {
            if (grafoRed.CapacidadIndices == 0) { MessageBox.Show("No hay paradas registradas en el sistema para eliminar."); return; }
            FormEliminarParada form = new FormEliminarParada(trieNombres, tablaParadas, grafoRed, conjuntosUF, Log);
            form.ShowDialog(this);
        }

        private void BuscarSugerencias(string prefijo, ListBox lst)
        {
            lst.Items.Clear();
            if (string.IsNullOrWhiteSpace(prefijo)) return;
            string[] sugerencias = new string[10]; string[] codigos = new string[10]; int contador = 0;
            trieNombres.ObtenerSugerencias(prefijo, ref sugerencias, ref codigos, ref contador, 10);
            for (int i = 0; i < contador; i++) lst.Items.Add($"{sugerencias[i]} [{codigos[i]}]");
        }

        private void SeleccionarParada(ListBox lst, ref string codigoSeleccionado, Label lblIndicador, string tipo)
        {
            if (lst.SelectedItem == null) return;
            string seleccion = lst.SelectedItem.ToString()!;
            codigoSeleccionado = seleccion.Substring(seleccion.IndexOf('[') + 1, seleccion.IndexOf(']') - seleccion.IndexOf('[') - 1);
            lblIndicador.Text = $"{tipo} seleccionado:\n{codigoSeleccionado}";
            Log($"Paso A completado: Se seleccionó {codigoSeleccionado} desde el Trie.");
        }

        private void BtnCalcularRuta_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoOrigenSeleccionado) || string.IsNullOrEmpty(codigoDestinoSeleccionado)) { MessageBox.Show("Debe seleccionar un origen y un destino."); return; }
            Log("\n--- INICIANDO CÁLCULO DE RUTA ---");
            Parada? origen = tablaParadas.Buscar(codigoOrigenSeleccionado);
            Parada? destino = tablaParadas.Buscar(codigoDestinoSeleccionado);
            if (origen == null || destino == null) { MessageBox.Show("Parada eliminada o no válida. Vuelva a buscarla."); return; }
            
            Log($"Paso B (Hash): Localizados en RAM -> {origen.Nombre} y {destino.Nombre}");
            int idxO = grafoRed.ObtenerIndice(origen.Codigo); int idxD = grafoRed.ObtenerIndice(destino.Codigo);
            
            if (!conjuntosUF.EstanConectados(idxO, idxD)) {
                Log(">>> BLOQUEADO POR UNION-FIND: Zonas desconectadas. Dijkstra omitido.");
                ultimaRutaCalculada = null; return;
            }
            
            Log("Union-Find confirma conectividad. Permiso concedido al Grafo.\nPaso D (Grafo): Ejecutando Dijkstra...\n");
            ultimaRutaCalculada = grafoRed.Dijkstra(origen.Codigo, destino.Codigo);
            if (ultimaRutaCalculada.Existe) {
                Log($">>> RUTA ENCONTRADA. Distancia Total: {ultimaRutaCalculada.DistanciaTotal:F2} km.");
                string camino = "";
                for (int i = 0; i < ultimaRutaCalculada.CantidadParadas; i++) {
                    camino += ultimaRutaCalculada.Paradas[i].Nombre;
                    if (i < ultimaRutaCalculada.CantidadParadas - 1) camino += " -> ";
                }
                Log("Camino Óptimo: " + camino);
            }
        }

        private void BtnEjecutarBFS_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoOrigenSeleccionado)) return;
            Parada? origen = tablaParadas.Buscar(codigoOrigenSeleccionado);
            if (origen == null) return;
            Log($"\n--- INICIANDO EXPLORACIÓN EN ANCHURA (BFS) desde {origen.Nombre} ---");
            int visitados; string[] resultado = grafoRed.BFS(codigoOrigenSeleccionado, out visitados);
            string caminoStr = "";
            for (int i = 0; i < visitados; i++) { caminoStr += resultado[i]; if (i < visitados - 1) caminoStr += " -> "; }
            Log($"Nodos explorados por nivel (BFS): {visitados}\nSecuencia: " + caminoStr);
        }

        private void BtnEjecutarDFS_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoOrigenSeleccionado)) return;
            Parada? origen = tablaParadas.Buscar(codigoOrigenSeleccionado);
            if (origen == null) return;
            Log($"\n--- INICIANDO EXPLORACIÓN EN PROFUNDIDAD (DFS) desde {origen.Nombre} ---");
            int visitados; string[] resultado = grafoRed.DFS(codigoOrigenSeleccionado, out visitados);
            string caminoStr = "";
            for (int i = 0; i < visitados; i++) { caminoStr += resultado[i]; if (i < visitados - 1) caminoStr += " -> "; }
            Log($"Nodos explorados por rama (DFS): {visitados}\nSecuencia: " + caminoStr);
        }

        private void Log(string mensaje) { rtbConsola.AppendText(mensaje + "\n"); rtbConsola.ScrollToCaret(); }
    }
}