# Monterra

Monterra es un videojuego 2D de captura y combate de criaturas desarrollado en Unity. El proyecto se enmarca dentro del género *Monster Battler* con mecánicas de rol y elementos inspirados en el *Battle Royale*.

El jugador explora un mapa donde puede encontrar y enfrentarse a distintas criaturas y enemigos controlados por inteligencia artificial, formando un equipo propio para combatir mediante un sistema por turnos.

---

## Estado actual del proyecto

Esta versión corresponde a una **fase temprana del desarrollo (prototipo funcional)**.

Actualmente incluye:

* Sistema de combate por turnos
* Equipo de criaturas del jugador
* Enemigos distribuidos por el mapa
* Inicio de combate al interactuar con enemigos
* Sistema básico de experiencia y progresión
* Transición a menú al finalizar la partida

### Condiciones actuales de final de partida

* El jugador gana tras derrotar a **5 enemigos**, lo que provoca el retorno al menú principal
* Si todos los miembros del equipo del jugador son derrotados, también se regresa al menú

> Nota: El sistema de victoria definitivo y condiciones completas de fin de partida se implementarán en futuras versiones.

---

## Tecnologías utilizadas

* **Motor:** Unity
* **Lenguaje:** C#
* **Arquitectura:** Programación orientada a objetos
* Uso de:

  * ScriptableObjects
  * Corrutinas
  * Sistema de eventos

---

## Cómo ejecutar el proyecto

1. Clonar el repositorio:

   ```
   git clone https://github.com/tu-usuario/monterra.git
   ```

2. Abrir el proyecto con **Unity Hub**

3. Seleccionar la escena principal (MainMenu o equivalente)

4. Ejecutar el proyecto desde el editor de Unity

---

## Controles (provisionales)

* Movimiento: WASD
* Mapa: M
* Opciones: ESC
* Combate: selección mediante interfaz de botones

---

## Estructura del proyecto

* `Scripts/` → Lógica del juego (combate, sistema de criaturas, UI)
* `ScriptableObjects/` → Definición de criaturas y habilidades
* `Scenes/` → Escenas del juego (menú, mundo, combate)
* `UI/` → Elementos de interfaz

---

## Trabajo futuro

* Implementación del sistema de victoria completo
* Mejora del sistema de progresión
* Balanceo del combate
* Sistema de captura de criaturas
* Mejora de UI/UX
* Integración del sistema tipo Battle Royale (tormenta)

---

## Autor

Desarrollado como parte del Trabajo de Fin de Grado en Ingeniería Informática (UOC).

---

## Licencia

Este proyecto se desarrolla con fines académicos.
