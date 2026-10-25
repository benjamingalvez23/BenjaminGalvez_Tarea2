# BenjaminGalvez_Tarea2
# Un mundo de fantasía creado por el computador

Proyecto de la **Tarea 2 de IA y PCG para videojuegos** (profesor Nicolás Romero).

Es un programa hecho en Unity que crea mundos de fantasía solo, sin que yo tenga que dibujarlos. Cada vez que lo uso sale un mundo distinto, con terreno, árboles, cuevas y criaturas.

- **Juego en Itch.io:** [agregar link]

## Capturas

| Reino de exploración | Tierras de mazmorras |
|---|---|
| ![Reino](capturas/reino.png) | ![Mazmorras](capturas/mazmorras.png) |

## Qué hace

Usé tres técnicas de generación procedural, cada una para una parte del mundo:

| Parte del mundo | Técnica | Explicado fácil |
|---|---|---|
| El terreno | Ruido fractal | Mezcla varias capas de ondulaciones para hacer agua, arena, pasto y roca, y reparte el pasto en 3 zonas de fantasía. |
| Los árboles | L-System | Un árbol crece a partir de una palabra y unas reglas, como si una tortuga dibujara las ramas. |
| Las mazmorras | Autómata celular | Cada cuadrito copia lo que hacen sus vecinos y el desorden se vuelve una cueva. |

También coloca animales y enemigos, y dentro de las cuevas pone cofres, cajas, enemigos y un jefe.

Todo parte de un número llamado **semilla**: con la misma semilla sale siempre el mismo mundo.

## Los dos tipos de mundo

- **Reino de exploración:** mucho pasto, bosques grandes y pocas mazmorras.
- **Tierras de mazmorras:** más roca, arbustos sueltos y varias cuevas grandes con más enemigos y tesoros.

Se cambia entre ellos con un botón del menú.

## Cómo abrirlo

1. Clona este repositorio:
   ```
   git clone https://github.com/benjamingalvez23/Tarea2-PCG-Videojuegos-BenjaminGalvez.git
   ```
2. Abre **Unity Hub**, pulsa **Add** y elige la carpeta del repositorio.
3. Usa la versión de Unity **[agregar tu versión]**.
4. Abre la escena **[agregar nombre de la escena]** y pulsa **Play**.

La primera vez Unity tarda unos minutos preparando el proyecto. No hay que instalar nada más.

## Controles

| Qué quiero hacer | Cómo |
|---|---|
| Moverme | `W A S D` o flechas (`Shift` = más rápido) |
| Arrastrar el mapa | Clic derecho y mover el mouse |
| Acercar o alejar | Rueda del mouse |
| Mostrar u ocultar el menú | `Tab` |

## El menú

Está a la izquierda y cada barra tiene una explicación corta. Con él puedo elegir el tipo de mundo, crear uno nuevo, cambiar las opciones de cada técnica, ocultar capas (el botón **Solo** deja una sola a la vista) y ver estadísticas del mundo generado.

## Uso de IA generativa

Usé Claude (Anthropic) como ayuda para guiarme paso a paso, escribir y corregir el código de Unity y diseñar el menú. Yo elegí el estilo y las técnicas, armé el proyecto, probé todo, ajusté las opciones y analicé los resultados.

## Autor

Benjamín Gálvez, 2026.
