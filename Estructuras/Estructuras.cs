using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace ProyectoRedTransporte
{
    public class Parada
    {
        public string Codigo;        
        public string Nombre;        
        public double Latitud;      
        public double Longitud;     

        public Parada(string codigo, string nombre, double latitud, double longitud)
        {
            Codigo = codigo; Nombre = nombre; Latitud = latitud; Longitud = longitud;
        }
    }

    // =====================================================================
    // TRIE Y HASH (CON FUNCIONES DE ELIMINAR)
    // =====================================================================
    public class NodoTrie
    {
        public char Caracter;
        public bool EsFinDePalabra;
        public string? CodigoAsociado;
        public string? PalabraCompleta;
        public NodoTrie?[] Hijos;

        public NodoTrie(char c)
        {
            Caracter = c; EsFinDePalabra = false; CodigoAsociado = null; PalabraCompleta = null;
            Hijos = new NodoTrie?[256];
        }
    }

    public class Trie
    {
        private NodoTrie raiz;
        public Trie() { raiz = new NodoTrie('\0'); }

        public void Insertar(string palabra, string codigoAsociado)
        {
            if (string.IsNullOrEmpty(palabra)) return;
            NodoTrie actual = raiz;
            foreach (char c in palabra)
            {
                int indice = (int)c;
                if (indice < 0 || indice >= 256) continue;
                if (actual.Hijos[indice] == null) actual.Hijos[indice] = new NodoTrie(c);
                actual = actual.Hijos[indice]!;
            }
            actual.EsFinDePalabra = true; actual.PalabraCompleta = palabra; actual.CodigoAsociado = codigoAsociado;
        }

        public void ObtenerSugerencias(string prefijo, ref string[] sugerencias, ref string[] codigos, ref int contador, int limiteMaximo)
        {
            if (string.IsNullOrEmpty(prefijo)) return;
            NodoTrie actual = raiz;
            foreach (char c in prefijo)
            {
                int indice = (int)c;
                if (indice < 0 || indice >= 256 || actual.Hijos[indice] == null) return;
                actual = actual.Hijos[indice]!;
            }
            RecolectarRecursivo(actual, ref sugerencias, ref codigos, ref contador, limiteMaximo);
        }

        private void RecolectarRecursivo(NodoTrie nodo, ref string[] sugerencias, ref string[] codigos, ref int contador, int limiteMaximo)
        {
            if (nodo == null || contador >= limiteMaximo) return;
            if (nodo.EsFinDePalabra && nodo.PalabraCompleta != null && nodo.CodigoAsociado != null)
            {
                sugerencias[contador] = nodo.PalabraCompleta; codigos[contador] = nodo.CodigoAsociado; contador++;
            }
            for (int i = 0; i < nodo.Hijos.Length; i++)
            {
                if (nodo.Hijos[i] != null) RecolectarRecursivo(nodo.Hijos[i]!, ref sugerencias, ref codigos, ref contador, limiteMaximo);
            }
        }

        // NUEVO: Permite eliminar palabras del árbol
        public bool Eliminar(string palabra)
        {
            if (string.IsNullOrEmpty(palabra)) return false;
            EliminarRecursivo(raiz, palabra, 0);
            return true;
        }

        private bool EliminarRecursivo(NodoTrie nodo, string palabra, int profundidad)
        {
            if (nodo == null) return false;
            if (profundidad == palabra.Length)
            {
                if (!nodo.EsFinDePalabra) return false;
                nodo.EsFinDePalabra = false; nodo.PalabraCompleta = null; nodo.CodigoAsociado = null;
                return TieneHijosVacios(nodo);
            }
            int indice = (int)palabra[profundidad];
            if (indice < 0 || indice >= 256 || nodo.Hijos[indice] == null) return false;
            bool debeBorrar = EliminarRecursivo(nodo.Hijos[indice]!, palabra, profundidad + 1);
            if (debeBorrar)
            {
                nodo.Hijos[indice] = null;
                return !nodo.EsFinDePalabra && TieneHijosVacios(nodo);
            }
            return false;
        }
        private bool TieneHijosVacios(NodoTrie nodo)
        {
            for (int i = 0; i < nodo.Hijos.Length; i++) if (nodo.Hijos[i] != null) return false;
            return true;
        }
    }

    public class NodoHash<TClave, TValor>
    {
        public TClave Clave;
        public TValor Valor;
        public NodoHash<TClave, TValor>? Siguiente;
        public NodoHash(TClave clave, TValor valor) { Clave = clave; Valor = valor; Siguiente = null; }
    }

    public class TablaHash<TClave, TValor>
    {
        private NodoHash<TClave, TValor>?[] cubos;
        private int capacidad;
        private int cantidadElementos;

        public TablaHash(int capacidad = 101)
        {
            this.capacidad = capacidad; this.cubos = new NodoHash<TClave, TValor>?[capacidad]; this.cantidadElementos = 0;
        }

        private int ObtenerIndiceHash(TClave clave)
        {
            if (clave == null) return 0;
            string textoClave = clave.ToString()!;
            int hash = 0;
            foreach (char c in textoClave) hash = (hash * 31 + c) % capacidad;
            return Math.Abs(hash);
        }

        public void Insertar(TClave clave, TValor valor)
        {
            if (clave == null) return;
            int indice = ObtenerIndiceHash(clave);
            NodoHash<TClave, TValor> nuevoNodo = new NodoHash<TClave, TValor>(clave, valor);
            if (cubos[indice] == null) { cubos[indice] = nuevoNodo; cantidadElementos++; }
            else
            {
                NodoHash<TClave, TValor>? actual = cubos[indice];
                while (actual != null)
                {
                    if (actual.Clave != null && actual.Clave.Equals(clave)) { actual.Valor = valor; return; }
                    if (actual.Siguiente == null) break;
                    actual = actual.Siguiente;
                }
                actual!.Siguiente = nuevoNodo; cantidadElementos++;
            }
        }

        public TValor? Buscar(TClave clave)
        {
            if (clave == null) return default;
            int indice = ObtenerIndiceHash(clave);
            NodoHash<TClave, TValor>? actual = cubos[indice];
            while (actual != null)
            {
                if (actual.Clave != null && actual.Clave.Equals(clave)) return actual.Valor;
                actual = actual.Siguiente;
            }
            return default;
        }

        // NUEVO: Elimina registros del bucket en tiempo O(1) promedio
        public bool Eliminar(TClave clave)
        {
            if (clave == null) return false;
            int indice = ObtenerIndiceHash(clave);
            NodoHash<TClave, TValor>? actual = cubos[indice];
            NodoHash<TClave, TValor>? anterior = null;
            while (actual != null)
            {
                if (actual.Clave != null && actual.Clave.Equals(clave))
                {
                    if (anterior == null) cubos[indice] = actual.Siguiente;
                    else anterior.Siguiente = actual.Siguiente;
                    cantidadElementos--;
                    return true;
                }
                anterior = actual;
                actual = actual.Siguiente;
            }
            return false;
        }
    }

    // =====================================================================
    // GRAFO CON SOPORTE PARA ELIMINACIÓN DE VÉRTICES
    // =====================================================================
    public class MinHeap {
        private int[] vertices; private double[] prioridades; private int tamano;
        public MinHeap(int capacidadInicial = 16) { vertices = new int[capacidadInicial]; prioridades = new double[capacidadInicial]; tamano = 0; }
        public void Insertar(int vertice, double prioridad) { if (tamano == vertices.Length) Crecer(); vertices[tamano] = vertice; prioridades[tamano] = prioridad; SubirElemento(tamano); tamano++; }
        public bool ExtraerMin(out int vertice, out double prioridad) { vertice = -1; prioridad = double.PositiveInfinity; if (tamano == 0) return false; vertice = vertices[0]; prioridad = prioridades[0]; tamano--; if (tamano > 0) { vertices[0] = vertices[tamano]; prioridades[0] = prioridades[tamano]; BajarElemento(0); } return true; }
        private void SubirElemento(int i) { while (i > 0) { int padre = (i - 1) / 2; if (prioridades[i] < prioridades[padre]) { Intercambiar(i, padre); i = padre; } else break; } }
        private void BajarElemento(int i) { while (true) { int izq = 2 * i + 1; int der = 2 * i + 2; int menor = i; if (izq < tamano && prioridades[izq] < prioridades[menor]) menor = izq; if (der < tamano && prioridades[der] < prioridades[menor]) menor = der; if (menor == i) break; Intercambiar(i, menor); i = menor; } }
        private void Intercambiar(int a, int b) { int tv = vertices[a]; vertices[a] = vertices[b]; vertices[b] = tv; double tp = prioridades[a]; prioridades[a] = prioridades[b]; prioridades[b] = tp; }
        private void Crecer() { int[] nV = new int[vertices.Length * 2]; double[] nP = new double[prioridades.Length * 2]; for (int i = 0; i < tamano; i++) { nV[i] = vertices[i]; nP[i] = prioridades[i]; } vertices = nV; prioridades = nP; }
    }

    public class NodoArista { public int Destino; public double Peso; public NodoArista? Siguiente; public NodoArista(int destino, double peso) { Destino = destino; Peso = peso; Siguiente = null; } }
    public class RutaResultado { public bool Existe = false; public double DistanciaTotal = 0; public int CantidadParadas = 0; public string[] Codigos = new string[0]; public Parada[] Paradas = new Parada[0]; public string Mensaje = ""; }

    public class Grafo
    {
        private bool dirigido; private int capacidad; private int cantidadSlots;
        private string[] codigos; private Parada?[] paradas; private bool[] activos; // <- NUEVO: Control de paradas borradas
        private NodoArista?[] cabezas; private NodoArista?[] colas; private TablaHash<string, int> indices;

        public Grafo(bool dirigido = false, int capacidadInicial = 16)
        {
            this.dirigido = dirigido; capacidad = capacidadInicial; codigos = new string[capacidad]; paradas = new Parada?[capacidad];
            activos = new bool[capacidad]; cabezas = new NodoArista?[capacidad]; colas = new NodoArista?[capacidad]; indices = new TablaHash<string, int>(101);
        }

        public int CapacidadIndices => cantidadSlots;
        public bool EsIndiceActivo(int u) { return u >= 0 && u < cantidadSlots && activos[u]; }
        public NodoArista? ObtenerCabeza(int u) { if (!EsIndiceActivo(u)) return null; return cabezas[u]; }
        public string ObtenerCodigoPorIndice(int idx) { if (!EsIndiceActivo(idx)) return ""; return codigos[idx]; }
        public int ObtenerIndice(string codigo) { if (string.IsNullOrEmpty(codigo)) return -1; int idx = indices.Buscar(codigo) - 1; return (idx != -1 && activos[idx]) ? idx : -1; }

        public bool AgregarVertice(Parada parada)
        {
            if (ObtenerIndice(parada.Codigo) != -1) return false;
            if (cantidadSlots == capacidad) CrecerArreglos();
            int idx = cantidadSlots; codigos[idx] = parada.Codigo; paradas[idx] = parada; activos[idx] = true;
            indices.Insertar(parada.Codigo, idx + 1); cantidadSlots++; return true;
        }

        public bool AgregarArista(string codigoOrigen, string codigoDestino, double? peso = null)
        {
            int u = ObtenerIndice(codigoOrigen); int v = ObtenerIndice(codigoDestino);
            if (u == -1 || v == -1) return false;
            double w = peso ?? DistanciaHaversine(paradas[u]!, paradas[v]!);
            PonerArco(u, v, w); if (!dirigido && u != v) PonerArco(v, u, w); return true;
        }

        private void PonerArco(int u, int v, double peso)
        {
            NodoArista? actual = cabezas[u];
            while (actual != null) { if (actual.Destino == v) { actual.Peso = peso; return; } actual = actual.Siguiente; }
            NodoArista nuevo = new NodoArista(v, peso);
            if (cabezas[u] == null) { cabezas[u] = nuevo; colas[u] = nuevo; } else { colas[u]!.Siguiente = nuevo; colas[u] = nuevo; }
        }

        // NUEVO: Elimina vértice y todas las conexiones que apuntaban a él
        public bool EliminarVertice(string codigo)
        {
            int idx = indices.Buscar(codigo) - 1;
            if (idx == -1 || !activos[idx]) return false;

            for (int u = 0; u < cantidadSlots; u++) { if (activos[u] && u != idx) EliminarArco(u, idx); }
            cabezas[idx] = null; colas[idx] = null; activos[idx] = false; paradas[idx] = null; indices.Eliminar(codigo);
            return true;
        }

        private bool EliminarArco(int u, int v)
        {
            NodoArista? anterior = null; NodoArista? actual = cabezas[u];
            while (actual != null) {
                if (actual.Destino == v) {
                    if (anterior == null) cabezas[u] = actual.Siguiente; else anterior.Siguiente = actual.Siguiente;
                    if (colas[u] == actual) colas[u] = anterior;
                    return true;
                }
                anterior = actual; actual = actual.Siguiente;
            }
            return false;
        }

        public static double DistanciaHaversine(Parada a, Parada b)
        {
            double lat1 = a.Latitud * Math.PI / 180.0; double lat2 = b.Latitud * Math.PI / 180.0;
            double h = Math.Sin((b.Latitud - a.Latitud) * Math.PI / 360.0) * Math.Sin((b.Latitud - a.Latitud) * Math.PI / 360.0) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin((b.Longitud - a.Longitud) * Math.PI / 360.0) * Math.Sin((b.Longitud - a.Longitud) * Math.PI / 360.0);
            return 6371.0 * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
        }

        public RutaResultado Dijkstra(string codigoOrigen, string codigoDestino)
        {
            RutaResultado resultado = new RutaResultado();
            int origen = ObtenerIndice(codigoOrigen); int destino = ObtenerIndice(codigoDestino);
            if (origen == -1 || destino == -1) { resultado.Mensaje = "Puntos no válidos."; return resultado; }

            double[] dist = new double[cantidadSlots]; int[] anterior = new int[cantidadSlots]; bool[] procesado = new bool[cantidadSlots];
            for (int i = 0; i < cantidadSlots; i++) { dist[i] = double.PositiveInfinity; anterior[i] = -1; }
            dist[origen] = 0; MinHeap cola = new MinHeap(); cola.Insertar(origen, 0);

            while (cola.ExtraerMin(out int u, out double d)) {
                if (procesado[u]) continue; procesado[u] = true; if (u == destino) break;
                for (NodoArista? a = cabezas[u]; a != null; a = a.Siguiente) {
                    int v = a.Destino; if (procesado[v] || !activos[v]) continue;
                    double nueva = d + a.Peso;
                    if (nueva < dist[v]) { dist[v] = nueva; anterior[v] = u; cola.Insertar(v, nueva); }
                }
            }

            if (double.IsPositiveInfinity(dist[destino])) { resultado.Mensaje = "No hay ruta."; return resultado; }
            int largo = 0; for (int v = destino; v != -1; v = anterior[v]) { largo++; if (v == origen) break; }
            int[] camino = new int[largo]; int pos = largo - 1; for (int v = destino; pos >= 0; v = anterior[v]) { camino[pos] = v; pos--; }
            resultado.Codigos = new string[largo]; resultado.Paradas = new Parada[largo];
            for (int i = 0; i < largo; i++) { resultado.Codigos[i] = codigos[camino[i]]; resultado.Paradas[i] = paradas[camino[i]]!; }
            resultado.Existe = true; resultado.CantidadParadas = largo; resultado.DistanciaTotal = dist[destino]; return resultado;
        }

        public string[] BFS(string codigoOrigen, out int totalVisitados)
        {
            totalVisitados = 0; int origen = ObtenerIndice(codigoOrigen);
            if (origen == -1) return new string[0];
            bool[] visitado = new bool[cantidadSlots]; string[] resultado = new string[cantidadSlots]; Queue<int> cola = new Queue<int>();
            visitado[origen] = true; cola.Enqueue(origen);
            while (cola.Count > 0) {
                int u = cola.Dequeue(); resultado[totalVisitados++] = codigos[u];
                for (NodoArista? a = cabezas[u]; a != null; a = a.Siguiente) {
                    int v = a.Destino; if (!visitado[v] && activos[v]) { visitado[v] = true; cola.Enqueue(v); }
                }
            }
            return resultado;
        }

        public string[] DFS(string codigoOrigen, out int totalVisitados)
        {
            totalVisitados = 0; int origen = ObtenerIndice(codigoOrigen);
            if (origen == -1) return new string[0];
            bool[] visitado = new bool[cantidadSlots]; string[] resultado = new string[cantidadSlots];
            DFSRecursivo(origen, visitado, resultado, ref totalVisitados); return resultado;
        }
        private void DFSRecursivo(int u, bool[] visitado, string[] resultado, ref int totalVisitados)
        {
            visitado[u] = true; resultado[totalVisitados++] = codigos[u];
            for (NodoArista? a = cabezas[u]; a != null; a = a.Siguiente) {
                int v = a.Destino; if (!visitado[v] && activos[v]) DFSRecursivo(v, visitado, resultado, ref totalVisitados);
            }
        }

        private void CrecerArreglos()
        {
            int nueva = capacidad * 2;
            string[] nC = new string[nueva]; Parada?[] nP = new Parada?[nueva]; bool[] nA = new bool[nueva]; NodoArista?[] nCab = new NodoArista?[nueva]; NodoArista?[] nCol = new NodoArista?[nueva];
            for (int i = 0; i < cantidadSlots; i++) { nC[i] = codigos[i]; nP[i] = paradas[i]; nA[i] = activos[i]; nCab[i] = cabezas[i]; nCol[i] = colas[i]; }
            codigos = nC; paradas = nP; activos = nA; cabezas = nCab; colas = nCol; capacidad = nueva;
        }
    }

    // =====================================================================
    // INTEGRANTE 3: ESTRUCTURA UNION-FIND Y PERSISTENCIA DE DATOS
    // =====================================================================
    public class UnionFind
    {
        private int[] padre; private int[] rango; private int capacidad;
        public UnionFind(int capacidadInicial = 16)
        {
            capacidad = capacidadInicial; padre = new int[capacidad]; rango = new int[capacidad]; Reiniciar();
        }

        // NUEVO: Reinicia el estado de todos los nodos (Usado después de borrar una parada)
        public void Reiniciar()
        {
            for (int i = 0; i < capacidad; i++) { padre[i] = i; rango[i] = 0; }
        }

        public void AsegurarCapacidad(int cantidadVertices)
        {
            if (cantidadVertices >= capacidad)
            {
                int nuevaCapacidad = capacidad * 2;
                while (nuevaCapacidad <= cantidadVertices) nuevaCapacidad *= 2;
                int[] nuevoPadre = new int[nuevaCapacidad]; int[] nuevoRango = new int[nuevaCapacidad];
                for (int i = 0; i < capacidad; i++) { nuevoPadre[i] = padre[i]; nuevoRango[i] = rango[i]; }
                for (int i = capacidad; i < nuevaCapacidad; i++) { nuevoPadre[i] = i; nuevoRango[i] = 0; }
                padre = nuevoPadre; rango = nuevoRango; capacidad = nuevaCapacidad;
            }
        }

        public int Find(int x) { if (padre[x] != x) padre[x] = Find(padre[x]); return padre[x]; }
        public void Union(int x, int y)
        {
            int rX = Find(x); int rY = Find(y); if (rX == rY) return;
            if (rango[rX] < rango[rY]) padre[rX] = rY; else if (rango[rX] > rango[rY]) padre[rY] = rX;
            else { padre[rY] = rX; rango[rX]++; }
        }
        public bool EstanConectados(int x, int y) { return Find(x) == Find(y); }
    }

    public static class LectorCSV
    {
        public static string CargarParadas(string ruta, Trie trie, TablaHash<string, Parada> hash, Grafo grafo, UnionFind uf)
        {
            if (!File.Exists(ruta)) return "Archivo de paradas no encontrado.";
            string[] lineas = File.ReadAllLines(ruta); int agregados = 0;
            for (int i = 0; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i]) || lineas[i].ToLower().StartsWith("codigo")) continue;
                string[] p = lineas[i].Split(',');
                if (p.Length >= 4 && double.TryParse(p[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) && double.TryParse(p[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                {
                    string codigo = p[0].Trim(); string nombre = p[1].Trim();
                    Parada parada = new Parada(codigo, nombre, lat, lon);
                    trie.Insertar(nombre, codigo); hash.Insertar(codigo, parada);
                    if (grafo.AgregarVertice(parada)) { uf.AsegurarCapacidad(grafo.CapacidadIndices); agregados++; }
                }
            }
            return $"Paradas cargadas: {agregados}";
        }

        public static string CargarConexiones(string ruta, Grafo grafo, UnionFind uf)
        {
            if (!File.Exists(ruta)) return "Archivo de conexiones no encontrado.";
            string[] lineas = File.ReadAllLines(ruta); int agregados = 0;
            for (int i = 0; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i]) || lineas[i].ToLower().StartsWith("origen")) continue;
                string[] p = lineas[i].Split(',');
                if (p.Length >= 2)
                {
                    string origen = p[0].Trim(); string destino = p[1].Trim(); double? peso = null;
                    if (p.Length >= 3 && double.TryParse(p[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double w)) peso = w;
                    if (grafo.AgregarArista(origen, destino, peso)) { uf.Union(grafo.ObtenerIndice(origen), grafo.ObtenerIndice(destino)); agregados++; }
                }
            }
            return $"Conexiones cargadas: {agregados}";
        }
    }
}