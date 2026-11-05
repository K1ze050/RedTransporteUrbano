using System;
using System.Windows.Forms;

namespace ProyectoRedTransporte
{
    // La otra mitad de la clase partial
    public partial class FormPrincipal : Form
    {
        // Instancias de las estructuras (Ahora el compilador sabe qué son porque comparten namespace)
        private Trie trieNombres = new Trie();
        private TablaHash<string, Parada> tablaParadas = new TablaHash<string, Parada>(101);
        private Grafo grafoRed = new Grafo(false);
        private UnionFind conjuntosUF = new UnionFind(16);

        private string codigoOrigenSeleccionado = "";
        private string codigoDestinoSeleccionado = "";

        public FormPrincipal()
        {
            // Llama a la configuración visual declarada en FormPrincipal.Designer.cs
            ConfigurarVentana();
        }

        private void BtnCargarDatos_Click(object sender, EventArgs e)
        {
            string msjP = LectorCSV.CargarParadas("paradas.csv", trieNombres, tablaParadas, grafoRed, conjuntosUF);
            string msjC = LectorCSV.CargarConexiones("conexiones.csv", grafoRed, conjuntosUF);
            Log("DATOS CARGADOS:\n" + msjP + "\n" + msjC);
        }

        private void BuscarSugerencias(string prefijo, ListBox lst)
        {
            lst.Items.Clear();
            if (string.IsNullOrWhiteSpace(prefijo)) return;
            string[] sugerencias = new string[10];
            string[] codigos = new string[10];
            int contador = 0;
            trieNombres.ObtenerSugerencias(prefijo, ref sugerencias, ref codigos, ref contador, 10);
            for (int i = 0; i < contador; i++) lst.Items.Add($"{sugerencias[i]} [{codigos[i]}]");
        }

        private void SeleccionarParada(ListBox lst, ref string codigoSeleccionado, Label lblIndicador, string tipo)
        {
            if (lst.SelectedItem == null) return;
            string seleccion = lst.SelectedItem.ToString();
            codigoSeleccionado = seleccion.Substring(seleccion.IndexOf('[') + 1, seleccion.IndexOf(']') - seleccion.IndexOf('[') - 1);
            lblIndicador.Text = $"{tipo} seleccionado:\n{codigoSeleccionado}";
            Log($"Paso A completado: Se seleccionó {codigoSeleccionado} desde el Trie.");
        }

        private void BtnCalcularRuta_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoOrigenSeleccionado) || string.IsNullOrEmpty(codigoDestinoSeleccionado))
            {
                MessageBox.Show("Debe seleccionar un origen y un destino usando el buscador.");
                return;
            }

            Log("\n--- INICIANDO CÁLCULO DE RUTA ---");
            Parada origen = tablaParadas.Buscar(codigoOrigenSeleccionado);
            Parada destino = tablaParadas.Buscar(codigoDestinoSeleccionado);
            
            Log($"Paso B (Hash): Localizados en RAM -> {origen.Nombre} y {destino.Nombre}");

            int idxO = grafoRed.ObtenerIndice(origen.Codigo);
            int idxD = grafoRed.ObtenerIndice(destino.Codigo);
            Log($"Paso C (Union-Find): Verificando conectividad...");
            
            if (!conjuntosUF.EstanConectados(idxO, idxD))
            {
                Log(">>> BLOQUEADO POR UNION-FIND: Los puntos pertenecen a zonas desconectadas. Dijkstra omitido.");
                return;
            }
            
            Log("Union-Find confirma conectividad. Permiso concedido al Grafo.");
            Log("Paso D (Grafo): Ejecutando Algoritmo de Dijkstra...\n");
            
            RutaResultado resultado = grafoRed.Dijkstra(origen.Codigo, destino.Codigo);
            if (resultado.Existe)
            {
                Log($">>> RUTA ENCONTRADA. Distancia Total: {resultado.DistanciaTotal:F2} unidades.");
                string camino = "";
                for (int i = 0; i < resultado.CantidadParadas; i++)
                {
                    camino += resultado.Paradas[i].Nombre;
                    if (i < resultado.CantidadParadas - 1) camino += " -> ";
                }
                Log("Camino Óptimo: " + camino);
            }
        }

        private void BtnEjecutarBFS_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoOrigenSeleccionado))
            {
                MessageBox.Show("Debe seleccionar un origen para iniciar el recorrido BFS.");
                return;
            }

            Log("\n--- INICIANDO EXPLORACIÓN EN ANCHURA (BFS) ---");
            Parada origen = tablaParadas.Buscar(codigoOrigenSeleccionado);
            Log($"Punto de partida: {origen.Nombre} [{origen.Codigo}]");

            int totalVisitados;
            string[] resultado = grafoRed.BFS(codigoOrigenSeleccionado, out totalVisitados);

            string caminoStr = "";
            for (int i = 0; i < totalVisitados; i++)
            {
                caminoStr += resultado[i];
                if (i < totalVisitados - 1) caminoStr += " -> ";
            }

            Log($"Nodos explorados por nivel (BFS): {totalVisitados}");
            Log("Secuencia de visita: " + caminoStr);
        }

        private void BtnEjecutarDFS_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(codigoOrigenSeleccionado))
            {
                MessageBox.Show("Debe seleccionar un origen para iniciar el recorrido DFS.");
                return;
            }

            Log("\n--- INICIANDO EXPLORACIÓN EN PROFUNDIDAD (DFS) ---");
            Parada origen = tablaParadas.Buscar(codigoOrigenSeleccionado);
            Log($"Punto de partida: {origen.Nombre} [{origen.Codigo}]");

            int totalVisitados;
            string[] resultado = grafoRed.DFS(codigoOrigenSeleccionado, out totalVisitados);

            string caminoStr = "";
            for (int i = 0; i < totalVisitados; i++)
            {
                caminoStr += resultado[i];
                if (i < totalVisitados - 1) caminoStr += " -> ";
            }

            Log($"Nodos explorados por rama (DFS): {totalVisitados}");
            Log("Secuencia de visita: " + caminoStr);
        }

        private void Log(string mensaje)
        {
            rtbConsola.AppendText(mensaje + "\n");
            rtbConsola.ScrollToCaret();
        }
    }
}
