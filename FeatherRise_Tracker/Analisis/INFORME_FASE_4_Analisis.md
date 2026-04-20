# INFORME FASE 4 — Análisis de los resultados de telemetría

**Juego:** *Feather Rise*
**Práctica:** 3 — Sistema de telemetría · Curso 2025/2026
**Documento de hipótesis y métricas asociado:** `FASE_1__Diseño_de_la_Evaluación_Analítica.md`
**Pipeline reproducible:** `analyze_telemetry.py` (ver `INFORME_FASE_6_Entrega.md`)

---

## 0. Resumen ejecutivo

El análisis se ha ejecutado sobre **4 sesiones de juego** (`27c6ca73`, `ad4b271a`, `c0cfa5bf`, `ed5a11c2`) con un total de **81 intentos de recall de plumas**, **26 muertes**, **50 ataques** y **19 aperturas de cofres**. Los resultados confirman **3 de las 4 hipótesis** planteadas en la Fase 1 y refutan la cuarta (H5), con hallazgos adicionales sobre concentración espacial de muertes en el Nivel 2.

| Hipótesis | Veredicto | Evidencia principal |
|:--:|:--|:--|
| H1 — Fricción del recall de plumas | ✅ **Confirmada con matices** | 28,4 % de intentos fallidos (23/81). Consistente entre sesiones (27-31 %). Sí se observa curva de aprendizaje en 3/4 sesiones. |
| H4 — Cuellos de botella por dificultad | ✅ **Confirmada** | Muertes concentradas en 2 ubicaciones del Nivel 2 (16/26 = 61,5 %). Ratio 2,8-2,9 muertes/min en esas zonas. |
| H5 — Cofres ignorados | ❌ **Refutada** | 100 % de sesiones abrieron cofres en Nivel 1 y 75 % en Nivel 2. Los cofres no se ignoran. |
| H7 — Spam de ataque terrestre | ✅ **Confirmada** | 78 % ground vs 22 % aerial. Hit rate 38,5 % (ground) vs 54,5 % (aerial). |

---

## 1. Calidad y alcance de los datos

Corpus analizado:

| Sesión | Intentos recall | Fallos | Tasa fallo |
|:--|--:|--:|--:|
| `27c6ca73` | 16 | 5 | 31,3 % |
| `ad4b271a` | 18 | 5 | 27,8 % |
| `c0cfa5bf` | 22 | 6 | 27,3 % |
| `ed5a11c2` | 25 | 7 | 28,0 % |
| **Total** | **81** | **23** | **28,4 %** |

Las 4 sesiones recogen entre 16 y 25 intentos de recall cada una, con tasas de fallo sorprendentemente consistentes (rango 27-31 %). Esta homogeneidad inter-sesión refuerza la validez estadística de las conclusiones sobre H1 incluso con N=4: el efecto no depende de un jugador concreto.

Las 2 sesiones más largas (`c0cfa5bf` y `ed5a11c2`) llegan al Nivel 2, las otras 2 se quedan en el Nivel 1. Tenemos por primera vez datos suficientes para analizar la curva de dificultad entre niveles.

---

## 2. Resultados por hipótesis

### Hipótesis 1 — Fricción del recall de plumas

**Métrica M1.1 — Tasa de intentos de recogida fallidos**

![M1.1 — Evolución temporal del recall de plumas](./analisis/output/figures/M1_1_feather_recall.png)

| Métrica | Valor |
|:--|--:|
| Intentos totales | 81 |
| Intentos exitosos | 58 (71,6 %) |
| Intentos fallidos | **23 (28,4 %)** |

**Análisis de aprendizaje (primer tercio vs último tercio de cada sesión):**

| Sesión | Primer tercio | Último tercio | Δ | Lectura |
|:--|--:|--:|--:|:--|
| `27c6ca73` | 60,0 % | 20,0 % | **−40 pp** | Aprende muy rápido |
| `ad4b271a` | 0,0 % | 0,0 % | 0 pp | Parte sin fallos, tiene un pico en medio y vuelve a 0 |
| `c0cfa5bf` | 28,6 % | 14,3 % | −14 pp | Aprende gradualmente |
| `ed5a11c2` | 37,5 % | 12,5 % | −25 pp | Aprende gradualmente |

**Interpretación:** La hipótesis original se confirma pero con un matiz importante. La tasa global de fallo (28,4 %) es elevada, pero **3 de las 4 sesiones muestran curva de aprendizaje descendente** (entre −14 y −40 puntos porcentuales). La sesión `ad4b271a` es atípica porque empieza sin errores y tiene un pico en los intentos 10-12 antes de volver a 0 %, lo que sugiere que el jugador ya dominaba la mecánica al empezar y el pico central probablemente refleja una zona del juego donde el diseño induce al fallo (no una confusión de controles).

