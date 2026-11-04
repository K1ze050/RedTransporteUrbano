using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic; // SOLO para Queue<T> en el BFS del grafo

namespace ProyectoRedTransporte
{
    // =====================================================================
    // CLASE BASE COMPARTIDA
    // =====================================================================
    public class Parada
    {
        public string Codigo;        // Código único de parada 
        public string Nombre;        // Nombre de la parada 
        public double Latitud;      // Coordenada latitud
        public double Longitud;     // Coordenada longitud

        public Parada(string codigo, string nombre, double latitud, double longitud)
        {
            Codigo = codigo;
            Nombre = nombre;
            Latitud = latitud;
            Longitud = longitud;
        }

        public void MostrarInformacion()
        {
            Console.WriteLine($"   Código:     {Codigo}");
            Console.WriteLine($"   Nombre:     {Nombre}");
            Console.WriteLine($"   Ubicación:  Latitud {Latitud}, Longitud {Longitud}");
        }
    }

    // =====================================================================
    // INTEGRANTE 1: MÓDULO DE AUTOCOMPLETADO Y BÚSQUEDA DIRECTA
    // =====================================================================
    
    // ESTRUCTURA 1: ÁRBOL DE PREFIJOS 
    public class NodoTrie
    {
        public char Caracter;
        public bool EsFinDePalabra;
        public string? CodigoAsociado;    // Guarda el código único que apunta a la Tabla Hash
        public string? PalabraCompleta;   // Nombre completo almacenado
        public NodoTrie?[] Hijos;         // Arreglo nativo de tamaño fijo 

        public NodoTrie(char c)
        {
            Caracter = c;
            EsFinDePalabra = false;
            CodigoAsociado = null;
            PalabraCompleta = null;
            Hijos = new NodoTrie?[256];   // 256 posiciones sin usar List<T> ni colecciones
        }
    }

    public class Trie
    {
        private NodoTrie raiz;

        public Trie()
        {
            raiz = new NodoTrie('\0');
        }

        // 1. Inserción en el Trie: O(m)
        public void Insertar(string palabra, string codigoAsociado)
        {
            if (string.IsNullOrEmpty(palabra)) return;

            NodoTrie actual = raiz;
            foreach (char c in palabra)
            {
                int indice = (int)c;
                if (indice < 0 || indice >= 256) continue;

                if (actual.Hijos[indice] == null)
                {
                    actual.Hijos[indice] = new NodoTrie(c);
                }
                actual = actual.Hijos[indice]!;
            }
            actual.EsFinDePalabra = true;
            actual.PalabraCompleta = palabra;
            actual.CodigoAsociado = codigoAsociado;
        }

        // 3. Autocompletado por prefijo: O(m + k)
        public void ObtenerSugerencias(string prefijo, ref string[] sugerencias, ref string[] codigos, ref int contador, int limiteMaximo)
        {
            if (string.IsNullOrEmpty(prefijo)) return;

            NodoTrie actual = raiz;
            foreach (char c in prefijo)
            {
                int indice = (int)c;
                if (indice < 0 || indice >= 256 || actual.Hijos[indice] == null)
                {
                    return; // No existen coincidencias con ese prefijo
                }
                actual = actual.Hijos[indice]!;
            }

            RecolectarRecursivo(actual, ref sugerencias, ref codigos, ref contador, limiteMaximo);
        }

        private void RecolectarRecursivo(NodoTrie nodo, ref string[] sugerencias, ref string[] codigos, ref int contador, int limiteMaximo)
        {
            if (nodo == null || contador >= limiteMaximo) return;

            if (nodo.EsFinDePalabra && nodo.PalabraCompleta != null && nodo.CodigoAsociado != null)
            {
                sugerencias[contador] = nodo.PalabraCompleta;
                codigos[contador] = nodo.CodigoAsociado;
                contador++;
            }

            for (int i = 0; i < nodo.Hijos.Length; i++)
            {
                if (nodo.Hijos[i] != null)
                {
                    RecolectarRecursivo(nodo.Hijos[i]!, ref sugerencias, ref codigos, ref contador, limiteMaximo);
                }
            }
        }
    }

    // ESTRUCTURA 2: TABLA HASH 
    public class NodoHash<TClave, TValor>
    {
        public TClave Clave;
        public TValor Valor;
        public NodoHash<TClave, TValor>? Siguiente;

