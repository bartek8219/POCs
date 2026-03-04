import argparse
import random
from dataclasses import dataclass

import torch
from torch import nn
from torch.utils.data import DataLoader, TensorDataset


def set_seed(seed: int) -> None:
    random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)


@dataclass
class Standardizer:
    mean: torch.Tensor
    std: torch.Tensor

    def transform(self, x: torch.Tensor) -> torch.Tensor:
        return (x - self.mean) / self.std


class SimpleClassifier(nn.Module):
    def __init__(self) -> None:
        super().__init__()
        self.net = nn.Sequential(
            nn.Linear(2, 16),
            nn.ReLU(),
            nn.Linear(16, 2),
        )

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        return self.net(x)


def make_dataset(n_samples: int, seed: int) -> tuple[torch.Tensor, torch.Tensor]:
    generator = torch.Generator().manual_seed(seed)
    half = n_samples // 2

    class0 = torch.randn(half, 2, generator=generator) * 0.8 + torch.tensor([-2.0, -2.0])
    class1 = torch.randn(n_samples - half, 2, generator=generator) * 0.8 + torch.tensor([2.0, 2.0])

    x = torch.cat([class0, class1], dim=0)
    y = torch.cat([
        torch.zeros(half, dtype=torch.long),
        torch.ones(n_samples - half, dtype=torch.long),
    ])

    perm = torch.randperm(n_samples, generator=generator)
    return x[perm], y[perm]


def split_dataset(
    x: torch.Tensor,
    y: torch.Tensor,
    train_ratio: float = 0.8,
) -> tuple[torch.Tensor, torch.Tensor, torch.Tensor, torch.Tensor]:
    split_idx = int(len(x) * train_ratio)
    x_train, x_test = x[:split_idx], x[split_idx:]
    y_train, y_test = y[:split_idx], y[split_idx:]
    return x_train, y_train, x_test, y_test


def fit_standardizer(x_train: torch.Tensor) -> Standardizer:
    mean = x_train.mean(dim=0, keepdim=True)
    std = x_train.std(dim=0, keepdim=True).clamp_min(1e-6)
    return Standardizer(mean=mean, std=std)


def accuracy_from_logits(logits: torch.Tensor, y_true: torch.Tensor) -> float:
    preds = logits.argmax(dim=1)
    return (preds == y_true).float().mean().item()


def train_one_epoch(
    model: nn.Module,
    loader: DataLoader,
    criterion: nn.Module,
    optimizer: torch.optim.Optimizer,
    device: torch.device,
) -> tuple[float, float]:
    model.train()
    total_loss = 0.0
    total_correct = 0
    total_examples = 0

    for xb, yb in loader:
        xb, yb = xb.to(device), yb.to(device)

        # Forward pass: model predicts logits for each class.
        logits = model(xb)
        loss = criterion(logits, yb)

        # Backward pass: compute gradients of loss w.r.t. model parameters.
        optimizer.zero_grad()
        loss.backward()

        # Update step: adjust weights and biases using optimizer.
        optimizer.step()

        batch_size = xb.size(0)
        total_loss += loss.item() * batch_size
        total_correct += (logits.argmax(dim=1) == yb).sum().item()
        total_examples += batch_size

    return total_loss / total_examples, total_correct / total_examples


@torch.no_grad()
def evaluate(
    model: nn.Module,
    x: torch.Tensor,
    y: torch.Tensor,
    criterion: nn.Module,
    device: torch.device,
) -> tuple[float, float]:
    model.eval()
    x, y = x.to(device), y.to(device)
    logits = model(x)
    loss = criterion(logits, y).item()
    acc = accuracy_from_logits(logits, y)
    return loss, acc


@torch.no_grad()
def print_parameter_snapshot(model: SimpleClassifier, title: str) -> None:
    first_layer: nn.Linear = model.net[0]  # type: ignore[assignment]
    out_layer: nn.Linear = model.net[2]  # type: ignore[assignment]

    print(f"\n{title}")
    print(f"Layer 1 weight shape: {tuple(first_layer.weight.shape)}")
    print(f"Layer 1 bias shape:   {tuple(first_layer.bias.shape)}")
    print(f"Layer 1 weight sample (first neuron): {first_layer.weight[0, :].cpu().tolist()}")
    print(f"Layer 1 bias sample (first 3): {first_layer.bias[:3].cpu().tolist()}")

    print(f"Output layer weight shape: {tuple(out_layer.weight.shape)}")
    print(f"Output layer bias shape:   {tuple(out_layer.bias.shape)}")
    print(f"Output layer weight sample (class 0): {out_layer.weight[0, :4].cpu().tolist()}")
    print(f"Output layer bias values: {out_layer.bias.cpu().tolist()}")


