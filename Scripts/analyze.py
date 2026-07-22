import argparse
import json
import sys
from pathlib import Path

import numpy as np
import essentia.standard as es


MAEST_MODEL_NAME = "discogs-maest-30s-pw-519l-2"
MAEST_MODEL_FILE = "discogs-maest-30s-pw-519l-2.pb"
MAEST_METADATA_FILE = "discogs-maest-30s-pw-519l-2.json"
DEFAULT_MODEL_DIRECTORY = Path(__file__).resolve().parent.parent / "Models" / "Essentia" / "DiscogsMAEST"
EXCLUDED_HEAD_MODELS = {"mtg_" + "jamen" + "do_" + "mood" + "theme"}


def error(message):
    print(json.dumps({"success": False, "error": message}, ensure_ascii=False, indent=2))
    sys.exit(1)


def read_metadata(path):
    with path.open("r", encoding="utf-8") as file:
        return json.load(file)


def output_name(metadata, purpose):
    for output in metadata.get("schema", {}).get("outputs", []):
        if output.get("output_purpose") == purpose:
            return output["name"]
    raise ValueError(f"Model '{metadata.get('name', 'unknown')}' has no {purpose} output")


def input_name(metadata):
    inputs = metadata.get("schema", {}).get("inputs", [])
    if not inputs:
        raise ValueError(f"Model '{metadata.get('name', 'unknown')}' has no input specification")
    return inputs[0]["name"]


def aggregate(values, labels, model_name):
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


def analyze_maest(audio, model_directory, top_count):
    metadata = read_metadata(model_directory / MAEST_METADATA_FILE)
    labels = metadata["classes"]
    predictions = es.TensorflowPredictMAEST(
        graphFilename=str(model_directory / MAEST_MODEL_FILE),
        output="PartitionedCall/Identity_13"
    )(audio)
    scores = np.asarray(predictions).reshape(-1, len(labels)).mean(axis=0)
    top_indices = scores.argsort()[-min(top_count, len(scores)):][::-1]
    return [
        {"label": labels[int(index)], "score": float(scores[int(index)])}
        for index in top_indices
    ], list(np.asarray(predictions).shape)


def run_effnet_embeddings(audio, extractor_directory):
    metadata = read_metadata(extractor_directory / "discogs-effnet-bs64-1.json")
    predictions = es.TensorflowPredictEffnetDiscogs(
        graphFilename=str(extractor_directory / "discogs-effnet-bs64-1.pb"),
        output=output_name(metadata, "embeddings")
    )(audio)
    return np.asarray(predictions)


def run_musicnn_embeddings(audio, extractor_directory):
    metadata = read_metadata(extractor_directory / "msd-musicnn-1.json")
    predictions = es.TensorflowPredictMusiCNN(
        graphFilename=str(extractor_directory / "msd-musicnn-1.pb"),
        output=output_name(metadata, "embeddings")
    )(audio)
    return np.asarray(predictions)


def discover_heads(heads_directory):
    if not heads_directory.exists():
        return []
    heads = []
    for metadata_path in sorted(heads_directory.rglob("*.json")):
        model_path = metadata_path.with_suffix(".pb")
        if not model_path.exists():
            continue
        metadata = read_metadata(metadata_path)
        if metadata.get("name", metadata_path.stem) in EXCLUDED_HEAD_MODELS:
            continue
        category = metadata_path.parent.relative_to(heads_directory).as_posix().replace("/", " / ")
        heads.append((category, metadata_path, model_path, metadata))
    return heads


def run_heads(embeddings, heads_directory, family):
    results = []
    errors = []
    for category, metadata_path, model_path, metadata in discover_heads(heads_directory):
        model_name = metadata.get("name", metadata_path.stem)
        try:
            predictions = es.TensorflowPredict2D(
                graphFilename=str(model_path),
                input=input_name(metadata),
                output=output_name(metadata, "predictions"),
                patchSize=1,
                patchHopSize=1
            )(embeddings)
            results.append({
                "family": family,
                "category": category,
                "model": model_name,
                "type": metadata.get("type", "classifier"),
                "description": metadata.get("description", ""),
                "values": aggregate(predictions, metadata["classes"], model_name)
            })
        except Exception as exception:
            errors.append({"family": family, "model": model_name, "error": str(exception)})
    return results, errors


def experimental_analysis(audio, essentia_directory):
    results = []
    errors = []
    families = [
        ("discogs-effnet", essentia_directory / "DiscogsEffnet", "Extractor", "Heads", run_effnet_embeddings),
        ("msd-musicnn", essentia_directory / "MusicNN", "Extractor", "Heads", run_musicnn_embeddings),
    ]

    for family, root, extractor_name, heads_name, extractor in families:
        heads_directory = root / heads_name
        if not heads_directory.exists():
            continue
        try:
            embeddings = extractor(audio, root / extractor_name)
            family_results, family_errors = run_heads(embeddings, heads_directory, family)
            results.extend(family_results)
            errors.extend(family_errors)
        except Exception as exception:
            errors.append({"family": family, "model": "extractor", "error": str(exception)})

    return results, errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("track_path", help="Path to the audio file inside the container")
    parser.add_argument("--top", type=int, default=20, help="Number of MAEST genre predictions to return")
    parser.add_argument("--model-directory", type=Path, default=DEFAULT_MODEL_DIRECTORY)
    args = parser.parse_args()

    if not Path(args.track_path).exists():
        error(f"Track file not found: {args.track_path}")

    audio = es.MonoLoader(filename=args.track_path, sampleRate=16000, resampleQuality=4)()
    if not (args.model_directory / MAEST_MODEL_FILE).exists():
        error(f"MAEST model file not found: {args.model_directory / MAEST_MODEL_FILE}")
    if not (args.model_directory / MAEST_METADATA_FILE).exists():
        error(f"MAEST metadata file not found: {args.model_directory / MAEST_METADATA_FILE}")

    bpm_audio = es.MonoLoader(filename=args.track_path, sampleRate=44100, resampleQuality=4)()
    bpm, _, _, _, _ = es.RhythmExtractor2013(method="multifeature")(bpm_audio)
    loudness_audio, loudness_sample_rate, _, *_ = es.AudioLoader(filename=args.track_path)()
    _, _, integrated_loudness, loudness_range = es.LoudnessEBUR128(sampleRate=loudness_sample_rate)(loudness_audio)

    genre_predictions, prediction_shape = analyze_maest(audio, args.model_directory, args.top)
    experimental_predictions, experimental_errors = experimental_analysis(
        audio,
        args.model_directory.parent,
    )

    print(json.dumps({
        "success": True,
        "trackPath": args.track_path,
        "model": MAEST_MODEL_NAME,
        "predictionShape": prediction_shape,
        "labelCount": len(read_metadata(args.model_directory / MAEST_METADATA_FILE)["classes"]),
        "bpm": float(bpm),
        "integratedLoudness": float(integrated_loudness),
        "loudnessRange": float(loudness_range),
        "predictions": genre_predictions,
        "experimentalPredictions": experimental_predictions,
        "experimentalErrors": experimental_errors
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        main()
    except Exception as exception:
        error(f"Analysis failed: {exception}")
