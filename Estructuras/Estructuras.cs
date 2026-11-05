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


    public class MinHeap
    {
        private int[] vertices;        // Guarda los índices de los vértices
        private double[] prioridades;  // Guarda las distancias o pesos asociados a cada vértice
        private int tamano;            // Cantidad actual de elementos almacenados en el montículo

        public MinHeap(int capacidadInicial = 16)
        {
            // Si nos pasan una capacidad inválida, usamos 16 por defecto
            if (capacidadInicial < 1) capacidadInicial = 16;
            vertices = new int[capacidadInicial];
            prioridades = new double[capacidadInicial];
            tamano = 0; // Al inicio el montículo está vacío
        }


        /// Inserta un nuevo par (vértice, prioridad) y reordena flotando el elemento.
        public void Insertar(int vertice, double prioridad)
        {
            // Si el arreglo se llena, duplicamos la capacidad manualmente
            if (tamano == vertices.Length) Crecer();

            // El nuevo elemento se coloca al final (primer espacio libre)...
            vertices[tamano] = vertice;
            prioridades[tamano] = prioridad;
            // ...y se "sube" hasta que quede en una posición válida del heap
            SubirElemento(tamano); // Reordenar hacia arriba
            tamano++;
        }


        /// Extrae la raíz (el elemento con menor distancia/prioridad).
        /// Devuelve false si el montículo está vacío.
        public bool ExtraerMin(out int vertice, out double prioridad)
        {
            // Valores por defecto en caso de que esté vacío
            vertice = -1;
            prioridad = double.PositiveInfinity;
            if (tamano == 0) return false;

            // La raíz (índice 0) siempre contiene el menor elemento
            vertice = vertices[0];
            prioridad = prioridades[0];

            tamano--;
            if (tamano > 0)
            {
                // Movemos el último elemento a la raíz y reordenamos hacia abajo
                // (así el arreglo no queda con un hueco en la posición 0)
                vertices[0] = vertices[tamano];
                prioridades[0] = prioridades[tamano];
                BajarElemento(0);
            }
            return true;
        }

        // Flota el nodo i mientras sea menor que su padre (Propiedad de Min-Heap)
        private void SubirElemento(int i)
        {
            while (i > 0) // Se detiene al llegar a la raíz
            {
                int padre = (i - 1) / 2;
                if (prioridades[i] < prioridades[padre])
                {
                    // El hijo es menor que el padre: se intercambian y seguimos subiendo
                    Intercambiar(i, padre);
                    i = padre;
                }
                else break; // Ya cumple la propiedad, no hay que seguir
            }
        }

        // Hunde el nodo i comparándolo con sus hijos izquierdo y derecho
        private void BajarElemento(int i)
        {
            while (true)
            {
                int izq = 2 * i + 1;
                int der = 2 * i + 2;
                int menor = i; // Asumimos que el menor es el propio nodo

                // Verificamos que el hijo exista (izq < tamano) y si es menor que el actual "menor"
                if (izq < tamano && prioridades[izq] < prioridades[menor]) menor = izq;
                if (der < tamano && prioridades[der] < prioridades[menor]) menor = der;

                // Si ningún hijo es menor, el nodo ya está en su lugar
                if (menor == i) break;

                // Si no, se intercambia con el hijo menor y se sigue bajando
                Intercambiar(i, menor);
                i = menor;
            }
        }

        // Intercambia posiciones en los arreglos paralelos
        // (hay que intercambiar en AMBOS arreglos para no desincronizar vértice y prioridad)
        private void Intercambiar(int a, int b)
        {
            int tv = vertices[a];
            vertices[a] = vertices[b];
            vertices[b] = tv;

            double tp = prioridades[a];
            prioridades[a] = prioridades[b];
            prioridades[b] = tp;
        }

        // Redimensionamiento manual de arreglos 
        // Crea arreglos del doble de tamaño y copia los elementos existentes
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


    // ---------------------------------------------------------------------
    // NODO DE LA LISTA ENLAZADA DE ADYACENCIA
    // ---------------------------------------------------------------------
    // Representa un arco o conexión orientada hacia un destino con un peso.
    // Cada vértice tiene una lista enlazada de estos nodos: sus "vecinos".
    public class NodoArista
    {
        public int Destino;          // Índice del vértice destino
        public double Peso;          // Peso/Distancia de la conexión
        public NodoArista? Siguiente;// Puntero a la siguiente arista adyacente

        public NodoArista(int destino, double peso)
        {
            Destino = destino;
            Peso = peso;
            Siguiente = null; // Al crearse, aún no tiene siguiente
        }
    }


    // ---------------------------------------------------------------------
    // ESTRUCTURA DE RETORNO DE RUTA
    // ---------------------------------------------------------------------
    // Almacena los resultados del algoritmo de Dijkstra.
    public class RutaResultado
    {
        public bool Existe = false;                  // true si se encontró una ruta
        public double DistanciaTotal = 0;            // Suma de los pesos del camino 
        public int CantidadParadas = 0;              // Cuántas paradas tiene el camino
        public string[] Codigos = new string[0];     // Códigos de las paradas, en orden origen -> destino
        public Parada[] Paradas = new Parada[0];     // Objetos Parada del camino, en el mismo orden
        public string Mensaje = "";                  // Mensaje de error
    }


    // ---------------------------------------------------------------------
    // ESTRUCTURA DEL GRAFO PONDERADO
    // ---------------------------------------------------------------------
    // Soporta Grafos Dirigidos / No Dirigidos con listas de adyacencia propias.
    //
    // Cada vértice (parada) recibe un índice numérico 0, 1, 2... y los datos
    // de ese vértice se guardan en la MISMA posición de varios arreglos:
    //   codigos[i], paradas[i], cabezas[i], colas[i]
    public class Grafo
    {
        private bool dirigido;                   // Flag para saber si es dirigido o no dirigido
        private int capacidad;                   // Tamaño del arreglo interno
        private int cantidadSlots;               // Cantidad actual de vértices registrados

        private string[] codigos;                // Arreglo de códigos de paradas
        private Parada?[] paradas;               // Arreglo de objetos Parada
        private NodoArista?[] cabezas;           // Lista de adyacencia: cabeza de la lista (primera arista de cada vértice)
        private NodoArista?[] colas;             // Lista de adyacencia: cola de la lista (última arista; permite insertar en O(1))
        private TablaHash<string, int> indices;  // Mapeo Hash: Código -> Índice en los arreglos

        public Grafo(bool dirigido = false, int capacidadInicial = 16)
        {
            this.dirigido = dirigido;
            capacidad = capacidadInicial;
            codigos = new string[capacidad];
            paradas = new Parada?[capacidad];
            cabezas = new NodoArista?[capacidad];
            colas = new NodoArista?[capacidad];
            indices = new TablaHash<string, int>(101); // 101 = tamaño de la tabla hash
        }

        // Propiedad de solo lectura: devuelve cuántos vértices hay registrados
        public int CapacidadIndices
        {
            get { return cantidadSlots; }
        }

        // Registra un nuevo vértice en los arreglos del grafo
        public bool AgregarVertice(Parada parada)
        {
            // Si el código ya existe, no se duplica el vértice
            if (ObtenerIndice(parada.Codigo) != -1) return false;
            // Si los arreglos están llenos, se agranda
            if (cantidadSlots == capacidad) CrecerArreglos();

            int idx = cantidadSlots; // El nuevo vértice ocupa la primera posición libre
            codigos[idx] = parada.Codigo;
            paradas[idx] = parada;
            // Se guarda idx + 1 porque la tabla hash devuelve 0 cuando NO encuentra
            // la clave, así el índice 0 real no se confunde con "no existe"
            indices.Insertar(parada.Codigo, idx + 1); // Guarda índice base 1 en Hash
            cantidadSlots++;
            return true;
        }

        // Obtiene el índice interno asociado a un código único
        public int ObtenerIndice(string codigo)
        {
            if (string.IsNullOrEmpty(codigo)) return -1;
            // Se resta 1 para deshacer el "+1" de AgregarVertice.
            // Si no existe, Buscar devuelve 0 y el resultado es -1
            return indices.Buscar(codigo) - 1; // Retorna -1 si no se encuentra
        }

        // Agrega una arista ponderada entre dos vértices
        public bool AgregarArista(string codigoOrigen, string codigoDestino, double? peso = null)
        {
            int u = ObtenerIndice(codigoOrigen);
            int v = ObtenerIndice(codigoDestino);
            // Si alguno de los dos vértices no existe, no se puede conectar
            if (u == -1 || v == -1) return false;

            // Si el peso es nulo, calcula la distancia Haversine automáticamente
            double w = peso ?? DistanciaHaversine(paradas[u]!, paradas[v]!);
            PonerArco(u, v, w);
            if (!dirigido && u != v) PonerArco(v, u, w); // Si es no dirigido, conecta ambos lados
            return true;
        }

        // Inserta un nodo de arista al final de la lista enlazada del vértice u
        private void PonerArco(int u, int v, double peso)
        {
            NodoArista nuevo = new NodoArista(v, peso);
            if (cabezas[u] == null)
            {
                // Lista vacía: el nuevo nodo es a la vez la cabeza y la cola
                cabezas[u] = nuevo;
                colas[u] = nuevo;
            }
            else
            {
                // Lista con elementos: se enlaza después de la cola y se actualiza la cola
                colas[u]!.Siguiente = nuevo;
                colas[u] = nuevo;
            }
        }

        // Cálculo de distancia 
        // Fórmula que calcula la distancia entre dos puntos (latitud/longitud)
        // El resultado está en kilómetros.
        public static double DistanciaHaversine(Parada a, Parada b)
        {
            // Convertimos grados a radianes (grados * PI / 180)
            double lat1 = a.Latitud * Math.PI / 180.0;
            double lat2 = b.Latitud * Math.PI / 180.0;
            double dLat = (b.Latitud - a.Latitud) * Math.PI / 180.0; // Diferencia de latitudes
            double dLon = (b.Longitud - a.Longitud) * Math.PI / 180.0; // Diferencia de longitudes
            // "h" es la parte interna de la fórmula de Haversine
            double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            // 6371 = radio medio de la Tierra en km
            return 6371.0 * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
        }


        /// ALGORITMO DE DIJKSTRA: Determina la ruta óptima de menor costo usando la MinHeap propia.
        /// Idea: ir "fijando" siempre el vértice no procesado con menor distancia conocida
        /// y, desde él, intentar mejorar las distancias de sus vecinos.
        public RutaResultado Dijkstra(string codigoOrigen, string codigoDestino)
        {
            RutaResultado resultado = new RutaResultado();
            int origen = ObtenerIndice(codigoOrigen);
            int destino = ObtenerIndice(codigoDestino);

            double[] dist = new double[cantidadSlots];   // dist[i] = mejor distancia conocida desde el origen hasta i
            int[] anterior = new int[cantidadSlots];     // anterior[i] = vértice previo en el mejor camino (para reconstruirlo)
            bool[] procesado = new bool[cantidadSlots];  // true cuando la distancia de i ya es definitiva

            // Al inicio todo es inalcanzable (infinito) y sin predecesor (-1)
            for (int i = 0; i < cantidadSlots; i++)
            {
                dist[i] = double.PositiveInfinity;
                anterior[i] = -1;
            }
            dist[origen] = 0; // La distancia del origen a sí mismo es 0

            MinHeap cola = new MinHeap();
            cola.Insertar(origen, 0);

            // Mientras queden elementos en la cola, sacamos el de menor distancia (u, d)
            while (cola.ExtraerMin(out int u, out double d))
            {
                // Un vértice puede estar varias veces en la cola con distancias distintas;
                // si ya fue procesado, esta entrada es vieja y se descarta
                if (procesado[u]) continue;
                procesado[u] = true;
                if (u == destino) break; // Ya tenemos el camino óptimo al destino, no hace falta seguir

                // Recorremos la lista enlazada de vecinos de u
                for (NodoArista? a = cabezas[u]; a != null; a = a.Siguiente)
                {
                    int v = a.Destino;
                    if (procesado[v]) continue; // Si ya es definitivo, no se puede mejorar

                    // Distancia al vecino pasando por u
                    double nueva = d + a.Peso;
                    // Si este camino es mejor que el que se conocía, lo actualizamos (relajación)
                    if (nueva < dist[v])
                    {
                        dist[v] = nueva;
                        anterior[v] = u;
                        cola.Insertar(v, nueva);
                    }
                }
            }

            // Si la distancia al destino sigue siendo infinita, nunca se pudo llegar a él
            if (double.IsPositiveInfinity(dist[destino]))
            {
                resultado.Mensaje = "No hay ruta disponible entre los puntos indicados.";
                return resultado;
            }

            // Reconstrucción del camino desde el destino hacia el origen
            // Paso 1: contar cuántos vértices tiene el camino siguiendo "anterior"
            int largo = 0;
            for (int v = destino; v != -1; v = anterior[v])
            {
                largo++;
                if (v == origen) break;
            }

            // Paso 2: llenar el arreglo de atrás hacia adelante
            // (se recorre del destino al origen, pero se guarda en orden origen -> destino)
            int[] camino = new int[largo];
            int pos = largo - 1;
            for (int v = destino; pos >= 0; v = anterior[v])
            {
                camino[pos] = v;
                pos--;
            }

            // Paso 3: convertir los índices del camino en códigos y objetos Parada
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


        /// RECORRIDO BFS (Búsqueda en Anchura)
        /// Devuelve un arreglo nativo string[] con los nombres/códigos visitados.
        /// Visita primero todos los vecinos directos, luego los vecinos de esos, y así por niveles.
        public string[] BFS(string codigoOrigen, out int totalVisitados)
        {
            totalVisitados = 0;
            int origen = ObtenerIndice(codigoOrigen);
            if (origen == -1) return new string[0]; // El origen no existe

            bool[] visitado = new bool[cantidadSlots];       // Evita visitar un vértice dos veces
            string[] resultado = new string[cantidadSlots]; // Arreglo nativo estático
            Queue<int> cola = new Queue<int>();             // Control de orden BFS autorizado (cola FIFO)

            // Se marca el origen como visitado al ENCOLARLO (no al sacarlo),
            // para que no se encole más de una vez
            visitado[origen] = true;
            cola.Enqueue(origen);

            while (cola.Count > 0)
            {
                int u = cola.Dequeue(); // Sale el más antiguo de la cola
                resultado[totalVisitados] = codigos[u]; // Se registra en el orden de visita
                totalVisitados++;

                // Se encolan todos los vecinos aún no visitados
                for (NodoArista? a = cabezas[u]; a != null; a = a.Siguiente)
                {
                    int v = a.Destino;
                    if (!visitado[v])
                    {
                        visitado[v] = true;
                        cola.Enqueue(v);
                    }
                }
            }
            return resultado;
        }


        /// RECORRIDO DFS (Búsqueda en Profundidad)
        /// Devuelve un arreglo nativo string[] con la secuencia de exploración.
        /// Avanza lo más profundo posible por un camino antes de retroceder y probar otro.
        public string[] DFS(string codigoOrigen, out int totalVisitados)
        {
            totalVisitados = 0;
            int origen = ObtenerIndice(codigoOrigen);
            if (origen == -1) return new string[0]; // El origen no existe

            bool[] visitado = new bool[cantidadSlots];
            string[] resultado = new string[cantidadSlots];

            // Inicia la exploración recursiva por profundidad
            DFSRecursivo(origen, visitado, resultado, ref totalVisitados);

            return resultado;
        }

        // Método auxiliar recursivo para DFS
        // "ref totalVisitados" permite que el contador se comparta entre todas las llamadas recursivas
        private void DFSRecursivo(int u, bool[] visitado, string[] resultado, ref int totalVisitados)
        {
            // Se marca y registra el vértice actual
            visitado[u] = true;
            resultado[totalVisitados] = codigos[u];
            totalVisitados++;

            // Por cada vecino no visitado, nos "metemos" a explorarlo de inmediato (recursión)
            for (NodoArista? a = cabezas[u]; a != null; a = a.Siguiente)
            {
                int v = a.Destino;
                if (!visitado[v])
                {
                    DFSRecursivo(v, visitado, resultado, ref totalVisitados);
                }
            }
        }

        // Duplica la capacidad de los arreglos nativos del Grafo
        // Crea arreglos nuevos del doble de tamaño y copia lo existente
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
            // Se reemplazan los arreglos viejos por los nuevos y se actualiza la capacidad
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
