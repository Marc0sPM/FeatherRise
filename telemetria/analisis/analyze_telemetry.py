"""
================================================================================
 Feather Rise — Análisis de Telemetría (Práctica 3)
================================================================================
Script reproducible y automático que calcula todas las métricas definidas en
la Fase 1 de la evaluación analítica a partir de los archivos de trazas.

USO:
    python analyze_telemetry.py [--data DIR] [--output DIR]

Por defecto:
    --data    ./data       (carpeta con los .json y .csv de telemetría)
    --output  ./output     (carpeta donde se generan los resultados)

SALIDAS:
    output/metrics_summary.json   →  todas las métricas en formato máquina
    output/metrics_summary.md     →  resumen legible
    output/events_normalized.csv  →  todos los eventos normalizados (auditoría)
    output/figures/*.png          →  gráficas de apoyo al análisis

REQUISITOS:
    pandas, matplotlib, numpy, seaborn
================================================================================
"""

from __future__ import annotations

import argparse
import csv
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import seaborn as sns

# ---------------------------------------------------------------------------
# 1.  CARGA Y NORMALIZACIÓN DE TRAZAS
# ---------------------------------------------------------------------------

def _load_json_file(path: Path) -> list[dict[str, Any]]:
    """Carga un archivo JSON de trazas.

    El serializador del juego escribe el archivo como una lista de listas,
    donde cada sub-lista es un *flush* (volcado a disco). Aplanamos todo a
    una única secuencia de eventos.
    """
    with path.open(encoding="utf-8") as f:
        raw = json.load(f)

    events: list[dict[str, Any]] = []
    if isinstance(raw, list):
        for item in raw:
            if isinstance(item, list):
                events.extend(item)
            elif isinstance(item, dict):
                events.append(item)
    elif isinstance(raw, dict):
        events.append(raw)
    return events


def _load_csv_file(path: Path) -> list[dict[str, Any]]:
    """Carga un archivo CSV de trazas.

    El serializador CSV guarda los atributos del evento serializados como
    JSON dentro de la columna `event_data`. Ese JSON tiene siempre TODOS
    los campos del evento, así que es la fuente más rica.
    """
    events: list[dict[str, Any]] = []
    with path.open(encoding="utf-8", newline="") as f:
        reader = csv.DictReader(f)
        for row in reader:
            blob = row.get("event_data", "").strip()
            try:
                ev = json.loads(blob)
            except (ValueError, TypeError):
                # Fallback: usar las columnas planas
                ev = {
                    "timestamp": int(row["timestamp"]),
                    "session_id": row["session_id"],
                    "event_type": row["event_type"],
                }
            events.append(ev)
    return events


def load_all_traces(data_dir: Path) -> pd.DataFrame:
    """Carga todos los archivos .json y .csv del directorio en un único DataFrame.

    Añade dos columnas auxiliares:
        - source_file: nombre del archivo origen (útil para depurar)
        - load_order:  índice de aparición dentro del archivo (para desempates)
    """
    rows: list[dict[str, Any]] = []
    files = sorted(list(data_dir.glob("*.json")) + list(data_dir.glob("*.csv")))
    if not files:
        raise FileNotFoundError(f"No se encontraron .json ni .csv en {data_dir}")

    for fp in files:
        loader = _load_json_file if fp.suffix == ".json" else _load_csv_file
        try:
            evs = loader(fp)
        except Exception as ex:
            print(f"[WARN] No se pudo leer {fp.name}: {ex}")
            continue
        for i, ev in enumerate(evs):
            ev = dict(ev)  # copia defensiva
            ev["source_file"] = fp.name
            ev["load_order"] = i
            rows.append(ev)

    df = pd.DataFrame(rows)
    if df.empty:
        raise RuntimeError("No se cargó ningún evento.")

    # Tipado y orden temporal
    df["timestamp"] = pd.to_numeric(df["timestamp"], errors="coerce").astype("Int64")
    df = df.sort_values(["session_id", "timestamp", "load_order"]).reset_index(drop=True)

    print(f"[INFO] Cargados {len(df)} eventos de {len(files)} archivos "
          f"({df['session_id'].nunique()} sesiones únicas).")
    return df


def deduplicate_events(df: pd.DataFrame) -> pd.DataFrame:
    """Limpieza defensiva de eventos duplicados.

    Aplicamos dos pasadas de saneamiento para hacer el análisis robusto
    frente a trazas generadas con versiones antiguas del tracker:
      1. Archivos cuyo contenido aparece duplicado fila a fila
         (caso conocido del CSV con la versión previa del tracker, ya
         corregida en la implementación actual). Mantenemos la pasada
         como red de seguridad por si se procesan archivos antiguos.
      2. `Level_End` repetido decenas de veces porque el trigger del nivel
         lo lanza varias veces en la misma sesión y nivel.

    Nota de diseño: NO deduplicamos `Player_Attack` ni `Feather_Recall_Attempt`,
    porque su valor está precisamente en su frecuencia (cada pulsación cuenta).
    """
    initial = len(df)

    # Caso 1: filas exactamente idénticas (excepto load_order/source_file)
    cmp_cols = [c for c in df.columns if c not in ("load_order", "source_file")]
    df = df.drop_duplicates(subset=cmp_cols, keep="first")

    # Caso 2: Level_End duplicados dentro de la misma sesión y nivel
    mask_level_end = df["event_type"] == "Level_End"
    if mask_level_end.any():
        dedup_le = (
            df.loc[mask_level_end]
              .drop_duplicates(subset=["session_id", "level_id"], keep="first")
        )
        df = pd.concat([df.loc[~mask_level_end], dedup_le], ignore_index=True)
        df = df.sort_values(["session_id", "timestamp", "load_order"]).reset_index(drop=True)

    removed = initial - len(df)
    if removed:
        print(f"[INFO] Eliminados {removed} eventos duplicados "
              f"({removed/initial:.1%} del total).")
    return df