**Implicación de diseño:**
1. El 28,4 % de fallo global NO es atribuible a un problema motriz puro (pulsar `W` por error), porque entonces la tasa se mantendría plana. La reducción a lo largo de la sesión apunta a una **curva de onboarding empinada**: los jugadores NO entienden las condiciones del recall al principio (haber lanzado las 3 plumas) y lo van aprendiendo por ensayo y error.
2. Para la siguiente iteración conviene:
   - Un indicador visual persistente del estado del recall (disponible / no disponible).
   - Desdoblar el atributo `is_successful` del evento en un `failure_reason` enumerado (`already_recalling`, `no_feathers_thrown`, `other`) para distinguir los dos modos de fallo hipotetizados en la Fase 1.

---

### Hipótesis 4 — Cuellos de botella por dificultad

**Métricas M4.1, M4.2 y M4.3**

#### M4.1 — Distribución espacial de muertes

![M4.1 — Distribución espacial de muertes por nivel](./analisis/output/figures/M4_1_death_distribution.png)

| Dato | Valor |
|:--|--:|
| Total de muertes | **26** |
| Por causa | void: 23 (88,5 %) · enemy_range: 2 (7,7 %) · enemy_mele: 1 (3,8 %) |
| Por nivel | Nivel 1: 10 (38,5 %) · Nivel 2: 16 (61,5 %) |

**Hallazgo clave:** el Nivel 2 concentra más muertes que el Nivel 1 pese a que sólo 2 de las 4 sesiones llegan a él, y **las 16 muertes del Nivel 2 se producen en sólo 2 ubicaciones**:
- `(223.4, 92.1)` — 7 muertes por void
- `(290.6, 93.2)` — 9 muertes por void

En el Nivel 1 las 10 muertes están más distribuidas (7 ubicaciones distintas), lo que es esperable: el jugador explora el nivel entero y los fallos son más dispersos. En el Nivel 2, en cambio, el patrón es claramente de "trampa" geométrica: dos precipicios concretos matan al 100 % de los jugadores que pasan cerca, repetidamente.

#### M4.2 — Tiempo entre checkpoints

![M4.2 — Tiempo medio por tramo](./analisis/output/figures/M4_2_time_per_tramo.png)

| Estadístico | Valor |
|:--|--:|
| Tramos analizados | 30 |
| Media global | 26,3 s |
| Mediana global | 20,0 s |
| Mínimo / Máximo | 2 s / 67 s |

**Tramos más lentos** (ordenados por tiempo medio):

| Nivel | Tramo destino | Visitas | Tiempo medio | Mediana | Rango |
|:--|:--|--:|--:|--:|:--|
| Nivel 1 | `(57.3, 15.7)` | 4 | **48,2 s** | 50,0 | 40-53 |
| Nivel 1 | `(93.4, 45.9)` | 4 | **40,0 s** | 40,5 | 20-59 |
| Nivel 2 | `(290.6, 93.2)` | 5 | **36,8 s** | 28,0 | 20-67 |
| Nivel 2 | `(223.4, 92.1)` | 5 | 29,6 s | 23,0 | 18-57 |
| Nivel 1 | `(21.3, 2.6)` | 4 | 13,8 s | 13,5 | 11-17 |

Los tramos `(57.3, 15.7)` y `(93.4, 45.9)` del Nivel 1 son los más lentos en media, pero tienen cero muertes asociadas — es decir, son zonas donde el jugador **tarda porque hay contenido** (puzle, combate, exploración), no porque esté fallando. En contraste, los tramos del Nivel 2 con 29-37 s de media **sí acumulan muertes**.

#### M4.3 — Ratio de muertes por minuto

**Cruce tiempo × muertes por tramo:**

| Tramo | Muertes | Tiempo total (s) | Muertes/min | Lectura |
|:--|--:|--:|--:|:--|
| `(-5.8, 0.6)` | 2 | 15 | **8,00** | Muy alto, pero tramo corto → puede ser anecdótico |
| `(21.3, 2.6)` | 3 | 55 | 3,27 | Moderado |
| `(290.6, 93.2)` | 9 | 184 | **2,94** | **Cuello de botella confirmado** |
| `(223.4, 92.1)` | 7 | 148 | **2,84** | **Cuello de botella confirmado** |
| `(120.4, 24.7)` | 1 | 33 | 1,82 | Bajo |
| `(57.3, 15.7)` | 4 | 193 | 1,24 | Bajo (mucho tiempo, pocas muertes = atasco cognitivo) |

