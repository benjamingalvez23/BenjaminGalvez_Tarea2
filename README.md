# BenjaminGalvez_Tarea2

Un mundo de fantasía creado por el computador

Este es mi proyecto de la Tarea 2 de IA y PCG para videojuegos (profesor Nicolás Romero).

Es un programa hecho en Unity que crea mundos de fantasía solo, sin que yo tenga que dibujarlos. Cada vez que lo uso sale un mundo distinto, con terreno, árboles, cuevas y criaturas.

Informe: [agregar link o archivo del informe]
Juego en Itch.io: [agregar link]
Capturas
Reino de exploración	Tierras de mazmorras
Reino	Mazmorras
Qué hace

Usé tres técnicas de generación procedural, cada una para una parte del mundo:

Parte del mundo	Técnica	Explicado fácil
El terreno	Ruido fractal	Mezcla varias capas de ondulaciones para hacer agua, arena, pasto y roca. También reparte el pasto en 3 zonas de fantasía con colores distintos.
Los árboles	L-System	Un árbol crece a partir de una palabra y unas reglas, como si una tortuga fuera dibujando las ramas.
Las mazmorras	Autómata celular	Cada cuadrito del mapa copia lo que hacen sus vecinos y, poco a poco, el desorden se vuelve una cueva.

Además, el programa coloca animales y enemigos según la zona, y dentro de las cuevas pone cofres, cajas, enemigos y un jefe.

Todo se genera a partir de un número llamado semilla. Con la misma semilla sale siempre el mismo mundo, y si la cambio, sale uno nuevo.

Los dos tipos de mundo
Reino de exploración: mucho pasto, bosques grandes, muchos animales y pocas mazmorras. Es para pasear y descubrir.
Tierras de mazmorras: más roca, arbustos sueltos y varias cuevas grandes con más enemigos y tesoros. Es para pelear y buscar botín.

Se cambia entre ellos con un botón del menú.

Cómo usarlo
Para probar el juego

Entra a la página de Itch.io y juega directamente en el navegador: [agregar link].

Para abrir el proyecto en Unity
Descarga o clona este repositorio:
   git clone https://github.com/benjamingalvez23/Tarea2-PCG-Videojuegos-BenjaminGalvez.git
Abre Unity Hub, pulsa Add y elige la carpeta del repositorio.
Usa la versión de Unity [agregar tu versión]. Si Unity te pregunta, deja que instale lo que falta.
Espera unos minutos la primera vez, porque Unity prepara el proyecto.
Abre la escena [agregar nombre de la escena] y pulsa Play.

No hay que instalar nada más: los dibujos se crean con código, así que no hay imágenes externas.

Controles
Qué quiero hacer	Cómo
Moverme por el mapa	Teclas W A S D o las flechas (con Shift voy más rápido)
Arrastrar el mapa	Clic derecho y mover el mouse
Acercar o alejar	Rueda del mouse
Mostrar u ocultar el menú	Tecla Tab
El menú

El menú está a la izquierda. Lo hice para que lo pueda usar cualquiera, aunque no sepa nada de estos temas: cada barra tiene una explicación corta de lo que pasa al subirla o bajarla.

Lo más importante:

Empezar aquí: elijo uno de los dos tipos de mundo o creo uno nuevo con una semilla al azar.
Capas visibles: puedo ocultar el suelo, los árboles, las mazmorras o las criaturas. El botón Solo deja una sola capa a la vista, y así se entiende qué hace cada técnica.
Secciones de cada técnica: barras para cambiar cómo sale el terreno, los árboles, las cuevas y las criaturas. Si dejo activado Aplicar automático, el mundo se actualiza solo al soltar una barra.
Resultados y estadísticas: muestra cuánto tardó cada técnica y cuántas cosas se generaron. Hay un botón para copiar esos datos y pegarlos en el informe.
Leyenda de colores: dice qué significa cada color del mapa.
Qué hay en cada carpeta
Assets/Scripts/
├── TileFactory.cs           crea los cuadritos de colores
├── TerrainGenerator.cs      el terreno y las zonas (y coordina todo lo demás)
├── LSystemTreeGenerator.cs  los árboles
├── DungeonGenerator.cs      las mazmorras
├── CreatureGenerator.cs     los animales, enemigos y jefes
├── GenerationMenu.cs        el menú
└── CameraController.cs      la cámara
Lo que no logré (todavía)
Los gráficos son solo cuadritos de colores, sin dibujos.
No se puede entrar a las mazmorras porque no hice un personaje.
Las criaturas no se mueven.
Con árboles muy grandes, el mundo tarda más en generarse.

Si tuviera más tiempo, haría dibujos en pixel art, pueblos, un personaje que explore y misiones.

Autor

Benjamín Gálvez, IA y PCG para videojuegos, Tarea 2 (2026).