# ---------------------------------------------------------------------------
# 2.  CÁLCULO DE MÉTRICAS
# ---------------------------------------------------------------------------

@dataclass
class MetricResult:
    code: str
    name: str
    value: Any
    detail: dict[str, Any]


def _attach_level_id(target: pd.DataFrame, df_all: pd.DataFrame) -> pd.DataFrame:
    """Devuelve `target` con una columna `level_id` (y `nivel_label`) válida.

    Asocia cada fila de `target` al `Level_Start` previo dentro de la misma
    sesión usando merge_asof backward. Si la fila ya traía `level_id`, se
    respeta y sólo se rellenan los huecos.
    """
    target = target.copy()
    if "level_id" not in target.columns:
        target["level_id"] = pd.NA

    if target["level_id"].isna().any():
        levels = df_all[df_all["event_type"] == "Level_Start"].copy()
        if not levels.empty and "level_id" in levels.columns:
            levels_sorted = (
                levels[["session_id", "timestamp", "level_id"]]
                    .rename(columns={"timestamp": "level_ts",
                                     "level_id": "level_id_inferred"})
                    .sort_values("level_ts").reset_index(drop=True)
            )
            target_sorted = target.sort_values("timestamp").reset_index(drop=True)
            merged = pd.merge_asof(
                target_sorted,
                levels_sorted,
                by="session_id",
                left_on="timestamp",
                right_on="level_ts",
                direction="backward",
            )
            target = merged
            target["level_id"] = target["level_id"].fillna(
                target["level_id_inferred"])

    target["nivel_label"] = target["level_id"].apply(
        lambda v: f"Nivel {int(v)}" if pd.notna(v) else "Sin nivel asignado"
    )
    return target