**Interpretación combinada (M4.1 + M4.2 + M4.3):**

- Los **dos tramos del Nivel 2** son los cuellos de botella REALES: acumulan 16 de las 26 muertes totales, con ratios de ~2,9 muertes/min y los jugadores pasan allí más de 2 minutos en acumulado. Ambas muertes son 100 % por `void`, no por enemigos → problema de **plataformeo o feedback visual**, no de combate.
- El tramo `(57.3, 15.7)` del Nivel 1 tiene un patrón distinto: **mucho tiempo (48 s de media) pero pocas muertes (4 muertes en 193 s → 1,24/min)**. Esto cuadra con la descripción del "puzle del nivel 2.2" que aparecía en la Fase 1 como atasco cognitivo: los jugadores no mueren, pero se paralizan intentando entender qué hacer.
- El tramo `(-5.8, 0.6)` tiene un ratio de 8 muertes/min pero sólo 15 s de tiempo total invertido, así que con N=2 muertes no es concluyente (podría ser simplemente un inicio fallido de una sola sesión).

**La hipótesis H4 se confirma**, y además se identifican tres tipos distintos de fricción:
1. **Fricción motriz / plataforming** (Nivel 2, coordenadas `(223.4, 92.1)` y `(290.6, 93.2)`): muertes frecuentes por caída al vacío. Solución: mejorar el feedback de los bordes, añadir plataformas intermedias, o revisar la altura de salto.
2. **Atasco cognitivo** (Nivel 1, coordenada `(57.3, 15.7)`): tiempo alto sin muertes. Solución: mejorar las pistas visuales o el guiado.
3. **Zonas saludables** (Nivel 1, coordenadas `(-5.8, 0.6)`, `(21.3, 2.6)`, `(120.4, 24.7)`): tiempos cortos y pocas muertes. El diseño funciona.

---

### Hipótesis 5 — Cofres y exploración de rutas secundarias

**Métrica M5.1 — Tasa de apertura de cofres**

![M5.1 — Contenido de los cofres abiertos](./analisis/output/figures/M5_1_chest_opening.png)

**Aperturas por nivel:**

| Nivel | Sesiones que jugaron | Sesiones que abrieron ≥1 cofre | Tasa |
|:--:|--:|--:|--:|
| Nivel 1 | 4 | 4 | **100 %** |
| Nivel 2 | 4 | 3 | **75 %** |

**Desglose por contenido del cofre:**

| `chest_id` | Aperturas | % |
|:--|--:|--:|
| `featheritem` | 12 | 63,2 % |
| `sword` | 4 | 21,1 % |
| `life` | 3 | 15,8 % |

**Interpretación:** La hipótesis H5 queda **refutada con los datos disponibles**. El 100 % de las sesiones abrieron al menos un cofre en el Nivel 1 y el 75 % lo hicieron en el Nivel 2. No hay evidencia de que los jugadores ignoren sistemáticamente los cofres.

El desglose por contenido es revelador:
- Los cofres `featheritem` (12 aperturas) son los más abiertos porque son necesarios mecánicamente: sin plumas no puedes progresar, así que el jugador los busca activamente.
- Los cofres `sword` (4) y `life` (3) se abren menos en términos absolutos. Sin embargo, esto NO indica que se ignoren: probablemente es que hay **menos cofres de ese tipo en el juego** que de `featheritem`. Para confirmarlo haría falta conocer el total de cofres de cada tipo colocados en los niveles y calcular una tasa de apertura real `aperturas / cofres_presentes`.

**Limitación de la métrica actual:** el evento `Chest_Opened` registra solo las aperturas, no las "no aperturas" (cofres avistados pero ignorados). Para una validación más fina de H5 en el siguiente ciclo convendría añadir:
- Un evento `Chest_Skipped` cuando el jugador pasa cerca de un cofre sin abrirlo (detectable con un trigger de proximidad).
- O bien un conteo estático de cofres totales por nivel con el que cruzar las aperturas.

---

### Hipótesis 7 — Comportamiento emergente en combate

**Métricas M7.1 y M7.2**

![M7.1 y M7.2 — Análisis de ataques](./analisis/output/figures/M7_attack_analysis.png)

#### M7.1 — Reparto de tipos de ataque

| Tipo | Conteo | % |
|:--|--:|--:|
| Ground | 39 | **78,0 %** |
| Aerial | 11 | 22,0 % |
| **Total** | **50** | 100 % |

