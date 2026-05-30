# Monterra

*Monterra* es un videojuego 2D de captura y combate de criaturas desarrollado en Unity como Trabajo Final de Grado en Ingeniería Informática en la UOC.

El proyecto se enmarca dentro del género *Monster Battler*, combinando exploración, captura de criaturas, gestión de equipo, combate por turnos, progresión mediante experiencia y una estructura de partida breve inspirada en la presión espacial de los juegos tipo *Battle Royale*.

El jugador explora un mapa abierto dividido en zonas, captura Mons salvajes, forma un equipo propio y se enfrenta a entrenadores controlados por inteligencia artificial. La partida se desarrolla bajo la presión de una tormenta que reduce progresivamente el área segura, obligando al jugador a desplazarse, gestionar recursos y tomar decisiones estratégicas.

---

## Estado actual del proyecto

Esta versión corresponde a la versión final del prototipo desarrollado para la PEC 4.

Actualmente incluye:

* Sistema de combate por turnos.
* Captura de criaturas salvajes mediante MonBalls.
* Equipo de hasta seis Mons.
* Cambio de Mon durante la exploración y el combate.
* Sistema de experiencia, subida de nivel, aprendizaje de movimientos y evolución.
* Relaciones de efectividad entre tipos.
* Entrenadores con equipos propios.
* Inteligencia artificial local para las decisiones de combate.
* Generación opcional de barks narrativos mediante backend y API externa.
* Mapa abierto con biomas, caminos, obstáculos, zonas de aparición y campanas de curación.
* Sistema de tormenta que reduce progresivamente el área segura.
* Persistencia mediante JSON.
* Guardado y carga de partida.
* Registro de MonBalls recogidas y entrenadores derrotados.
* Desbloqueo visual de Mons capturados en el menú principal.
* Menú principal, menú de opciones, HUD, mapa, tabla de tipos y pantallas de victoria y derrota.
* Feedback visual y sonoro para ataques, capturas, cambios de Mon, pasos y eventos principales.

---

## Condiciones de final de partida

El jugador gana la partida al derrotar a todos los entrenadores activos del escenario.

El jugador pierde si todos los Mons de su equipo quedan debilitados, ya sea durante un combate o por el daño de la tormenta.

---

## Tecnologías utilizadas

* **Motor:** Unity.
* **Lenguaje:** C#.
* **Arquitectura:** programación orientada a objetos y arquitectura modular.
* **Persistencia:** serialización JSON y PlayerPrefs.
* **IA narrativa opcional:** backend local en Node.js y API externa.
* **Control de versiones:** Git y GitHub.

Funcionalidades y herramientas destacadas:

* ScriptableObjects.
* Corrutinas.
* Prefabs.
* Tilemaps.
* Colliders 2D.
* AudioMixer.
* Unity Input System.
* Unity UI.
* Backend local mediante `server.js`.

---

## Cómo ejecutar el proyecto desde Unity

1. Clonar el repositorio:

   ```bash
   git clone https://github.com/AngelRuizFlores/Monterra.git
   ```

2. Abrir el proyecto con Unity Hub.

3. Abrir la escena principal del menú.

4. Ejecutar el proyecto desde el editor de Unity.

---

## Cómo ejecutar la build

1. Descargar el archivo comprimido de la versión final desde la sección de releases del repositorio.

2. Descomprimir el archivo `.zip`.

3. Ejecutar el archivo principal del juego.

El juego está preparado para sistemas Windows y no requiere instalación adicional.

---

## Controles

* **WASD:** mover al personaje.
* **Botón izquierdo del ratón:** interactuar con menús, botones e interfaz de combate.
* **ESC:** abrir o cerrar el menú de opciones.
* **M:** abrir o cerrar el mapa.
* **T:** abrir o cerrar la tabla de tipos.
* **1, 2, 3, 4 y 5:** cambiar de Mon fuera de combate, siempre que haya criaturas disponibles en esos slots.

Durante el combate:

* **Botones de ataque:** seleccionar el movimiento que se desea utilizar.
* **SWITCH:** cambiar de Mon.
* **CATCH:** intentar capturar una criatura salvaje.

---

## Sistema de barks narrativos

El proyecto incluye un sistema opcional de barks narrativos para los entrenadores.

La lógica de combate principal funciona en local mediante el modo clásico de IA, por lo que el juego no depende de servicios externos para resolver los turnos enemigos. Sin embargo, si se ejecuta el backend local, los entrenadores pueden generar frases narrativas personalizadas al inicio del combate.

La arquitectura utilizada es la siguiente:

```text
Unity -> Backend local -> API externa -> Backend local -> Unity
```

Unity envía un contexto en formato JSON al backend local. Este contexto incluye información como el entrenador, su personalidad, el Mon enemigo, el Mon del jugador y el estado del combate. El backend utiliza una API key privada para comunicarse con el servicio externo y devuelve a Unity un JSON con el bark generado.

La API key no se almacena dentro del proyecto de Unity, sino en el archivo `.env` del backend local.

---

## Ejecución del backend de barks

Para usar los barks narrativos generados por API, es necesario ejecutar el backend local.

Desde la carpeta del backend:

```bash
node server.js
```

El servidor se ejecuta localmente en:

```text
http://localhost:3000
```

El endpoint utilizado por Unity para los barks es:

```text
http://localhost:3000/api/enemy/bark
```

Si el backend no está activo o la API externa no está disponible, el juego puede seguir funcionando con la lógica local de combate.

---

## Estructura general del proyecto

* `Scripts/` → Lógica principal del juego.
* `ScriptableObjects/` → Definición de Mons, movimientos y entrenadores.
* `Scenes/` → Escenas del juego.
* `Prefabs/` → Objetos reutilizables.
* `UI/` → Elementos de interfaz.
* `Audio/` → Música y efectos sonoros.
* `Sprites/` → Recursos gráficos del juego.
* `Backend/` → Servidor local para barks narrativos, si se incluye en el repositorio.

---

## Autor

Desarrollado por Ángel Ruiz Flores como parte del Trabajo Final de Grado en Ingeniería Informática de la Universitat Oberta de Catalunya.

Tutor del Trabajo Final: Paolo Gambardella.

---

## Créditos

El proyecto utiliza recursos gráficos y sonoros de terceros debidamente acreditados en la pantalla de créditos del videojuego.

Los diseños principales de los Mons han sido realizados por Javier Asensio Lozano, quien también ha participado en las fases de prueba del prototipo.

---

## Licencia

Este proyecto se desarrolla con fines académicos.

La memoria del trabajo está sujeta a la licencia indicada en el documento de entrega. Los recursos externos utilizados mantienen sus respectivas licencias originales.
