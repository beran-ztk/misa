FROM continuumio/miniconda3:24.7.1-0

ARG ESSENTIA_VERSION=2.1b6.dev1389

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    TF_CPP_MIN_LOG_LEVEL=2 \
    NVIDIA_VISIBLE_DEVICES=all \
    NVIDIA_DRIVER_CAPABILITIES=compute,utility \
    LD_LIBRARY_PATH=/opt/conda/lib

RUN conda install --yes --channel conda-forge \
        python=3.10 \
        cudatoolkit=11.2 \
        cudnn=8.1 \
    && conda clean --all --yes

RUN apt-get update \
    && apt-get install --yes --no-install-recommends ffmpeg libsndfile1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY src/AnalysisServer/requirements.txt /tmp/requirements.txt
RUN python -m pip install --no-cache-dir --upgrade pip \
    && python -m pip install --no-cache-dir --requirement /tmp/requirements.txt \
    && python -c "import essentia.standard as es; assert hasattr(es, 'TensorflowPredictMAEST')"

RUN useradd --create-home --uid 10001 analyzer

COPY --chown=analyzer:analyzer src/AnalysisServer/api.py /app/api.py
COPY --chown=analyzer:analyzer Scripts/analyze.py /app/Scripts/analyze.py
COPY --chown=analyzer:analyzer Models/Essentia /app/Models/Essentia

USER analyzer

EXPOSE 8000

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health', timeout=3).read()"

CMD ["uvicorn", "api:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "1", "--no-access-log"]
