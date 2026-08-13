FROM python:3.10-slim-bookworm

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    TF_CPP_MIN_LOG_LEVEL=2

RUN apt-get update \
    && apt-get install --yes --no-install-recommends ffmpeg libgomp1 libsndfile1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY src/AnalysisServer/requirements.txt /tmp/requirements.txt
RUN python -m pip install --no-cache-dir --upgrade pip \
    && python -m pip install --no-cache-dir --requirement /tmp/requirements.txt \
    && python -c "import essentia.standard as es; assert hasattr(es, 'TensorflowPredictMAEST')"

RUN useradd --create-home --uid 10001 analyzer

COPY --chown=analyzer:analyzer src/AnalysisServer/api.py /app/api.py
COPY --chown=analyzer:analyzer src/AnalysisServer/analyze.py /app/analyze.py
COPY --chown=analyzer:analyzer models/Essentia/DiscogsMAEST /app/models/Essentia/DiscogsMAEST
COPY --chown=analyzer:analyzer models/Essentia/MusicNN/Extractor /app/models/Essentia/MusicNN/Extractor
COPY --chown=analyzer:analyzer models/Essentia/MusicNN/Heads/Mood /app/models/Essentia/MusicNN/Heads/Mood

USER analyzer

EXPOSE 8000

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD python -c "import os, urllib.request; port=os.environ.get('ANALYZER_PORT', '8000'); urllib.request.urlopen(f'http://127.0.0.1:{port}/health', timeout=3).read()"

CMD ["python", "api.py"]