def metric_m11_feather_recall_failure(df: pd.DataFrame) -> MetricResult:
    """M1.1 — Tasa de intentos de recogida fallidos.

    Porcentaje de eventos `Feather_Recall_Attempt` con `is_successful == false`
    respecto al total de intentos.
    """
    sub = df[df["event_type"] == "Feather_Recall_Attempt"].copy()
    total = len(sub)
    if total == 0:
        return MetricResult("M1.1", "Tasa de recogidas fallidas", None,
                            {"total_intentos": 0})

    sub = sub.sort_values(["session_id", "timestamp"]).copy()
    sub["is_successful"] = sub["is_successful"].astype("boolean")
    sub["fallo"] = (~sub["is_successful"]).astype(int)
    failed = int(sub["fallo"].sum())
    rate = failed / total

    # Numeración cronológica de intentos por sesión (1, 2, 3, …) y
    # tiempo relativo desde el primer intento de cada sesión, para
    # poder analizar evolución temporal y detectar curva de aprendizaje.
    sub["intento_n"] = sub.groupby("session_id").cumcount() + 1
    sub["t_rel_s"] = sub.groupby("session_id")["timestamp"].transform(
        lambda s: s - s.min()
    )

    # Tasa de fallo acumulada (a partir del intento N, ¿cuánto llevo fallando?)
    sub["tasa_fallo_acum"] = (
        sub.groupby("session_id")["fallo"]
           .transform(lambda s: s.expanding().mean())
           .round(4)
    )

    # Tasa de fallo móvil con ventana de 5 intentos (suaviza la señal y
    # permite ver tendencias locales sin que un único acierto/fallo
    # domine la curva)
    sub["tasa_fallo_movil"] = (
        sub.groupby("session_id")["fallo"]
           .transform(lambda s: s.rolling(window=5, min_periods=1).mean())
           .round(4)
    )

    # Por sesión, para ver si la fricción es transversal o de un solo jugador
    by_session = (
        sub.groupby("session_id")
           .agg(intentos=("fallo", "size"),
                fallos=("fallo", "sum"))
           .assign(tasa_fallo=lambda d: (d["fallos"] / d["intentos"]).round(4))
           .reset_index()
    )

    # Serie temporal completa para la visualización de aprendizaje
    serie_temporal = sub[["session_id", "timestamp", "t_rel_s",
                          "intento_n", "fallo",
                          "tasa_fallo_acum", "tasa_fallo_movil"]].to_dict(orient="records")

    # Comparación primer tercio vs último tercio de los intentos de cada
    # sesión: si hay aprendizaje, la tasa de fallo bajaría
    aprendizaje = []
    for sid, grp in sub.groupby("session_id"):
        n = len(grp)
        if n < 6:  # con menos de 6 intentos los tercios no son significativos
            continue
        tercio = max(1, n // 3)
        primer = grp.iloc[:tercio]["fallo"].mean()
        ultimo = grp.iloc[-tercio:]["fallo"].mean()
        aprendizaje.append({
            "session_id": sid,
            "n_intentos": int(n),
            "tasa_fallo_primer_tercio": round(float(primer), 4),
            "tasa_fallo_ultimo_tercio": round(float(ultimo), 4),
            "delta": round(float(ultimo - primer), 4),
        })

    return MetricResult(
        code="M1.1",
        name="Tasa de intentos de recogida fallidos",
        value=round(rate, 4),
        detail={
            "intentos_totales": total,
            "intentos_fallidos": failed,
            "intentos_exitosos": total - failed,
            "tasa_fallo_pct": round(rate * 100, 2),
            "por_sesion": by_session.to_dict(orient="records"),
            "aprendizaje_primer_vs_ultimo_tercio": aprendizaje,
            "serie_temporal": serie_temporal,
        },
    )


def metric_m41_death_distribution(df: pd.DataFrame) -> MetricResult:
    """M4.1 — Distribución espacial de muertes (coordenadas + nivel).

    Si los eventos `Player_Death` no llevan `level_id` propio, se infiere
    asociando cada muerte al `Level_Start` previo de la misma sesión.
    """
    sub = df[df["event_type"] == "Player_Death"].copy()
    if sub.empty:
        return MetricResult("M4.1", "Distribución espacial de muertes", None,
                            {"total_muertes": 0})

    sub = _attach_level_id(sub, df)

    by_cause = sub["cause_of_death"].value_counts(dropna=False).to_dict()
    by_level = sub["nivel_label"].value_counts(dropna=False).to_dict()

    # En la salida dejamos solo `nivel_label` (ya humanizado).
    # Mantenemos `level_id` internamente en el DataFrame `sub` para que
    # el heatmap pueda ordenar los niveles correctamente, pero NO lo
    # exportamos al JSON de métricas.
    cols = ["session_id", "timestamp", "pos_x", "pos_y",
            "cause_of_death", "nivel_label"]
    coords = sub[[c for c in cols if c in sub.columns]].to_dict(orient="records")

    return MetricResult(
        code="M4.1",
        name="Distribución espacial de muertes",
        value=len(sub),
        detail={
            "total_muertes": len(sub),
            "por_causa": by_cause,
            "por_nivel": by_level,
            "coordenadas": coords,
        },
    )


def metric_m42_time_between_checkpoints(df: pd.DataFrame) -> MetricResult:
    """M4.2 — Tiempo promedio por tramo (segundos entre checkpoints
    consecutivos dentro de la misma sesión).

    Cada tramo se identifica por el nivel y la posición del checkpoint
    de destino, para que la lectura sea inequívoca.
    """
    sub = df[df["event_type"] == "Checkpoint_Reached"].copy()
    if sub.empty:
        return MetricResult("M4.2", "Tiempo promedio por tramo", None,
                            {"tramos_analizados": 0})

    # Asociar cada checkpoint a su nivel (mediante el Level_Start previo)
    sub = _attach_level_id(sub, df)
    sub = sub.sort_values(["session_id", "timestamp"])
    sub["delta_s"] = sub.groupby("session_id")["timestamp"].diff()
    deltas = sub["delta_s"].dropna()

    if deltas.empty:
        return MetricResult("M4.2", "Tiempo promedio por tramo", None,
                            {"tramos_analizados": 0,
                             "nota": "Cada sesión tiene 1 solo checkpoint, "
                                     "no hay tramos comparables."})

    # Identificamos cada tramo por (nivel, posición del checkpoint de DESTINO).
    # Redondeamos a 1 decimal para agrupar entre sesiones distintas.
    sub["pos_label"] = (
        "(" + sub["pos_x"].round(1).astype(str)
        + ", " + sub["pos_y"].round(1).astype(str) + ")"
    )
    sub["tramo_label"] = sub["nivel_label"] + " · " + sub["pos_label"]

    by_tramo = (
        sub.dropna(subset=["delta_s"])
           .groupby(["nivel_label", "level_id", "pos_label", "tramo_label"],
                    dropna=False)["delta_s"]
           .agg(["count", "mean", "median", "min", "max"])
           .round(2)
           .reset_index()
           .rename(columns={"count": "n_visitas",
                            "mean": "media_s",
                            "median": "mediana_s",
                            "min": "min_s",
                            "max": "max_s"})
           # Ordenar por nivel y luego por tiempo medio descendente
           .sort_values(["level_id", "media_s"],
                        ascending=[True, False],
                        na_position="last")
    )

    return MetricResult(
        code="M4.2",
        name="Tiempo promedio por tramo (segundos)",
        value=round(float(deltas.mean()), 2),
        detail={
            "tramos_analizados": int(len(deltas)),
            "media_global_s": round(float(deltas.mean()), 2),
            "mediana_global_s": round(float(deltas.median()), 2),
            "min_s": int(deltas.min()),
            "max_s": int(deltas.max()),
            "por_tramo": by_tramo.to_dict(orient="records"),
        },
    )


def metric_m43_deaths_per_minute(df: pd.DataFrame) -> MetricResult:
    """M4.3 — Ratio de muertes por minuto en cada tramo.

    Asociamos cada muerte al último Checkpoint_Reached de la misma sesión
    y calculamos cuántas muertes ocurren por minuto en ese tramo.
    """
    deaths = df[df["event_type"] == "Player_Death"].copy()
    if deaths.empty:
        return MetricResult("M4.3", "Ratio de muertes por minuto", None,
                            {"total_muertes": 0})

    # Asociar cada muerte al último checkpoint anterior en la misma sesión
    cps = df[df["event_type"] == "Checkpoint_Reached"].copy()
    if cps.empty:
        return MetricResult("M4.3", "Ratio de muertes por minuto", None,
                            {"total_muertes": int(len(deaths)),
                             "nota": "No hay checkpoints para asociar muertes."})

    # merge_asof exige que ambos lados estén ordenados por la clave temporal
    cps_sorted = (cps[["session_id", "timestamp", "pos_x", "pos_y"]]
                  .rename(columns={"timestamp": "cp_ts",
                                   "pos_x": "cp_x", "pos_y": "cp_y"})
                  .sort_values("cp_ts").reset_index(drop=True))
    deaths_sorted = (deaths[["session_id", "timestamp", "pos_x", "pos_y",
                             "cause_of_death"]]
                     .sort_values("timestamp").reset_index(drop=True))

    # merge_asof empareja cada muerte con el checkpoint inmediatamente anterior
    # de la misma sesión (by=session_id, direction="backward")
    deaths_with_cp = pd.merge_asof(
        deaths_sorted,
        cps_sorted,
        by="session_id",
        left_on="timestamp",
        right_on="cp_ts",
        direction="backward",
    )

    deaths_with_cp = deaths_with_cp.dropna(subset=["cp_ts"])
    if deaths_with_cp.empty:
        return MetricResult("M4.3", "Ratio de muertes por minuto", 0,
                            {"total_muertes": int(len(deaths)),
                             "nota": "Todas las muertes ocurrieron antes "
                                     "del primer checkpoint."})

    deaths_with_cp["tramo_id"] = (
        "(" + deaths_with_cp["cp_x"].round(1).astype(str)
        + ", " + deaths_with_cp["cp_y"].round(1).astype(str) + ")"
    )

    # Tiempo total que cada sesión pasó en cada tramo
    cps2 = cps.copy()
    cps2["delta_s"] = cps2.groupby("session_id")["timestamp"].diff()
    cps2["tramo_id"] = (
        "(" + cps2["pos_x"].round(1).astype(str)
        + ", " + cps2["pos_y"].round(1).astype(str) + ")"
    )
    tiempo_por_tramo = (
        cps2.dropna(subset=["delta_s"])
            .groupby("tramo_id")["delta_s"].sum()
            .reset_index(name="tiempo_total_s")
    )

    muertes_por_tramo = (
        deaths_with_cp.groupby("tramo_id")
                      .size().reset_index(name="muertes")
    )

    ratio = muertes_por_tramo.merge(tiempo_por_tramo, on="tramo_id", how="left")
    ratio["muertes_por_min"] = (
        ratio["muertes"] / ratio["tiempo_total_s"].replace(0, np.nan) * 60
    ).round(3)

    return MetricResult(
        code="M4.3",
        name="Ratio de muertes por minuto (por tramo)",
        value=round(float(ratio["muertes_por_min"].mean()), 3),
        detail={
            "total_muertes": int(len(deaths)),
            "tramos_con_muerte": int(len(ratio)),
            "ratio_global_muertes_min": round(
                float(ratio["muertes_por_min"].mean()), 3),
            "por_tramo": ratio.to_dict(orient="records"),
        },
    )


def metric_m51_chest_open_rate(df: pd.DataFrame) -> MetricResult:
    """M5.1 — Tasa de apertura de cofres secundarios.

    Porcentaje de sesiones que llegaron a un nivel y abrieron al menos un
    cofre en él. Calculamos por nivel y desglosamos por chest_id.
    """
    chests = df[df["event_type"] == "Chest_Opened"].copy()
    if chests.empty:
        return MetricResult("M5.1", "Tasa de apertura de cofres secundarios",
                            None, {"total_aperturas": 0})

    # Sesiones que jugaron cada nivel (Level_Start o checkpoints con level_id)
    if "level_id" in df.columns:
        sesiones_por_nivel = (
            df.dropna(subset=["level_id"])
              .groupby("level_id")["session_id"]
              .nunique().reset_index(name="sesiones_en_nivel")
        )
    else:
        sesiones_por_nivel = pd.DataFrame(columns=["level_id", "sesiones_en_nivel"])

    # Sesiones que abrieron cofre en cada nivel
    aperturas_por_nivel = (
        chests.groupby("level_id")["session_id"]
              .nunique().reset_index(name="sesiones_que_abrieron")
    )

    tabla = aperturas_por_nivel.merge(sesiones_por_nivel,
                                      on="level_id", how="left")
    tabla["tasa_apertura"] = (
        tabla["sesiones_que_abrieron"] / tabla["sesiones_en_nivel"].replace(0, np.nan)
    ).round(4)

    # Desglose por tipo de cofre
    por_tipo = chests.groupby("chest_id").size().reset_index(name="aperturas_totales")

    return MetricResult(
        code="M5.1",
        name="Tasa de apertura de cofres secundarios",
        value=round(float(tabla["tasa_apertura"].mean()), 4)
              if not tabla.empty else None,
        detail={
            "total_aperturas": int(len(chests)),
            "sesiones_unicas_que_abrieron": int(chests["session_id"].nunique()),
            "por_nivel": tabla.to_dict(orient="records"),
            "por_tipo_cofre": por_tipo.to_dict(orient="records"),
        },
    )


def metric_m71_attack_type_breakdown(df: pd.DataFrame) -> MetricResult:
    """M7.1 — Porcentaje de uso por tipo de ataque (Ground vs. Aerial)."""
    sub = df[df["event_type"] == "Player_Attack"].copy()
    total = len(sub)
    if total == 0:
        return MetricResult("M7.1", "Distribución de tipo de ataque", None,
                            {"total_ataques": 0})

    # Normalizamos a minúsculas (el código del juego ya lo hace, pero por seguridad)
    sub["attack_type"] = sub["attack_type"].astype(str).str.lower()
    counts = sub["attack_type"].value_counts().to_dict()
    pct = {k: round(v / total * 100, 2) for k, v in counts.items()}

    return MetricResult(
        code="M7.1",
        name="Porcentaje de uso por tipo de ataque",
        value=pct,
        detail={
            "total_ataques": total,
            "conteo": counts,
            "porcentaje": pct,
        },
    )


def metric_m72_hit_rate(df: pd.DataFrame) -> MetricResult:
    """M7.2 — Hit rate global y por tipo de ataque."""
    sub = df[df["event_type"] == "Player_Attack"].copy()
    total = len(sub)
    if total == 0:
        return MetricResult("M7.2", "Hit rate", None, {"total_ataques": 0})

    sub["enemy_hit"] = sub["enemy_hit"].astype("boolean")
    sub["attack_type"] = sub["attack_type"].astype(str).str.lower()

    hits_total = int(sub["enemy_hit"].sum())
    rate_global = round(hits_total / total, 4)

    por_tipo = (
        sub.groupby("attack_type")["enemy_hit"]
           .agg(intentos="size",
                aciertos=lambda s: int(s.astype("boolean").sum()))
           .assign(hit_rate=lambda d: (d["aciertos"] / d["intentos"]).round(4))
           .reset_index()
    )

    return MetricResult(
        code="M7.2",
        name="Hit Rate (global y por tipo)",
        value=rate_global,
        detail={
            "total_ataques": total,
            "aciertos": hits_total,
            "hit_rate_global": rate_global,
            "hit_rate_pct": round(rate_global * 100, 2),
            "por_tipo": por_tipo.to_dict(orient="records"),
        },
    )


# ---------------------------------------------------------------------------
# 3.  VISUALIZACIONES
# ---------------------------------------------------------------------------

def _setup_style() -> None:
    sns.set_theme(style="whitegrid", context="talk")
    plt.rcParams["figure.dpi"] = 110
    plt.rcParams["savefig.bbox"] = "tight"


def plot_feather_recall(m11: MetricResult, out: Path) -> None:
    """Visualización temporal de la métrica M1.1 — versión curva.

    Dos paneles:
        1. Secuencia cronológica de intentos por sesión (verde=éxito, rojo=fallo).
        2. Curva de % de fallo móvil (ventana = 5 intentos) para cada sesión.
           Una pendiente descendente indica aprendizaje; una pendiente
           ascendente o plana indica que no lo hay.
    """
    serie = m11.detail.get("serie_temporal", [])
    if not serie:
        return

    df = pd.DataFrame(serie)
    if df.empty:
        return

    sessions = list(df["session_id"].unique())
    palette = sns.color_palette("tab10", n_colors=len(sessions))
    color_map = {sid: palette[i] for i, sid in enumerate(sessions)}
    short = {sid: sid[:8] for sid in sessions}

    fig, axes = plt.subplots(2, 1, figsize=(12, 9),
                             gridspec_kw={"height_ratios": [1, 1.2]})

    # ----- Panel 1: secuencia cronológica de intentos -----
    ax = axes[0]
    for i, sid in enumerate(sessions):
        grp = df[df["session_id"] == sid].sort_values("intento_n")
        ax.plot(grp["intento_n"], np.full(len(grp), i),
                color=color_map[sid], linewidth=1.0, alpha=0.4, zorder=1)
        for _, row in grp.iterrows():
            color = "#D62246" if row["fallo"] == 1 else "#4C9F70"
            ax.scatter(row["intento_n"], i, c=color, s=180,
                       edgecolor="black", linewidth=0.7, zorder=2)
    ax.set_yticks(range(len(sessions)))
    ax.set_yticklabels([f"{short[s]}  (n={int((df['session_id']==s).sum())})"
                        for s in sessions])
    ax.set_xlabel("Nº de intento de recall (orden cronológico)")
    ax.set_ylabel("Sesión")
    ax.set_title("Secuencia cronológica de intentos "
                 "(verde = éxito · rojo = fallo)")
    from matplotlib.lines import Line2D
    legend_elems = [
        Line2D([0], [0], marker="o", color="w",
               markerfacecolor="#4C9F70", markeredgecolor="black",
               markersize=12, label="Éxito"),
        Line2D([0], [0], marker="o", color="w",
               markerfacecolor="#D62246", markeredgecolor="black",
               markersize=12, label="Fallo"),
    ]
    ax.legend(handles=legend_elems, loc="upper right", fontsize=10)

    # ----- Panel 2: curva de % fallo móvil (ventana=5) -----
    ax = axes[1]
    for sid in sessions:
        grp = df[df["session_id"] == sid].sort_values("intento_n")
        if len(grp) < 2:
            continue
        ax.plot(grp["intento_n"], grp["tasa_fallo_movil"] * 100,
                marker="o", color=color_map[sid],
                label=short[sid], linewidth=2.5, markersize=8)
    ax.set_xlabel("Nº de intento de recall")
    ax.set_ylabel("% de fallo (media de los últimos 5 intentos)")
    ax.set_ylim(-5, 105)
    ax.axhline(50, color="gray", linestyle=":", linewidth=1, alpha=0.7)
    ax.set_title("Curva de aprendizaje · % de fallo a lo largo de la sesión\n"
                 "(pendiente bajando = aprende · subiendo/plana = no aprende)")
    ax.legend(title="Sesión", fontsize=10, loc="best")

    fig.suptitle("M1.1 · Evolución temporal del recall de plumas",
                 fontsize=15, y=1.00)
    fig.tight_layout()
    fig.savefig(out / "M1_1_feather_recall.png")
    plt.close(fig)


def plot_death_heatmap(m41: MetricResult, out: Path) -> None:
    """Visualización mejorada de M4.1.

    Diseño:
        - Fila 0: panel resumen con barras del total de muertes por nivel,
          desglosadas por causa. Asegura que el conteo total sea visible
          incluso cuando los puntos espaciales se solapan.
        - Filas siguientes: un panel por nivel con scatter cuyo TAMAÑO
          y NÚMERO ANOTADO son proporcionales al conteo de muertes en
          esa coordenada (resuelve el solapamiento). Si hay ≥ 4 puntos
          únicos se añade un KDE de fondo para ver clusters.
    """
    coords = m41.detail.get("coordenadas", [])
    if not coords:
        return

    df = pd.DataFrame(coords)
    if "nivel_label" not in df.columns:
        df["nivel_label"] = "Sin nivel asignado"

    def _level_sort_key(label: str):
        if label.startswith("Nivel"):
            try:
                return (0, int(label.split()[1]))
            except (IndexError, ValueError):
                return (0, 999)
        return (1, 0)

    levels = sorted(df["nivel_label"].unique(), key=_level_sort_key)
    n_levels = len(levels)

    cause_palette = {
        "void": "#3F88C5",
        "enemy_mele": "#D62246",
        "enemy_range": "#FFB347",
    }
    # Causas no contempladas reciben un color por defecto
    all_causes = sorted(df["cause_of_death"].dropna().unique())
    for c in all_causes:
        cause_palette.setdefault(c, "#888888")

    # --- Layout: 1 fila resumen + filas de heatmaps ---
    n_cols = min(3, n_levels)
    n_rows_maps = (n_levels + n_cols - 1) // n_cols
    fig = plt.figure(figsize=(6 * n_cols, 4 + 5 * n_rows_maps))
    gs = fig.add_gridspec(n_rows_maps + 1, n_cols,
                          height_ratios=[1.2] + [1.6] * n_rows_maps,
                          hspace=0.45, wspace=0.3)

    # ====== Panel resumen: muertes totales por nivel y causa ======
    ax_sum = fig.add_subplot(gs[0, :])
    pivot = (df.groupby(["nivel_label", "cause_of_death"])
               .size()
               .unstack(fill_value=0))
    pivot = pivot.reindex(levels)  # orden estable por nivel

    bottom = np.zeros(len(pivot))
    x_pos = np.arange(len(pivot))
    for cause in pivot.columns:
        vals = pivot[cause].values
        bars = ax_sum.bar(x_pos, vals, bottom=bottom,
                          color=cause_palette.get(cause, "#888"),
                          edgecolor="black", linewidth=0.6,
                          label=f"{cause} (total: {int(vals.sum())})")
        # Anotar conteo dentro de cada segmento si es ≥ 1
        for i, v in enumerate(vals):
            if v > 0:
                ax_sum.text(x_pos[i], bottom[i] + v / 2,
                            str(int(v)), ha="center", va="center",
                            fontsize=11, fontweight="bold",
                            color="white")
        bottom += vals
    # Total encima de cada barra apilada
    for i, total in enumerate(bottom):
        ax_sum.text(x_pos[i], total + 0.15, f"n = {int(total)}",
                    ha="center", va="bottom", fontsize=12,
                    fontweight="bold")

    ax_sum.set_xticks(x_pos)
    ax_sum.set_xticklabels(pivot.index, fontsize=11)
    ax_sum.set_ylabel("Nº de muertes")
    ax_sum.set_ylim(0, bottom.max() * 1.18 + 0.5)
    ax_sum.set_title(f"Resumen · Total de muertes = {len(df)} "
                     f"(desglose por nivel y causa)")
    ax_sum.legend(loc="upper right", fontsize=9, title="Causa")

    # ====== Heatmaps por nivel ======
    for idx, lvl in enumerate(levels):
        row, col = divmod(idx, n_cols)
        ax = fig.add_subplot(gs[row + 1, col])

        sub = df[df["nivel_label"] == lvl]
        n_total = len(sub)

        # Agrupamos por (pos_x, pos_y) redondeado para ver los puntos
        # solapados como un único marcador de tamaño proporcional
        sub_g = sub.copy()
        sub_g["px_r"] = sub_g["pos_x"].round(1)
        sub_g["py_r"] = sub_g["pos_y"].round(1)
        clusters = (sub_g.groupby(["px_r", "py_r", "cause_of_death"])
                         .size().reset_index(name="conteo"))
        # Para cada coordenada, además, sumamos todas las causas para
        # decidir el tamaño del marcador "global" (visible cuando hay
        # más de una causa en la misma posición)
        coord_totals = (sub_g.groupby(["px_r", "py_r"]).size()
                              .reset_index(name="conteo_total"))
        clusters = clusters.merge(coord_totals, on=["px_r", "py_r"])

        # Fondo tipo "heatmap" para indicar intensidad de muertes en la zona.
        # Intentamos primero con KDE (sns); si el espacio tiene muy poca
        # variabilidad (todos los puntos casi iguales, pocos puntos únicos)
        # el KDE falla silenciosamente y usamos un halo gaussiano manual
        # alrededor de cada cluster.
        kde_rendered = False
        if n_total >= 2:
            try:
                # Comprobamos variabilidad en cada eje antes de intentar KDE
                x_std = float(sub["pos_x"].std() or 0)
                y_std = float(sub["pos_y"].std() or 0)
                if x_std > 0.1 and y_std > 0.1 and len(sub_g[["px_r", "py_r"]]
                                                       .drop_duplicates()) >= 3:
                    sns.kdeplot(x=sub["pos_x"], y=sub["pos_y"], fill=True,
                                cmap="Reds", thresh=0.05, alpha=0.55, ax=ax,
                                warn_singular=False, bw_adjust=1.2)
                    kde_rendered = True
            except Exception:
                kde_rendered = False

        # Fallback: halos gaussianos manuales alrededor de cada cluster.
        # Usamos scatter (tamaño en puntos², independiente del aspect
        # ratio del eje) en vez de Circle para evitar deformaciones
        # cuando los rangos de X e Y son muy diferentes.
        if not kde_rendered:
            max_count = int(coord_totals["conteo_total"].max())
            for _, row_c in coord_totals.iterrows():
                intensity = row_c["conteo_total"] / max_count
                # 6 capas concéntricas, tamaño creciente y alpha decreciente
                for k, size_mult in enumerate([6.0, 4.5, 3.3, 2.4, 1.7, 1.2]):
                    ax.scatter(row_c["px_r"], row_c["py_r"],
                               s=(500 + 2800 * intensity) * size_mult,
                               c="#D62246",
                               alpha=0.035 + 0.02 * intensity,
                               edgecolors="none",
                               zorder=0)

        # Scatter: tamaño proporcional al conteo en esa coordenada
        # (multiplicador grande para que sean bien visibles)
        for _, row_c in clusters.iterrows():
            color = cause_palette.get(row_c["cause_of_death"], "#888")
            size = 200 + row_c["conteo_total"] * 220  # base + escala
            ax.scatter(row_c["px_r"], row_c["py_r"],
                       s=size, c=color, alpha=0.85,
                       edgecolor="black", linewidth=1.2)

        # Anotar el conteo TOTAL de muertes en cada coordenada (claro y grande)
        import matplotlib.patheffects as mpe
        for _, row_c in coord_totals.iterrows():
            ax.text(row_c["px_r"], row_c["py_r"],
                    str(int(row_c["conteo_total"])),
                    ha="center", va="center",
                    fontsize=12, fontweight="bold", color="white",
                    path_effects=[mpe.withStroke(linewidth=2.5,
                                                 foreground="black")])

        # Leyenda de causas presente en este nivel
        causes_here = sub["cause_of_death"].value_counts()
        legend_handles = [
            Line2D_for_legend(cause_palette.get(c, "#888"),
                              f"{c} (n={int(causes_here[c])})")
            for c in causes_here.index
        ]
        if legend_handles:
            ax.legend(handles=legend_handles, loc="best", fontsize=9,
                      title="Causa")

        ax.set_title(f"{lvl} · {n_total} muertes en "
                     f"{len(coord_totals)} ubicaciones")
        ax.set_xlabel("pos_x")
        ax.set_ylabel("pos_y")
        ax.grid(True, alpha=0.3)
        # Margen extra alrededor de los puntos para que los marcadores
        # grandes no salgan del frame
        if not sub.empty:
            x_margin = max(2.0, (sub["pos_x"].max() - sub["pos_x"].min()) * 0.15)
            y_margin = max(2.0, (sub["pos_y"].max() - sub["pos_y"].min()) * 0.15)
            ax.set_xlim(sub["pos_x"].min() - x_margin,
                        sub["pos_x"].max() + x_margin)
            ax.set_ylim(sub["pos_y"].min() - y_margin,
                        sub["pos_y"].max() + y_margin)

    fig.suptitle(f"M4.1 · Distribución espacial de muertes por nivel",
                 fontsize=15, y=0.995)
    fig.savefig(out / "M4_1_death_distribution.png")
    plt.close(fig)


def Line2D_for_legend(color: str, label: str):
    """Helper para crear handles de leyenda para los scatters."""
    from matplotlib.lines import Line2D
    return Line2D([0], [0], marker="o", color="w",
                  markerfacecolor=color, markeredgecolor="black",
                  markersize=11, label=label)


def plot_time_per_tramo(m42: MetricResult, out: Path) -> None:
    """Visualización de M4.2 agrupando por nivel.

    Barras horizontales con el tiempo medio por tramo, ETIQUETADAS
    explícitamente con el nivel y la posición del checkpoint destino.
    Cada nivel recibe un color distinto para distinguirlo de un vistazo.
    """
    if not m42.detail.get("por_tramo"):
        return
    df = pd.DataFrame(m42.detail["por_tramo"])
    if df.empty:
        return

    # Etiqueta combinada: "Nivel X · (px, py)"
    if "tramo_label" not in df.columns:
        df["tramo_label"] = (df.get("nivel_label", "Sin nivel").astype(str)
                             + " · " + df.get("pos_label",
                             df.get("tramo_destino", "")).astype(str))

    # Ordenamos por nivel y dentro de cada nivel por tiempo medio descendente
    if "level_id" in df.columns:
        df = df.sort_values(["level_id", "media_s"],
                            ascending=[True, False],
                            na_position="last").reset_index(drop=True)
    else:
        df = df.sort_values("media_s", ascending=False).reset_index(drop=True)

    # Color por nivel
    nivel_unique = df["nivel_label"].unique() if "nivel_label" in df.columns \
                   else ["(sin nivel)"]
    palette = sns.color_palette("Set2", n_colors=len(nivel_unique))
    color_by_nivel = {nv: palette[i] for i, nv in enumerate(nivel_unique)}
    bar_colors = [color_by_nivel.get(nv, "#888")
                  for nv in df.get("nivel_label", ["(sin nivel)"] * len(df))]

    fig, ax = plt.subplots(figsize=(11, max(4.5, 0.55 * len(df) + 2)))

    y_pos = np.arange(len(df))
    ax.barh(y_pos, df["media_s"], color=bar_colors,
            edgecolor="black", linewidth=0.7)

    for i, row in df.iterrows():
        ax.text(row["media_s"] + 0.6, i,
                f"{row['media_s']:.1f}s   (n={int(row['n_visitas'])})",
                va="center", fontsize=10)

    ax.set_yticks(y_pos)
    ax.set_yticklabels(df["tramo_label"], fontsize=10)
    ax.invert_yaxis()  # primer tramo arriba
    ax.set_xlabel("Tiempo medio entre checkpoints (segundos)")
    ax.set_title("M4.2 · Tiempo medio por tramo (agrupado por nivel)")
    ax.set_xlim(0, df["media_s"].max() * 1.20 + 5)

    # Leyenda explícita de niveles
    handles = [
        plt.Rectangle((0, 0), 1, 1,
                      facecolor=color_by_nivel[nv], edgecolor="black",
                      label=nv)
        for nv in nivel_unique
    ]
    ax.legend(handles=handles, title="Nivel", loc="lower right", fontsize=10)

    fig.tight_layout()
    fig.savefig(out / "M4_2_time_per_tramo.png")
    plt.close(fig)


def plot_attack_breakdown(m71: MetricResult, m72: MetricResult, out: Path) -> None:
    if m71.value is None:
        return

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(14, 5))

    # M7.1 — pastel
    counts = m71.detail["conteo"]
    labels_pie = list(counts.keys())
    sizes = list(counts.values())
    colors = sns.color_palette("Set2", n_colors=len(labels_pie))
    ax1.pie(sizes, labels=labels_pie, autopct="%1.1f%%",
            colors=colors, startangle=90,
            wedgeprops=dict(edgecolor="black"))
    ax1.set_title(f"M7.1 · Tipo de ataque (n={m71.detail['total_ataques']})")

    # M7.2 — barras de hit rate por tipo
    if m72.detail.get("por_tipo"):
        df72 = pd.DataFrame(m72.detail["por_tipo"])
        bars = ax2.bar(df72["attack_type"], df72["hit_rate"] * 100,
                       color=colors[:len(df72)], edgecolor="black")
        for b, row in zip(bars, df72.itertuples()):
            ax2.text(b.get_x() + b.get_width() / 2, b.get_height() + 1,
                     f"{row.hit_rate*100:.1f}%\n({row.aciertos}/{row.intentos})",
                     ha="center", va="bottom", fontsize=11)
        ax2.set_ylim(0, 110)
        ax2.set_ylabel("Hit Rate (%)")
        ax2.axhline(m72.detail["hit_rate_global"] * 100,
                    color="red", linestyle="--", linewidth=1.5,
                    label=f"Global = {m72.detail['hit_rate_pct']}%")
        ax2.legend()
        ax2.set_title("M7.2 · Hit Rate por tipo de ataque")

    fig.savefig(out / "M7_attack_analysis.png")
    plt.close(fig)