        public NodoHash(TClave clave, TValor valor)
        {
            Clave = clave;
            Valor = valor;
            Siguiente = null;
        }
    }

    public class TablaHash<TClave, TValor>
    {
        private NodoHash<TClave, TValor>?[] cubos;
        private int capacidad;
        private int cantidadElementos;

        public TablaHash(int capacidad = 101) // Número primo para reducir colisiones
        {
            this.capacidad = capacidad;
            this.cubos = new NodoHash<TClave, TValor>?[capacidad];
            this.cantidadElementos = 0;
        }

        // Función Hash polinómica: h(k) = (sum(c * 31^i)) mod M
        private int ObtenerIndiceHash(TClave clave)
        {
            if (clave == null) return 0;
            string textoClave = clave.ToString()!;
            int hash = 0;
            foreach (char c in textoClave)
            {
                hash = (hash * 31 + c) % capacidad;
            }
            return Math.Abs(hash);
        }

        // 1. Inserción: O(1) promedio
        public void Insertar(TClave clave, TValor valor)
        {
            if (clave == null) return;

            int indice = ObtenerIndiceHash(clave);
            NodoHash<TClave, TValor> nuevoNodo = new NodoHash<TClave, TValor>(clave, valor);

            if (cubos[indice] == null)
            {
                cubos[indice] = nuevoNodo;
                cantidadElementos++;
            }
            else
            {
                NodoHash<TClave, TValor>? actual = cubos[indice];
                while (actual != null)
                {
                    if (actual.Clave != null && actual.Clave.Equals(clave))
                    {
                        actual.Valor = valor; // Clave duplicada: actualiza el valor
                        return;
                    }
                    if (actual.Siguiente == null) break;
                    actual = actual.Siguiente;
                }
                actual!.Siguiente = nuevoNodo; // Encadenamiento al final de la lista
                cantidadElementos++;
            }
        }

        // 2. Búsqueda directa: O(1) promedio
        public TValor? Buscar(TClave clave)
        {
            if (clave == null) return default;

            int indice = ObtenerIndiceHash(clave);
            NodoHash<TClave, TValor>? actual = cubos[indice];

            while (actual != null)
            {
                if (actual.Clave != null && actual.Clave.Equals(clave))
                {
                    return actual.Valor;
                }
                actual = actual.Siguiente;
            }
            return default;
        }
    }

    // =====================================================================
    // INTEGRANTE 2: GRAFOS Y ALGORITMOS DE CAMINOS ÓPTIMOS
    // =====================================================================
    
    // MinHeap.cs  -  Cola de prioridad propia (min-heap binario sobre arreglos)
    public class MinHeap
    {
        private int[] vertices;
        private double[] prioridades;
        private int tamano;

        public MinHeap(int capacidadInicial = 16)
        {
            if (capacidadInicial < 1) capacidadInicial = 16;
            vertices = new int[capacidadInicial];
            prioridades = new double[capacidadInicial];
            tamano = 0;
        }

        // Inserta un par (vértice, prioridad): O(log n)
        public void Insertar(int vertice, double prioridad)
        {
            if (tamano == vertices.Length) Crecer();

            vertices[tamano] = vertice;
            prioridades[tamano] = prioridad;
            SubirElemento(tamano);
            tamano++;
        }

        // Extrae el par con menor prioridad: O(log n). Devuelve false si está vacío.
        public bool ExtraerMin(out int vertice, out double prioridad)
        {
            vertice = -1;
            prioridad = double.PositiveInfinity;
            if (tamano == 0) return false;

            vertice = vertices[0];
            prioridad = prioridades[0];

            tamano--;
            if (tamano > 0)
            {
                vertices[0] = vertices[tamano];
                prioridades[0] = prioridades[tamano];
                BajarElemento(0);
            }
            return true;
        }

        private void SubirElemento(int i)
        {
            while (i > 0)
            {
                int padre = (i - 1) / 2;
                if (prioridades[i] < prioridades[padre])
                {
                    Intercambiar(i, padre);
                    i = padre;
                }
                else break;
            }
        }