def maybe_plot_decision_boundary(
    model: nn.Module,
    x: torch.Tensor,
    y: torch.Tensor,
    device: torch.device,
) -> None:
    try:
        import matplotlib.pyplot as plt
    except ImportError:
        print("matplotlib is not installed. Skipping plot.")
        return

    model.eval()
    x_cpu = x.cpu()
    y_cpu = y.cpu()

    x_min, x_max = x_cpu[:, 0].min().item() - 1.0, x_cpu[:, 0].max().item() + 1.0
    y_min, y_max = x_cpu[:, 1].min().item() - 1.0, x_cpu[:, 1].max().item() + 1.0

    xx, yy = torch.meshgrid(
        torch.linspace(x_min, x_max, 200),
        torch.linspace(y_min, y_max, 200),
        indexing="xy",
    )
    grid = torch.stack([xx.reshape(-1), yy.reshape(-1)], dim=1).to(device)

    with torch.no_grad():
        logits = model(grid)
        preds = logits.argmax(dim=1).reshape(xx.shape).cpu()

    plt.figure(figsize=(7, 5))
    plt.contourf(xx.numpy(), yy.numpy(), preds.numpy(), alpha=0.25, levels=2)
    plt.scatter(x_cpu[:, 0].numpy(), x_cpu[:, 1].numpy(), c=y_cpu.numpy(), s=20, edgecolors="k")
    plt.title("Decision Boundary - Simple PyTorch Classifier")
    plt.xlabel("Feature 1")
    plt.ylabel("Feature 2")
    plt.tight_layout()
    plt.show()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="PyTorch hello-world classifier demo.")
    parser.add_argument("--epochs", type=int, default=100, help="Number of training epochs.")
    parser.add_argument("--lr", type=float, default=0.01, help="Learning rate for Adam.")
    parser.add_argument("--batch-size", type=int, default=32, help="Mini-batch size.")
    parser.add_argument("--seed", type=int, default=42, help="Random seed.")
    parser.add_argument("--n-samples", type=int, default=1000, help="Number of synthetic samples.")
    parser.add_argument("--plot", action="store_true", help="Show 2D decision boundary plot.")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    set_seed(args.seed)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Using device: {device}")

    x, y = make_dataset(n_samples=args.n_samples, seed=args.seed)
    x_train, y_train, x_test, y_test = split_dataset(x, y, train_ratio=0.8)

    standardizer = fit_standardizer(x_train)
    x_train = standardizer.transform(x_train)
    x_test = standardizer.transform(x_test)

    train_loader = DataLoader(TensorDataset(x_train, y_train), batch_size=args.batch_size, shuffle=True)

    model = SimpleClassifier().to(device)
    criterion = nn.CrossEntropyLoss()
    optimizer = torch.optim.Adam(model.parameters(), lr=args.lr)

    print_parameter_snapshot(model, "Initial parameters (before training):")

    print("\nTraining...")
    for epoch in range(1, args.epochs + 1):
        train_loss, train_acc = train_one_epoch(model, train_loader, criterion, optimizer, device)
        test_loss, test_acc = evaluate(model, x_test, y_test, criterion, device)

        if epoch == 1 or epoch % 10 == 0 or epoch == args.epochs:
            print(
                f"Epoch {epoch:03d}/{args.epochs} | "
                f"train_loss={train_loss:.4f} train_acc={train_acc:.4f} | "
                f"test_loss={test_loss:.4f} test_acc={test_acc:.4f}"
            )

    print_parameter_snapshot(model, "Final parameters (after training):")

    final_loss, final_acc = evaluate(model, x_test, y_test, criterion, device)
    print(f"\nFinal test metrics -> loss: {final_loss:.4f}, accuracy: {final_acc:.4f}")

    if args.plot:
        maybe_plot_decision_boundary(model, x_test, y_test, device)


if __name__ == "__main__":
    main()