def plot_chest_opening(m51: MetricResult, out: Path) -> None:
    """Visualización de M5.1 — distribución de contenidos de cofres abiertos.

    Muestra qué hay dentro de los cofres que los jugadores SÍ abren. Permite
    detectar si hay tipos de cofre (según su contenido) que se ignoran
    sistemáticamente en comparación con otros.
    """
    por_tipo = m51.detail.get("por_tipo_cofre", [])
    if not por_tipo:
        return

    df = pd.DataFrame(por_tipo).sort_values("aperturas_totales", ascending=False)
    if df.empty:
        return

    total = int(df["aperturas_totales"].sum())

    fig, ax = plt.subplots(figsize=(9, max(4.5, 0.55 * len(df) + 2)))
    palette = sns.color_palette("Set2", n_colors=len(df))
    y_pos = np.arange(len(df))

    bars = ax.barh(y_pos, df["aperturas_totales"],
                   color=palette, edgecolor="black", linewidth=0.7)

    # Anotar conteo absoluto y porcentaje sobre el total
    for b, (_, row) in zip(bars, df.iterrows()):
        pct = row["aperturas_totales"] / total * 100 if total else 0
        ax.text(b.get_width() + 0.08, b.get_y() + b.get_height() / 2,
                f"{int(row['aperturas_totales'])}   ({pct:.1f} %)",
                va="center", fontsize=11)

    ax.set_yticks(y_pos)
    ax.set_yticklabels(df["chest_id"], fontsize=11)
    ax.invert_yaxis()  # el más abierto arriba
    ax.set_xlabel("Nº de aperturas registradas")
    ax.set_title(f"M5.1 · Contenido de los cofres abiertos\n"
                 f"(total de aperturas = {total})")
    ax.set_xlim(0, df["aperturas_totales"].max() * 1.25 + 1)
    ax.grid(axis="x", alpha=0.3)

    fig.tight_layout()
    fig.savefig(out / "M5_1_chest_opening.png")
    plt.close(fig)


