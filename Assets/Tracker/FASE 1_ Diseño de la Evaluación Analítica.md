# **FASE 1: Diseño de la Evaluación Analítica**

Este documento define la estrategia de telemetría implementada para Feather Rise. El objetivo es recopilar datos cuantitativos para validar o refutar de forma estadística las fricciones de diseño (brechas M, D y A) detectadas previamente durante el playtesting cualitativo.

## **1\. Objetivos e Hipótesis de Evaluación**

Se han seleccionado las hipótesis que resultaron refutadas (o parcialmente refutadas) en el análisis cualitativo para medir su impacto real a través del comportamiento de los jugadores.

**Hipótesis 1 (Gestión de Recursos y Fricción de Controles):**

* **Objetivo Analítico:** Cuantificar la sobrecarga cognitiva y la fricción motriz generada por el sistema de recogida de plumas.  
* **Hipótesis a validar:** El sistema actual genera un alto índice de intentos de recogida fallidos. Un intento se considera fallido y genera un bloqueo en el flujo de juego bien porque el jugador intenta recoger las plumas sin cumplir la condición necesaria (haber lanzado 3\) o bien porque pulsa la tecla de recogida ('W') de forma errónea al intentar saltar.

**Hipótesis 4 (Dificultad y Cuellos de Botella Generales):**

* **Objetivo Analítico:** Analizar la distribución de muertes a lo largo de todo el juego para identificar cuellos de botella no previstos y evaluar la curva de dificultad global de los niveles.  
* **Hipótesis a validar:** Existen picos de dificultad desbalanceados a lo largo de los niveles que provocan frustración en los jugadores, no limitándose únicamente a las zonas ya detectadas. Específicamente, se asume que áreas problemáticas como el puzle del nivel 2.2 concentrarán una tasa de muertes desproporcionadamente alta debido a fallos de diseño (falta de feedback visual), pero el análisis exploratorio permitirá descubrir otros posibles cuellos de botella en el resto del juego. Además, se correlacionará el tiempo de supervivencia por tramo con la cantidad de muertes para distinguir si la dificultad se debe a ensayo y error rápido (problema motriz/mecánico) o a atascos de comprensión (problema cognitivo).

**Hipótesis 5 (Exploración y Recompensa \- Cofres):**

* **Objetivo Analítico:** Evaluar la tasa de interacción y el atractivo de las rutas secundarias.  
* **Hipótesis a validar:** Los jugadores ignoran los cofres de las rutas secundarias porque no comprenden su utilidad ni asimilan qué ventaja les proporcionan.

**Hipótesis 7 (Comportamiento Emergente en Combate):**

* **Objetivo Analítico:** Evaluar la eficiencia y el uso táctico del sistema de combate bimodal (ataque terrestre vs. aéreo) para identificar si los jugadores dominan las mecánicas o si recurren a estrategias de "spam".  
* **Hipótesis a validar:** Debido al diseño de los encuentros, los jugadores subutilizan el ataque aéreo y abusan del ataque terrestre de forma descontrolada ("spam"). Esto se evidenciará en una desproporción en el uso de ataques terrestres frente a los aéreos y en una baja tasa de aciertos (hit rate) general.

## **2\. Definición de Métricas**

* **M1.1:** Tasa de intentos de recogida fallidos. (Porcentaje de veces que se pulsa la tecla de recogida 'W' sin que se active la mecánica de recuperar las plumas).  
* **M4.1:** Distribución espacial de muertes (Coordenadas X, Y agrupadas por nivel).  
* **M4.2:** Tiempo promedio por tramo (Tiempo transcurrido entre un checkpoint y el siguiente).  
* **M4.3:** Ratio de muertes por minuto (Frecuencia de muertes asociada a cada tramo).  
* **M5.1:** Tasa de Apertura de Cofres Secundarios.  
* **M7.1:** Porcentaje de uso por tipo de ataque (Terrestre vs. Aéreo).  
* **M7.2:** Tasa de acierto de ataques o "Hit Rate" (Porcentaje de ataques que impactan en un enemigo sobre el total de ataques realizados).