        private void BajarElemento(int i)
        {
            while (true)
            {
                int izq = 2 * i + 1;
                int der = 2 * i + 2;
                int menor = i;

                if (izq < tamano && prioridades[izq] < prioridades[menor]) menor = izq;
                if (der < tamano && prioridades[der] < prioridades[menor]) menor = der;

                if (menor == i) break;

                Intercambiar(i, menor);
                i = menor;
            }
        }

        private void Intercambiar(int a, int b)
        {
            int tv = vertices[a];
            vertices[a] = vertices[b];
            vertices[b] = tv;

            double tp = prioridades[a];
            prioridades[a] = prioridades[b];
            prioridades[b] = tp;
        }

        // Duplica la capacidad copiando manualmente (sin colecciones nativas).
        private void Crecer()
        {
            int[] nuevosVertices = new int[vertices.Length * 2];
            double[] nuevasPrioridades = new double[prioridades.Length * 2];
            for (int i = 0; i < tamano; i++)
            {
                nuevosVertices[i] = vertices[i];
                nuevasPrioridades[i] = prioridades[i];
            }
            vertices = nuevosVertices;
            prioridades = nuevasPrioridades;
        }
    }

    // Nodo de la lista enlazada de adyacencia (una arista saliente).
    public class NodoArista
    {
        public int Destino;
        public double Peso;
        public NodoArista? Siguiente;

        public NodoArista(int destino, double peso)
        {
            Destino = destino;
            Peso = peso;
            Siguiente = null;
        }
    }

    // Resultado de una consulta de ruta.
    public class RutaResultado
    {
        public bool Existe = false;
        public double DistanciaTotal = 0;
        public int CantidadParadas = 0;
        public string[] Codigos = new string[0];
        public Parada[] Paradas = new Parada[0];
        public string Mensaje = "";
    }

    public class Grafo
    {
        private bool dirigido;
        private int capacidad;
        private int cantidadSlots;       // índices asignados

        private string[] codigos;
        private Parada?[] paradas;
        private NodoArista?[] cabezas;
        private NodoArista?[] colas;
        private TablaHash<string, int> indices;   // código -> (índice + 1)

        public Grafo(bool dirigido = false, int capacidadInicial = 16)
        {
            this.dirigido = dirigido;
            capacidad = capacidadInicial;
            codigos = new string[capacidad];
            paradas = new Parada?[capacidad];
            cabezas = new NodoArista?[capacidad];
            colas = new NodoArista?[capacidad];
            indices = new TablaHash<string, int>(101);
        }

        // Total de índices asignados (útil para dimensionar Union-Find: 0..CapacidadIndices-1).
        public int CapacidadIndices
        {
            get { return cantidadSlots; }
        }

        // Agrega una parada como vértice: O(1) promedio. false si ya existe el código.
        public bool AgregarVertice(Parada parada)
        {
            if (ObtenerIndice(parada.Codigo) != -1) return false;
            if (cantidadSlots == capacidad) CrecerArreglos();

            int idx = cantidadSlots;
            codigos[idx] = parada.Codigo;
            paradas[idx] = parada;
            indices.Insertar(parada.Codigo, idx + 1);
            cantidadSlots++;
            return true;
        }

        // Índice interno de un código, o -1 si no existe: O(1) promedio (TablaHash).
        public int ObtenerIndice(string codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return -1;
            return indices.Buscar(codigo) - 1;     // 0 (no encontrado) -> -1
        }

        // Agrega (o actualiza) una arista. Si peso es null se usa la distancia Haversine.
        public bool AgregarArista(string codigoOrigen, string codigoDestino, double? peso = null)
        {
            int u = ObtenerIndice(codigoOrigen);
            int v = ObtenerIndice(codigoDestino);
            if (u == -1 || v == -1) return false;

            double w = peso ?? DistanciaHaversine(paradas[u]!, paradas[v]!);
            PonerArco(u, v, w);
            if (!dirigido && u != v) PonerArco(v, u, w);
            return true;
        }

        // Inserta el arco u->v al final de la lista.
        private void PonerArco(int u, int v, double peso)
        {
            NodoArista nuevo = new NodoArista(v, peso);
            if (cabezas[u] == null)
            {
                cabezas[u] = nuevo;
                colas[u] = nuevo;
            }
            else
            {
                colas[u]!.Siguiente = nuevo;
                colas[u] = nuevo;
            }
        }

