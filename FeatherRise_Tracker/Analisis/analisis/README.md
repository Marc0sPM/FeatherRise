# Análisis de telemetría — Feather Rise

Pipeline reproducible para calcular las métricas de la Práctica 3 a partir de los archivos de trazas generados por el sistema de telemetría del juego.

## Requisitos

- Python ≥ 3.10
- Dependencias en `requirements.txt`

## Instalación

```bash
python3 -m venv venv
#py                               # depende de la version
source venv/bin/activate          # Linux / macOS
# venv\Scripts\Activate.ps1       # Windows
pip install -r requirements.txt
```

## Uso

Coloca los archivos `.json` y/o `.csv` de telemetría en `data/` y ejecuta:

```bash
python analyze_telemetry.py
```

Argumentos opcionales:

| Argumento | Default | Descripción |
|:--|:--|:--|
| `--data` | `data` | Carpeta de entrada con las trazas |
| `--output` | `output` | Carpeta donde escribir los resultados |

## Salidas generadas

```
output/
├── metrics_summary.json     # todas las métricas en formato máquina
├── metrics_summary.md       # resumen legible con detalles
├── events_normalized.csv    # eventos tras limpieza (auditoría)
└── figures/                 # gráficas .png
    ├── M1_1_feather_recall.png
    ├── M4_1_death_distribution.png
    ├── M4_2_time_per_tramo.png
    ├── M5_1_chest_opening.png
    └── M7_attack_analysis.png
```

## Métricas calculadas

Ver `FASE_1__Diseño_de_la_Evaluación_Analítica.md` para la definición completa. En resumen:

- **M1.1** — Tasa de fallos en el recall de plumas
- **M4.1** — Distribución espacial de muertes
- **M4.2** — Tiempo medio entre checkpoints
- **M4.3** — Muertes por minuto en cada tramo
- **M5.1** — Tasa de apertura de cofres por nivel
- **M7.1** — Reparto Ground/Aerial de ataques
- **M7.2** — Hit Rate global y por tipo

## Limpieza de datos

El script aplica automáticamente dos pasadas de saneamiento:

1. **Deduplicación exacta** — elimina filas idénticas (causadas por el doble `Save` síncrono al cierre de aplicación del tracker).
2. **Deduplicación de `Level_End`** — conserva sólo el primer `Level_End` por par `(session_id, level_id)` (causado por el trigger del componente de fin de nivel disparándose en cada frame).

Los eventos de jugabilidad de alta frecuencia (`Player_Attack`, `Feather_Recall_Attempt`, `Checkpoint_Reached`) **NO** se deduplican, porque su valor analítico está en su frecuencia.

## Conclusiones

Ver `Analisis_Telemetria.md` para el informe de resultados con conclusiones por hipótesis.
