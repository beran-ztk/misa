import argparse
import json
import sys
from pathlib import Path

import numpy as np
import essentia.standard as es


# Discogs-MAEST 30-second, 519-label genre classifier.
MODEL_NAME = "discogs-maest-30s-pw-519l-2"
MODEL_FILE = "discogs-maest-30s-pw-519l-2.pb"
METADATA_FILE = "discogs-maest-30s-pw-519l-2.json"
DEFAULT_MODEL_DIRECTORY = Path(__file__).resolve().parent.parent / "Models" / "Essentia" / "DiscogsMAEST"


def error(message):
    print(json.dumps({
        "success": False,
        "error": message
    }, ensure_ascii=False, indent=2))
    sys.exit(1)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("track_path", help="Path to the audio file inside the container")
    parser.add_argument("--top", type=int, default=20, help="Number of predictions to return")
    parser.add_argument(
        "--model-directory",
        type=Path,
        default=DEFAULT_MODEL_DIRECTORY,
        help="Directory containing the .pb model and its metadata JSON (default: solution Models directory)"
    )
    args = parser.parse_args()

    model_path = args.model_directory / MODEL_FILE
    metadata_path = args.model_directory / METADATA_FILE

    if not Path(args.track_path).exists():
        error(f"Track file not found: {args.track_path}")

    if not model_path.exists():
        error(f"Model file not found: {model_path}")

    if not metadata_path.exists():
        error(f"Metadata file not found: {metadata_path}")

    with open(metadata_path, "r", encoding="utf-8") as f:
        metadata = json.load(f)

    labels = metadata["classes"]

    audio = es.MonoLoader(
        filename=args.track_path,
        sampleRate=16000,
        resampleQuality=4
    )()

    predictions = es.TensorflowPredictMAEST(
        graphFilename=str(model_path),
        output="PartitionedCall/Identity_13"
    )(audio)

    scores = np.asarray(predictions)

    if scores.shape[-1] != len(labels):
        error(f"Last prediction dimension does not match label count: predictions={scores.shape}, labels={len(labels)}")

    scores = scores.reshape(-1, len(labels)).mean(axis=0)

    if scores.ndim != 1:
        error(f"Unexpected score shape after aggregation: {scores.shape}")

    top_count = min(args.top, len(labels), len(scores))
    top_indices = scores.argsort()[-top_count:][::-1]

    result = {
        "success": True,
        "trackPath": args.track_path,
        "model": MODEL_NAME,
        "predictionShape": list(predictions.shape),
        "labelCount": len(labels),
        "predictions": [
            {
                "label": labels[int(i)],
                "score": float(scores[int(i)])
            }
            for i in top_indices
        ]
    }

    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