# ---------------------------------------------------------------------------
# 4.  PIPELINE PRINCIPAL
# ---------------------------------------------------------------------------

def write_summary_md(metrics: list[MetricResult], df: pd.DataFrame, out: Path) -> None:
    lines: list[str] = []
    lines.append("# Resumen automático de métricas\n")
    lines.append(f"- Eventos cargados: **{len(df)}**\n")
    lines.append(f"- Sesiones únicas: **{df['session_id'].nunique()}**\n")
    lines.append(f"- Archivos procesados: **{df['source_file'].nunique()}**\n\n")
    lines.append("---\n\n")
    for m in metrics:
        lines.append(f"## {m.code} — {m.name}\n")
        lines.append(f"**Valor principal:** `{m.value}`\n\n")
        lines.append("```json\n")
        lines.append(json.dumps(m.detail, indent=2, ensure_ascii=False, default=str))
        lines.append("\n```\n\n")
    (out / "metrics_summary.md").write_text("".join(lines), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Análisis de telemetría Feather Rise")
    parser.add_argument("--data", type=Path, default=Path("data"),
                        help="Carpeta con los archivos de trazas (.json y .csv)")
    parser.add_argument("--output", type=Path, default=Path("output"),
                        help="Carpeta donde escribir resultados")
    args = parser.parse_args()

    args.output.mkdir(parents=True, exist_ok=True)
    figs_dir = args.output / "figures"
    figs_dir.mkdir(exist_ok=True)

    print("\n=== 1. Carga y normalización ===")
    df = load_all_traces(args.data)
    df = deduplicate_events(df)

    # Guardamos eventos normalizados para auditoría
    audit_cols = [c for c in ["timestamp", "session_id", "event_type", "level_id",
                              "pos_x", "pos_y", "is_successful", "attack_type",
                              "enemy_hit", "chest_id", "cause_of_death", "result",
                              "source_file"] if c in df.columns]
    df[audit_cols].to_csv(args.output / "events_normalized.csv",
                          index=False, encoding="utf-8")

    print("\n=== 2. Cálculo de métricas ===")
    metrics: list[MetricResult] = []
    for fn in (metric_m11_feather_recall_failure,
               metric_m41_death_distribution,
               metric_m42_time_between_checkpoints,
               metric_m43_deaths_per_minute,
               metric_m51_chest_open_rate,
               metric_m71_attack_type_breakdown,
               metric_m72_hit_rate):
        m = fn(df)
        metrics.append(m)
        print(f"  [{m.code}] {m.name}: {m.value}")

    print("\n=== 3. Visualizaciones ===")
    _setup_style()
    plot_feather_recall(metrics[0], figs_dir)
    plot_death_heatmap(metrics[1], figs_dir)
    plot_time_per_tramo(metrics[2], figs_dir)
    plot_chest_opening(metrics[4], figs_dir)
    plot_attack_breakdown(metrics[5], metrics[6], figs_dir)
    print(f"  Gráficas escritas en {figs_dir}")

    print("\n=== 4. Volcado de resultados ===")
    summary = {m.code: {"name": m.name, "value": m.value, "detail": m.detail}
               for m in metrics}
    (args.output / "metrics_summary.json").write_text(
        json.dumps(summary, indent=2, ensure_ascii=False, default=str),
        encoding="utf-8")
    write_summary_md(metrics, df, args.output)
    print(f"  metrics_summary.json y metrics_summary.md generados en {args.output}")
    print("\nListo.\n")


if __name__ == "__main__":
    main()