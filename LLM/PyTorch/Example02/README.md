# PyTorch Hello World - Prosta Klasyfikacja

Ten projekt pokazuje podstawy uczenia sieci neuronowej w PyTorch na prostym zadaniu klasyfikacji 2 klas.

## Czego sie nauczysz
- Jak zbudowac model `nn.Module` z warstwami liniowymi.
- Co to sa `weights` (wagi) i `biases` (biasy).
- Jak dziala petla treningowa: `forward -> loss -> backward -> optimizer.step()`.
- Co oznaczaja epoki (`epochs`), `learning rate` i metryki (`loss`, `accuracy`).

## Struktura modelu
Model ma architekture:
- `Linear(2, 16)`
- `ReLU()`
- `Linear(16, 2)`

Wejscie ma 2 cechy, wyjscie to logits dla 2 klas.

## Instalacja
```bash
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
```

## Profile zaleznosci (requirements)
- `requirements.txt` -> minimalny profil (`torch`), dla:
  - `binary-classification-network-console.py`
  - `parity-classification-network-console.py`
- `requirements-plot.txt` -> `torch + matplotlib`, dla:
  - `train_classifier.py` (szczegolnie opcja `--plot`)
- `requirements-tensorboard.txt` -> `torch + tensorboard`, dla:
  - `binary-classification-network.py`

Przyklady instalacji:
```bash
pip install -r requirements.txt
pip install -r requirements-plot.txt
pip install -r requirements-tensorboard.txt
```

## Instalacja torch tylko w `.venv` (bez pluginow VSCode)
Jesli nie chcesz instalowac nic przez pluginy VSCode, wystarczy lokalne srodowisko projektu:

```bash
python -m venv .venv
.venv\Scripts\activate
python -m pip install --upgrade pip
pip install torch
```

W VSCode ustaw interpreter projektu na:
`C:\tmp\PyTorch\Example02\.venv\Scripts\python.exe`

## `.venv` a Git
Katalogu `.venv` nie wrzucamy do repozytorium Git.
To sa lokalne pliki srodowiska (duze, zalezne od systemu, nieprzenoszalne).

Do repo dodaj tylko:
- kod,
- `requirements.txt` (lub `pyproject.toml`),
- ewentualnie `README.md`.

W `.gitignore` warto miec wpis:

```gitignore
.venv/
```

## Uruchomienie
```bash
python train_classifier.py
```

Przyklad z parametrami:
```bash
python train_classifier.py --epochs 50 --lr 0.005 --batch-size 64 --seed 42
```

Opcjonalny wykres granicy decyzyjnej:
```bash
python train_classifier.py --plot
```

## Parametry CLI
- `--epochs` (domyslnie `100`)
- `--lr` (domyslnie `0.01`)
- `--batch-size` (domyslnie `32`)
- `--seed` (domyslnie `42`)
- `--n-samples` (domyslnie `1000`)
- `--plot` (flaga, pokazuje wykres)

## Co pokazac na demo
- Wartosci wag i biasow przed treningiem.
- Spadek `train_loss` wraz z epokami.
- Wzrost `train_acc` i `test_acc`.
- Zmiane wag i biasow po treningu.

## Typowy wynik
Dla domyslnych ustawien model zwykle dochodzi do wysokiej skutecznosci (`test_acc` ~ 0.95+), bo dane sa dobrze separowalne.