        // Distancia en kilómetros entre dos paradas (fórmula de Haversine).
        public static double DistanciaHaversine(Parada a, Parada b)
        {
            double lat1 = a.Latitud * Math.PI / 180.0;
            double lat2 = b.Latitud * Math.PI / 180.0;
            double dLat = (b.Latitud - a.Latitud) * Math.PI / 180.0;
            double dLon = (b.Longitud - a.Longitud) * Math.PI / 180.0;
            double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 6371.0 * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
        }

        // Dijkstra (camino de menor costo) con MinHeap propio
        public RutaResultado Dijkstra(string codigoOrigen, string codigoDestino)
        {
            RutaResultado resultado = new RutaResultado();
            int origen = ObtenerIndice(codigoOrigen);
            int destino = ObtenerIndice(codigoDestino);

            double[] dist = new double[cantidadSlots];
            int[] anterior = new int[cantidadSlots];
            bool[] procesado = new bool[cantidadSlots];

            for (int i = 0; i < cantidadSlots; i++)
            {
                dist[i] = double.PositiveInfinity;
                anterior[i] = -1;
            }
            dist[origen] = 0;

            MinHeap cola = new MinHeap();
            cola.Insertar(origen, 0);

            while (cola.ExtraerMin(out int u, out double d))
            {
                if (procesado[u]) continue;
                procesado[u] = true;
                if (u == destino) break;

                for (NodoArista? a = cabezas[u]; a != null; a = a.Siguiente)
                {
                    int v = a.Destino;
                    if (procesado[v]) continue;

                    double nueva = d + a.Peso;
                    if (nueva < dist[v])
                    {
                        dist[v] = nueva;
                        anterior[v] = u;
                        cola.Insertar(v, nueva);
                    }
                }
            }

            if (double.IsPositiveInfinity(dist[destino]))
            {
                resultado.Mensaje = "No hay ruta.";
                return resultado;
            }

            int largo = 0;
            for (int v = destino; v != -1; v = anterior[v])
            {
                largo++;
                if (v == origen) break;
            }

            int[] camino = new int[largo];
            int pos = largo - 1;
            for (int v = destino; pos >= 0; v = anterior[v])
            {
                camino[pos] = v;
                pos--;
            }

            resultado.Codigos = new string[largo];
            resultado.Paradas = new Parada[largo];
            for (int i = 0; i < largo; i++)
            {
                resultado.Codigos[i] = codigos[camino[i]];
                resultado.Paradas[i] = paradas[camino[i]]!;
            }

            resultado.Existe = true;
            resultado.CantidadParadas = largo;
            resultado.DistanciaTotal = dist[destino];
            return resultado;
        }

        private void CrecerArreglos()
        {
            int nueva = capacidad * 2;
            string[] nCodigos = new string[nueva];
            Parada?[] nParadas = new Parada?[nueva];
            NodoArista?[] nCabezas = new NodoArista?[nueva];
            NodoArista?[] nColas = new NodoArista?[nueva];

            for (int i = 0; i < cantidadSlots; i++)
            {
                nCodigos[i] = codigos[i];
                nParadas[i] = paradas[i];
                nCabezas[i] = cabezas[i];
                nColas[i] = colas[i];
            }
            codigos = nCodigos; paradas = nParadas; cabezas = nCabezas; colas = nColas; capacidad = nueva;
        }
    }

    // =====================================================================
    // INTEGRANTE 3: ESTRUCTURA UNION-FIND Y PERSISTENCIA DE DATOS
    // =====================================================================
    public class UnionFind
    {
        private int[] padre;
        private int[] rango;
        private int capacidad;

        public UnionFind(int capacidadInicial = 16)
        {
            capacidad = capacidadInicial;
            padre = new int[capacidad];
            rango = new int[capacidad];

            for (int i = 0; i < capacidad; i++)
            {
                padre[i] = i;
                rango[i] = 0;
            }
        }

