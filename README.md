# SISTEMA RED DE TRANSPORTE URBANO
----------------------------------

##  DESCRIPCIÓN
Sistema de rutas y optimización. 
Mediante unos datos predefinidos usamos un grafo con pesos
para poder determinar, mediante la elección del usuario, cual resulta la distancia 
de menor costo entre 2 puntos cualesquiera.



## ESTRUCTURAS
### TRIE
Usado en la busqueda de puntos de inicio y fin  prediciendo palabras
mediante sus prefijos, como buscador de internet.

### Grafo
La ubicación entre 2 puntos, mediante grafos determinamos que destino (nodos) debe de recorrer
antes de llegar al destino elegido. Y mediante Dikstra ubicamos cual es la ruta de menos coste.

### TABLA HASH
Una vez seleccionado el destino y para futuras referencias, con hashing buscamos el dato de manera
instantanea con o(1).

### UNIONFIND
Comprueba la veracidad de los caminos que se desea recorrer. Cuando usamos dikstra para el 
camino más corto, con unionfind verificamos que aunque existe un camino más corto, si no esta
conectado con una arista.

### MinHeap
Para hacer recorrido mediante BFS usaremos el minHeap para apoyarnos de la elección
del objeto de mayor prioridad.


## EJECUCIÓN
Únicamente dirigiendote al archivo llamado "RedDeTransporte.bat" y dando doble click
se abrira el programa. Usando un .bat te ahorrara el escribir un comando en terminal.

## Datos
Los datos se encuentran bien ubicados en la carpeta llamada datos. 