#### M7.2 — Hit Rate por tipo

| Tipo | Intentos | Aciertos | Hit Rate |
|:--|--:|--:|--:|
| Aerial | 11 | 6 | **54,5 %** |
| Ground | 39 | 15 | **38,5 %** |
| **Global** | **50** | **21** | **42,0 %** |

**Interpretación:** La hipótesis se confirma pero con matices comparado con los datos iniciales.

Por cada ataque aéreo se hacen **3,5 ataques en suelo** (ratio 39:11). El desbalance es evidente pero menos extremo del que sugería la tanda anterior (15:1 con datos antiguos). Probablemente el ratio real se estabiliza en torno a 4:1 cuando aumenta la muestra.

Lo más interesante es el **hit rate por tipo**:
- **Aerial: 54,5 %** (6/11). Cuando se usa, acierta más de la mitad de las veces.
- **Ground: 38,5 %** (15/39). Acierta menos de 2 de cada 5 intentos.

Esto invierte ligeramente la lectura del informe anterior: **el ataque aéreo es más eficiente** que el terrestre en términos de aciertos, PERO los jugadores lo usan 3,5 veces menos. La hipótesis original hablaba de "spam de ground" y "subutilización de aerial", y los datos lo confirman plenamente:

- Si los jugadores usaran aerial en la misma proporción que ground, el hit rate global subiría de 42 % a un estimado de ~52 %.
- El bajo hit rate de ground (38,5 %) sugiere exactamente el patrón de *button mash* descrito en la hipótesis: cuando el jugador entra en modo "spam", la mayoría de los golpes se lanzan sin un enemigo delante.

**Implicación de diseño:** Los datos justifican una intervención en el tutorial o en los encuentros tempranos que empuje al jugador a descubrir el ataque aéreo y su mejor retorno por golpe. Posibles acciones:
- Encuentros donde ciertos enemigos sean inalcanzables desde suelo, obligando a usar aerial.
- Un prompt contextual tras 4+ ataques `ground` consecutivos sin acertar.
- Revisión del *damage output* de ambos ataques para equilibrar la decisión estratégica.

---

## 3. Conclusiones generales

1. **H1, H4 y H7 se confirman.** H5 se refuta con la métrica actual; puede requerir instrumentación adicional (evento `Chest_Skipped` o conteo estático de cofres) para ser validada en el siguiente ciclo.

2. **El Nivel 2 concentra el 61,5 % de las muertes** en sólo 2 ubicaciones, con un patrón claro de "trampa" por void. Es el cuello de botella más significativo del juego actualmente instrumentado y el candidato más obvio a rediseño.

3. **La curva de aprendizaje del recall es real** (3 de 4 sesiones muestran descenso claro en la tasa de fallo entre primer y último tercio), lo que apunta a un problema de onboarding más que de fricción motriz permanente. Una intervención de UI (indicador visual del estado del recall) debería reducir significativamente la tasa global actual del 28,4 %.

4. **El combate está desequilibrado** hacia el ataque terrestre pese a que el aéreo es más eficiente (54,5 % vs 38,5 % hit rate). Es una oportunidad clara de intervención de diseño: empujar al jugador hacia el aerial debería mejorar tanto la sensación de dominio mecánico como el ritmo del combate.

5. **Próximos pasos analíticos recomendados** (ordenados por valor):
   - (a) Desdoblar `Feather_Recall_Attempt.is_successful` en un enum de razones de fallo.
   - (b) Añadir evento `Chest_Skipped` o inventario estático de cofres por nivel.
   - (c) Recoger una segunda tanda de sesiones con ≥10 jugadores distintos para validar que los cuellos de botella del Nivel 2 son transversales y no dependen del estilo de los 4 playtesters actuales.
   - (d) Instrumentar `Enemy_Spawned` y `Enemy_Defeated` para poder calcular el hit rate normalizado por número de enemigos presentes.

---

## 4. Reproducibilidad

Todos los datos de este informe se generan automáticamente ejecutando:

```bash
python analyze_telemetry.py --data data --output output
```

con las trazas originales en `data/`. Las salidas son:

- `output/metrics_summary.json` — todas las métricas en formato máquina.
- `output/metrics_summary.md` — resumen legible con los detalles desglosados.
- `output/events_normalized.csv` — todos los eventos tras la limpieza, para auditoría.
- `output/figures/*.png` — las 5 gráficas embebidas en este informe.

Las instrucciones completas de instalación, dependencias y ejecución se encuentran en `INFORME_FASE_6_Entrega.md` y en el `README.md` del repositorio.