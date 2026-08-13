import argparse
import json
import sys
from pathlib import Path

import essentia.standard as es
import numpy as np


MAEST_MODEL_NAME = "discogs-maest-30s-pw-519l-2"
MAEST_MODEL_FILE = f"{MAEST_MODEL_NAME}.pb"
MAEST_METADATA_FILE = f"{MAEST_MODEL_NAME}.json"
MUSICNN_MODEL_NAME = "msd-musicnn-1"
MIREX_MODEL_FILE = "moods_mirex-msd-musicnn-1.pb"
MIREX_METADATA_FILE = "moods_mirex-msd-musicnn-1.json"
SCRIPT_DIRECTORY = Path(__file__).resolve().parent
CONTAINER_MODELS_ROOT = SCRIPT_DIRECTORY / "models" / "Essentia"
REPOSITORY_MODELS_ROOT = SCRIPT_DIRECTORY.parent.parent / "models" / "Essentia"
DEFAULT_MODELS_ROOT = (
    CONTAINER_MODELS_ROOT if CONTAINER_MODELS_ROOT.is_dir() else REPOSITORY_MODELS_ROOT
)


def error(message: str) -> None:
    print(json.dumps({"success": False, "error": message}, ensure_ascii=False))
    raise SystemExit(1)


def read_metadata(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as file:
        return json.load(file)


def schema_name(metadata: dict, section: str, purpose: str | None = None) -> str:
    entries = metadata.get("schema", {}).get(section, [])
    if purpose is not None:
        entries = [entry for entry in entries if entry.get("output_purpose") == purpose]
    if not entries or not entries[0].get("name"):
        model = metadata.get("name", "unknown")
        description = purpose or section
        raise ValueError(f"Model '{model}' has no {description} tensor")
    return entries[0]["name"]


def aggregate_predictions(values, labels: list[str], model_name: str) -> list[dict]:
    scores = np.asarray(values)
    if scores.size == 0:
        raise ValueError(f"Model '{model_name}' returned no predictions")

    scores = scores.reshape(-1, len(labels)).mean(axis=0)
    if scores.size != len(labels):
        raise ValueError(
            f"Model '{model_name}' returned {scores.size} scores for {len(labels)} labels"
        )

    return [
        {"label": label, "score": float(score)}
        for label, score in zip(labels, scores)
    ]


def analyze_genres(audio, models_root: Path, top_count: int) -> tuple[list[dict], list[int], int]:
    directory = models_root / "DiscogsMAEST"
    metadata = read_metadata(directory / MAEST_METADATA_FILE)
    labels = metadata["classes"]
    predictions = es.TensorflowPredictMAEST(
        graphFilename=str(directory / MAEST_MODEL_FILE),
        output=schema_name(metadata, "outputs", "predictions"),
    )(audio)

    scores = np.asarray(predictions).reshape(-1, len(labels)).mean(axis=0)
    top_indices = scores.argsort()[-min(top_count, len(scores)) :][::-1]
    values = [
        {"label": labels[int(index)], "score": float(scores[int(index)])}
        for index in top_indices
    ]
    return values, list(np.asarray(predictions).shape), len(labels)


def analyze_mirex(audio, models_root: Path) -> dict:
    extractor_directory = models_root / "MusicNN" / "Extractor"
    extractor_metadata = read_metadata(extractor_directory / f"{MUSICNN_MODEL_NAME}.json")
    embeddings = es.TensorflowPredictMusiCNN(
        graphFilename=str(extractor_directory / f"{MUSICNN_MODEL_NAME}.pb"),
        output=schema_name(extractor_metadata, "outputs", "embeddings"),
    )(audio)

    head_directory = models_root / "MusicNN" / "Heads" / "Mood"
    head_metadata = read_metadata(head_directory / MIREX_METADATA_FILE)
    model_name = head_metadata.get("name", "moods mirex")
    predictions = es.TensorflowPredict2D(
        graphFilename=str(head_directory / MIREX_MODEL_FILE),
        input=schema_name(head_metadata, "inputs"),
        output=schema_name(head_metadata, "outputs", "predictions"),
        patchSize=1,
        patchHopSize=1,
    )(embeddings)

    return {
        "family": "msd-musicnn",
        "category": "Mood",
        "model": model_name,
        "type": head_metadata.get("type", "multi-class classifier"),
        "description": head_metadata.get("description", ""),
        "values": aggregate_predictions(predictions, head_metadata["classes"], model_name),
    }


def analyze_track(track_path: Path, models_root: Path, top_count: int) -> dict:
    audio_16k = es.MonoLoader(
        filename=str(track_path), sampleRate=16000, resampleQuality=4
    )()
    genre_predictions, prediction_shape, label_count = analyze_genres(
        audio_16k, models_root, top_count
    )

    bpm_audio = es.MonoLoader(
        filename=str(track_path), sampleRate=44100, resampleQuality=4
    )()
    bpm, _, _, _, _ = es.RhythmExtractor2013(method="multifeature")(bpm_audio)

    loudness_audio, sample_rate, _, *_ = es.AudioLoader(filename=str(track_path))()
    _, _, integrated_loudness, loudness_range = es.LoudnessEBUR128(
        sampleRate=sample_rate
    )(loudness_audio)

    experimental_predictions = []
    experimental_errors = []
    try:
        experimental_predictions.append(analyze_mirex(audio_16k, models_root))
    except Exception as exception:
        # Core analysis remains useful if only the optional emotional model fails.
        experimental_errors.append(
            {"family": "msd-musicnn", "model": "moods mirex", "error": str(exception)}
        )

    return {
        "success": True,
        "trackPath": str(track_path),
        "model": MAEST_MODEL_NAME,
        "predictionShape": prediction_shape,
        "labelCount": label_count,
        "bpm": float(bpm),
        "integratedLoudness": float(integrated_loudness),
        "loudnessRange": float(loudness_range),
        "predictions": genre_predictions,
        "experimentalPredictions": experimental_predictions,
        "experimentalErrors": experimental_errors,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Analyze one audio track for Resona")
    parser.add_argument("track_path", type=Path)
    parser.add_argument("--top", type=int, default=20, help="Number of genre scores to return")
    parser.add_argument("--models-root", type=Path, default=DEFAULT_MODELS_ROOT)
    args = parser.parse_args()

    if not args.track_path.is_file():
        error(f"Track file not found: {args.track_path}")
    if args.top < 1:
        error("--top must be at least 1")

    print(
        json.dumps(
            analyze_track(args.track_path, args.models_root, args.top),
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    try:
        main()
    except SystemExit:
        raise
    except Exception as exception:
        error(f"Analysis failed: {exception}")