        // Método para asegurar que el arreglo tenga espacio cuando el Grafo agrega nuevas paradas
        public void AsegurarCapacidad(int cantidadVertices)
        {
            if (cantidadVertices >= capacidad)
            {
                int nuevaCapacidad = capacidad * 2;
                while (nuevaCapacidad <= cantidadVertices) nuevaCapacidad *= 2;

                int[] nuevoPadre = new int[nuevaCapacidad];
                int[] nuevoRango = new int[nuevaCapacidad];

                for (int i = 0; i < capacidad; i++)
                {
                    nuevoPadre[i] = padre[i];
                    nuevoRango[i] = rango[i];
                }
                for (int i = capacidad; i < nuevaCapacidad; i++)
                {
                    nuevoPadre[i] = i;
                    nuevoRango[i] = 0;
                }

                padre = nuevoPadre;
                rango = nuevoRango;
                capacidad = nuevaCapacidad;
            }
        }

        // Path Compression (Búsqueda con compresión de ruta)
        public int Find(int x)
        {
            if (padre[x] != x)
            {
                padre[x] = Find(padre[x]);
            }
            return padre[x];
        }

        // Union by Rank (Unión por rango/tamaño)
        public void Union(int x, int y)
        {
            int raizX = Find(x);
            int raizY = Find(y);

            if (raizX == raizY) return;

            if (rango[raizX] < rango[raizY]) padre[raizX] = raizY;
            else if (rango[raizX] > rango[raizY]) padre[raizY] = raizX;
            else
            {
                padre[raizY] = raizX;
                rango[raizX]++;
            }
        }

        // Validar si dos elementos pertenecen a la misma zona conectada
        public bool EstanConectados(int x, int y)
        {
            return Find(x) == Find(y);
        }
    }

    public static class LectorCSV
    {
        // Carga las paradas y las inyecta en el Trie, la Tabla Hash, el Grafo y asegura espacio en UnionFind
        public static string CargarParadas(string ruta, Trie trie, TablaHash<string, Parada> hash, Grafo grafo, UnionFind uf)
        {
            if (!File.Exists(ruta)) return "Archivo de paradas no encontrado.";

            // Único método de lectura permitido por las restricciones del proyecto
            string[] lineas = File.ReadAllLines(ruta);
            int agregados = 0;

            for (int i = 0; i < lineas.Length; i++)
            {
                string linea = lineas[i];
                if (string.IsNullOrWhiteSpace(linea) || linea.ToLower().StartsWith("codigo")) continue;

                string[] partes = linea.Split(',');
                if (partes.Length >= 4)
                {
                    string codigo = partes[0].Trim();
                    string nombre = partes[1].Trim();
                    
                    if (double.TryParse(partes[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(partes[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                    {
                        Parada p = new Parada(codigo, nombre, lat, lon);
                        
                        // Inserción en todas las estructuras (Sincronización total)
                        trie.Insertar(nombre, codigo);
                        hash.Insertar(codigo, p);
                        
                        if (grafo.AgregarVertice(p))
                        {
                            uf.AsegurarCapacidad(grafo.CapacidadIndices);
                            agregados++;
                        }
                    }
                }
            }
            return $"Paradas cargadas exitosamente: {agregados}";
        }

        // Carga las conexiones, crea las aristas en el Grafo y agrupa los conjuntos en Union-Find
        public static string CargarConexiones(string ruta, Grafo grafo, UnionFind uf)
        {
            if (!File.Exists(ruta)) return "Archivo de conexiones no encontrado.";

            string[] lineas = File.ReadAllLines(ruta);
            int agregados = 0;

            for (int i = 0; i < lineas.Length; i++)
            {
                string linea = lineas[i];
                if (string.IsNullOrWhiteSpace(linea) || linea.ToLower().StartsWith("origen")) continue;

                string[] partes = linea.Split(',');
                if (partes.Length >= 2)
                {
                    string origen = partes[0].Trim();
                    string destino = partes[1].Trim();
                    double? peso = null;

                    if (partes.Length >= 3 && double.TryParse(partes[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double w))
                    {
                        peso = w;
                    }

                    if (grafo.AgregarArista(origen, destino, peso))
                    {
                        // PASO C PREVIO: Al crear la conexión física, unimos las paradas lógicamente en Union-Find
                        int idxOrigen = grafo.ObtenerIndice(origen);
                        int idxDestino = grafo.ObtenerIndice(destino);
                        uf.Union(idxOrigen, idxDestino);
                        agregados++;
                    }
                }
            }
            return $"Conexiones cargadas exitosamente: {agregados}";
        }
    }
}