## **3\. Definición de Eventos**

**Evento 1: Feather\_Recall\_Attempt** 

* **Cuándo se lanza:** Cada vez que el jugador pulsa la tecla asignada para la recogida de plumas ('W').  
* **Atributos:** is\_successful (Booleano: true si se ejecutó la acción de recoger las plumas, false si no se cumplían las condiciones y la acción falló).

**Evento 2: Player\_Death**

* **Cuándo se lanza:** Cuando la salud del jugador llega a 0\.  
* **Atributos:** pos\_x (Float), pos\_y (Float), cause\_of\_death (String), level\_id (String).

**Evento 3: Chest\_Opened**

* **Cuándo se lanza:** Al interactuar con éxito con un cofre.  
* **Atributos:** item\_given (String).

**Evento 4: Player\_Attack**

* **Cuándo se lanza:** Cada vez que el jugador hace clic para atacar con la espada.  
* **Atributos:** attack\_type (Enum: "Ground", "Aerial"), enemy\_hit (Booleano: true si acertó).

**Evento 5: Checkpoint\_Reached**

* **Cuándo se lanza:** Cuando el jugador alcanza y activa un punto de control.  
* **Atributos:** pos\_x (Float), pos\_y (Float).

## **4\. Cálculo de las métricas a partir de los eventos**

* **Cálculo de M1.1 (Tasa de intentos de recogida fallidos):** Se filtrará el evento Feather\_Recall\_Attempt. Se dividirá el número de eventos en los que is\_successful \== false entre el total de eventos Feather\_Recall\_Attempt recopilados, obteniendo así el porcentaje de fallos sobre el total de intentos.  
* **Cálculo de M4.1 (Distribución espacial de muertes):** Se extraerán los atributos pos\_x, pos\_y y level\_id de todos los eventos Player\_Death. Este conjunto de coordenadas constituirá la métrica, la cual será renderizada visualmente en forma de mapas de calor por nivel mediante scripts de Python para identificar fácilmente los clústeres de alta mortalidad en cualquier zona del juego.  
* **Cálculo de M4.2 (Tiempo promedio por tramo):** Utilizando el campo *timestamp* interno que guarda el tracker, se restará el tiempo del evento Checkpoint\_Reached actual menos el del Checkpoint\_Reached anterior.  
* **Cálculo de M4.3 (Ratio de muertes por minuto y Análisis de comportamiento):** De forma externa (mediante scripts de Python), cada evento Player\_Death se asociará al último Checkpoint\_Reached registrado justo antes de la muerte. Cruzando el número de muertes con el tiempo transcurrido en el tramo (M4.2), se calculará el ratio de muertes por minuto para deducir el tipo de fricción experimentada por el jugador (ej: mucho tiempo y pocas muertes \= atasco cognitivo/parálisis por análisis; poco tiempo y muchas muertes \= fricción motriz/ensayo y error rápido).  
* **Cálculo de M5.1 (Interacción de Cofres):** Se contabilizarán cuántos session\_id únicos han generado el evento Chest\_Opened para el cofre del Nivel 2.2, y se dividirá entre el total de session\_id únicos registrados en ese nivel.  
* **Cálculo de M7.1 (Porcentaje de uso por tipo de ataque):** A partir del evento Player\_Attack, se contabilizará el total de ataques. Luego, se calculará qué porcentaje de esos eventos tienen el atributo attack\_type \== "Ground" y qué porcentaje tienen attack\_type \== "Aerial".  
* **Cálculo de M7.2 (Hit Rate):** Se tomará el total de eventos Player\_Attack registrados. Se dividirá la cantidad de eventos donde el atributo enemy\_hit \== true entre el total de eventos Player\_Attack. (Opcionalmente, se puede calcular el hit rate por separado agrupando previamente por attack\_type).  